using CryptoTrading.Core.Interfaces;
using CryptoTrading.Core.Models.DTOs;
using CryptoTrading.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using System.Web;
namespace CryptoTrading.Services
{
    public class TwoFactorService : ITwoFactorService
    {
        private readonly AppDbContext _context;
        private const string Issuer = "CryptoTrading";

        public TwoFactorService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TwoFactorSetupDto> GenerateSetupAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User not found");
            if (user.TwoFactorEnabled)
                throw new Exception("2FA is already enabled");
            // Generate secret key
            var key = KeyGeneration.GenerateRandomKey(20);
            var base32String = Base32Encoding.ToString(key);

            // Store secret temporarily (will be saved when user verifies)
            user.TwoFactorSecret = base32String;
            await _context.SaveChangesAsync();

            // Generate QR code URI
            var qrCodeUri = GenerateQrCodeUri(user.Email, base32String);
            return new TwoFactorSetupDto
            {
                Secret = base32String,
                QrCodeUri = qrCodeUri,
                ManualEntryKey = FormatSecretForDisplay(base32String)
            };
        }

        public async Task<bool> EnableTwoFactorAsync(int userId, string code)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User not found");
            if (string.IsNullOrEmpty(user.TwoFactorSecret))
                throw new Exception("2FA setup not initiated");
            // Verify the code
            if (!VerifyCode(user.TwoFactorSecret, code))
                throw new Exception("Invalid verification code");
            user.TwoFactorEnabled = true;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DisableTwoFactorAsync(int userId, string password)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User not found");
            if (!user.TwoFactorEnabled)
                throw new Exception("2FA is not enabled");
            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new Exception("Invalid password");
            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> VerifyTwoFactorCodeAsync(int userId, string code)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
                return false;
            return VerifyCode(user.TwoFactorSecret, code);
        }

        private bool VerifyCode(string secret, string code)
        {
            var key = Base32Encoding.ToBytes(secret);
            var totp = new Totp(key);

            // Verify with time window (allows for slight clock skew)
            return totp.VerifyTotp(code, out _, new VerificationWindow(2, 2));
        }

        private string GenerateQrCodeUri(string email, string secret)
        {
            var encodedIssuer = HttpUtility.UrlEncode(Issuer);
            var encodedEmail = HttpUtility.UrlEncode(email);

            return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secret}&issuer={encodedIssuer}";
        }

        private string FormatSecretForDisplay(string secret)
        {
            // Format as XXXX XXXX XXXX XXXX for easier manual entry
            return string.Join(" ", Enumerable.Range(0, secret.Length / 4)
                .Select(i => secret.Substring(i * 4, 4)));
        }
    }
}