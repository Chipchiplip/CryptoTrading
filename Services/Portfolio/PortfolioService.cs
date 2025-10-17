using CryptoTrading.Controllers;
using CryptoTrading.Interfaces;

namespace CryptoTrading.Services.Portfolio;

public class PortfolioService : IPortfolioService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public PortfolioService(IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<WatchlistDto>> GetWatchlistsAsync()
    {
        // TODO: Implement get watchlists logic
        throw new NotImplementedException("Get watchlists functionality will be implemented by team member");
    }

    public async Task<WatchlistDto> CreateWatchlistAsync(CreateWatchlistDto dto)
    {
        // TODO: Implement create watchlist logic
        throw new NotImplementedException("Create watchlist functionality will be implemented by team member");
    }

    public async Task<WatchlistDto?> GetWatchlistAsync(Guid id)
    {
        // TODO: Implement get watchlist by id logic
        throw new NotImplementedException("Get watchlist functionality will be implemented by team member");
    }

    public async Task<bool> DeleteWatchlistAsync(Guid id)
    {
        // TODO: Implement delete watchlist logic
        throw new NotImplementedException("Delete watchlist functionality will be implemented by team member");
    }

    public async Task<bool> AddToWatchlistAsync(Guid watchlistId, int cryptocurrencyId)
    {
        // TODO: Implement add to watchlist logic
        throw new NotImplementedException("Add to watchlist functionality will be implemented by team member");
    }

    public async Task<bool> RemoveFromWatchlistAsync(Guid watchlistId, int cryptocurrencyId)
    {
        // TODO: Implement remove from watchlist logic
        throw new NotImplementedException("Remove from watchlist functionality will be implemented by team member");
    }

    public async Task<PortfolioOverviewDto> GetPortfolioOverviewAsync()
    {
        // TODO: Implement get portfolio overview logic
        throw new NotImplementedException("Get portfolio overview functionality will be implemented by team member");
    }

    public async Task<IEnumerable<PositionDto>> GetPositionsAsync()
    {
        // TODO: Implement get positions logic
        throw new NotImplementedException("Get positions functionality will be implemented by team member");
    }
}
