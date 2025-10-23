using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Interfaces
{
    public interface IWatchlistService
    {
        // Use Case 36: Create new watchlist
        Task<WatchlistDto> CreateWatchlistAsync(int userId, CreateWatchlistDto dto);

        // Use Case 40: View all watchlists
        Task<List<WatchlistSummaryDto>> GetAllWatchlistsAsync(int userId);

        // Use Case 40: Get specific watchlist with coins
        Task<WatchlistDto> GetWatchlistAsync(int userId, Guid watchlistId);

        // Use Case 41: Rename watchlist
        Task<WatchlistDto> RenameWatchlistAsync(int userId, Guid watchlistId, RenameWatchlistDto dto);

        // Use Case 42: Delete watchlist
        Task<bool> DeleteWatchlistAsync(int userId, Guid watchlistId);

        // Use Case 37: Add coin to default watchlist
        Task<bool> AddCoinToDefaultWatchlistAsync(int userId, string coinSymbol);

        // Use Case 38: Add coin to specific watchlist
        Task<bool> AddCoinToWatchlistAsync(int userId, Guid watchlistId, string coinSymbol);

        // Use Case 39: Remove coin from watchlist
        Task<bool> RemoveCoinFromWatchlistAsync(int userId, Guid watchlistId, string coinSymbol);

        // Use Case 43: Get realtime updates for watchlist
        Task<WatchlistRealtimeUpdateDto> GetWatchlistRealtimeUpdatesAsync(Guid watchlistId);

        // Use Case 44: Check watchlist quota
        Task<WatchlistQuotaDto> GetWatchlistQuotaAsync(int userId);

        // Use Case 45: Get default watchlist
        Task<WatchlistDto> GetDefaultWatchlistAsync(int userId);

        // Helper methods
        Task<bool> EnsureDefaultWatchlistExistsAsync(int userId);
        Task<bool> CanCreateMoreWatchlistsAsync(int userId);
    }
}
