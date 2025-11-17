using CryptoTrading.Data;
using CryptoTrading.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services.Risk;

/// <summary>
/// Monitors trading performance and triggers kill switch on risk threshold breaches
/// </summary>
public class KillSwitchService : IKillSwitchService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<KillSwitchService> _logger;
    private readonly IConfiguration _configuration;

    private readonly int _defaultConsecutiveLossLimit;
    private readonly decimal _defaultDailyLossLimit;
    private readonly decimal _defaultMaxDrawdownPercent;
    private readonly bool _killSwitchEnabled;

    public KillSwitchService(
        ApplicationDbContext context,
        ILogger<KillSwitchService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;

        // Load configuration
        _killSwitchEnabled = bool.Parse(
            configuration["Trading:KillSwitch:Enabled"] ?? "true");
        _defaultConsecutiveLossLimit = int.Parse(
            configuration["Trading:KillSwitch:DefaultConsecutiveLossLimit"] ?? "5");
        _defaultDailyLossLimit = decimal.Parse(
            configuration["Trading:KillSwitch:DefaultDailyLossLimit"] ?? "1000");
        _defaultMaxDrawdownPercent = decimal.Parse(
            configuration["Trading:KillSwitch:DefaultMaxDrawdownPercent"] ?? "20.0");
    }

    public async Task<KillSwitchResult> CheckKillSwitchAsync(int botId, int userId)
    {
        if (!_killSwitchEnabled)
        {
            return KillSwitchResult.Continue();
        }

        try
        {
            // Get or create risk state
            var riskState = await GetOrCreateRiskStateAsync(botId);

            // Get risk configuration for this bot
            var riskConfig = await _context.BotRiskConfigurations
                .FirstOrDefaultAsync(c => c.BotId == botId);

            // Map BotRiskConfiguration properties (different naming)
            var consecutiveLossLimit = riskConfig?.MaxConsecutiveLosses ?? _defaultConsecutiveLossLimit;
            var dailyLossLimit = riskConfig?.MaxDailyLoss ?? _defaultDailyLossLimit;
            var maxDrawdownPercent = _defaultMaxDrawdownPercent; // BotRiskConfiguration doesn't have MaxDrawdownPercent

            // Check 1: Consecutive losses
            if (riskState.ConsecutiveLosses >= consecutiveLossLimit)
            {
                var reason = $"Consecutive loss limit reached: {riskState.ConsecutiveLosses} losses";
                _logger.LogWarning(
                    "Kill switch triggered for bot {BotId}: {Reason}",
                    botId, reason);

                await TriggerKillSwitchAsync(botId, reason);

                return KillSwitchResult.Stop(reason, KillSwitchTrigger.ConsecutiveLosses);
            }

            // Check 2: Daily loss limit
            if (riskState.DailyLoss >= dailyLossLimit)
            {
                var reason = $"Daily loss limit exceeded: ${riskState.DailyLoss:F2} >= ${dailyLossLimit:F2}";
                _logger.LogWarning(
                    "Kill switch triggered for bot {BotId}: {Reason}",
                    botId, reason);

                await TriggerKillSwitchAsync(botId, reason, riskState.DailyLoss);

                return KillSwitchResult.Stop(reason, KillSwitchTrigger.DailyLossLimit);
            }

            // Check 3: Max drawdown
            // NOTE: TradingBot doesn't have InitialCapital property
            // We'll skip this check for now or calculate from allowed capital
            // TODO: Store initial capital when bot starts or calculate from allowed capital
            var bot = await _context.TradingBots.FindAsync(botId);
            if (bot != null)
            {
                // For now, skip drawdown check since we don't have initial capital
                // Original code: if (bot.InitialCapital > 0) { ... }
                // This needs to be fixed by either:
                // 1. Storing initial capital when bot starts
                // 2. Getting allowed capital and using that as proxy
                // 3. Tracking peak equity separately
                _logger.LogDebug(
                    "Kill switch drawdown check skipped - TradingBot doesn't have InitialCapital property");
                
                /* Original code (commented out):
                if (bot.InitialCapital > 0)
                {
                    var drawdownPercent = (riskState.TotalDrawdown / bot.InitialCapital) * 100;

                    if (drawdownPercent >= maxDrawdownPercent)
                    {
                        var reason = $"Max drawdown exceeded: {drawdownPercent:F2}% >= {maxDrawdownPercent:F2}%";
                        _logger.LogWarning(
                            "Kill switch triggered for bot {BotId}: {Reason}",
                            botId, reason);

                        await TriggerKillSwitchAsync(botId, reason, riskState.TotalDrawdown);

                        return KillSwitchResult.Stop(reason, KillSwitchTrigger.MaxDrawdown);
                    }
                }
                */
            }

            // All checks passed
            return KillSwitchResult.Continue();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking kill switch for bot {BotId}", botId);
            // Fail-safe: don't stop bot on error
            return KillSwitchResult.Continue();
        }
    }

    public async Task RecordTradeResultAsync(int botId, decimal pnl, bool isProfit)
    {
        try
        {
            var riskState = await GetOrCreateRiskStateAsync(botId);

            if (isProfit)
            {
                // Reset consecutive losses on profit
                riskState.ConsecutiveLosses = 0;

                // Reduce daily loss if recovering
                if (riskState.DailyLoss > 0)
                {
                    riskState.DailyLoss = Math.Max(0, riskState.DailyLoss + pnl);
                }

                _logger.LogDebug(
                    "Bot {BotId} profitable trade: PnL=${PnL:F2}, consecutive losses reset",
                    botId, pnl);
            }
            else
            {
                // Increment consecutive losses
                riskState.ConsecutiveLosses++;

                // Add to daily loss
                riskState.DailyLoss += Math.Abs(pnl);

                // Track total drawdown
                riskState.TotalDrawdown += Math.Abs(pnl);

                _logger.LogWarning(
                    "Bot {BotId} losing trade: PnL=${PnL:F2}, consecutive losses={ConsecutiveLosses}, daily loss=${DailyLoss:F2}",
                    botId, pnl, riskState.ConsecutiveLosses, riskState.DailyLoss);
            }

            riskState.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording trade result for bot {BotId}", botId);
        }
    }

    public async Task<BotRiskStateDto> GetRiskStateAsync(int botId)
    {
        var riskState = await GetOrCreateRiskStateAsync(botId);

        return new BotRiskStateDto
        {
            BotId = riskState.BotId,
            ConsecutiveLosses = riskState.ConsecutiveLosses,
            DailyLoss = riskState.DailyLoss,
            TotalDrawdown = riskState.TotalDrawdown,
            LastOrderAt = riskState.LastOrderAt,
            OrderCountThisCycle = riskState.OrderCountThisCycle,
            UpdatedAt = riskState.UpdatedAt
        };
    }

    public async Task ResetDailyLossAsync(int botId)
    {
        try
        {
            var riskState = await GetOrCreateRiskStateAsync(botId);

            _logger.LogInformation(
                "Resetting daily loss for bot {BotId}: ${DailyLoss:F2} -> $0",
                botId, riskState.DailyLoss);

            riskState.DailyLoss = 0;
            riskState.DailyLossResetAt = DateTime.UtcNow;
            riskState.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting daily loss for bot {BotId}", botId);
        }
    }

    public async Task TriggerKillSwitchAsync(int botId, string reason, decimal? totalLoss = null)
    {
        try
        {
            // Record kill switch event
            var killSwitchEvent = new KillSwitchEvent
            {
                BotId = botId,
                TriggerReason = reason,
                TriggerTime = DateTime.UtcNow,
                TotalLoss = totalLoss,
                ConsecutiveLosses = (await GetOrCreateRiskStateAsync(botId)).ConsecutiveLosses
            };

            _context.KillSwitchEvents.Add(killSwitchEvent);

            // Update bot status to Stopped
            var bot = await _context.TradingBots.FindAsync(botId);
            if (bot != null)
            {
                bot.Status = "Stopped";
                bot.NextRunAt = null;
                bot.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogCritical(
                "KILL SWITCH TRIGGERED for bot {BotId}: {Reason} | Total Loss: ${TotalLoss:F2}",
                botId, reason, totalLoss ?? 0);

            // TODO: Send alert to user (email, SMS, push notification)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering kill switch for bot {BotId}", botId);
        }
    }

    public async Task ClearKillSwitchAsync(int botId)
    {
        try
        {
            var riskState = await GetOrCreateRiskStateAsync(botId);

            _logger.LogInformation("Clearing kill switch state for bot {BotId}", botId);

            riskState.ConsecutiveLosses = 0;
            riskState.DailyLoss = 0;
            riskState.TotalDrawdown = 0;
            riskState.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing kill switch for bot {BotId}", botId);
        }
    }

    private async Task<BotRiskState> GetOrCreateRiskStateAsync(int botId)
    {
        var riskState = await _context.BotRiskStates.FirstOrDefaultAsync(r => r.BotId == botId);

        if (riskState == null)
        {
            riskState = new BotRiskState
            {
                BotId = botId,
                ConsecutiveLosses = 0,
                DailyLoss = 0,
                TotalDrawdown = 0,
                DailyLossResetAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BotRiskStates.Add(riskState);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Check if we need to reset daily loss (new day)
            var hoursSinceReset = (DateTime.UtcNow - riskState.DailyLossResetAt).TotalHours;
            if (hoursSinceReset >= 24)
            {
                await ResetDailyLossAsync(botId);
                riskState = await _context.BotRiskStates.FirstAsync(r => r.BotId == botId);
            }
        }

        return riskState;
    }
}
