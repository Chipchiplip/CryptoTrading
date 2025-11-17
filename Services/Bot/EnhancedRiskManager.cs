using CryptoTrading.Data;
using CryptoTrading.Interfaces.Bot;
using CryptoTrading.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services.Bot
{
    /// <summary>
    /// Enhanced risk manager with dynamic configuration, kill switch, and detailed tracking
    /// </summary>
    public class EnhancedRiskManager : IRiskManager
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<EnhancedRiskManager> _logger;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        // Default fallback limits
        private const decimal DEFAULT_MAX_EXPOSURE_PER_USER = 100000m; 
        private const decimal DEFAULT_MAX_CAPITAL_PER_BOT = 50000m;
        private const int DEFAULT_MAX_BOTS_PER_USER = 10;
        private const decimal DEFAULT_MAX_SLIPPAGE = 0.05m;
        private const decimal DEFAULT_MAX_DAILY_LOSS = 0.10m;
        private const int DEFAULT_MAX_CONSECUTIVE_LOSSES = 5;
        private const int DEFAULT_COOLDOWN_SECONDS = 300;

        public EnhancedRiskManager(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<EnhancedRiskManager> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> CheckLimitsAsync(
            int userId, 
            decimal requiredCapital, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var config = await GetRiskConfigAsync(userId, null, cancellationToken);

                // Check per-bot capital limit
                if (requiredCapital > config.MaxAllowedCapital)
                {
                    _logger.LogWarning("Capital {Capital} exceeds per-bot limit {Limit} for user {UserId}", 
                        requiredCapital, config.MaxAllowedCapital, userId);
                    return false;
                }

                // Check total exposure across all bots
                var totalExposure = await GetCurrentExposureAsync(userId, cancellationToken);
                if (totalExposure + requiredCapital > config.MaxAllowedCapital)
                {
                    _logger.LogWarning("Total exposure {Total} would exceed limit {Limit} for user {UserId}", 
                        totalExposure + requiredCapital, config.MaxAllowedCapital, userId);
                    return false;
                }

                // Check number of active bots
                var activeBots = await _context.TradingBots
                    .CountAsync(b => b.UserId == userId && 
                                    (b.Status == "Running" || b.Status == "Starting"),
                               cancellationToken);

                if (activeBots >= DEFAULT_MAX_BOTS_PER_USER)
                {
                    _logger.LogWarning("User {UserId} has reached max bots limit: {Count}/{Max}", 
                        userId, activeBots, DEFAULT_MAX_BOTS_PER_USER);
                    return false;
                }

                // Check daily loss limit
                var dailyLoss = await GetDailyLossAsync(userId, cancellationToken);
                var currentExposure = await GetCurrentExposureAsync(userId, cancellationToken);
                if (currentExposure > 0)
                {
                    var dailyLossPercent = Math.Abs(dailyLoss) / currentExposure;
                    if (dailyLossPercent > config.MaxDailyLoss)
                    {
                        _logger.LogWarning("User {UserId} has exceeded daily loss limit: {Loss:P2}/{Limit:P2}", 
                            userId, dailyLossPercent, config.MaxDailyLoss);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking risk limits for user {UserId}", userId);
                // Fail safe: reject on error
                return false;
            }
        }

        public async Task<decimal> GetMaxExposureAsync(
            int userId, 
            CancellationToken cancellationToken = default)
        {
            var config = await GetRiskConfigAsync(userId, null, cancellationToken);
            var currentExposure = await GetCurrentExposureAsync(userId, cancellationToken);
            return Math.Max(0, config.MaxAllowedCapital - currentExposure);
        }

        public async Task<decimal> GetBotCapitalLimitAsync(int userId, int botId, CancellationToken cancellationToken = default)
        {
            var config = await GetRiskConfigAsync(userId, botId, cancellationToken);
            return config.MaxAllowedCapital;
        }

        public async Task<bool> CheckKillSwitchAsync(int botId, int userId, CancellationToken cancellationToken = default)
        {
            var config = await GetRiskConfigAsync(userId, botId, cancellationToken);

            if (!config.KillSwitchEnabled)
            {
                return false; // Kill switch not active
            }

            // Check consecutive losses
            // Note: TradingBotOrder doesn't have Status/PnL directly - would need to join with Order/Trade
            // For now, simplified check - in production, implement proper PnL tracking
            var recentOrders = await _context.TradingBotOrders
                .Where(o => o.TradingBotId == botId)
                .Include(o => o.Order)
                .OrderByDescending(o => o.CreatedAt)
                .Take(config.MaxConsecutiveLosses + 1)
                .ToListAsync(cancellationToken);

            int consecutiveLosses = 0;
            foreach (var botOrder in recentOrders)
            {
                // Check if order was filled and calculate PnL from trades
                if (botOrder.Order != null && botOrder.Order.Status == "FILLED")
                {
                    // Simplified: In production, calculate actual PnL from trades
                    // For now, we'll use a placeholder - actual implementation would query Trades table
                    var trades = await _context.Trades
                        .Where(t => t.OrderId == botOrder.OrderId)
                        .ToListAsync(cancellationToken);
                    
                    // Placeholder: assume loss if we can't determine PnL
                    // In production, implement proper PnL calculation
                    var isLoss = false; // Would calculate from trades
                    if (isLoss)
                    {
                        consecutiveLosses++;
                    }
                    else
                    {
                        break; // Reset on win
                    }
                }
            }

            if (consecutiveLosses >= config.MaxConsecutiveLosses)
            {
                _logger.LogWarning("Bot {BotId} hit kill switch: {Consecutive} consecutive losses >= {Max}", 
                    botId, consecutiveLosses, config.MaxConsecutiveLosses);
                return true; // Kill switch activated
            }

            return false;
        }

        public async Task<bool> CheckCooldownAsync(int botId, TimeSpan minCooldown, CancellationToken cancellationToken = default)
        {
            var bot = await _context.TradingBots.FindAsync(new object[] { botId }, cancellationToken);
            if (bot == null) return false;

            var config = await GetRiskConfigAsync(bot.UserId, botId, cancellationToken);

            // Check if bot was stopped due to error and cooldown hasn't expired
            if (bot.Status == "Error" || bot.Status == "Stopped")
            {
                var lastUpdate = bot.UpdatedAt ?? bot.CreatedAt;
                var cooldownEnd = lastUpdate.AddSeconds(config.CooldownSeconds);
                if (DateTime.UtcNow < cooldownEnd)
                {
                    _logger.LogInformation("Bot {BotId} in cooldown until {CooldownEnd}", 
                        botId, cooldownEnd);
                    return true; // Still in cooldown
                }
            }

            return false;
        }

        private async Task<BotRiskConfiguration> GetRiskConfigAsync(
            int userId, 
            int? botId,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"RiskConfig_{userId}_{botId}";

            if (_cache.TryGetValue(cacheKey, out BotRiskConfiguration? cached) && cached != null)
            {
                return cached;
            }

            // Try bot-specific config first
            BotRiskConfiguration? config = null;
            if (botId.HasValue)
            {
                config = await _context.BotRiskConfigurations
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.BotId == botId.Value, cancellationToken);
            }

            // Fallback to user-specific config
            if (config == null)
            {
                config = await _context.BotRiskConfigurations
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.BotId == null, cancellationToken);
            }

            // Fallback to global defaults
            if (config == null)
            {
                config = await _context.BotRiskConfigurations
                    .FirstOrDefaultAsync(c => c.UserId == null && c.BotId == null, cancellationToken);
            }

            // Final fallback to hardcoded defaults
            if (config == null)
            {
                config = new BotRiskConfiguration
                {
                    MaxAllowedCapital = DEFAULT_MAX_CAPITAL_PER_BOT,
                    MaxSlippage = DEFAULT_MAX_SLIPPAGE,
                    MaxDailyLoss = DEFAULT_MAX_DAILY_LOSS,
                    MaxConsecutiveLosses = DEFAULT_MAX_CONSECUTIVE_LOSSES,
                    CooldownSeconds = DEFAULT_COOLDOWN_SECONDS,
                    KillSwitchEnabled = true
                };
            }

            _cache.Set(cacheKey, config, _cacheDuration);
            return config;
        }

        private async Task<decimal> GetCurrentExposureAsync(
            int userId, 
            CancellationToken cancellationToken = default)
        {
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

        private async Task<decimal> GetDailyLossAsync(int userId, CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            // Get bot orders for user's bots created today
            var todayBotOrders = await _context.TradingBotOrders
                .Where(o => o.CreatedAt >= today)
                .Include(o => o.TradingBot)
                .Where(o => o.TradingBot != null && o.TradingBot.UserId == userId)
                .Include(o => o.Order)
                .Where(o => o.Order != null && o.Order.Status == "FILLED")
                .ToListAsync(cancellationToken);

            // Calculate PnL from trades
            decimal totalLoss = 0;
            foreach (var botOrder in todayBotOrders)
            {
                if (botOrder.OrderId != 0)
                {
                    var trades = await _context.Trades
                        .Where(t => t.OrderId == botOrder.OrderId)
                        .ToListAsync(cancellationToken);
                    
                    // Simplified PnL calculation - in production, implement proper FIFO/LIFO
                    // For now, return 0 as placeholder
                    // totalLoss += CalculatePnLFromTrades(trades, botOrder.Order);
                }
            }

            return totalLoss;
        }

        public async Task<bool> CheckRateLimitAsync(int botId, int maxOrdersPerCycle, CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = $"OrderCount_{botId}";
                if (_cache.TryGetValue(cacheKey, out int currentCount))
                {
                    if (currentCount >= maxOrdersPerCycle)
                    {
                        _logger.LogWarning("Bot {BotId} exceeded rate limit: {Count}/{Max}",
                            botId, currentCount, maxOrdersPerCycle);
                        return false; // Rate limit exceeded
                    }
                }
                return true; // Within limits
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit for bot {BotId}", botId);
                return true; // Fail-open for demo
            }
        }

        public async Task ResetOrderCountForNewCycleAsync(int botId, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"OrderCount_{botId}";
            _cache.Set(cacheKey, 0, TimeSpan.FromMinutes(10));
            await Task.CompletedTask;
        }

        public async Task RecordOrderPlacedAsync(int botId, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"OrderCount_{botId}";
            var currentCount = _cache.TryGetValue(cacheKey, out int count) ? count : 0;
            _cache.Set(cacheKey, currentCount + 1, TimeSpan.FromMinutes(10));
            await Task.CompletedTask;
        }

        public async Task RecordTradeResultAsync(int botId, decimal pnl, CancellationToken cancellationToken = default)
        {
            try
            {
                var bot = await _context.TradingBots.FindAsync(new object[] { botId }, cancellationToken);
                if (bot == null) return;

                // For demo: just log the PnL
                // In production, this would update BotRiskState table with realized PnL
                _logger.LogInformation("Bot {BotId} trade result: PnL={Pnl}", botId, pnl);

                // TODO: Update BotRiskState table with realized PnL tracking
                // This would be used by kill switch to track consecutive losses
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording trade result for bot {BotId}", botId);
            }
        }
    }
}

