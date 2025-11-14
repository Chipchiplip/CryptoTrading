using CryptoTrading.Data;
using CryptoTrading.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoTrading.Services.Configuration
{
    public interface ITradingConfigurationService
    {
        Task<decimal> GetFeeRateAsync();
        Task<decimal> GetMarketPriceBufferAsync();
        Task<string> GetEnvironmentAsync();
        Task<T> GetConfigValueAsync<T>(string key, T defaultValue);
        Task SetConfigValueAsync(string key, string value, string? description = null);
    }

    public class TradingConfigurationService : ITradingConfigurationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _appConfig;
        private readonly ILogger<TradingConfigurationService> _logger;
        private readonly string _environment;
        private const string CacheKeyPrefix = "TradingConfig_";
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        public TradingConfigurationService(
            ApplicationDbContext context,
            IMemoryCache cache,
            IConfiguration appConfig,
            IWebHostEnvironment env,
            ILogger<TradingConfigurationService> logger)
        {
            _context = context;
            _cache = cache;
            _appConfig = appConfig;
            _logger = logger;
            _environment = env.EnvironmentName;
        }

        public async Task<decimal> GetFeeRateAsync()
        {
            return await GetConfigValueAsync("FeeRate", 0.001m);
        }

        public async Task<decimal> GetMarketPriceBufferAsync()
        {
            return await GetConfigValueAsync("MarketPriceBuffer", 0.05m);
        }

        public Task<string> GetEnvironmentAsync()
        {
            return Task.FromResult(_environment);
        }

        public async Task<T> GetConfigValueAsync<T>(string key, T defaultValue)
        {
            var cacheKey = $"{CacheKeyPrefix}{_environment}_{key}";

            // Try cache first
            if (_cache.TryGetValue(cacheKey, out T? cachedValue) && cachedValue != null)
            {
                return cachedValue;
            }

            try
            {
                // Try database
                var config = await _context.TradingConfigurations
                    .Where(c => c.ConfigKey == key && c.Environment == _environment)
                    .FirstOrDefaultAsync();

                if (config != null)
                {
                    var value = ConvertValue<T>(config.ConfigValue);
                    _cache.Set(cacheKey, value, _cacheDuration);
                    return value;
                }

                // Fallback to appsettings
                var appSettingsValue = _appConfig[$"TradingSettings:{key}"];
                if (appSettingsValue != null)
                {
                    var value = ConvertValue<T>(appSettingsValue);
                    _cache.Set(cacheKey, value, _cacheDuration);
                    return value;
                }

                // Return default
                _logger.LogWarning("Configuration key {Key} not found, using default: {Default}", key, defaultValue);
                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration value for key {Key}", key);
                return defaultValue;
            }
        }

        public async Task SetConfigValueAsync(string key, string value, string? description = null)
        {
            try
            {
                var config = await _context.TradingConfigurations
                    .Where(c => c.ConfigKey == key && c.Environment == _environment)
                    .FirstOrDefaultAsync();

                if (config != null)
                {
                    config.ConfigValue = value;
                    config.Description = description ?? config.Description;
                    config.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    config = new TradingConfiguration
                    {
                        ConfigKey = key,
                        ConfigValue = value,
                        Description = description,
                        Environment = _environment,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.TradingConfigurations.Add(config);
                }

                await _context.SaveChangesAsync();

                // Invalidate cache
                var cacheKey = $"{CacheKeyPrefix}{_environment}_{key}";
                _cache.Remove(cacheKey);

                _logger.LogInformation("Updated configuration: {Key} = {Value}", key, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting configuration value for key {Key}", key);
                throw;
            }
        }

        private T ConvertValue<T>(string value)
        {
            try
            {
                var targetType = typeof(T);
                
                if (targetType == typeof(string))
                    return (T)(object)value;
                
                if (targetType == typeof(decimal))
                    return (T)(object)decimal.Parse(value);
                
                if (targetType == typeof(int))
                    return (T)(object)int.Parse(value);
                
                if (targetType == typeof(bool))
                    return (T)(object)bool.Parse(value);
                
                throw new NotSupportedException($"Type {targetType} not supported for configuration conversion");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting config value {Value} to type {Type}", value, typeof(T));
                throw;
            }
        }
    }
}

