using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models.DTOs
{
    /// <summary>
    /// Portfolio holding information
    /// </summary>
    public class PortfolioHoldingDto
    {
        /// <summary>
        /// Cryptocurrency symbol
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Cryptocurrency name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Amount held
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Average purchase price (cost basis)
        /// </summary>
        public decimal AvgPrice { get; set; }

        /// <summary>
        /// Current market price
        /// </summary>
        public decimal CurrentPrice { get; set; }

        /// <summary>
        /// Current value in USD
        /// </summary>
        public decimal Value { get; set; }

        /// <summary>
        /// Unrealized profit/loss
        /// </summary>
        public decimal Pnl { get; set; }

        /// <summary>
        /// Unrealized profit/loss percentage
        /// </summary>
        public decimal PnlPercent { get; set; }

        /// <summary>
        /// Portfolio allocation percentage
        /// </summary>
        public decimal Allocation { get; set; }

        /// <summary>
        /// Total cost basis
        /// </summary>
        public decimal Cost { get; set; }

        /// <summary>
        /// Cryptocurrency image/logo URL
        /// </summary>
        public string? ImageUrl { get; set; }
    }

    /// <summary>
    /// NAV (Net Asset Value) data point for performance chart
    /// </summary>
    public class NavDataPoint
    {
        /// <summary>
        /// Date of the NAV value
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Portfolio value at this date
        /// </summary>
        public decimal Value { get; set; }
    }

    /// <summary>
    /// Complete portfolio overview
    /// </summary>
    public class PortfolioOverviewDto
    {
        /// <summary>
        /// Total portfolio value in USD
        /// </summary>
        public decimal TotalValue { get; set; }

        /// <summary>
        /// Total cost basis (initial investment)
        /// </summary>
        public decimal TotalCost { get; set; }

        /// <summary>
        /// Unrealized profit/loss (from current holdings)
        /// </summary>
        public decimal UnrealizedPnL { get; set; }

        /// <summary>
        /// Unrealized profit/loss percentage
        /// </summary>
        public decimal UnrealizedPnLPercent { get; set; }

        /// <summary>
        /// Realized profit/loss (from closed trades)
        /// </summary>
        public decimal RealizedPnL { get; set; }

        /// <summary>
        /// List of holdings
        /// </summary>
        public List<PortfolioHoldingDto> Holdings { get; set; } = new();

        /// <summary>
        /// NAV history for performance chart (last 30 days)
        /// </summary>
        public List<NavDataPoint> NavHistory { get; set; } = new();
    }

    /// <summary>
    /// Portfolio performance metrics
    /// </summary>
    public class PortfolioPerformanceDto
    {
        /// <summary>
        /// Start date
        /// </summary>
        public DateTime From { get; set; }

        /// <summary>
        /// End date
        /// </summary>
        public DateTime To { get; set; }

        /// <summary>
        /// NAV history data points
        /// </summary>
        public List<NavDataPoint> NavHistory { get; set; } = new();

        /// <summary>
        /// Total return percentage
        /// </summary>
        public decimal TotalReturnPercent { get; set; }

        /// <summary>
        /// Best day return
        /// </summary>
        public decimal BestDayReturn { get; set; }

        /// <summary>
        /// Worst day return
        /// </summary>
        public decimal WorstDayReturn { get; set; }
    }
}

