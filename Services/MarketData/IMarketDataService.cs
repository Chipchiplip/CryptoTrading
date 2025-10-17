using CryptoTrading.Models;

namespace CryptoTrading.Services.MarketData;

public interface IMarketDataService
{
    Task<IEnumerable<CryptocurrencyDto>> GetCryptocurrenciesAsync();
    Task<CryptocurrencyDto?> GetCryptocurrencyAsync(string symbol);
    Task<IEnumerable<PriceDto>> GetPricesAsync(string[] symbols);
    Task<MarketStatsDto> GetMarketStatsAsync();
    Task UpdatePricesFromApiAsync();
    Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAsync(string symbol, int days = 30);
}

// DTOs
public record CryptocurrencyDto(int Id, string Symbol, string Name, string? IconUrl, int? MarketCapRank, bool IsActive);
public record PriceDto(string Symbol, decimal Price, decimal? MarketCap, decimal? Volume24h, decimal? PercentChange24h, DateTime Timestamp);
public record MarketStatsDto(decimal TotalMarketCap, decimal Total24hVolume, decimal BtcDominance, int ActiveCryptocurrencies);
public record PriceHistoryDto(DateTime Timestamp, decimal Price, decimal? Volume);
