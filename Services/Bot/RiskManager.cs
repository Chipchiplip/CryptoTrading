using CryptoTrading.Data;
using CryptoTrading.Interfaces.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services.Bot
{
    /// <summary>
    /// Risk manager for bot trading limits
    /// </summary>
    public class RiskManager : IRiskManager
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RiskManager> _logger;

        // Default limits
        private const decimal DEFAULT_MAX_EXPOSURE_PER_USER = 100000m; // $100k
        private const decimal DEFAULT_MAX_CAPITAL_PER_BOT = 50000m; // $50k
        private const int DEFAULT_MAX_BOTS_PER_USER = 10;

        public RiskManager(
            ApplicationDbContext context,
            ILogger<RiskManager> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> CheckLimitsAsync(
            int userId, 
            decimal requiredCapital, 
            CancellationToken cancellationToken = default)
        {
            // Check per-bot capital limit
            if (requiredCapital > DEFAULT_MAX_CAPITAL_PER_BOT)
            {
                _logger.LogWarning("Capital {Capital} exceeds per-bot limit {Limit}", 
                    requiredCapital, DEFAULT_MAX_CAPITAL_PER_BOT);
                return false;
            }

            // Check total exposure across all bots
            var totalExposure = await GetCurrentExposureAsync(userId, cancellationToken);
            if (totalExposure + requiredCapital > DEFAULT_MAX_EXPOSURE_PER_USER)
            {
                _logger.LogWarning("Total exposure would exceed limit for user {UserId}", userId);
                return false;
            }

            // Check number of active bots
            var activeBots = await _context.TradingBots
                .CountAsync(b => b.UserId == userId && 
                                (b.Status == "Running" || b.Status == "Starting"),
                           cancellationToken);

            if (activeBots >= DEFAULT_MAX_BOTS_PER_USER)
            {
                _logger.LogWarning("User {UserId} has reached max bots limit", userId);
                return false;
            }

            return true;
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
}

