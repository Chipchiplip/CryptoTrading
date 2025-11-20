using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Interfaces
{
    /// <summary>
    /// Service for portfolio management and performance tracking
    /// </summary>
    public interface IPortfolioService
    {
        /// <summary>
        /// Get complete portfolio overview including holdings, PnL, and NAV history
        /// </summary>
        Task<PortfolioOverviewDto> GetPortfolioOverviewAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get portfolio performance metrics for a date range
        /// </summary>
        Task<PortfolioPerformanceDto> GetPerformanceAsync(int userId, DateTime from, DateTime to);

        /// <summary>
        /// Calculate realized PnL using FIFO method
        /// </summary>
        Task<decimal> CalculateRealizedPnLAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculate average cost basis for a symbol at a specific point in time
        /// </summary>
        Task<decimal> GetAverageCostBasisAsync(int userId, string symbol, DateTime asOfDate, CancellationToken cancellationToken = default);
    }
}

