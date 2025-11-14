using CryptoTrading.Data;
using CryptoTrading.Interfaces.Bot;
using CryptoTradingApp.Models.Bot;
using CryptoTradingApp.Services.Risk;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace CryptoTrading.Services.Bot
{
    /// <summary>
    /// Enhanced risk manager with kill switch integration and dynamic capital limits
    /// </summary>
    public class RiskManager : IRiskManager
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RiskManager> _logger;
        private readonly IKillSwitchService? _killSwitchService;
        private readonly IPositionTracker? _positionTracker;
        private readonly IPortfolioService? _portfolioService;

        // Default limits (used when UserCapitalLimits not configured)
        private const decimal DEFAULT_MAX_EXPOSURE_PER_USER = 10000m; // $10k (lowered from 100k)
        private const decimal DEFAULT_MAX_CAPITAL_PER_BOT = 5000m; // $5k (lowered from 50k)
        private const int DEFAULT_MAX_BOTS_PER_USER = 5; // Lowered from 10

        public RiskManager(
            ApplicationDbContext context,
            ILogger<RiskManager> logger,
            IKillSwitchService? killSwitchService = null,
            IPositionTracker? positionTracker = null,
            IPortfolioService? portfolioService = null)
        {
            _context = context;
            _logger = logger;
            _killSwitchService = killSwitchService;
            _positionTracker = positionTracker;
            _portfolioService = portfolioService;
        }

        public async Task<bool> CheckLimitsAsync(
            int userId,
            decimal requiredCapital,
            CancellationToken cancellationToken = default)
        {
            // Get user capital limits from database or use defaults
            var userLimits = await GetUserCapitalLimitsAsync(userId, cancellationToken);

            // Check per-bot capital limit
            if (requiredCapital > userLimits.MaxCapitalPerBot)
            {
                _logger.LogWarning(
                    "Capital {Capital} exceeds per-bot limit {Limit} for user {UserId}",
                    requiredCapital, userLimits.MaxCapitalPerBot, userId);
                return false;
            }

            // Check total exposure across all bots
            decimal currentExposure;
            if (_positionTracker != null)
            {
                // Use position tracker for accurate exposure calculation
                currentExposure = await _positionTracker.GetUserTotalExposureAsync(userId);
                _logger.LogDebug("User {UserId} current exposure from PositionTracker: {Exposure}",
                    userId, currentExposure);
            }
            else
            {
                // Fallback to simple calculation
                currentExposure = await GetCurrentExposureAsync(userId, cancellationToken);
            }

            if (currentExposure + requiredCapital > userLimits.MaxTotalExposure)
            {
                _logger.LogWarning(
                    "Total exposure ({Current} + {Required} = {Total}) would exceed limit {Limit} for user {UserId}",
                    currentExposure, requiredCapital, currentExposure + requiredCapital,
                    userLimits.MaxTotalExposure, userId);
                return false;
            }

            // Check number of active bots
            var activeBots = await _context.TradingBots
                .CountAsync(b => b.UserId == userId &&
                                (b.Status == "Running" || b.Status == "Starting"),
                           cancellationToken);

            if (activeBots >= userLimits.MaxBotsAllowed)
            {
                _logger.LogWarning(
                    "User {UserId} has reached max bots limit: {Active}/{Max}",
                    userId, activeBots, userLimits.MaxBotsAllowed);
                return false;
            }

            // Check portfolio balance if available
            if (_portfolioService != null)
            {
                try
                {
                    var balance = await _portfolioService.GetBalanceAsync(userId, "USDT");
                    var availableBalance = balance.Available;

                    if (requiredCapital > availableBalance)
                    {
                        _logger.LogWarning(
                            "User {UserId} insufficient balance: Required={Required}, Available={Available}",
                            userId, requiredCapital, availableBalance);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check portfolio balance for user {UserId}", userId);
                    // Don't fail the check if portfolio service unavailable
                }
            }

            _logger.LogDebug(
                "Risk limits check passed for user {UserId}: Capital={Capital}, Exposure={Exposure}/{Max}, Bots={Bots}/{MaxBots}",
                userId, requiredCapital, currentExposure, userLimits.MaxTotalExposure,
                activeBots, userLimits.MaxBotsAllowed);

            return true;
        }

        /// <summary>
        /// Gets bot capital limit for allocation
        /// </summary>
        public async Task<decimal> GetBotCapitalLimitAsync(int userId, Guid botId, CancellationToken cancellationToken = default)
        {
            var userLimits = await GetUserCapitalLimitsAsync(userId, cancellationToken);

            // Get bot's current position sizing if configured
            var bot = await _context.TradingBots.FindAsync(new object[] { botId }, cancellationToken);
            if (bot != null && !string.IsNullOrEmpty(bot.PositionSizing))
            {
                try
                {
                    var sizing = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(bot.PositionSizing);
                    if (sizing != null && sizing.TryGetValue("capitalAllocation", out var capital))
                    {
                        var configuredCapital = Convert.ToDecimal(capital);
                        // Return configured capital, capped at user's per-bot limit
                        return Math.Min(configuredCapital, userLimits.MaxCapitalPerBot);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse position sizing for bot {BotId}", botId);
                }
            }

            // Default to per-bot limit
            return userLimits.MaxCapitalPerBot;
        }

        /// <summary>
        /// Checks if kill switch is triggered for a bot
        /// </summary>
        public async Task<bool> CheckKillSwitchAsync(Guid botId, int userId, CancellationToken cancellationToken = default)
        {
            if (_killSwitchService == null)
            {
                _logger.LogDebug("Kill switch service not available, check skipped");
                return false; // Not triggered if service not available
            }

            try
            {
                var result = await _killSwitchService.CheckKillSwitchAsync((int)(long)botId, userId);

                if (result.ShouldStop)
                {
                    _logger.LogWarning(
                        "Kill switch triggered for bot {BotId}: {Reason}",
                        botId, result.Reason);
                }

                return result.ShouldStop;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking kill switch for bot {BotId}", botId);
                return false; // Don't stop bot if check fails
            }
        }

        /// <summary>
        /// Checks cooldown for order placement
        /// </summary>
        public async Task<bool> CheckCooldownAsync(Guid botId, TimeSpan minCooldown, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get bot risk state
                var riskState = await _context.Set<BotRiskState>()
                    .FirstOrDefaultAsync(r => r.BotId == (int)(long)botId, cancellationToken);

                if (riskState?.LastOrderAt == null)
                {
                    return true; // No previous order, cooldown passed
                }

                var timeSinceLastOrder = DateTime.UtcNow - riskState.LastOrderAt.Value;
                var cooldownPassed = timeSinceLastOrder >= minCooldown;

                if (!cooldownPassed)
                {
                    _logger.LogDebug(
                        "Bot {BotId} in cooldown: {Elapsed}s / {Required}s",
                        botId, timeSinceLastOrder.TotalSeconds, minCooldown.TotalSeconds);
                }

                return cooldownPassed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking cooldown for bot {BotId}", botId);
                return true; // Allow if check fails
            }
        }

        /// <summary>
        /// Checks rate limit for order placement
        /// </summary>
        public async Task<bool> CheckRateLimitAsync(Guid botId, int maxOrdersPerCycle, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get bot risk state
                var riskState = await _context.Set<BotRiskState>()
                    .FirstOrDefaultAsync(r => r.BotId == (int)(long)botId, cancellationToken);

                if (riskState == null)
                {
                    return true; // No state, allow
                }

                var withinLimit = riskState.OrderCountThisCycle < maxOrdersPerCycle;

                if (!withinLimit)
                {
                    _logger.LogDebug(
                        "Bot {BotId} rate limit reached: {Count}/{Max} orders this cycle",
                        botId, riskState.OrderCountThisCycle, maxOrdersPerCycle);
                }

                return withinLimit;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking rate limit for bot {BotId}", botId);
                return true; // Allow if check fails
            }
        }

        /// <summary>
        /// Resets order count for a new execution cycle
        /// </summary>
        public async Task ResetOrderCountForNewCycleAsync(Guid botId, CancellationToken cancellationToken = default)
        {
            try
            {
                var riskState = await _context.Set<BotRiskState>()
                    .FirstOrDefaultAsync(r => r.BotId == (int)(long)botId, cancellationToken);

                if (riskState == null)
                {
                    // Create new risk state
                    riskState = new BotRiskState
                    {
                        BotId = (int)(long)botId,
                        OrderCountThisCycle = 0,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Set<BotRiskState>().Add(riskState);
                }
                else
                {
                    // Reset counter for new cycle
                    riskState.OrderCountThisCycle = 0;
                    riskState.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Reset order count for bot {BotId} (new cycle)", botId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reset order count for bot {BotId}", botId);
                // Don't throw - fail gracefully
            }
        }

        /// <summary>
        /// Records that an order was placed (updates timestamp and counter)
        /// </summary>
        public async Task RecordOrderPlacedAsync(Guid botId, CancellationToken cancellationToken = default)
        {
            try
            {
                var riskState = await _context.Set<BotRiskState>()
                    .FirstOrDefaultAsync(r => r.BotId == (int)(long)botId, cancellationToken);

                if (riskState == null)
                {
                    // Create new risk state
                    riskState = new BotRiskState
                    {
                        BotId = (int)(long)botId,
                        LastOrderAt = DateTime.UtcNow,
                        OrderCountThisCycle = 1,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Set<BotRiskState>().Add(riskState);
                }
                else
                {
                    // Update existing state
                    riskState.LastOrderAt = DateTime.UtcNow;
                    riskState.OrderCountThisCycle++;
                    riskState.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug(
                    "Recorded order placed for bot {BotId} (count this cycle: {Count})",
                    botId, riskState.OrderCountThisCycle);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record order placed for bot {BotId}", botId);
                // Don't throw - fail gracefully
            }
        }

        /// <summary>
        /// Records a trade result for kill switch monitoring (Guid wrapper)
        /// </summary>
        public async Task RecordTradeResultAsync(Guid botId, decimal pnl, CancellationToken cancellationToken = default)
        {
            if (_killSwitchService == null)
            {
                _logger.LogDebug("Kill switch service not available, trade result not recorded");
                return;
            }

            try
            {
                var isProfit = pnl > 0;
                await _killSwitchService.RecordTradeResultAsync((int)(long)botId, pnl, isProfit);

                _logger.LogDebug(
                    "Recorded trade result for bot {BotId}: PnL=${PnL:F2}, IsProfit={IsProfit}",
                    botId, pnl, isProfit);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record trade result for bot {BotId}", botId);
                // Don't throw - fail gracefully
            }
        }

        private async Task<UserCapitalLimitsDto> GetUserCapitalLimitsAsync(int userId, CancellationToken cancellationToken)
        {
            try
            {
                // Try to get from database
                var limits = await _context.Set<UserCapitalLimits>()
                    .FirstOrDefaultAsync(l => l.UserId == userId, cancellationToken);

                if (limits != null)
                {
                    return new UserCapitalLimitsDto
                    {
                        MaxTotalExposure = limits.MaxTotalExposure,
                        MaxCapitalPerBot = limits.MaxCapitalPerBot,
                        MaxBotsAllowed = limits.MaxBotsAllowed,
                        MaxDailyLoss = limits.MaxDailyLoss
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load user capital limits from database for user {UserId}", userId);
            }

            // Return defaults if not found
            return new UserCapitalLimitsDto
            {
                MaxTotalExposure = DEFAULT_MAX_EXPOSURE_PER_USER,
                MaxCapitalPerBot = DEFAULT_MAX_CAPITAL_PER_BOT,
                MaxBotsAllowed = DEFAULT_MAX_BOTS_PER_USER,
                MaxDailyLoss = 500m // Default daily loss limit
            };
        }

        public async Task<decimal> GetMaxExposureAsync(
            int userId, 
            CancellationToken cancellationToken = default)
        {
            var currentExposure = await GetCurrentExposureAsync(userId, cancellationToken);
            return Math.Max(0, DEFAULT_MAX_EXPOSURE_PER_USER - currentExposure);
        }

        private async Task<decimal> GetCurrentExposureAsync(
            int userId, 
            CancellationToken cancellationToken = default)
        {
            // Sum up capital allocation from all running bots
            var bots = await _context.TradingBots
                .Where(b => b.UserId == userId && 
                           (b.Status == "Running" || b.Status == "Starting"))
                .ToListAsync(cancellationToken);

            decimal totalExposure = 0;
            foreach (var bot in bots)
            {
                if (!string.IsNullOrEmpty(bot.PositionSizing))
                {
                    try
                    {
                        var sizing = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(bot.PositionSizing);
                        if (sizing != null && sizing.TryGetValue("capitalAllocation", out var capital))
                        {
                            totalExposure += Convert.ToDecimal(capital);
                        }
                    }
                    catch
                    {
                        // Ignore parse errors
                    }
                }
            }

            return totalExposure;
        }
    }

    /// <summary>
    /// DTO for user capital limits
    /// </summary>
    internal class UserCapitalLimitsDto
    {
        public decimal MaxTotalExposure { get; set; }
        public decimal MaxCapitalPerBot { get; set; }
        public int MaxBotsAllowed { get; set; }
        public decimal MaxDailyLoss { get; set; }
    }
}

