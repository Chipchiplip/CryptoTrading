using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CryptoTrading.Data;
using CryptoTrading.Interfaces;
using CryptoTrading.Interfaces.Bot;
using UserPortfolioService = CryptoTrading.Interfaces.IPortfolioService;
using CryptoTrading.Models;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services.Ai;

public interface IAiTradingChatService
{
    Task<AiChatResponseDto> HandleMessageAsync(AiChatMessageRequest request, CancellationToken ct = default);
    Task<TradingBotDetailDto> ApplySuggestionAsync(Guid suggestionId, int userId, CancellationToken ct = default);
}

public class AiTradingChatService : IAiTradingChatService
{
    private readonly ApplicationDbContext _db;
    private readonly IAiChatSessionStore _sessionStore;
    private readonly UserPortfolioService _portfolioService;
    private readonly IBotApplicationService _botApplicationService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiTradingChatService> _logger;
    private readonly ICoinGeckoService _coinGeckoService;
    private readonly IGeminiService _geminiService;
    private static readonly JsonSerializerOptions SnakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private static readonly JsonSerializerOptions ResponseOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private const string BotHintMessage = "Nếu muốn mình dựng bot tự động, cứ gõ /taobot bất kỳ lúc nào.";

    public AiTradingChatService(
        ApplicationDbContext db,
        IAiChatSessionStore sessionStore,
        UserPortfolioService portfolioService,
        IBotApplicationService botApplicationService,
        IHttpClientFactory httpClientFactory,
        ILogger<AiTradingChatService> logger,
        ICoinGeckoService coinGeckoService,
        IGeminiService geminiService)
    {
        _db = db;
        _sessionStore = sessionStore;
        _portfolioService = portfolioService;
        _botApplicationService = botApplicationService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _coinGeckoService = coinGeckoService;
        _geminiService = geminiService;
    }

    public async Task<AiChatResponseDto> HandleMessageAsync(AiChatMessageRequest request, CancellationToken ct = default)
    {
        var session = await _sessionStore.GetOrCreateAsync(request.UserId, request.SessionId, ct);
        if (IsCreateBotCommand(request.Message))
        {
            // Parse message to update session context before creating bot
            // This ensures we have the latest information from the conversation
            var (updatedSession, _, _) = ParseMessage(session, request.Message);
            var updatedForCommand = AppendConversationEntry(updatedSession, request.Message);
            await _sessionStore.SaveAsync(updatedForCommand, ct);
            return await HandleCreateBotCommandAsync(updatedForCommand, request, ct);
        }

        return await HandleChatMessageAsync(session, request, ct);
    }

    private async Task<AiChatResponseDto> HandleChatMessageAsync(
        AiChatSessionContext session,
        AiChatMessageRequest request,
        CancellationToken ct)
    {
        var (updatedSession, missingFields, newInfoCaptured) = ParseMessage(session, request.Message);
        var intent = ResolveIntent(request.Message);
        var shouldCollectDetails = intent == "chat";

        var withHistory = AppendConversationEntry(updatedSession, request.Message);
        var conversationSummary = shouldCollectDetails && newInfoCaptured
            ? BuildConversationSummary(withHistory)
            : string.Empty;
        var followUpQuestion = shouldCollectDetails ? BuildFollowUpQuestion(missingFields) : string.Empty;

        var showBotHint = ShouldShowBotReminder(intent, withHistory.HasShownBotHint);
        var sessionToPersist = showBotHint && !withHistory.HasShownBotHint
            ? withHistory with { HasShownBotHint = true }
            : withHistory;
        await _sessionStore.SaveAsync(sessionToPersist, ct);

        var highlightResult = await BuildMarketHighlightsAsync(3, ct);
        
        // Build conversation history for Gemini
        var conversationHistory = BuildConversationHistory(sessionToPersist);
        
        // Add market context to user message if available
        var enhancedMessage = request.Message;
        if (highlightResult.Highlights.Count > 0 && intent == "market_scan")
        {
            var topCoins = string.Join(", ", highlightResult.Highlights
                .Take(3)
                .Select(h =>
                {
                    var highlight = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(h));
                    var symbol = highlight.TryGetProperty("symbol", out var s) ? s.GetString() : "N/A";
                    var change = highlight.TryGetProperty("change_24h", out var c) && c.ValueKind == JsonValueKind.Number
                        ? c.GetDouble()
                        : 0;
                    return $"{symbol} (+{change:F1}%)";
                }));
            enhancedMessage = $"{request.Message}\n\nThông tin thị trường: Top coin đang tăng: {topCoins}.";
        }

        string? geminiReply = null;
        try
        {
            _logger.LogInformation("Calling Gemini API for user {UserId}, message length: {Length}, history count: {HistoryCount}", 
                request.UserId, enhancedMessage.Length, conversationHistory?.Count ?? 0);
            
            geminiReply = await _geminiService.ChatAsync(enhancedMessage, conversationHistory, ct);
            
            _logger.LogInformation("Gemini API responded successfully, reply length: {Length}", geminiReply?.Length ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API for user {UserId}. Exception: {ExceptionType}, Message: {Message}", 
                request.UserId, ex.GetType().Name, ex.Message);
        }

        var replySegments = new List<string>();

        // If Gemini replied successfully, use it as primary response
        if (!string.IsNullOrWhiteSpace(geminiReply))
        {
            // Clean up markdown formatting from Gemini response
            var cleanedReply = CleanMarkdownFormatting(geminiReply);
            
            // Use Gemini reply as main response - let Gemini handle the conversation naturally
            // Check if Gemini already acknowledged the user's information in its reply
            // Check for common number patterns and currency mentions
            var geminiAcknowledgedInfo = newInfoCaptured && 
                (cleanedReply.Contains("vốn", StringComparison.OrdinalIgnoreCase) ||
                 cleanedReply.Contains("USD", StringComparison.OrdinalIgnoreCase) ||
                 cleanedReply.Contains("BTC", StringComparison.OrdinalIgnoreCase) ||
                 cleanedReply.Contains("ETH", StringComparison.OrdinalIgnoreCase) ||
                 cleanedReply.Contains("SOL", StringComparison.OrdinalIgnoreCase) ||
                 // Check for common number patterns (1000, 5000, 10000, 15000, etc.)
                 Regex.IsMatch(cleanedReply, @"\b\d{1,2}[.,]\d{3}\b") || // 15.000, 10.000
                 Regex.IsMatch(cleanedReply, @"\b\d{4,}\b")); // 10000, 15000
            
            // Only add conversation summary if Gemini didn't acknowledge the info
            // This prevents duplicate information
            if (newInfoCaptured && shouldCollectDetails && !string.IsNullOrWhiteSpace(conversationSummary) && 
                intent == "chat" && !geminiAcknowledgedInfo)
            {
                replySegments.Add(conversationSummary);
            }
            
            // Add Gemini's reply
            replySegments.Add(cleanedReply);
            
            // Add bot hint only if:
            // 1. It's appropriate to show (showBotHint is true)
            // 2. Gemini didn't already mention /taobot or bot
            // 3. This is a general chat, not a specific command
            var geminiMentionedBot = cleanedReply.Contains("/taobot", StringComparison.OrdinalIgnoreCase) ||
                                     cleanedReply.Contains("bot tự động", StringComparison.OrdinalIgnoreCase) ||
                                     cleanedReply.Contains("dựng bot", StringComparison.OrdinalIgnoreCase);
            
            if (showBotHint && !geminiMentionedBot && intent == "chat")
            {
                replySegments.Add(BotHintMessage);
            }
        }
        else
        {
            // Fallback if Gemini fails
            if (intent == "direct_advice")
            {
                replySegments.Add("Tạm thời mình không thể đưa ra lệnh giao dịch. Vui lòng thử lại sau hoặc kiểm tra kết nối AI service.");
            }
            else if (intent == "market_scan")
            {
                if (highlightResult.IsMarketDown)
                {
                    replySegments.Add("Hiện tại thị trường đang đỏ, chưa có coin nào đáng mua. Bạn nên chờ tín hiệu rõ ràng hơn.");
                }
                else if (highlightResult.Highlights.Count > 0)
                {
                    var topCoins = string.Join(", ", highlightResult.Highlights
                        .Take(3)
                        .Select(h =>
                        {
                            var highlight = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(h));
                            var symbol = highlight.TryGetProperty("symbol", out var s) ? s.GetString() : "N/A";
                            var change = highlight.TryGetProperty("change_24h", out var c) && c.ValueKind == JsonValueKind.Number
                                ? c.GetDouble()
                                : 0;
                            return $"{symbol} (+{change:F1}%)";
                        }));
                    replySegments.Add($"Top coin đang tăng: {topCoins}. Bạn có thể xem xét các coin này.");
                }
                else
                {
                    replySegments.Add("Hiện chưa có coin nào có tín hiệu tăng mạnh. Mình sẽ tiếp tục theo dõi và báo lại khi có cơ hội.");
                }
            }
            else
            {
                replySegments.Add("Xin lỗi, mình đang gặp sự cố kỹ thuật. Vui lòng thử lại sau.");
            }
            
            if (showBotHint)
            {
                replySegments.Add(BotHintMessage);
            }
        }

        var finalReply = string.Join("\n\n", replySegments.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new AiChatResponseDto
        {
            SessionId = sessionToPersist.SessionId.ToString(),
            Reply = finalReply,
            Bots = new List<AiChatBotSuggestionDto>(),
            TradeSuggestion = null // Gemini doesn't return trade suggestions in this flow
        };
    }

    private async Task<AiChatResponseDto> HandleCreateBotCommandAsync(
        AiChatSessionContext session,
        AiChatMessageRequest request,
        CancellationToken ct)
    {
        var missingFields = BuildMissingFieldList(session);
        if (missingFields.Count > 0)
        {
            var summary = BuildConversationSummary(session);
            var followUp = BuildFollowUpQuestion(missingFields);
            var lines = new List<string>
            {
                "Để dựng bot cho bạn mình cần thêm một chút thông tin"
            };
            if (!string.IsNullOrWhiteSpace(summary))
            {
                lines.Add(summary);
            }
            if (!string.IsNullOrWhiteSpace(followUp))
            {
                lines.Add(followUp);
            }

            return new AiChatResponseDto
            {
                SessionId = session.SessionId.ToString(),
                Reply = string.Join("\n\n", lines)
            };
        }

        // Build conversation history from session
        var conversationHistory = BuildConversationHistory(session);
        
        _logger.LogInformation("Creating bot from session - UserId: {UserId}, SessionId: {SessionId}, RiskMode: {RiskMode}, Capital: {Capital}, Symbols: {Symbols}, TimeHorizon: {TimeHorizon}",
            session.UserId, session.SessionId, session.RiskMode, session.TotalEquity, 
            string.Join(", ", session.PreferredSymbols), session.TimeHorizon);
        
        BotConfigJson? botConfig = null;
        List<AiChatBotSuggestionDto> botDtos = new();
        
        try
        {
            // Use Gemini to extract bot config from conversation
            botConfig = await _geminiService.ExtractBotConfigAsync(conversationHistory, ct);
            
            _logger.LogInformation("Gemini extracted config - RiskMode: {RiskRisk}, CapitalPerTrade: {PerTrade}, DailyExposure: {Daily}, Strategy: {Strategy}",
                botConfig?.RiskMode, botConfig?.MaxCapitalPerTrade, botConfig?.MaxDailyExposure, botConfig?.StrategyType);
            
            if (botConfig != null)
            {
                // PRIORITIZE session context over Gemini extraction for critical fields
                // Session context is parsed directly from user conversation, more reliable
                var riskMode = !string.IsNullOrEmpty(session.RiskMode)
                    ? session.RiskMode.ToUpperInvariant()
                    : botConfig.RiskMode.ToUpperInvariant();
                
                // Validate risk mode
                if (riskMode != "AGGRESSIVE" && riskMode != "BALANCED" && riskMode != "SAFE")
                {
                    riskMode = "BALANCED"; // Default fallback
                }
                
                _logger.LogInformation("Bot config extraction - Session risk: {SessionRisk}, Gemini risk: {GeminiRisk}, Final: {FinalRisk}", 
                    session.RiskMode, botConfig.RiskMode, riskMode);
                
                // Calculate capital - prioritize session context
                decimal totalCapital = session.TotalEquity ?? 0;
                if (totalCapital == 0 && botConfig.MaxDailyExposure > 0)
                {
                    // Estimate from maxDailyExposure (assume 50-100% of total capital)
                    // For aggressive: ~90%, balanced: ~70%, safe: ~50%
                    var exposureRatio = riskMode == "AGGRESSIVE" ? 0.90m : 
                                       riskMode == "BALANCED" ? 0.70m : 0.50m;
                    totalCapital = (decimal)botConfig.MaxDailyExposure / exposureRatio;
                }
                
                // If still zero, use default
                if (totalCapital == 0)
                {
                    totalCapital = 10000m; // Default fallback
                }
                
                _logger.LogInformation("Bot config - Total capital: {Capital}, Risk mode: {Risk}", totalCapital, riskMode);
                
                // ALWAYS recalculate based on session context to ensure consistency
                // This ensures capital allocation matches the actual risk mode from conversation
                decimal maxCapitalPerTrade;
                decimal maxDailyExposure;
                
                // Recalculate based on risk mode and total capital
                if (riskMode == "AGGRESSIVE")
                {
                    maxCapitalPerTrade = totalCapital * 0.15m; // 15% per trade
                    maxDailyExposure = totalCapital * 0.90m; // 90% daily
                }
                else if (riskMode == "BALANCED")
                {
                    maxCapitalPerTrade = totalCapital * 0.12m; // 12% per trade
                    maxDailyExposure = totalCapital * 0.70m; // 70% daily
                }
                else // SAFE
                {
                    maxCapitalPerTrade = totalCapital * 0.08m; // 8% per trade
                    maxDailyExposure = totalCapital * 0.50m; // 50% daily
                }
                
                _logger.LogInformation("Bot config calculated - Per trade: {PerTrade}, Daily: {Daily}", 
                    maxCapitalPerTrade, maxDailyExposure);
                
                // Ensure symbols are not empty
                var symbols = botConfig.Symbols?.Any() == true 
                    ? botConfig.Symbols.ToArray() 
                    : (session.PreferredSymbols.Any() 
                        ? session.PreferredSymbols.ToArray() 
                        : new[] { "BTCUSD" });
                
                // Create bot suggestion from extracted config
                var botSuggestion = new AiChatBotSuggestionDto
                {
                    SuggestionId = Guid.NewGuid(),
                    Name = botConfig.Name ?? $"Bot {string.Join(", ", symbols)}",
                    Symbols = symbols,
                    StrategyType = botConfig.StrategyType ?? "grid",
                    RiskMode = riskMode,
                    MaxCapitalPerTrade = maxCapitalPerTrade,
                    MaxDailyExposure = maxDailyExposure,
                    TimeHorizon = botConfig.TimeHorizon ?? session.TimeHorizon ?? "intraday",
                    ExpectedReturnPct = null,
                    RiskNote = $"Bot được tạo từ cuộc trò chuyện với AI. Risk mode: {riskMode}, Strategy: {botConfig.StrategyType ?? "grid"}"
                };
                
                // Persist to database
                var entity = new AiGeneratedBotProfile
                {
                    Id = botSuggestion.SuggestionId,
                    UserId = session.UserId,
                    SessionId = session.SessionId,
                    Name = botSuggestion.Name,
                    SymbolsJson = JsonSerializer.Serialize(botSuggestion.Symbols),
                    StrategyType = botSuggestion.StrategyType,
                    RiskMode = botSuggestion.RiskMode,
                    MaxCapitalPerTrade = botSuggestion.MaxCapitalPerTrade,
                    MaxDailyExposure = botSuggestion.MaxDailyExposure,
                    TimeHorizon = botSuggestion.TimeHorizon,
                    ExpectedReturnPct = botSuggestion.ExpectedReturnPct,
                    RiskNote = botSuggestion.RiskNote,
                    CreatedAtUtc = DateTime.UtcNow
                };
                
                _db.AiGeneratedBotProfiles.Add(entity);
                await _db.SaveChangesAsync(ct);
                
                botDtos.Add(botSuggestion);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting bot config from Gemini for user {UserId}", request.UserId);
        }

        var replyLines = new List<string>();
        
        if (botConfig != null && botDtos.Count > 0)
        {
            replyLines.Add("Mình đã phân tích cuộc trò chuyện và tạo bot proposal cho bạn:");
            replyLines.Add($"• Tên: {botDtos[0].Name}");
            replyLines.Add($"• Symbols: {string.Join(", ", botDtos[0].Symbols)}");
            replyLines.Add($"• Strategy: {botDtos[0].StrategyType}");
            replyLines.Add($"• Risk: {botDtos[0].RiskMode}");
            replyLines.Add($"• Vốn/lệnh: {botDtos[0].MaxCapitalPerTrade:N0} USD");
            replyLines.Add($"• Vốn/ngày: {botDtos[0].MaxDailyExposure:N0} USD");
            replyLines.Add($"• Time horizon: {botDtos[0].TimeHorizon}");
            replyLines.Add("\nBạn có thể apply bot này hoặc nói thêm nếu muốn chỉnh sửa.");
        }
        else
        {
            // Fallback: Try to create bot from session context if Gemini extraction failed
            // but we have enough information in session
            if (session.TotalEquity.HasValue && 
                !string.IsNullOrEmpty(session.RiskMode) && 
                session.PreferredSymbols.Any() && 
                !string.IsNullOrEmpty(session.TimeHorizon))
            {
                _logger.LogInformation("Gemini extraction failed, but session has enough info. Creating bot from session context.");
                
                // Create bot from session context
                var riskMode = session.RiskMode.ToUpperInvariant();
                var totalCapital = session.TotalEquity.Value;
                
                // Calculate capital allocation
                decimal maxCapitalPerTrade;
                decimal maxDailyExposure;
                if (riskMode == "AGGRESSIVE")
                {
                    maxCapitalPerTrade = totalCapital * 0.15m;
                    maxDailyExposure = totalCapital * 0.90m;
                }
                else if (riskMode == "BALANCED")
                {
                    maxCapitalPerTrade = totalCapital * 0.12m;
                    maxDailyExposure = totalCapital * 0.70m;
                }
                else // SAFE
                {
                    maxCapitalPerTrade = totalCapital * 0.08m;
                    maxDailyExposure = totalCapital * 0.50m;
                }
                
                // Choose strategy based on risk mode and time horizon
                var strategyType = session.TimeHorizon.ToLowerInvariant() switch
                {
                    "scalping" => riskMode == "AGGRESSIVE" ? "momentum_scalping" : "grid-basic",
                    "intraday" => riskMode == "AGGRESSIVE" ? "aggressive_forex" : "grid-basic",
                    "swing" => "grid-basic",
                    _ => "grid-basic"
                };
                
                var botSuggestion = new AiChatBotSuggestionDto
                {
                    SuggestionId = Guid.NewGuid(),
                    Name = $"Bot {string.Join(", ", session.PreferredSymbols)}",
                    Symbols = session.PreferredSymbols.ToArray(),
                    StrategyType = strategyType,
                    RiskMode = riskMode,
                    MaxCapitalPerTrade = maxCapitalPerTrade,
                    MaxDailyExposure = maxDailyExposure,
                    TimeHorizon = session.TimeHorizon,
                    ExpectedReturnPct = null,
                    RiskNote = $"Bot được tạo từ cuộc trò chuyện với AI. Risk mode: {riskMode}, Strategy: {strategyType}"
                };
                
                // Persist to database
                var entity = new AiGeneratedBotProfile
                {
                    Id = botSuggestion.SuggestionId,
                    UserId = session.UserId,
                    SessionId = session.SessionId,
                    Name = botSuggestion.Name,
                    SymbolsJson = JsonSerializer.Serialize(botSuggestion.Symbols),
                    StrategyType = botSuggestion.StrategyType,
                    RiskMode = botSuggestion.RiskMode,
                    MaxCapitalPerTrade = botSuggestion.MaxCapitalPerTrade,
                    MaxDailyExposure = botSuggestion.MaxDailyExposure,
                    TimeHorizon = botSuggestion.TimeHorizon,
                    ExpectedReturnPct = botSuggestion.ExpectedReturnPct,
                    RiskNote = botSuggestion.RiskNote,
                    CreatedAtUtc = DateTime.UtcNow
                };
                
                _db.AiGeneratedBotProfiles.Add(entity);
                await _db.SaveChangesAsync(ct);
                
                botDtos.Add(botSuggestion);
                
                replyLines.Add("Mình đã phân tích cuộc trò chuyện và tạo bot proposal cho bạn:");
                replyLines.Add($"• Tên: {botSuggestion.Name}");
                replyLines.Add($"• Symbols: {string.Join(", ", botSuggestion.Symbols)}");
                replyLines.Add($"• Strategy: {botSuggestion.StrategyType}");
                replyLines.Add($"• Risk: {botSuggestion.RiskMode}");
                replyLines.Add($"• Vốn/lệnh: {botSuggestion.MaxCapitalPerTrade:N0} USD");
                replyLines.Add($"• Vốn/ngày: {botSuggestion.MaxDailyExposure:N0} USD");
                replyLines.Add($"• Time horizon: {botSuggestion.TimeHorizon}");
                replyLines.Add("\nBạn có thể apply bot này hoặc nói thêm nếu muốn chỉnh sửa.");
            }
            else
            {
                replyLines.Add("Xin lỗi, mình không thể extract đủ thông tin từ cuộc trò chuyện để tạo bot.");
                replyLines.Add("Vui lòng cung cấp thêm thông tin về: vốn, symbols, risk mode, và time horizon.");
            }
        }

        return new AiChatResponseDto
        {
            SessionId = session.SessionId.ToString(),
            Reply = string.Join("\n", replyLines),
            Bots = botDtos,
            TradeSuggestion = null
        };
    }

    public async Task<TradingBotDetailDto> ApplySuggestionAsync(Guid suggestionId, int userId, CancellationToken ct = default)
    {
        var profile = await _db.AiGeneratedBotProfiles
            .FirstOrDefaultAsync(p => p.Id == suggestionId && p.UserId == userId, ct);

        if (profile == null)
        {
            throw new KeyNotFoundException("Bot suggestion not found");
        }

        var symbols = JsonSerializer.Deserialize<string[]>(profile.SymbolsJson) ?? Array.Empty<string>();
        if (symbols.Length == 0)
        {
            symbols = new[] { "BTCUSD" };
        }

        var (baseAsset, quoteAsset) = ParseSymbol(symbols[0]);

        var strategy = await ResolveStrategyDefinitionAsync(profile.StrategyType, ct)
            ?? throw new InvalidOperationException("No active bot strategy available for AI suggestions.");

        // Tự động set default parameters cho Grid Trading nếu strategy là grid-basic
        var parameters = new Dictionary<string, object>
        {
            ["ai_source"] = "chat",
            ["symbols"] = symbols
        };

        // Nếu là Grid Trading, thêm default parameters dựa trên giá thị trường
        if (strategy.StrategyKey == "grid-basic")
        {
            // Lấy giá thị trường hiện tại để set grid range
            var currentPrice = 0m;
            try
            {
                var marketData = await _coinGeckoService.GetMarketDataAsync(false);
                var crypto = marketData?.FirstOrDefault(c => 
                    c.Symbol?.Equals(baseAsset, StringComparison.OrdinalIgnoreCase) == true ||
                    c.Id?.Equals($"{baseAsset.ToLower()}-{quoteAsset.ToLower()}", StringComparison.OrdinalIgnoreCase) == true);
                currentPrice = crypto?.CurrentPrice ?? 0m;
            }
            catch (Exception ex)
            {
                // Nếu không lấy được giá, dùng default values
                _logger.LogWarning(ex, "Could not fetch market price for grid trading default parameters");
                currentPrice = 0m;
            }

            if (currentPrice > 0)
            {
                // Set grid range: lowerBound = 70% currentPrice, upperBound = 150% currentPrice
                var lowerBound = currentPrice * 0.7m;
                var upperBound = currentPrice * 1.5m;
                var gridLevels = 20;
                var orderSize = 0.1m;
                var capitalAllocation = profile.MaxCapitalPerTrade > 0 ? profile.MaxCapitalPerTrade : 10000m;

                parameters["lowerBound"] = Math.Round(lowerBound, 2);
                parameters["upperBound"] = Math.Round(upperBound, 2);
                parameters["gridLevels"] = gridLevels;
                parameters["orderSize"] = orderSize;
                parameters["capitalAllocation"] = capitalAllocation;
                parameters["refreshIntervalSeconds"] = 60;
                // Thêm parameters cho market orders
                parameters["maxBuyOrders"] = 3;  // Tối đa 3 BUY market orders
                parameters["maxSellOrders"] = 3; // Tối đa 3 SELL market orders
                parameters["orderType"] = "MARKET"; // Bot dùng market orders
            }
            else
            {
                // Fallback nếu không lấy được giá
                parameters["lowerBound"] = 2000m;
                parameters["upperBound"] = 5000m;
                parameters["gridLevels"] = 20;
                parameters["orderSize"] = 0.1m;
                parameters["capitalAllocation"] = profile.MaxCapitalPerTrade > 0 ? profile.MaxCapitalPerTrade : 10000m;
                parameters["refreshIntervalSeconds"] = 60;
                // Thêm parameters cho market orders
                parameters["maxBuyOrders"] = 3;
                parameters["maxSellOrders"] = 3;
                parameters["orderType"] = "MARKET";
            }
        }

        var createRequest = new CreateBotRequest
        {
            Name = profile.Name,
            StrategyDefinitionId = strategy.Id,
            BaseAsset = baseAsset,
            QuoteAsset = quoteAsset,
            RiskProfile = profile.RiskMode,
            Parameters = parameters,
            PositionSizing = new Dictionary<string, object>
            {
                ["maxCapitalPerTrade"] = profile.MaxCapitalPerTrade,
                ["maxDailyExposure"] = profile.MaxDailyExposure
            }
        };

        return await _botApplicationService.CreateAsync(userId, createRequest);
    }

    private (AiChatSessionContext Session, List<string> MissingFields, bool NewInfoCaptured) ParseMessage(
        AiChatSessionContext session,
        string message)
    {
        var parsedCapital = TryParseCapital(message);
        var totalEquity = parsedCapital ?? session.TotalEquity;
        var parsedRisk = TryParseRiskMode(message);
        var riskMode = parsedRisk ?? session.RiskMode;
        var parsedSymbols = TryParseSymbols(message);
        var symbols = parsedSymbols.Any() ? parsedSymbols : session.PreferredSymbols;
        var parsedHorizon = TryParseTimeHorizon(message);
        var timeHorizon = parsedHorizon ?? session.TimeHorizon;

        var updated = session with
        {
            TotalEquity = totalEquity,
            RiskMode = riskMode,
            PreferredSymbols = symbols,
            TimeHorizon = timeHorizon
        };

        var newInfoCaptured = (parsedCapital.HasValue && parsedCapital != session.TotalEquity) ||
                              (!string.IsNullOrEmpty(parsedRisk) && !string.Equals(parsedRisk, session.RiskMode, StringComparison.OrdinalIgnoreCase)) ||
                              (parsedSymbols.Any() && !session.PreferredSymbols.SequenceEqual(parsedSymbols)) ||
                              (!string.IsNullOrEmpty(parsedHorizon) && !string.Equals(parsedHorizon, session.TimeHorizon, StringComparison.OrdinalIgnoreCase));

        var missingFields = BuildMissingFieldList(updated);
        return (updated, missingFields, newInfoCaptured);
    }

    private async Task<object> BuildPythonPayloadAsync(
        AiChatSessionContext session,
        int userId,
        string userMessage,
        string mode,
        string? contextSummary,
        string intent,
        MarketHighlightResult marketContext,
        CancellationToken ct)
    {
        PortfolioOverviewDto? portfolio = null;
        try
        {
            portfolio = await _portfolioService.GetPortfolioOverviewAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching portfolio for user {UserId}, using defaults", userId);
        }

        var symbols = session.PreferredSymbols
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        // Pick a snapshot symbol that reflects user intent (avoid defaulting to BTC on market scan)
        var snapshotSymbol = symbols.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(snapshotSymbol) && marketContext.Highlights.Count > 0)
        {
            try
            {
                var json = JsonSerializer.Serialize(marketContext.Highlights[0]);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("symbol", out var symProp) &&
                    symProp.ValueKind == JsonValueKind.String)
                {
                    snapshotSymbol = symProp.GetString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting symbol from market highlights");
            }
        }
        snapshotSymbol ??= "BTCUSD";
        object? marketSnapshot = null;
        try
        {
            marketSnapshot = await BuildMarketSnapshotAsync(snapshotSymbol, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error building market snapshot for {Symbol}", snapshotSymbol);
        }

        if (marketSnapshot == null)
        {
            marketSnapshot = new
            {
                symbol = snapshotSymbol,
                has_price = false,
                price = (double?)null,
                trend_1h = "neutral",
                trend_4h = "neutral",
                volatility = (double?)null,
                support = (double?)null,
                resistance = (double?)null
            };
        }

        var portfolioValue = portfolio?.TotalValue ?? 0m;
        var totalEquityDecimal = session.TotalEquity ?? portfolioValue;
        if (totalEquityDecimal <= 0)
        {
            totalEquityDecimal = 1_000m; // minimum placeholder to satisfy AI service validation
        }
        var totalEquity = (double)totalEquityDecimal;
        var riskRules = RiskRuleBook.Get(session.RiskMode ?? "BALANCED");
        var maxCapitalPerTrade = totalEquity * (double)riskRules.TradePct;
        var maxDailyExposure = totalEquity * (double)riskRules.DailyPct;

        var tradingPlan = new
        {
            preferred_symbols = symbols,
            strategy_type = "trend_following",
            risk_mode = (session.RiskMode ?? "BALANCED").ToLowerInvariant(),
            max_capital_per_trade = maxCapitalPerTrade,
            max_daily_exposure = maxDailyExposure,
            time_horizon = session.TimeHorizon ?? "intraday",
            // Thông tin về bot strategy: Grid Trading với Market Orders
            bot_strategy_info = new
            {
                strategy_key = "grid-basic",
                order_type = "MARKET", // Bot dùng market orders
                execution_mode = "direct_market", // Trade trực tiếp với market ảo
                description = "Bot sẽ tạo MARKET orders để trade trực tiếp với market ảo (virtual counterparty). Orders sẽ được execute ngay lập tức với market price hiện tại."
            }
        };

        decimal? usdtBalance = null;
        if (portfolio?.Holdings != null)
        {
            usdtBalance = portfolio.Holdings
                .FirstOrDefault(h => h.Symbol?.Equals("USDT", StringComparison.OrdinalIgnoreCase) == true)?.Amount;
        }
        var availableUsdt = usdtBalance ?? portfolioValue;

        var holdings = new List<object>();
        if (portfolio?.Holdings != null)
        {
            foreach (var h in portfolio.Holdings)
            {
                if (h != null && !string.IsNullOrWhiteSpace(h.Symbol))
                {
                    holdings.Add(new
                    {
                        symbol = h.Symbol ?? string.Empty,
                        amount = (double)h.Amount,
                        value = (double)h.Value
                    });
                }
            }
        }

        var portfolioPayload = new
        {
            total_value = (double)portfolioValue,
            available_usdt = (double)availableUsdt,
            holdings
        };

        return new
        {
            user_id = userId,
            bot_id = (string?)null,
            trading_plan_id = session.SessionId.ToString(),
            user_message = userMessage,
            trading_plan = tradingPlan,
            market_snapshot = marketSnapshot,
            portfolio = portfolioPayload,
            mode,
            context_summary = contextSummary,
            intent,
            market_highlights = marketContext.Highlights,
            market_down = marketContext.IsMarketDown
        };
    }

    private async Task<MarketHighlightResult> BuildMarketHighlightsAsync(int count, CancellationToken ct)
    {
        try
        {
            // Láº¥y táº¥t cáº£ báº£n ghi cÃ³ PercentChange24h > 0, sau Ä‘Ã³ group vÃ  láº¥y latest per symbol
            var allPositivePrices = await _db.CryptoPrices
                .Include(p => p.Cryptocurrency)
                .Where(p => p.PercentChange24h.HasValue && 
                           p.PercentChange24h > 0 && 
                           p.Cryptocurrency != null &&
                           p.Cryptocurrency.Symbol != null)
                .ToListAsync(ct);

            if (allPositivePrices.Count == 0)
            {
                return new MarketHighlightResult(new List<object>(), true);
            }

            // Group theo CryptocurrencyId, láº¥y báº£n ghi má»›i nháº¥t (CollectedAtUtc má»›i nháº¥t) per crypto
            var latestPerCrypto = allPositivePrices
                .GroupBy(p => p.CryptocurrencyId)
                .Select(g => g.OrderByDescending(p => p.CollectedAtUtc).First())
                .ToList();

            // Distinct theo symbol (náº¿u cÃ³ nhiá»u CryptocurrencyId cÃ¹ng symbol), sort theo PercentChange24h, take top N
            var highlights = latestPerCrypto
                .GroupBy(p => p.Cryptocurrency!.Symbol)
                .Select(g => g.OrderByDescending(p => p.PercentChange24h).First())
                .OrderByDescending(p => p.PercentChange24h)
                .Take(count)
                .Select(p => new
                {
                    symbol = p.Cryptocurrency!.Symbol,
                    price = (double)p.PriceUsd,
                    change_24h = (double?)p.PercentChange24h
                })
                .ToList();

            var highlightObjects = highlights.Cast<object>().ToList();
            var isMarketDown = highlightObjects.Count == 0;

            return new MarketHighlightResult(highlightObjects, isMarketDown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building market highlights");
            return new MarketHighlightResult(new List<object>(), true);
        }
    }

    private async Task<PythonAiChatResponse> SendAiChatAsync(object payload, CancellationToken ct)
    {
        HttpClient? httpClient = null;
        string? baseAddress = null;
        string payloadJson = string.Empty;
        
        try
        {
            httpClient = _httpClientFactory.CreateClient("AiChatService");
            baseAddress = httpClient.BaseAddress?.ToString() ?? "unknown";
            
            // Log payload summary for debugging (khÃ´ng log toÃ n bá»™ Ä‘á»ƒ trÃ¡nh spam)
            payloadJson = JsonSerializer.Serialize(payload, SnakeCaseOptions);
            var payloadPreview = payloadJson.Length > 500 ? payloadJson.Substring(0, 500) + "..." : payloadJson;
            _logger.LogInformation("Calling AI chat service at {BaseAddress}/ai/chat. Payload preview: {PayloadPreview}", 
                baseAddress, payloadPreview);
            
            var response = await httpClient.PostAsJsonAsync("/ai/chat", payload, SnakeCaseOptions, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("AI chat service returned {StatusCode}: {ErrorContent}. Full payload: {Payload}", 
                    response.StatusCode, errorContent, payloadJson);
                throw new HttpRequestException($"AI service returned {response.StatusCode}: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<PythonAiChatResponse>(ResponseOptions, cancellationToken: ct);
            if (result == null)
            {
                _logger.LogError("AI service returned empty/null payload. Response status: {StatusCode}, Payload sent: {Payload}", 
                    response.StatusCode, payloadJson);
                throw new InvalidOperationException("AI service returned empty payload");
            }
            
            _logger.LogInformation("AI chat service responded successfully");
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling AI chat service: {Message}. BaseAddress: {BaseAddress}, Payload: {Payload}", 
                ex.Message, baseAddress ?? "unknown", payloadJson);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling AI chat service (timeout: {Timeout}s). BaseAddress: {BaseAddress}, Payload: {Payload}", 
                httpClient?.Timeout.TotalSeconds ?? 0, baseAddress ?? "unknown", payloadJson);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling AI chat service: {Message}. Exception type: {ExceptionType}, BaseAddress: {BaseAddress}, Payload: {Payload}", 
                ex.Message, ex.GetType().Name, baseAddress ?? "unknown", payloadJson);
            throw;
        }
    }

    private static AiChatSessionContext AppendConversationEntry(AiChatSessionContext session, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return session;
        }

        var notes = session.ConversationNotes.ToList();
        notes.Add(message.Trim());
        if (notes.Count > 40)
        {
            notes.RemoveAt(0);
        }

        return session with { ConversationNotes = notes };
    }

    private static List<ChatMessage> BuildConversationHistory(AiChatSessionContext session)
    {
        var history = new List<ChatMessage>();
        
        // Convert conversation notes to chat messages
        // Assume alternating user/assistant messages
        for (int i = 0; i < session.ConversationNotes.Count; i++)
        {
            var note = session.ConversationNotes[i];
            // First message is always from user, then alternate
            var role = (i % 2 == 0) ? "user" : "assistant";
            history.Add(new ChatMessage
            {
                Role = role,
                Content = note
            });
        }
        
        return history;
    }

    private static string CleanMarkdownFormatting(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Remove markdown bold (**text** -> text)
        text = Regex.Replace(text, @"\*\*([^*]+)\*\*", "$1");
        
        // Remove markdown italic (*text* -> text)
        text = Regex.Replace(text, @"\*([^*]+)\*", "$1");
        
        // Remove markdown headers (# Header -> Header)
        text = Regex.Replace(text, @"^#+\s+", "", RegexOptions.Multiline);
        
        // Remove markdown list markers (1. , 2. , - , * )
        text = Regex.Replace(text, @"^\s*[\d\-*•]\s+", "", RegexOptions.Multiline);
        
        // Clean up multiple newlines
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        
        return text.Trim();
    }

    private static bool IsCreateBotCommand(string message)
    {
        return !string.IsNullOrWhiteSpace(message)
               && message.Trim().Equals("/taobot", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly CultureInfo ViCulture = new("vi-VN");

    private static List<string> BuildMissingFieldList(AiChatSessionContext ctx)
    {
        var missing = new List<string>();
        if (!ctx.TotalEquity.HasValue) missing.Add("capital");
        if (string.IsNullOrEmpty(ctx.RiskMode)) missing.Add("risk");
        if (!ctx.PreferredSymbols.Any()) missing.Add("symbols");
        if (string.IsNullOrEmpty(ctx.TimeHorizon)) missing.Add("horizon");
        return missing;
    }

    private static string BuildConversationSummary(AiChatSessionContext ctx)
    {
        var parts = new List<string>();
        if (ctx.TotalEquity.HasValue)
        {
            var formatted = ctx.TotalEquity.Value.ToString("N0", ViCulture);
            parts.Add($"vốn khoảng {formatted} USD");
        }
        if (!string.IsNullOrEmpty(ctx.RiskMode))
        {
            parts.Add($"ưu tiên chế độ {ctx.RiskMode.ToLowerInvariant()}");
        }
        if (ctx.PreferredSymbols.Any())
        {
            parts.Add($"đang nhìn vào {string.Join(", ", ctx.PreferredSymbols)}");
        }
        if (!string.IsNullOrEmpty(ctx.TimeHorizon))
        {
            parts.Add($"khung thời gian {ctx.TimeHorizon}");
        }

        return parts.Count == 0
            ? string.Empty
            : $"Bạn đã chia sẻ: {string.Join(", ", parts)}.";
    }

    private static string BuildBotContextSummary(AiChatSessionContext ctx)
    {
        var lines = new List<string>();
        if (ctx.TotalEquity.HasValue)
        {
            lines.Add($"• Vốn: {ctx.TotalEquity.Value.ToString("N0", ViCulture)} USD");
        }
        if (!string.IsNullOrEmpty(ctx.RiskMode))
        {
            lines.Add($"• Risk: {ctx.RiskMode}");
        }
        if (ctx.PreferredSymbols.Any())
        {
            lines.Add($"• Cặp: {string.Join(", ", ctx.PreferredSymbols)}");
        }
        if (!string.IsNullOrEmpty(ctx.TimeHorizon))
        {
            lines.Add($"• Khung: {ctx.TimeHorizon}");
        }

        return string.Join("\n", lines);
    }

    private static string BuildFollowUpQuestion(List<string> missingFields)
    {
        if (missingFields.Count == 0)
        {
            return string.Empty;
        }

        var field = missingFields[0];
        return field switch
        {
            "capital" => "Bạn dự định dùng khoảng bao nhiêu vốn cho kế hoạch này để mình canh tỷ trọng chuẩn hơn?",
            "risk" => "Bạn thiên về phong cách mạo hiểm, cân bằng hay an toàn để mình chọn chiến lược phù hợp?",
            "symbols" => "Bạn muốn tập trung vào cặp nào? Ví dụ BTCUSD hay ETHUSD cũng được.",
            "horizon" => "Bạn đang trade nhanh kiểu scalping, intraday hay giữ swing vài ngày?",
            _ => string.Empty
        };
    }

    private static bool IsDirectAdviceIntent(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var plain = NormalizeVietnamese(message).ToLowerInvariant();
        return plain.Contains("nen mua")
               || plain.Contains("mua coin")
               || plain.Contains("mua gi")
               || plain.Contains("mua cai gi")
               || plain.Contains("vao lenh")
               || plain.Contains("mua bitcoin")
               || plain.Contains("mua btc")
               || plain.Contains("mua bicoin")
               || plain.Contains("buy now")
               || plain.Contains("what to buy")
               || plain.Contains("entry nao")
               || (plain.Contains("coin") && (plain.Contains("nao") || plain.Contains("khac") || plain.Contains("ngoai")))
               || plain.Contains("co cai nao")
               || plain.Contains("co dong nao");
    }

    private static bool IsSmallTalkIntent(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var plain = NormalizeVietnamese(message).ToLowerInvariant();
        return plain.Contains("xin chao")
               || plain.Contains("hello")
               || plain.Contains("hi ")
               || plain.Contains("sao khong chat")
               || plain.Contains("noi chuyen")
               || plain.Contains("tro chuyen")
               || plain.Contains("ban oi");
    }

    private static bool IsMarketScanIntent(string plainMessage)
    {
        if (string.IsNullOrWhiteSpace(plainMessage))
        {
            return false;
        }

        var patterns = new[]
        {
            "nen mua coin nao",
            "nen mua gi",
            "mua coin gi",
            "mua gi thi ok",
            "mua con nao",
            "nen vao con nao",
            "nen vao coin nao",
            "mua coin nao",
            "coin nao gia dang tang",
            "coin nao dang tang",
            "coin nao dang len",
            "coin nao dang pump",
            "coin nao dang tang gia",
            "coin nao gia dang len",
            "co coin nao dang tang",
            "co coin nao gia dang tang",
            "co coin nao dang len",
            "co coin nao dang pump",
            "coin nao dang tang khong",
            "coin nao gia dang tang khong",
            "coin nao dang len khong",
            "coin nao dang pump khong",
            "co coin nao gia dang tang khong",
            "coin nao tang manh",
            "coin nao gia tang manh",
            "gia dang tang khong",
            "coin nao dang tang manh",
            "buy what",
            "what to buy",
            "what should i buy",
            "good coin to buy",
            "any coin to buy",
            "any good coin",
            "which coin is rising",
            "which coin is pumping",
            "which coin is going up"
        };

        return patterns.Any(plainMessage.Contains);
    }

    private static readonly HashSet<string> KnownSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        "BTC", "ETH", "SOL", "ZEC", "OP", "INJ", "LINK", "BNB", "XRP", "ADA",
        "DOGE", "MATIC", "ARB", "AVAX", "LTC", "DOT", "ATOM", "SUI", "SEI"
    };

    private static bool ContainsKnownSymbol(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var upper = message.ToUpperInvariant();
        foreach (var symbol in KnownSymbols)
        {
            if (upper.Contains($"{symbol}USDT", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (upper.Contains($"{symbol}/USDT", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (Regex.IsMatch(upper, $@"\b{Regex.Escape(symbol)}\b", RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /*
     * AI Trading Chat â€“ Intent Resolution Spec
     *
     * Má»¥c tiÃªu:
     * - Dá»±a trÃªn ná»™i dung message cá»§a user Ä‘á»ƒ phÃ¢n loáº¡i sang 4 intent:
     *   1. "direct_advice"  â€“ há»i tháº³ng vá» giao dá»‹ch (mua/bÃ¡n/vÃ o lá»‡nh).
     *   2. "market_scan"    â€“ há»i "nÃªn mua coin nÃ o" mÃ  khÃ´ng chá»‰ rÃµ coin.
     *   3. "smalltalk"      â€“ chÃ o há»i / than phiá»n / nÃ³i chuyá»‡n chung chung.
     *   4. "chat"           â€“ cÃ¡c trÆ°á»ng há»£p cÃ²n láº¡i (káº¿ hoáº¡ch, giáº£i thÃ­ch, v.v.).
     *
     * Quy táº¯c xá»­ lÃ½:
     *
     * 1) direct_advice â€“ user nháº¯m vÃ o 1 coin cá»¥ thá»ƒ hoáº·c hÃ nh Ä‘á»™ng cá»¥ thá»ƒ
     *    VÃ­ dá»¥:
     *      - "tÃ´i muá»‘n mua BTC"
     *      - "mua ZEC Ä‘Æ°á»£c khÃ´ng"
     *      - "cÃ³ nÃªn vÃ o SOL bÃ¢y giá» khÃ´ng"
     *      - "buy BTC now?"
     *      - "entry nÃ o cho ETH"
     *
     *    Nháº­n diá»‡n:
     *      - CÃ³ tá»« "mua", "bÃ¡n", "muá»‘n mua", "muá»‘n bÃ¡n", "vÃ o lá»‡nh", "entry"
     *      - VÃ  trong cÃ¢u cÃ³ nháº¯c tá»›i 1 ticker/coin cá»¥ thá»ƒ (BTC, ETH, ZEC, SOL, v.v.)
     *      - Hoáº·c tiáº¿ng Anh: "buy", "sell", "entry", "long", "short" kÃ¨m tÃªn coin.
     *
     *    Má»¥c Ä‘Ã­ch:
     *      - Backend/LLM nÃªn tráº£ lá»i táº­p trung vÃ o coin Ä‘Ã³, khÃ´ng há»i thÃªm vá» vá»‘n/risk náº¿u khÃ´ng báº¯t buá»™c.
     *
     * 2) market_scan â€“ user há»i "nÃªn mua coin nÃ o" nhÆ°ng khÃ´ng chá»‰ rÃµ coin
     *    VÃ­ dá»¥:
     *      - "nÃªn mua coin nÃ o"
     *      - "bÃ¢y giá» mua coin gÃ¬ thÃ¬ ok"
     *      - "giá» nÃªn vÃ o con nÃ o"
     *      - "what should I buy now"
     *      - "any good coin to buy?"
     *
     *    Nháº­n diá»‡n:
     *      - CÃ³ cÃ¡c pattern nhÆ°: "nen mua coin nao", "nen mua gi", "mua coin gi",
     *        "nen mua con nao", "mua con nao", "buy what", "what to buy",
     *        "good coin to buy", "any coin to buy"â€¦
     *      - KhÃ´ng cÃ³ tÃªn ticker cá»¥ thá»ƒ trong cÃ¢u (khÃ´ng chá»©a BTC/ETH/SOL/â€¦)
     *
     *    Má»¥c Ä‘Ã­ch:
     *      - Backend/LLM nÃªn dÃ¹ng market_highlights Ä‘á»ƒ gá»£i Ã½ 1â€“3 coin Ä‘ang cÃ³ tÃ­n hiá»‡u tá»‘t.
     *      - KhÃ´ng auto fallback vá» BTCUSDT.
     *
     * 3) smalltalk â€“ chÃ o há»i, than phiá»n, trÃ² chuyá»‡n
     *    VÃ­ dá»¥:
     *      - "xin chÃ o", "hello", "hi bot"
     *      - "sao khÃ´ng chat vá»›i tÃ´i"
     *      - "mÃ y cÃ²n Ä‘Ã³ khÃ´ng", "nÃ³i chuyá»‡n Ä‘i"
     *
     *    Nháº­n diá»‡n:
     *      - Chá»©a cÃ¡c tá»« khÃ³a greeting / complain:
     *        "xin chao", "hello", "hi ", "sao khong chat", "noi chuyen",
     *        "tro chuyen", "ban oi", "con do khong", "dang lam gi", v.v.
     *
     *    Má»¥c Ä‘Ã­ch:
     *      - Chá»‰ tráº£ lá»i thÃ¢n thiá»‡n, khÃ´ng báº­t flow há»i vá»‘n/risk/symbol.
     *
     * 4) chat â€“ fallback
     *    - Náº¿u khÃ´ng rÆ¡i vÃ o 3 nhÃ³m trÃªn thÃ¬ gÃ¡n intent = "chat".
     *    - DÃ¹ng cho viá»‡c:
     *      - Há»i vá» káº¿ hoáº¡ch chung ("tÃ´i muá»‘n trade dÃ i háº¡n", "quáº£n lÃ½ vá»‘n sao cho há»£p lÃ½"),
     *      - Há»i giáº£i thÃ­ch khÃ¡i niá»‡m,
     *      - NÃ³i vá» risk mode, vá»‘n, timeframeâ€¦
     *    - Trong intent nÃ y: cÃ³ thá»ƒ nháº¹ nhÃ ng há»i thÃªm 1 thÃ´ng tin cÃ²n thiáº¿u (vá»‘n/risk/symbol/horizon).
     *
     * Æ¯u tiÃªn phÃ¢n loáº¡i:
     *   - Náº¿u lÃ  direct_advice (cÃ³ "mua/bÃ¡n/vÃ o lá»‡nh" + tÃªn coin) â†’ tráº£ vá» "direct_advice".
     *   - else náº¿u lÃ  market_scan (há»i "nÃªn mua coin nÃ o" nhÆ°ng khÃ´ng cÃ³ ticker cá»¥ thá»ƒ) â†’ tráº£ vá» "market_scan".
     *   - else náº¿u lÃ  smalltalk â†’ tráº£ vá» "smalltalk".
     *   - else â†’ "chat".
     *
     * LÆ°u Ã½:
     *   - NÃªn normalize tiáº¿ng Viá»‡t (bá» dáº¥u) trÆ°á»›c khi check pattern.
     *   - Danh sÃ¡ch ticker cÃ³ thá»ƒ láº¥y tá»« cáº¥u hÃ¬nh (BTC, ETH, SOL, ZEC, OP, INJ, LINKâ€¦).
     */
    private static string ResolveIntent(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "chat";
        }

        var plain = NormalizeVietnamese(message).ToLowerInvariant();
        var hasKnownSymbol = ContainsKnownSymbol(message);

        if (IsDirectAdviceIntent(message) && hasKnownSymbol)
        {
            return "direct_advice";
        }

        if (IsMarketScanIntent(plain) && !hasKnownSymbol)
        {
            return "market_scan";
        }

        if (IsSmallTalkIntent(message))
        {
            return "smalltalk";
        }

        return "chat";
    }

    private static bool ShouldShowBotReminder(string intent, bool hasShown)
    {
        if (hasShown)
        {
            return false;
        }

        return intent is "chat" or "direct_advice";
    }

    private static string NormalizeVietnamese(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(input.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static decimal? TryParseCapital(string message)
    {
        var match = Regex.Match(message, @"(\d+(?:[\,\.]\d+)?)(\s*(k|nghÃ¬n|ngan|ngÃ n|tr|triá»‡u|m|tá»·|ty|billion)?)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return null;
        }

        if (!decimal.TryParse(match.Groups[1].Value.Replace(",", string.Empty), out var baseValue))
        {
            return null;
        }

        var suffix = match.Groups[3].Value?.Trim().ToLowerInvariant();
        return suffix switch
        {
            "k" or "nghÃ¬n" or "ngan" or "ngÃ n" => baseValue * 1_000m,
            "tr" or "triá»‡u" or "m" => baseValue * 1_000_000m,
            "tá»·" or "ty" or "billion" => baseValue * 1_000_000_000m,
            _ => baseValue
        };
    }

    private static string? TryParseRiskMode(string message)
    {
        if (Regex.IsMatch(message, "mạo hiểm|aggressive", RegexOptions.IgnoreCase))
            return "AGGRESSIVE";
        if (Regex.IsMatch(message, "cÃ¢n báº±ng|balanced|bÃ¬nh thÆ°á»ng", RegexOptions.IgnoreCase))
            return "BALANCED";
        if (Regex.IsMatch(message, "an toàn|safe|phòng thủ", RegexOptions.IgnoreCase))
            return "SAFE";
        return null;
    }

    private static List<string> TryParseSymbols(string message)
    {
        var normalized = message.ToUpperInvariant();
        var results = new List<string>();
        
        // First, try to match full symbols like BTCUSD, ETHUSD, BTC/USD, etc.
        // Match both USDT and USD suffixes
        var fullMatches = Regex.Matches(normalized, @"([A-Z]{2,10})(?:/|-)?(USD|USDT)", RegexOptions.IgnoreCase);
        foreach (Match match in fullMatches)
        {
            var symbol = match.Groups[1].Value;
            var quote = match.Groups[2].Value;
            // Normalize to USD (not USDT) for consistency
            results.Add($"{symbol}USD");
        }
        
        // Also match standalone BTCUSD, ETHUSD without separator
        var directMatches = Regex.Matches(normalized, @"\b([A-Z]{2,10})(USD|USDT)\b", RegexOptions.IgnoreCase);
        foreach (Match match in directMatches)
        {
            var symbol = match.Groups[1].Value;
            var quote = match.Groups[2].Value;
            results.Add($"{symbol}USD");
        }

        // Capture standalone tickers (BTC, ETH, ZEC, SOL, OP, etc.) even without the /USD suffix
        var standalone = Regex.Matches(normalized, @"\b([A-Z]{2,5})\b")
            .Select(m => m.Groups[1].Value)
            .Where(v => KnownSymbols.Contains(v))
            .Select(v => $"{v}USD");

        results.AddRange(standalone);
        return results.Distinct().Take(5).ToList();
    }

    private static string? TryParseTimeHorizon(string message)
    {
        if (Regex.IsMatch(message, "scalping|\\b\\d{1,2}m\\b", RegexOptions.IgnoreCase)) return "scalping";
        if (Regex.IsMatch(message, "swing", RegexOptions.IgnoreCase)) return "swing";
        if (Regex.IsMatch(message, "intraday|trong ngày|1-2 ngày", RegexOptions.IgnoreCase)) return "intraday";
        return null;
    }

    private async Task<object?> BuildMarketSnapshotAsync(string? symbol, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return null;
        }

        var normalized = symbol.Replace("/", string.Empty).ToUpperInvariant();
        var candidates = BuildSymbolCandidates(normalized);
        
        CryptoPrice? latest = null;
        try
        {
            latest = await _db.CryptoPrices
                .Include(p => p.Cryptocurrency)
                .Where(p => p.Cryptocurrency != null &&
                            p.Cryptocurrency.Symbol != null &&
                            candidates.Contains(p.Cryptocurrency.Symbol))
                .OrderByDescending(p => p.CollectedAtUtc)
                .FirstOrDefaultAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying price data for {Symbol}", symbol);
        }

        if (latest == null || latest.PriceUsd <= 0m || latest.Cryptocurrency == null)
        {
            _logger.LogWarning("No price data for {Symbol}. Sending snapshot placeholder.", symbol);
            return new
            {
                symbol = normalized,
                has_price = false
            };
        }

        decimal? change1h = null;
        decimal? change4h = null;
        decimal? change24h = null;
        
        try
        {
            change1h = latest.PercentChange1h ?? await ComputeChangeOverWindowAsync(latest, TimeSpan.FromHours(1), ct);
            change4h = await ComputeChangeOverWindowAsync(latest, TimeSpan.FromHours(4), ct);
            change24h = latest.PercentChange24h ?? await ComputeChangeOverWindowAsync(latest, TimeSpan.FromHours(24), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error computing price changes for {Symbol}, using available data only", symbol);
        }
        
        // Fallback: náº¿u change_24h váº«n null sau khi compute, dÃ¹ng PercentChange24h trá»±c tiáº¿p
        if (!change24h.HasValue && latest.PercentChange24h.HasValue)
        {
            change24h = latest.PercentChange24h.Value;
        }
        
        var change7d = latest.PercentChange7d;

        double? volumeVsMa = null;
        var currentVolume24h = latest.Volume24h;
        if (currentVolume24h.HasValue && latest.CryptocurrencyId > 0)
        {
            try
            {
                var volumeSamples = await _db.CryptoPrices
                    .Where(p => p.CryptocurrencyId == latest.CryptocurrencyId && 
                               p.CollectedAtUtc <= latest.CollectedAtUtc &&
                               p.Volume24h.HasValue)
                    .OrderByDescending(p => p.CollectedAtUtc)
                    .Take(16)
                    .Select(p => p.Volume24h)
                    .ToListAsync(ct);

                var historicalVolumes = volumeSamples
                    .Skip(1)
                    .Where(v => v.HasValue && v.Value > 0m)
                    .Select(v => v!.Value)
                    .ToList();

                if (historicalVolumes.Count > 0 && currentVolume24h.HasValue)
                {
                    var averageVolume = historicalVolumes.Average();
                    if (averageVolume > 0)
                    {
                        volumeVsMa = (double)(currentVolume24h.Value / averageVolume);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error computing volume vs MA for CryptocurrencyId {Id}", latest.CryptocurrencyId);
            }
        }

        var trend1h = ResolveTrendLabel(change1h);
        var trend4h = ResolveTrendLabel(change4h);
        var trend24h = ResolveTrendLabel(change24h);
        
        // Náº¿u change_24h â‰¤ -1 thÃ¬ set trend_24h = "bearish" vÃ  is_bearish = true
        if (change24h.HasValue && change24h.Value <= -1m)
        {
            trend24h = "bearish";
        }
        
        var isBearish = new[] { change1h, change4h, change24h }.Any(v => v.HasValue && v.Value <= -1m);
        var price = latest.PriceUsd;

        return new
        {
            symbol = normalized,
            price = (double)price,
            has_price = true,
            change_1h = change1h.HasValue ? (double)change1h.Value : (double?)null,
            change_4h = change4h.HasValue ? (double)change4h.Value : (double?)null,
            change_24h = change24h.HasValue ? (double)change24h.Value : (double?)null,
            change_7d = change7d.HasValue ? (double)change7d.Value : (double?)null,
            trend_1h = trend1h,
            trend_4h = trend4h,
            trend_24h = trend24h,
            volume_vs_ma = volumeVsMa,
            volatility = change24h.HasValue ? Math.Abs((double)change24h.Value) / 100d : 0.05,
            support = (double)(price * 0.97m),
            resistance = (double)(price * 1.03m),
            is_bearish = isBearish
        };
    }

    private static HashSet<string> BuildSymbolCandidates(string normalized)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { normalized };
        if (normalized.EndsWith("USDT", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith("USD", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(normalized[..^3]); // remove USD/USDT suffix
            if (normalized.EndsWith("USDT", StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add(normalized[..^4]);
            }
        }
        if (normalized.Contains("USDT", StringComparison.OrdinalIgnoreCase) && !normalized.Contains("/"))
        {
            candidates.Add(normalized.Replace("USDT", "/USDT", StringComparison.OrdinalIgnoreCase));
        }
        return candidates;
    }

    private async Task<decimal?> ComputeChangeOverWindowAsync(CryptoPrice latest, TimeSpan window, CancellationToken ct)
    {
        if (latest.CryptocurrencyId <= 0 || latest.PriceUsd <= 0m)
        {
            return null;
        }

        try
        {
            var targetTime = latest.CollectedAtUtc - window;
            var past = await _db.CryptoPrices
                .Where(p => p.CryptocurrencyId == latest.CryptocurrencyId && 
                           p.CollectedAtUtc <= targetTime &&
                           p.PriceUsd > 0m)
                .OrderByDescending(p => p.CollectedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (past == null || past.PriceUsd <= 0m)
            {
                return null;
            }

            return (latest.PriceUsd - past.PriceUsd) / past.PriceUsd * 100m;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error computing change over window for CryptocurrencyId {Id}", latest.CryptocurrencyId);
            return null;
        }
    }

    private static string ResolveTrendLabel(decimal? change)
    {
        if (!change.HasValue)
        {
            return "neutral";
        }

        if (change.Value >= 1m)
        {
            return "bullish";
        }

        if (change.Value <= -1m)
        {
            return "bearish";
        }

        return "neutral";
    }

    private async Task<List<AiChatBotSuggestionDto>> PersistBotSuggestionsAsync(
        AiChatSessionContext session,
        List<PythonBotSuggestion>? bots,
        CancellationToken ct)
    {
        var results = new List<AiChatBotSuggestionDto>();
        if (bots == null || bots.Count == 0)
        {
            return results;
        }

        foreach (var bot in bots)
        {
            var entity = new AiGeneratedBotProfile
            {
                Id = Guid.NewGuid(),
                UserId = session.UserId,
                SessionId = session.SessionId,
                Name = bot.Name,
                SymbolsJson = JsonSerializer.Serialize(bot.Symbols),
                StrategyType = bot.StrategyType,
                RiskMode = bot.RiskMode,
                MaxCapitalPerTrade = (decimal)bot.MaxCapitalPerTrade,
                MaxDailyExposure = (decimal)bot.MaxDailyExposure,
                TimeHorizon = bot.TimeHorizon,
                ExpectedReturnPct = bot.ExpectedReturnPct.HasValue ? (decimal)bot.ExpectedReturnPct.Value : null,
                RiskNote = bot.RiskNote,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.AiGeneratedBotProfiles.Add(entity);
            results.Add(new AiChatBotSuggestionDto
            {
                SuggestionId = entity.Id,
                Name = entity.Name,
                Symbols = bot.Symbols,
                StrategyType = entity.StrategyType,
                RiskMode = entity.RiskMode,
                MaxCapitalPerTrade = entity.MaxCapitalPerTrade,
                MaxDailyExposure = entity.MaxDailyExposure,
                TimeHorizon = entity.TimeHorizon,
                ExpectedReturnPct = entity.ExpectedReturnPct,
                RiskNote = entity.RiskNote
            });
        }

        await _db.SaveChangesAsync(ct);
        return results;
    }

    private async Task<BotStrategyDefinition?> ResolveStrategyDefinitionAsync(string strategyKey, CancellationToken ct)
    {
        var normalizedKey = NormalizeStrategyKey(strategyKey);

        // Prefer an exact match with the normalized key, but fall back to any active strategy
        return await _db.BotStrategyDefinitions
                   .Where(s => s.IsActive && s.StrategyKey == normalizedKey)
                   .OrderByDescending(s => s.CreatedAt)
                   .FirstOrDefaultAsync(ct)
               ?? await _db.BotStrategyDefinitions
                   .Where(s => s.IsActive)
                   .OrderBy(s => s.CreatedAt)
                   .FirstOrDefaultAsync(ct);
    }

    private static string NormalizeStrategyKey(string strategyType)
    {
        if (string.IsNullOrWhiteSpace(strategyType))
        {
            return string.Empty;
        }

        var key = strategyType.Trim()
            .Replace(" ", "-")
            .Replace("_", "-")
            .ToLowerInvariant();

        return key switch
        {
            // Map AI suggestion labels to concrete built-in strategies
            // Grid Trading dùng MARKET orders để trade trực tiếp với market ảo (virtual counterparty)
            // Không match với limit orders của users nữa
            "trend-following" => "grid-basic",
            "breakout" => "grid-basic",
            "scalping" => "grid-basic",
            "momentum" => "grid-basic",
            "dca" => "grid-basic",
            "dca-pullback" => "grid-basic",
            "grid" => "grid-basic",
            _ => "grid-basic"  // Default về grid-basic
        };
    }

    private static (string BaseAsset, string QuoteAsset) ParseSymbol(string symbol)
    {
        if (symbol.Contains("/"))
        {
            var parts = symbol.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2) return (parts[0], parts[1]);
        }

        var quotes = new[] { "USDT", "USD", "USDC", "BTC", "ETH" };
        foreach (var quote in quotes)
        {
            if (symbol.EndsWith(quote, StringComparison.OrdinalIgnoreCase))
            {
                return (symbol[..^quote.Length], quote);
            }
        }

        return (symbol, "USDT");
    }

    private sealed record MarketHighlightResult(List<object> Highlights, bool IsMarketDown);

    private sealed class PythonAiChatResponse
    {
        public string Reply { get; set; } = string.Empty;
        public PythonTradeSuggestion? TradeSuggestion { get; set; }
        public List<PythonBotSuggestion>? Bots { get; set; }
    }

    private sealed class PythonTradeSuggestion
    {
        public string Decision { get; set; } = "NO_TRADE";
        public string Symbol { get; set; } = string.Empty;
        public double Amount_Usdt { get; set; }
        public double? Expected_Return_Pct { get; set; }
        public double Confidence { get; set; }
        public string Time_Horizon { get; set; } = "intraday";

        public AiTradeSuggestion ToDto() => new()
        {
            Decision = Decision,
            Symbol = Symbol,
            AmountUsdt = (decimal)Amount_Usdt,
            ExpectedReturnPct = Expected_Return_Pct.HasValue ? (decimal)Expected_Return_Pct.Value : null,
            Confidence = Confidence,
            TimeHorizon = Time_Horizon
        };
    }

    private sealed class PythonBotSuggestion
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("symbols")]
        public string[] Symbols { get; set; } = Array.Empty<string>();

        [JsonPropertyName("strategy_type")]
        public string StrategyType { get; set; } = string.Empty;

        [JsonPropertyName("risk_mode")]
        public string RiskMode { get; set; } = string.Empty;

        [JsonPropertyName("max_capital_per_trade")]
        public double MaxCapitalPerTrade { get; set; }

        [JsonPropertyName("max_daily_exposure")]
        public double MaxDailyExposure { get; set; }

        [JsonPropertyName("time_horizon")]
        public string TimeHorizon { get; set; } = string.Empty;

        [JsonPropertyName("expected_return_pct")]
        public double? ExpectedReturnPct { get; set; }

        [JsonPropertyName("risk_note")]
        public string? RiskNote { get; set; }
    }
}

