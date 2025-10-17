namespace CryptoTrading.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendEmailVerificationAsync(string to, string verificationLink);
    Task SendPasswordResetAsync(string to, string resetLink);
    Task SendWelcomeEmailAsync(string to, string firstName);
    Task SendSubscriptionConfirmationAsync(string to, string planName);
}
