using CryptoTrading.Models;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Interfaces.Bot
{
    /// <summary>
    /// Application service for bot management
    /// </summary>
    public interface IBotApplicationService
    {
        // ========== BOT CRUD ==========
        Task<TradingBotDetailDto> CreateAsync(int userId, CreateBotRequest request);
        Task<TradingBotDetailDto> UpdateAsync(int userId, Guid botId, UpdateBotRequest request);
        Task DeleteAsync(int userId, Guid botId);
        Task<TradingBotDetailDto> GetAsync(int userId, Guid botId);
        Task<PaginatedResponse<TradingBotSummaryDto>> GetListAsync(int userId, BotListQuery query);

        // ========== BOT CONTROL ==========
        Task<string> StartAsync(int userId, Guid botId, StartBotRequest request);
        Task StopAsync(int userId, Guid botId, StopBotRequest request);
        Task NudgeAsync(int userId, Guid botId);

        // ========== BOT LOGS & ORDERS ==========
        Task<PaginatedResponse<BotLogDto>> GetLogsAsync(int userId, Guid botId, BotLogsQuery query);
        Task<PaginatedResponse<BotOrderDto>> GetOrdersAsync(int userId, Guid botId, int page = 1, int pageSize = 20);

        // ========== SIMULATION ==========
        Task<SimulationResultDto> SimulateAsync(int userId, Guid botId, SimulationRequest request);

        // ========== INVENTORY MANAGEMENT ==========
        Task<ResetInventoryResultDto> ResetInventoryAsync(int userId, Guid botId);
        Task<ResetInventoryResultDto> ResetInventoryAdminAsync(Guid botId); // Admin version - no userId check
        
        // ========== ADMIN/DEBUG ==========
        Task<List<BotSearchResultDto>> SearchBotsByNameAsync(string namePattern);
        Task<List<BotSearchResultDto>> GetBotsByUserIdAsync(int userId); // Get all bots for a specific user
    }

    /// <summary>
    /// Query parameters for bot list
    /// </summary>
    public class BotListQuery
    {
        public string? Status { get; set; }
        public string? StrategyKey { get; set; }
        public string? BaseAsset { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

