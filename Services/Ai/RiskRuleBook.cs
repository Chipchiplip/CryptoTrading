namespace CryptoTrading.Services.Ai;

public static class RiskRuleBook
{
    private static readonly Dictionary<string, RiskRules> Rules =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["AGGRESSIVE"] = new RiskRules { TradePct = 0.05m, DailyPct = 0.40m },
            ["BALANCED"] = new RiskRules { TradePct = 0.03m, DailyPct = 0.25m },
            ["SAFE"] = new RiskRules { TradePct = 0.015m, DailyPct = 0.15m },
        };

    public static RiskRules Get(string? riskMode)
        => Rules.TryGetValue(riskMode ?? "BALANCED", out var rule)
            ? rule
            : Rules["BALANCED"];
}

public sealed class RiskRules
{
    public decimal TradePct { get; init; }
    public decimal DailyPct { get; init; }
}


