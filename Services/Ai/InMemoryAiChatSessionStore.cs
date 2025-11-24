using System.Collections.Concurrent;

namespace CryptoTrading.Services.Ai;

public class InMemoryAiChatSessionStore : IAiChatSessionStore
{
    private readonly ConcurrentDictionary<string, AiChatSessionContext> _sessions = new();

    public Task<AiChatSessionContext> GetOrCreateAsync(int userId, string? sessionId, CancellationToken ct = default)
    {
        var sessionGuid = Guid.TryParse(sessionId, out var parsed) ? parsed : Guid.NewGuid();
        var key = BuildKey(userId, sessionGuid);
        var context = _sessions.GetOrAdd(
            key,
            _ => new AiChatSessionContext(sessionGuid, userId, null, null, new List<string>(), null, new List<string>(), false));
        return Task.FromResult(context);
    }

    public Task SaveAsync(AiChatSessionContext context, CancellationToken ct = default)
    {
        _sessions[BuildKey(context.UserId, context.SessionId)] = context;
        return Task.CompletedTask;
    }

    private static string BuildKey(int userId, Guid sessionId) => $"{userId}:{sessionId}";
}


