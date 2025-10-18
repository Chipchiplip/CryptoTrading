using CryptoTrading.Interfaces;

namespace CryptoTrading.Services
{
    public class EmailService : IEmailSender
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            await SendEmailAsync(to, subject, body, false);
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml)
        {
            // TODO: Implement actual email sending (SMTP, SendGrid, etc.)
            // For now, just log the email
            _logger.LogInformation("Email would be sent to {To} with subject: {Subject}", to, subject);
            _logger.LogDebug("Email body: {Body}", body);
            
            // Simulate async operation
            await Task.Delay(100);
        }

        public async Task SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string body)
        {
            foreach (var recipient in recipients)
            {
                await SendEmailAsync(recipient, subject, body);
            }
        }
    }
}
