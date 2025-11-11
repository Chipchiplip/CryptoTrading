using CryptoTrading.Data;
using CryptoTrading.Interfaces.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services.Bot
{
    /// <summary>
    /// Registry for managing trading strategies (built-in and plugins)
    /// </summary>
    public class StrategyRegistry : IStrategyRegistry
    {
        private readonly Dictionary<string, ITradingStrategy> _strategies = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StrategyRegistry> _logger;

        public StrategyRegistry(
            IServiceProvider serviceProvider,
            ILogger<StrategyRegistry> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void RegisterStrategy(ITradingStrategy strategy)
        {
            if (_strategies.ContainsKey(strategy.Key))
            {
                _logger.LogWarning("Strategy {Key} already registered, overwriting", strategy.Key);
            }

            _strategies[strategy.Key] = strategy;
            _logger.LogInformation("Registered strategy: {Key} v{Version}", 
                strategy.Key, strategy.Metadata.Version);
        }

        public ITradingStrategy? GetStrategy(string strategyKey)
        {
            return _strategies.GetValueOrDefault(strategyKey);
        }

        public IEnumerable<StrategyDescriptor> GetAllStrategies()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var dbStrategies = context.BotStrategyDefinitions
                .Where(s => s.IsActive)
                .Select(s => new StrategyDescriptor
                {
                    Id = s.Id,
                    Key = s.StrategyKey,
                    Version = s.Version,
                    DisplayName = s.DisplayName,
                    Description = s.Description ?? "",
                    IsActive = s.IsActive,
                    IsBuiltIn = string.IsNullOrEmpty(s.AssemblyName)
                })
                .ToList();

            return dbStrategies;
        }

        public IEnumerable<StrategyDescriptor> GetActiveStrategies()
        {
            return GetAllStrategies().Where(s => s.IsActive);
        }

        public bool HasStrategy(string strategyKey)
        {
            return _strategies.ContainsKey(strategyKey);
        }

        public async Task LoadPluginStrategiesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var pluginStrategies = await context.BotStrategyDefinitions
                .Where(s => s.IsActive && !string.IsNullOrEmpty(s.AssemblyName))
                .ToListAsync();

            foreach (var plugin in pluginStrategies)
            {
                try
                {
                    // TODO: Implement plugin loading from assembly
                    // For now, we only support built-in strategies
                    _logger.LogWarning("Plugin loading not yet implemented for {Key}", plugin.StrategyKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load plugin strategy {Key}", plugin.StrategyKey);
                }
            }
        }
    }
}

