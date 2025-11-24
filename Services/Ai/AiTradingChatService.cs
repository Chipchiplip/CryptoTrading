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

        var highlights = await BuildMarketHighlightsAsync(3, ct);
        var payload = await BuildPythonPayloadAsync(
            sessionToPersist,
            request.UserId,
            request.Message,
            mode: "chat",
            contextSummary: conversationSummary,
            intent: intent,
            marketHighlights: highlights,
            ct);

        var aiResponse = await SendAiChatAsync(payload, ct);
        var replySegments = new List<string>();
        if (!string.IsNullOrWhiteSpace(conversationSummary))
        {
            replySegments.Add(conversationSummary);
        }
        if (!string.IsNullOrWhiteSpace(aiResponse.Reply))
        {
            replySegments.Add(aiResponse.Reply);
        }
        if (!string.IsNullOrWhiteSpace(followUpQuestion))
        {
            replySegments.Add(followUpQuestion);
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
            TradeSuggestion = aiResponse.TradeSuggestion?.ToDto()
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
                "Để dựng bot cho bạn mình cần thêm một chút thông tin."
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
        var highlights = await BuildMarketHighlightsAsync(3, ct);
        var payload = await BuildPythonPayloadAsync(
            session,
            request.UserId,
            request.Message,
            mode: "create_bot",
            contextSummary: conversationSummary,
            intent: "create_bot",
            marketHighlights: highlights,
            ct);

        var aiResponse = await SendAiChatAsync(payload, ct);
        var botDtos = await PersistBotSuggestionsAsync(session, aiResponse.Bots, ct);

        var replyLines = new List<string>();
        var botSummary = BuildBotContextSummary(session);
        if (!string.IsNullOrWhiteSpace(botSummary))
        {
            replyLines.Add("Mình đang dựng bot dựa trên cấu hình sau:");
            replyLines.Add(botSummary);
        }
        replyLines.Add(aiResponse.Reply);
        replyLines.Add("Bạn cứ nói thêm nếu muốn chỉnh sửa thông số hoặc dựng bot khác.");

        return new AiChatResponseDto
        {
            SessionId = session.SessionId.ToString(),
            Reply = string.Join("\n\n", replyLines.Where(s => !string.IsNullOrWhiteSpace(s))),
            Bots = botDtos,
            TradeSuggestion = aiResponse.TradeSuggestion?.ToDto()
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
        IReadOnlyCollection<object> marketHighlights,
        CancellationToken ct)
    {
        var portfolio = await _portfolioService.GetPortfolioOverviewAsync(userId);
        var symbols = session.PreferredSymbols.Any()
            ? session.PreferredSymbols
            : new List<string> { "BTCUSDT" };

        var marketSnapshot = await BuildMarketSnapshotAsync(symbols.First(), ct);

        var totalEquityDecimal = session.TotalEquity ?? portfolio.TotalValue;
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

        decimal? usdtBalance = portfolio.Holdings
            .FirstOrDefault(h => h.Symbol.Equals("USDT", StringComparison.OrdinalIgnoreCase))?.Amount;
        var availableUsdt = usdtBalance ?? portfolio.TotalValue;

        var portfolioPayload = new
        {
            total_value = (double)portfolio.TotalValue,
            available_usdt = (double)availableUsdt,
            holdings = portfolio.Holdings.Select(h => new
            {
                symbol = h.Symbol,
                amount = (double)h.Amount,
                value = (double)h.Value
            }).ToList()
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
            market_highlights = marketHighlights
        };
    }

    private async Task<List<object>> BuildMarketHighlightsAsync(int count, CancellationToken ct)
    {
        var highlights = await _db.CryptoPrices
            .Include(p => p.Cryptocurrency)
            .Where(p => p.PercentChange24h.HasValue && p.Cryptocurrency != null)
            .OrderByDescending(p => p.PercentChange24h)
            .Take(count)
            .Select(p => new
            {
                symbol = p.Cryptocurrency!.Symbol,
                price = (double)p.PriceUsd,
                change_24h = (double?)p.PercentChange24h
            })
            .ToListAsync(ct);

        return highlights.Cast<object>().ToList();
    }

    private async Task<PythonAiChatResponse> SendAiChatAsync(object payload, CancellationToken ct)
    {
        var httpClient = _httpClientFactory.CreateClient("AiChatService");
        var response = await httpClient.PostAsJsonAsync("/ai/chat", payload, SnakeCaseOptions, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PythonAiChatResponse>(ResponseOptions, cancellationToken: ct)
            ?? throw new InvalidOperationException("AI service returned empty payload");
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
            "capital" => "Bạn dự định dùng khoảng bao nhiêu vốn cho kế hoạch này để mình canh tỷ trọng chuẩn hơn?",
            "risk" => "Bạn thiên về phong cách mạo hiểm, cân bằng hay an toàn để mình chọn chiến lược phù hợp?",
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

    private static string ResolveIntent(string message)
    {
        if (IsDirectAdviceIntent(message))
        {
            return "direct_advice";
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
        var match = Regex.Match(message, @"(\d+(?:[\,\.]\d+)?)(\s*(k|nghìn|ngan|ngàn|tr|triệu|m|tỷ|ty|billion)?)", RegexOptions.IgnoreCase);
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
            "k" or "nghìn" or "ngan" or "ngàn" => baseValue * 1_000m,
            "tr" or "triệu" or "m" => baseValue * 1_000_000m,
            "tỷ" or "ty" or "billion" => baseValue * 1_000_000_000m,
            _ => baseValue
        };
    }

    private static string? TryParseRiskMode(string message)
    {
        if (Regex.IsMatch(message, "mạo hiểm|aggressive", RegexOptions.IgnoreCase))
            return "AGGRESSIVE";
        if (Regex.IsMatch(message, "cân bằng|balanced|bình thường", RegexOptions.IgnoreCase))
            return "BALANCED";
        if (Regex.IsMatch(message, "an toàn|safe|phòng thủ", RegexOptions.IgnoreCase))
            return "SAFE";
        return null;
    }

    private static List<string> TryParseSymbols(string message)
    {
        var normalized = message.ToUpperInvariant();
        var matches = Regex.Matches(normalized, @"[A-Z]{2,10}(?:/|-)?USDT");
        var results = matches.Select(m => m.Value.Replace("-", "/")).ToList();
        var standalone = Regex.Matches(normalized, @"\b[A-Z]{2,5}\b")
            .Select(m => m.Value)
            .Where(v => v is "BTC" or "ETH")
            .Select(v => $"{v}USDT");
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

    private async Task<object> BuildMarketSnapshotAsync(string symbol, CancellationToken ct)
    {
        var normalized = symbol.Replace("/", string.Empty);
        var candidates = BuildSymbolCandidates(normalized);
        var latest = await _db.CryptoPrices
            .Include(p => p.Cryptocurrency)
            .Where(p => candidates.Contains(p.Cryptocurrency!.Symbol))
            .OrderByDescending(p => p.CollectedAtUtc)
            .FirstOrDefaultAsync(ct);

        var hasPrice = latest?.PriceUsd > 0m;
        var price = hasPrice ? latest!.PriceUsd : 0m;
        if (!hasPrice)
        {
            _logger.LogWarning("No price data for {Symbol}. Sending snapshot without price.", symbol);
        }
        return new
        {
            symbol = normalized,
            price = hasPrice ? (double)price : 0d,
            has_price = hasPrice,
            trend_1h = "neutral",
            trend_4h = "neutral",
            volume_vs_ma = 0,
            volatility = 0.05,
            support = hasPrice ? (double)(price * 0.97m) : (double?)null,
            resistance = hasPrice ? (double)(price * 1.03m) : (double?)null,
            usdt_balance = 0,
            btc_holding = 0,
            eth_holding = 0
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


