using CryptoTrading.Data;
using CryptoTrading.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CryptoTrading.Services
{
    public interface IAuditService
    {
        Task LogEventAsync(string eventType, int userId, string entityType, ulong? entityId, 
            object? beforeState, object? afterState, int? botId = null, string? metadata = null);
        Task<List<AuditEvent>> GetUserAuditTrailAsync(int userId, DateTime from, DateTime to);
        Task<List<AuditEvent>> GetEntityAuditTrailAsync(string entityType, ulong entityId);
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogEventAsync(
            string eventType, 
            int userId, 
            string entityType, 
            ulong? entityId,
            object? beforeState, 
            object? afterState, 
            int? botId = null,
            string? metadata = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var correlationId = httpContext?.Items["CorrelationId"]?.ToString() 
                    ?? Guid.NewGuid().ToString();

                var auditEvent = new AuditEvent
                {
                    EventType = eventType,
                    UserId = userId,
                    BotId = botId,
                    CorrelationId = correlationId,
                    EntityType = entityType,
                    EntityId = entityId,
                    BeforeState = beforeState != null ? JsonSerializer.Serialize(beforeState) : null,
                    AfterState = afterState != null ? JsonSerializer.Serialize(afterState) : null,
                    Metadata = metadata,
                    IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.AuditEvents.Add(auditEvent);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Audit event logged: {EventType} for user {UserId}, entity {EntityType}:{EntityId}", 
                    eventType, userId, entityType, entityId);
            }
            catch (Exception ex)
            {
                // Don't let audit failures break the main flow
                _logger.LogError(ex, "Failed to log audit event: {EventType}", eventType);
            }
        }

        public async Task<List<AuditEvent>> GetUserAuditTrailAsync(int userId, DateTime from, DateTime to)
        {
            return await _context.AuditEvents
                .Where(a => a.UserId == userId && a.CreatedAt >= from && a.CreatedAt <= to)
                .OrderByDescending(a => a.CreatedAt)
                .Take(1000) // Limit to prevent excessive load
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<AuditEvent>> GetEntityAuditTrailAsync(string entityType, ulong entityId)
        {
            return await _context.AuditEvents
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}

