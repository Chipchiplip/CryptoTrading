using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Services.Ai;

public interface IAiChatSessionStore
{
    Task<AiChatSessionContext> GetOrCreateAsync(int userId, string? sessionId, CancellationToken ct = default);
    Task SaveAsync(AiChatSessionContext context, CancellationToken ct = default);
}

public record AiChatSessionContext(
    Guid SessionId,
    int UserId,
    decimal? TotalEquity,
    string? RiskMode,
    List<string> PreferredSymbols,
    string? TimeHorizon,
    List<string> ConversationNotes,
    bool HasShownBotHint);


