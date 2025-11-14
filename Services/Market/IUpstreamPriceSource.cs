using System.Threading;
using System.Threading.Tasks;

namespace CryptoTradingApp.Services.Market;

/// <summary>
/// Interface for fetching spot prices from upstream data sources
/// </summary>
public interface IUpstreamPriceSource
{
    /// <summary>
    /// Gets the spot price for a symbol from the upstream source
    /// </summary>
    /// <param name="symbol">Trading symbol (e.g., "BTCUSDT", "BTC")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Spot price or null if unavailable</returns>
    Task<decimal?> GetSpotPriceAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Source name for logging and identification
    /// </summary>
    string SourceName { get; }
}
