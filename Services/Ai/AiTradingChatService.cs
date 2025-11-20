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
        ILogger<AiTradingChatService> logger)
    {
        _db = db;
        _sessionStore = sessionStore;
        _portfolioService = portfolioService;
        _botApplicationService = botApplicationService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<AiChatResponseDto> HandleMessageAsync(AiChatMessageRequest request, CancellationToken ct = default)
    {
        var session = await _sessionStore.GetOrCreateAsync(request.UserId, request.SessionId, ct);
        if (IsCreateBotCommand(request.Message))
        {
            var updatedForCommand = AppendConversationEntry(session, request.Message);
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
        
        PythonAiChatResponse? aiResponse = null;
        try
        {
            var payload = await BuildPythonPayloadAsync(
                sessionToPersist,
                request.UserId,
                request.Message,
                mode: "chat",
                contextSummary: conversationSummary,
                intent: intent,
                marketContext: highlightResult,
                ct);

            aiResponse = await SendAiChatAsync(payload, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AI chat service for user {UserId}", request.UserId);
        }

        var replySegments = new List<string>();

        var shouldForceMarketScanFallback = intent == "market_scan" &&
                                            (aiResponse == null ||
                                             string.IsNullOrWhiteSpace(aiResponse?.Reply) ||
                                             aiResponse.TradeSuggestion == null ||
                                             string.Equals(aiResponse.TradeSuggestion.Decision, "NO_TRADE", StringComparison.OrdinalIgnoreCase));

        if (intent == "market_scan" && shouldForceMarketScanFallback)
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
            if (!string.IsNullOrWhiteSpace(conversationSummary))
            {
                replySegments.Add(conversationSummary);
            }

            if (aiResponse != null && !string.IsNullOrWhiteSpace(aiResponse.Reply))
            {
                replySegments.Add(aiResponse.Reply);
            }
            else if (aiResponse == null)
            {
                if (intent == "direct_advice")
                {
                    replySegments.Add("Tạm thời mình không thể đưa ra lệnh giao dịch. Vui lòng thử lại sau hoặc kiểm tra kết nối AI service.");
                }
                else
                {
                    replySegments.Add("Xin lỗi, mình đang gặp sự cố kỹ thuật. Vui lòng thử lại sau.");
                }
            }

            if (!string.IsNullOrWhiteSpace(followUpQuestion) && intent == "chat" && aiResponse != null)
            {
                replySegments.Add(followUpQuestion);
            }
        }

        if (showBotHint)
        {
            replySegments.Add(BotHintMessage);
        }

        var finalReply = string.Join("\n\n", replySegments.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new AiChatResponseDto
        {
            SessionId = sessionToPersist.SessionId.ToString(),
            Reply = finalReply,
            Bots = new List<AiChatBotSuggestionDto>(),
            TradeSuggestion = aiResponse?.TradeSuggestion?.ToDto()
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
                "Äá»ƒ dá»±ng bot cho báº¡n mÃ¬nh cáº§n thÃªm má»™t chÃºt thÃ´ng tin."
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

        var conversationSummary = BuildConversationSummary(session);
        var highlightResult = await BuildMarketHighlightsAsync(3, ct);
        
        PythonAiChatResponse? aiResponse = null;
        List<AiChatBotSuggestionDto> botDtos = new();
        
        try
        {
            var payload = await BuildPythonPayloadAsync(
                session,
                request.UserId,
                request.Message,
                mode: "create_bot",
                contextSummary: conversationSummary,
                intent: "create_bot",
                marketContext: highlightResult,
                ct);

            aiResponse = await SendAiChatAsync(payload, ct);
            botDtos = await PersistBotSuggestionsAsync(session, aiResponse.Bots, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bot for user {UserId}", request.UserId);
        }

        var replyLines = new List<string>();
        var botSummary = BuildBotContextSummary(session);
        if (!string.IsNullOrWhiteSpace(botSummary))
        {
            replyLines.Add("Mình đang dựng bot dựa trên cấu hình sau:");
            replyLines.Add(botSummary);
        }
        
        if (aiResponse != null && !string.IsNullOrWhiteSpace(aiResponse.Reply))
        {
            replyLines.Add(aiResponse.Reply);
            replyLines.Add("Bạn cứ nói thêm nếu muốn chỉnh sửa thông số hoặc dựng bot khác.");
        }
        else
        {
            replyLines.Add("Xin lỗi, mình đang gặp sự cố kỹ thuật khi tạo bot. Vui lòng thử lại sau.");
        }

        return new AiChatResponseDto
        {
            SessionId = session.SessionId.ToString(),
            Reply = string.Join("\n\n", replyLines.Where(s => !string.IsNullOrWhiteSpace(s))),
            Bots = botDtos,
            TradeSuggestion = aiResponse?.TradeSuggestion?.ToDto()
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
            symbols = new[] { "BTCUSDT" };
        }

        var (baseAsset, quoteAsset) = ParseSymbol(symbols[0]);

        var strategy = await ResolveStrategyDefinitionAsync(profile.StrategyType, ct)
            ?? throw new InvalidOperationException("No active bot strategy available for AI suggestions.");

        var createRequest = new CreateBotRequest
        {
            Name = profile.Name,
            StrategyDefinitionId = strategy.Id,
            BaseAsset = baseAsset,
            QuoteAsset = quoteAsset,
            RiskProfile = profile.RiskMode,
            Parameters = new Dictionary<string, object>
            {
                ["ai_source"] = "chat",
                ["symbols"] = symbols
            },
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
        snapshotSymbol ??= "BTCUSDT";
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
            time_horizon = session.TimeHorizon ?? "intraday"
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
            parts.Add($"vốn khoảng {formatted} USDT");
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
            parts.Add($"khung thá»i gian {ctx.TimeHorizon}");
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
            lines.Add($"• Vốn: {ctx.TotalEquity.Value.ToString("N0", ViCulture)} USDT");
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
            "capital" => "Báº¡n dá»± Ä‘á»‹nh dÃ¹ng khoáº£ng bao nhiÃªu vá»‘n cho káº¿ hoáº¡ch nÃ y Ä‘á»ƒ mÃ¬nh canh tá»· trá»ng chuáº©n hÆ¡n?",
            "risk" => "Báº¡n thiÃªn vá» phong cÃ¡ch máº¡o hiá»ƒm, cÃ¢n báº±ng hay an toÃ n Ä‘á»ƒ mÃ¬nh chá»n chiáº¿n lÆ°á»£c phÃ¹ há»£p?",
            "symbols" => "Bạn muốn tập trung vào cặp nào? Ví dụ BTCUSDT hay ETHUSDT cũng được.",
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
        if (Regex.IsMatch(message, "máº¡o hiá»ƒm|aggressive", RegexOptions.IgnoreCase))
            return "AGGRESSIVE";
        if (Regex.IsMatch(message, "cÃ¢n báº±ng|balanced|bÃ¬nh thÆ°á»ng", RegexOptions.IgnoreCase))
            return "BALANCED";
        if (Regex.IsMatch(message, "an toÃ n|safe|phÃ²ng thá»§", RegexOptions.IgnoreCase))
            return "SAFE";
        return null;
    }

    private static List<string> TryParseSymbols(string message)
    {
        var normalized = message.ToUpperInvariant();
        var matches = Regex.Matches(normalized, @"[A-Z]{2,10}(?:/|-)?USDT");
        var results = matches.Select(m => m.Value.Replace("-", "/")).ToList();

        // Capture standalone tickers (BTC, ETH, ZEC, SOL, OP, etc.) even without the /USDT suffix
        var standalone = Regex.Matches(normalized, @"\b[A-Z]{2,5}\b")
            .Select(m => m.Value)
            .Where(v => KnownSymbols.Contains(v))
            .Select(v => $"{v}USDT");

        results.AddRange(standalone);
        return results.Distinct().Take(5).ToList();
    }

    private static string? TryParseTimeHorizon(string message)
    {
        if (Regex.IsMatch(message, "scalping|\\b\\d{1,2}m\\b", RegexOptions.IgnoreCase)) return "scalping";
        if (Regex.IsMatch(message, "swing", RegexOptions.IgnoreCase)) return "swing";
        if (Regex.IsMatch(message, "intraday|trong ngÃ y|1-2 ngÃ y", RegexOptions.IgnoreCase)) return "intraday";
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
        return await _db.BotStrategyDefinitions
            .Where(s => s.IsActive && s.StrategyKey == strategyKey)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct)
            ?? await _db.BotStrategyDefinitions
                .Where(s => s.IsActive)
                .OrderBy(s => s.CreatedAt)
                .FirstOrDefaultAsync(ct);
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

