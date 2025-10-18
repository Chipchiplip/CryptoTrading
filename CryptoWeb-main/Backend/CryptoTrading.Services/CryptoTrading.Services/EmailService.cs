using System.Net;
using System.Net.Mail;
using CryptoTrading.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services
{
    public interface IEmailService
    {
        Task SendEmailConfirmationAsync(string email, string token);
        Task SendPasswordResetEmailAsync(string email, string token);
        Task Send2FACodeEmailAsync(string email, string code);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _appUrl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["EmailSettings:SmtpUsername"] ?? "";
            _smtpPassword = _configuration["EmailSettings:SmtpPassword"] ?? "";
            _fromEmail = _configuration["EmailSettings:FromEmail"] ?? "";
            _fromName = _configuration["EmailSettings:FromName"] ?? "CryptoTrading";
            _appUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:3000";
        }

        public async Task SendEmailConfirmationAsync(string email, string token)
        {
            var confirmUrl = $"{_appUrl}/confirm-email?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";

            var subject = "Confirm Your Email - CryptoTrading";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #667eea;'>Welcome to CryptoTrading!</h2>
                        <p>Thank you for registering. Please confirm your email address by clicking the button below:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{confirmUrl}' 
                               style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
                                      color: white; 
                                      padding: 12px 30px; 
                                      text-decoration: none; 
                                      border-radius: 5px;
                                      display: inline-block;'>
                                Confirm Email
                            </a>
                        </div>
                        <p>Or copy and paste this link into your browser:</p>
                        <p style='word-break: break-all; color: #666;'>{confirmUrl}</p>
                        <p style='color: #999; font-size: 12px; margin-top: 30px;'>
                            This link will expire in 24 hours. If you didn't create an account, please ignore this email.
                        </p>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string email, string token)
        {
            var resetUrl = $"{_appUrl}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";

            var subject = "Reset Your Password - CryptoTrading";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #667eea;'>Password Reset Request</h2>
                        <p>You requested to reset your password. Click the button below to create a new password:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{resetUrl}' 
                               style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
                                      color: white; 
                                      padding: 12px 30px; 
                                      text-decoration: none; 
                                      border-radius: 5px;
                                      display: inline-block;'>
                                Reset Password
                            </a>
                        </div>
                        <p>Or copy and paste this link into your browser:</p>
                        <p style='word-break: break-all; color: #666;'>{resetUrl}</p>
                        <p style='color: #999; font-size: 12px; margin-top: 30px;'>
                            This link will expire in 1 hour. If you didn't request a password reset, please ignore this email.
                        </p>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task Send2FACodeEmailAsync(string email, string code)
        {
            var subject = "Your 2FA Code - CryptoTrading";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #667eea;'>Two-Factor Authentication</h2>
                        <p>Your verification code is:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <div style='background: #f5f7fa; 
                                        padding: 20px; 
                                        border-radius: 10px;
                                        font-size: 32px;
                                        font-weight: bold;
                                        letter-spacing: 5px;
                                        color: #667eea;'>
                                {code}
                            </div>
                        </div>
                        <p style='color: #999; font-size: 12px; margin-top: 30px;'>
                            This code will expire in 5 minutes. If you didn't request this code, please secure your account immediately.
                        </p>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(email, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword)
                };

                var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(toEmail);

                await client.SendMailAsync(message);
                _logger.LogInformation($"Email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}");
                throw;
            }
        }
    }
}