using CryptoTrading.Models;

namespace CryptoTrading.Interfaces.Bot
{
    /// <summary>
    /// Registry for managing trading strategies (built-in and plugins)
    /// </summary>
    public interface IStrategyRegistry
    {
        /// <summary>
        /// Registers a built-in strategy
        /// </summary>
        void RegisterStrategy(ITradingStrategy strategy);

        /// <summary>
        /// Gets a strategy instance by key
        /// </summary>
        ITradingStrategy? GetStrategy(string strategyKey);

        /// <summary>
        /// Gets all registered strategies
        /// </summary>
        IEnumerable<StrategyDescriptor> GetAllStrategies();

        /// <summary>
        /// Gets active strategies
        /// </summary>
        IEnumerable<StrategyDescriptor> GetActiveStrategies();

        /// <summary>
        /// Checks if a strategy exists
        /// </summary>
        bool HasStrategy(string strategyKey);

        /// <summary>
        /// Loads plugin strategies from database
        /// </summary>
        Task LoadPluginStrategiesAsync();
    }

    /// <summary>
    /// Descriptor for a strategy
    /// </summary>
    public class StrategyDescriptor
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsBuiltIn { get; set; }
        public StrategyMetadata Metadata { get; set; } = new();
    }
}

