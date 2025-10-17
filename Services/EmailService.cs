using CryptoTrading.Interfaces;

namespace CryptoTrading.Services;

public class EmailService : IEmailSender
{
    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        // TODO: Implement email sending logic using SMTP or email service provider
        throw new NotImplementedException("Email sending functionality will be implemented by team member");
    }

    public async Task SendEmailVerificationAsync(string to, string verificationLink)
    {
        var subject = "Verify Your Email Address";
        var body = $@"
            <h2>Welcome to CryptoTrading!</h2>
            <p>Please click the link below to verify your email address:</p>
            <a href='{verificationLink}'>Verify Email</a>
            <p>If you didn't create an account, please ignore this email.</p>
        ";
        
        await SendEmailAsync(to, subject, body, true);
    }

    public async Task SendPasswordResetAsync(string to, string resetLink)
    {
        var subject = "Reset Your Password";
        var body = $@"
            <h2>Password Reset Request</h2>
            <p>Click the link below to reset your password:</p>
            <a href='{resetLink}'>Reset Password</a>
            <p>This link will expire in 1 hour.</p>
            <p>If you didn't request a password reset, please ignore this email.</p>
        ";
        
        await SendEmailAsync(to, subject, body, true);
    }

    public async Task SendWelcomeEmailAsync(string to, string firstName)
    {
        var subject = "Welcome to CryptoTrading!";
        var body = $@"
            <h2>Welcome {firstName}!</h2>
            <p>Thank you for joining CryptoTrading. You're now ready to start your crypto trading journey!</p>
            <p>Here are some things you can do:</p>
            <ul>
                <li>Create your first watchlist</li>
                <li>Explore market data</li>
                <li>Start paper trading</li>
            </ul>
            <p>Happy trading!</p>
        ";
        
        await SendEmailAsync(to, subject, body, true);
    }

    public async Task SendSubscriptionConfirmationAsync(string to, string planName)
    {
        var subject = "Subscription Confirmed";
        var body = $@"
            <h2>Subscription Confirmed!</h2>
            <p>Your {planName} subscription has been activated successfully.</p>
            <p>You now have access to all premium features.</p>
            <p>Thank you for your subscription!</p>
        ";
        
        await SendEmailAsync(to, subject, body, true);
    }
}
