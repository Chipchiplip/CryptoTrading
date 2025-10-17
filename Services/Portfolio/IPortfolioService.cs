using CryptoTrading.Controllers;

namespace CryptoTrading.Services.Portfolio;

public interface IPortfolioService
{
    Task<IEnumerable<WatchlistDto>> GetWatchlistsAsync();
    Task<WatchlistDto> CreateWatchlistAsync(CreateWatchlistDto dto);
    Task<WatchlistDto?> GetWatchlistAsync(Guid id);
    Task<bool> DeleteWatchlistAsync(Guid id);
    Task<bool> AddToWatchlistAsync(Guid watchlistId, int cryptocurrencyId);
    Task<bool> RemoveFromWatchlistAsync(Guid watchlistId, int cryptocurrencyId);
    Task<PortfolioOverviewDto> GetPortfolioOverviewAsync();
    Task<IEnumerable<PositionDto>> GetPositionsAsync();
}

// DTOs
public record PositionDto(Guid Id, string Symbol, string Name, decimal Quantity, decimal AverageCost, decimal? CurrentPrice, decimal? UnrealizedPnL, decimal? PercentChange);
