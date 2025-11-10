using CryptoTrading.Data;
using CryptoTrading.Interfaces.Bot;
using CryptoTrading.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CryptoTrading.Services.Bot
{
    /// <summary>
    /// Logger for bot execution events
    /// </summary>
    public class BotLogger : IBotLogger
    {
        private readonly ApplicationDbContext _context;
        private readonly Guid _botId;
        private readonly ILogger<BotLogger> _logger;
        private readonly List<TradingBotLog> _pendingLogs = new();

        public BotLogger(
            ApplicationDbContext context,
            Guid botId,
            ILogger<BotLogger> logger)
        {
            _context = context;
            _botId = botId;
            _logger = logger;
        }

        public void LogInfo(string category, string message, object? payload = null)
        {
            Log("Info", category, message, payload);
        }

        public void LogWarning(string category, string message, object? payload = null)
        {
            Log("Warn", category, message, payload);
        }

        public void LogError(string category, string message, object? payload = null)
        {
            Log("Error", category, message, payload);
        }

        private void Log(string level, string category, string message, object? payload)
        {
            var log = new TradingBotLog
            {
                TradingBotId = _botId,
                Level = level,
                Category = category,
                Message = message,
                Payload = payload != null ? JsonSerializer.Serialize(payload) : null,
                CreatedAt = DateTime.UtcNow
            };

            _pendingLogs.Add(log);

            // Also log to system logger
            var logLevel = level switch
            {
                "Error" => LogLevel.Error,
                "Warn" => LogLevel.Warning,
                _ => LogLevel.Information
            };

            _logger.Log(logLevel, "[Bot {BotId}] [{Category}] {Message}", _botId, category, message);
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_pendingLogs.Count == 0)
                return;

            try
            {
                await _context.TradingBotLogs.AddRangeAsync(_pendingLogs, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                _pendingLogs.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush bot logs for {BotId}", _botId);
            }
        }
    }

    /// <summary>
    /// Event collector for bot events
    /// </summary>
    public class EventCollector : IEventCollector
    {
        private readonly List<BotEvent> _events = new();

        public void AddEvent(BotEvent evt)
        {
            _events.Add(evt);
        }

        public List<BotEvent> GetEvents()
        {
            return new List<BotEvent>(_events);
        }

        public void Clear()
        {
            _events.Clear();
        }
    }
}

