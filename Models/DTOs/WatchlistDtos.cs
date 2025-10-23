using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models.DTOs
{
    // Use Case 36: Create new watchlist
    public record CreateWatchlistDto(
        [Required] string Name,
        bool IsDefault = false
    );

    // Use Case 41: Rename watchlist
    public record RenameWatchlistDto(
        [Required] string Name
    );

    // Use Case 37, 38: Add coin to watchlist
    public record AddCoinToWatchlistDto(
        [Required] string CoinSymbol,
        Guid? WatchlistId = null // null = add to default watchlist
    );

    // Use Case 39: Remove coin from watchlist
    public record RemoveCoinFromWatchlistDto(
        [Required] string CoinSymbol,
        [Required] Guid WatchlistId
    );

    // Response DTOs
    public record WatchlistDto(
        Guid Id,
        string Name,
        bool IsDefault,
        int CoinCount,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        List<WatchlistCoinDto> Coins
    );

    public record WatchlistCoinDto(
        string Symbol,
        string Name,
        string IconUrl,
        decimal CurrentPrice,
        decimal PriceChange24h,
        decimal PriceChangePercent24h,
        DateTime AddedAt
    );

    public record WatchlistSummaryDto(
        Guid Id,
        string Name,
        bool IsDefault,
        int CoinCount,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

    // Use Case 44: Multiple watchlists quota
    public record WatchlistQuotaDto(
        int CurrentCount,
        int MaxAllowed,
        string SubscriptionTier,
        bool CanCreateMore
    );

    // Use Case 43: Realtime update response
    public record WatchlistRealtimeUpdateDto(
        Guid WatchlistId,
        List<CoinPriceUpdateDto> Updates
    );

    public record CoinPriceUpdateDto(
        string Symbol,
        decimal NewPrice,
        decimal PriceChange,
        decimal PriceChangePercent,
        DateTime UpdatedAt
    );
}
