using CryptoTrading.Core.Interfaces;
using CryptoTrading.Core.Models;
using CryptoTrading.Core.Models.DTOs;
using CryptoTrading.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CryptoTrading.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly IEmailService _emailService;
        private readonly ILoggingService _loggingService;

        public AuthService(
            AppDbContext context,
            IOptions<JwtSettings> jwtSettings,
            IEmailService emailService,
            ILoggingService loggingService)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _emailService = emailService;
            _loggingService = loggingService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
                {
                    _loggingService.LogAuthenticationAttempt(registerDto.Email, false);
                    throw new Exception("Email already registered");
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

                var emailConfirmToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

                var user = new User
                {
                    Email = registerDto.Email,
                    PasswordHash = passwordHash,
                    FullName = registerDto.FullName,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = false,
                    EmailConfirmationToken = emailConfirmToken,
                    EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(24)
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await _emailService.SendEmailConfirmationAsync(user.Email, emailConfirmToken);

                _loggingService.LogAuthenticationAttempt(registerDto.Email, true);

                return new AuthResponseDto
                {
                    AccessToken = string.Empty,
                    RefreshToken = string.Empty,
                    ExpiresAt = DateTime.UtcNow,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FullName = user.FullName,
                        TwoFactorEnabled = false
                    }
                };
            }
            catch (Exception ex)
            {
                _loggingService.LogAuthenticationAttempt(registerDto.Email, false);
                throw;
            }
        }

        public async Task<bool> ConfirmEmailAsync(string email, string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(
                u => u.Email == email &&
                u.EmailConfirmationToken == token);

            if (user == null)
            {
                throw new Exception("Invalid confirmation token");
            }

            if (user.EmailConfirmationTokenExpiry < DateTime.UtcNow)
            {
                throw new Exception("Confirmation token has expired");
            }

            user.EmailConfirmed = true;
            user.EmailConfirmationToken = null;
            user.EmailConfirmationTokenExpiry = null;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

                if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    _loggingService.LogAuthenticationAttempt(loginDto.Email, false);
                    throw new Exception("Invalid email or password");
                }

                if (!user.EmailConfirmed)
                {
                    _loggingService.LogAuthenticationAttempt(loginDto.Email, false);
                    throw new Exception("Please confirm your email before logging in");
                }

                if (user.TwoFactorEnabled)
                {
                    _loggingService.LogAuthenticationAttempt(loginDto.Email, false);
                    return new AuthResponseDto
                    {
                        AccessToken = string.Empty,
                        RefreshToken = string.Empty,
                        ExpiresAt = DateTime.UtcNow,
                        User = new UserDto
                        {
                            Id = user.Id,
                            Email = user.Email,
                            FullName = user.FullName,
                            TwoFactorEnabled = true
                        }
                    };
                }

                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _loggingService.LogAuthenticationAttempt(loginDto.Email, true);

                return await GenerateAuthResponse(user);
            }
            catch (Exception ex)
            {
                _loggingService.LogAuthenticationAttempt(loginDto.Email, false);
                throw;
            }
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new Exception("Invalid or expired refresh token");
            }

            return await GenerateAuthResponse(user);
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null) return false;

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<string> GeneratePasswordResetTokenAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                throw new Exception("User not found");

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync();

            await _emailService.SendPasswordResetEmailAsync(email, token);

            return token;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto resetDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(
                u => u.Email == resetDto.Email &&
                u.PasswordResetToken == resetDto.Token);

            if (user == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
                throw new Exception("Invalid or expired reset token");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(resetDto.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _context.SaveChangesAsync();

            _loggingService.LogPasswordChange(user.Id);

            return true;
        }

        public async Task<AuthResponseDto> LoginWith2FAAsync(string email, string code)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null || !user.TwoFactorEnabled)
                {
                    _loggingService.LogAuthenticationAttempt(email, false);
                    throw new Exception("Invalid request");
                }

                if (string.IsNullOrEmpty(user.TwoFactorSecret))
                    throw new Exception("2FA not properly configured");

                var key = OtpNet.Base32Encoding.ToBytes(user.TwoFactorSecret);
                var totp = new OtpNet.Totp(key);

                if (!totp.VerifyTotp(code, out _, new OtpNet.VerificationWindow(2, 2)))
                {
                    _loggingService.LogAuthenticationAttempt(email, false);
                    throw new Exception("Invalid 2FA code");
                }

                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _loggingService.LogAuthenticationAttempt(email, true);

                return await GenerateAuthResponse(user);
            }
            catch (Exception ex)
            {
                _loggingService.LogAuthenticationAttempt(email, false);
                throw;
            }
        }

        private async Task<AuthResponseDto> GenerateAuthResponse(User user)
        {
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    TwoFactorEnabled = user.TwoFactorEnabled
                }
            };
        }

        private string GenerateAccessToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}