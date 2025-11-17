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
        Task<TradingBotDetailDto> UpdateAsync(int userId, int botId, UpdateBotRequest request);
        Task DeleteAsync(int userId, int botId);
        Task<TradingBotDetailDto> GetAsync(int userId, int botId);
        Task<PaginatedResponse<TradingBotSummaryDto>> GetListAsync(int userId, BotListQuery query);

        // ========== BOT CONTROL ==========
        Task<string> StartAsync(int userId, int botId, StartBotRequest request);
        Task StopAsync(int userId, int botId, StopBotRequest request);
        Task NudgeAsync(int userId, int botId);

        // ========== BOT LOGS & ORDERS ==========
        Task<PaginatedResponse<BotLogDto>> GetLogsAsync(int userId, int botId, BotLogsQuery query);
        Task<PaginatedResponse<BotOrderDto>> GetOrdersAsync(int userId, int botId, int page = 1, int pageSize = 20);

        // ========== SIMULATION ==========
        Task<SimulationResultDto> SimulateAsync(int userId, int botId, SimulationRequest request);
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

