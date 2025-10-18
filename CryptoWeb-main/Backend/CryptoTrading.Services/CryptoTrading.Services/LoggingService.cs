using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services
{
    public interface ILoggingService
    {
        void LogAuthenticationAttempt(string email, bool success);
        void LogPasswordChange(int userId);
        void Log2FASetup(int userId, bool enabled);
    }

    public class LoggingService : ILoggingService
    {
        private readonly ILogger<LoggingService> _logger;
        public LoggingService(ILogger<LoggingService> logger)
        {
            _logger = logger;
        }

        public void LogAuthenticationAttempt(string email, bool success)
        {
            if (success)
                _logger.LogInformation("Successful login for {Email}", email);
            else
                _logger.LogWarning("Failed login attempt for {Email}", email);
        }

        public void LogPasswordChange(int userId)
        {
            _logger.LogInformation("Password changed for user {UserId}", userId);
        }

        public void Log2FASetup(int userId, bool enabled)
        {
            _logger.LogInformation("2FA {Status} for user {UserId}",
                enabled ? "enabled" : "disabled", userId);
        }
    }
}