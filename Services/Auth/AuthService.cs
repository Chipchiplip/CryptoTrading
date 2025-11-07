using CryptoTrading.Interfaces;
using CryptoTrading.Models;
using CryptoTrading.Services;
using CryptoTrading.Models.DTOs;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CryptoTrading.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtSettings _jwtSettings;
        private readonly IEmailSender _emailSender;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<AuthService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRoleService _roleService;
        private readonly ILevelService _levelService;

        public AuthService(
            IUnitOfWork unitOfWork,
            IOptions<JwtSettings> jwtSettings,
            IEmailSender emailSender,
            IDateTimeProvider dateTimeProvider,
            ILogger<AuthService> logger,
            IHttpContextAccessor httpContextAccessor,
            IRoleService roleService,
            ILevelService levelService)
        {
            _unitOfWork = unitOfWork;
            _jwtSettings = jwtSettings.Value;
            _emailSender = emailSender;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _roleService = roleService;
            _levelService = levelService;
        }

        // ====== ĐĂNG KÝ (REGISTER) - MẶC ĐỊNH ROLE/LEVEL ======
        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                if (await _unitOfWork.Users.ExistsAsync(u => u.Email == registerDto.Email))
                {
                    _logger.LogWarning("Registration attempt with existing email: {Email}", registerDto.Email);
                    throw new Exception("Email already registered");
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);
                var emailConfirmToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

                var user = new User
                {
                    Email = registerDto.Email,
                    PasswordHash = passwordHash,
                    FullName = registerDto.FullName,
                    CreatedAt = _dateTimeProvider.UtcNow,
                    EmailConfirmed = false,
                    EmailConfirmationToken = emailConfirmToken,
                    EmailConfirmationTokenExpiry = _dateTimeProvider.UtcNow.AddHours(24),

                    // ========== MẶC ĐỊNH ==========
                    Role = "User",
                    Level = "Beginner",
                    IsActive = true
                    // ==============================
                };

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // Gửi mail xác nhận
                await SendEmailConfirmationAsync(user.Email, emailConfirmToken);

                _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);

                return new AuthResponseDto
                {
                    AccessToken = string.Empty,
                    RefreshToken = string.Empty,
                    ExpiresAt = _dateTimeProvider.UtcNow,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FullName = user.FullName,
                        TwoFactorEnabled = false,
                        Role = user.Role,
                        Level = user.Level
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for email: {Email}", registerDto.Email);
                throw;
            }
        }

        public async Task<bool> ConfirmEmailAsync(string email, string token)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(
                u => u.Email == email && u.EmailConfirmationToken == token);

            if (user == null)
                throw new Exception("Invalid confirmation token");

            if (user.EmailConfirmationTokenExpiry < _dateTimeProvider.UtcNow)
                throw new Exception("Confirmation token has expired");

            user.EmailConfirmed = true;
            user.EmailConfirmationToken = null;
            user.EmailConfirmationTokenExpiry = null;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Email confirmed for user: {Email}", email);
            return true;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

                if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    if (user != null)
                    {
                        await _unitOfWork.LoginActivities.AddAsync(new LoginActivity
                        {
                            UserId = user.Id,
                            Ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown",
                            UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString(),
                            Success = false,
                            CreatedAt = DateTime.UtcNow
                        });
                        await _unitOfWork.SaveChangesAsync();
                    }

                    _logger.LogWarning("Login failed for {Email}", loginDto.Email);
                    throw new Exception("Invalid credentials");
                }

                if (!user.IsActive)
                    throw new Exception("Account is locked. Contact admin.");

                if (!user.EmailConfirmed)
                    throw new Exception("Please confirm your email before logging in.");

                user.LastLoginAt = _dateTimeProvider.UtcNow;
                await _unitOfWork.LoginActivities.AddAsync(new LoginActivity
                {
                    UserId = user.Id,
                    Ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown",
                    UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString(),
                    Success = true,
                    CreatedAt = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                if (user.TwoFactorEnabled)
                {
                    _logger.LogInformation("Login successful, 2FA required for: {Email}", loginDto.Email);

                    return new AuthResponseDto
                    {
                        RequiresTwoFactor = true,
                        AccessToken = string.Empty,
                        RefreshToken = string.Empty,
                        ExpiresAt = DateTime.MinValue,
                        User = new UserDto
                        {
                            Id = user.Id,
                            Email = user.Email,
                            FullName = user.FullName,
                            Role = user.Role,
                            Level = user.Level,
                            TwoFactorEnabled = user.TwoFactorEnabled
                        }
                    };
                }

                _logger.LogInformation("User logged in successfully (2FA disabled): {Email}", loginDto.Email);

                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return new AuthResponseDto
                {
                    AccessToken = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FullName = user.FullName,
                        Role = user.Role,
                        Level = user.Level,
                        TwoFactorEnabled = user.TwoFactorEnabled
                    },
                    RequiresTwoFactor = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", loginDto.Email);
                throw;
            }
        }

        // ====== REFRESH TOKEN ======
        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

                if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Invalid or expired refresh token");
                    throw new Exception("Invalid or expired refresh token");
                }

                var newAccessToken = GenerateJwtToken(user);
                var newRefreshToken = GenerateRefreshToken();

                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Token refreshed for user: {Email}", user.Email);

                return new AuthResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FullName = user.FullName,
                        Role = user.Role,
                        Level = user.Level,
                        TwoFactorEnabled = user.TwoFactorEnabled
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                throw;
            }
        }

        // ====== REVOKE TOKEN ======
        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

                if (user == null)
                {
                    return false;
                }

                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Token revoked for user: {Email}", user.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token revocation failed");
                throw;
            }
        }

        // ====== PASSWORD RESET ======
        public async Task<string> GeneratePasswordResetTokenAsync(string email)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    // Don't reveal if email exists
                    _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
                    return "If the email exists, a reset link will be sent.";
                }

                var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

                user.PasswordResetToken = resetToken;
                user.PasswordResetTokenExpiry = _dateTimeProvider.UtcNow.AddHours(1);
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                await SendPasswordResetEmailAsync(user.Email, resetToken);

                _logger.LogInformation("Password reset token generated for: {Email}", email);
                return "Password reset link has been sent to your email.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset token generation failed");
                throw;
            }
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto resetDto)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(
                    u => u.Email == resetDto.Email && u.PasswordResetToken == resetDto.Token);

                if (user == null)
                {
                    throw new Exception("Invalid reset token");
                }

                if (user.PasswordResetTokenExpiry < _dateTimeProvider.UtcNow)
                {
                    throw new Exception("Reset token has expired");
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(resetDto.NewPassword);
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;

                // Revoke all refresh tokens for security
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Password reset successfully for: {Email}", resetDto.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset failed");
                throw;
            }
        }

        // ====== LOGIN WITH 2FA ======
        public async Task<AuthResponseDto> LoginWith2FAAsync(string email, string code)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    throw new Exception("User not found");
                }

                if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
                {
                    throw new Exception("2FA is not enabled for this account");
                }

                if (!VerifyTwoFactorCode(user.TwoFactorSecret, code))
                {
                    _logger.LogWarning("Invalid 2FA code for user: {Email}", email);
                    throw new Exception("Invalid 2FA code");
                }

                user.LastLoginAt = _dateTimeProvider.UtcNow;

                await _unitOfWork.LoginActivities.AddAsync(new LoginActivity
                {
                    UserId = user.Id,
                    Ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown",
                    UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString(),
                    Success = true,
                    CreatedAt = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("User logged in with 2FA: {Email}", email);

                return new AuthResponseDto
                {
                    AccessToken = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FullName = user.FullName,
                        Role = user.Role,
                        Level = user.Level,
                        TwoFactorEnabled = user.TwoFactorEnabled
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "2FA login failed for {Email}", email);
                throw;
            }
        }

        // ====== USER PROFILE ======
        public async Task<UserProfileDto> GetProfileAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);

                if (user == null)
                {
                    throw new Exception("User not found");
                }

                return new UserProfileDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    Level = user.Level,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    PhoneNumber = user.PhoneNumber,
                    Timezone = user.Timezone
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get profile failed for userId: {UserId}", userId);
                throw;
            }
        }

        public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);

                if (user == null)
                {
                    throw new Exception("User not found");
                }

                if (!string.IsNullOrWhiteSpace(dto.FullName))
                {
                    user.FullName = dto.FullName;
                }
                if (dto.AvatarUrl != null)
                {
                    user.AvatarUrl = dto.AvatarUrl;
                }
                if (dto.Bio != null)
                {
                    user.Bio = dto.Bio;
                }
                if (dto.PhoneNumber != null)
                {
                    user.PhoneNumber = dto.PhoneNumber;
                }
                if (dto.Timezone != null)
                {
                    user.Timezone = dto.Timezone;
                }

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Profile updated for userId: {UserId}", userId);

                return await GetProfileAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update profile failed for userId: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);

                if (user == null)
                {
                    throw new Exception("User not found");
                }

                if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                {
                    throw new Exception("Current password is incorrect");
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

                // Revoke all refresh tokens for security
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Password changed for userId: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Change password failed for userId: {UserId}", userId);
                throw;
            }
        }

        // ====== ADMIN - USER MANAGEMENT ======
        public async Task<IEnumerable<UserListDto>> GetAllUsersAsync()
        {
            try
            {
                var users = await _unitOfWork.Users.GetAllAsync();

                return users.Select(u => new UserListDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    Role = u.Role,
                    Level = u.Level,
                    IsActive = u.IsActive,
                    EmailConfirmed = u.EmailConfirmed,
                    TwoFactorEnabled = u.TwoFactorEnabled,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get all users failed");
                throw;
            }
        }

        public async Task<bool> UpdateUserRoleAsync(int userId, UpdateUserRoleDto dto)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);

                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var role = await _roleService.GetRoleByIdAsync(dto.RoleId);
                if (role == null)
                {
                    throw new Exception($"Role with ID {dto.RoleId} not found.");
                }

                user.Role = role.Name;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Role updated for userId: {UserId} to {Role}", userId, user.Role);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update role failed for userId: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateUserLevelAsync(int userId, UpdateLevelDtoUser dto)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);

                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var level = await _levelService.GetLevelByIdAsync(dto.LevelId);
                if (level == null)
                {
                    throw new Exception($"Level with ID {dto.LevelId} not found.");
                }

                user.Level = level.Name;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Level updated for userId: {UserId} to {Level}", userId, user.Level);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update level failed for userId: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateUserStatusAsync(int userId, UpdateUserStatusDto dto)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);

                if (user == null)
                {
                    throw new Exception("User not found");
                }

                user.IsActive = dto.IsActive;

                // If deactivating, revoke all tokens
                if (!dto.IsActive)
                {
                    user.RefreshToken = null;
                    user.RefreshTokenExpiryTime = null;
                }

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Status updated for userId: {UserId} to {Status}",
                    userId, dto.IsActive ? "Active" : "Inactive");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update status failed for userId: {UserId}", userId);
                throw;
            }
        }

        // ====== 2FA MANAGEMENT ======
        public async Task<object> Enable2FAAsync()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                throw new Exception("User not found");
            }

            if (currentUser.TwoFactorEnabled)
            {
                throw new Exception("2FA is already enabled");
            }

            // Generate secret key
            var key = OtpNet.KeyGeneration.GenerateRandomKey(20);
            var secret = OtpNet.Base32Encoding.ToString(key);

            currentUser.TwoFactorSecret = secret;
            _unitOfWork.Users.Update(currentUser);
            await _unitOfWork.SaveChangesAsync();

            // Generate QR code URL
            var qrCodeUrl = $"otpauth://totp/CryptoTrading:{currentUser.Email}?secret={secret}&issuer=CryptoTrading";

            _logger.LogInformation("2FA setup initiated for user: {Email}", currentUser.Email);

            return new
            {
                secret = secret,
                qrCodeUrl = qrCodeUrl,
                message = "Scan the QR code with your authenticator app and verify with a code"
            };
        }

        public async Task<object> Verify2FAAsync(string code)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                throw new Exception("User not found");
            }

            if (string.IsNullOrEmpty(currentUser.TwoFactorSecret))
            {
                throw new Exception("2FA setup not initiated. Please enable 2FA first.");
            }

            if (!VerifyTwoFactorCode(currentUser.TwoFactorSecret, code))
            {
                throw new Exception("Invalid 2FA code");
            }

            currentUser.TwoFactorEnabled = true;
            _unitOfWork.Users.Update(currentUser);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("2FA enabled successfully for user: {Email}", currentUser.Email);

            return new
            {
                message = "2FA has been enabled successfully",
                twoFactorEnabled = true
            };
        }

        public async Task<bool> Disable2FAAsync(DisableTwoFactorDto dto)
        {
            // Lấy người dùng hiện tại từ token
            var user = await GetCurrentUserAsync();

            if (!user.TwoFactorEnabled)
            {
                throw new Exception("2FA is not currently enabled");
            }

            // Yêu cầu xác nhận mật khẩu trước khi tắt 2FA
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed 2FA disable attempt (wrong password) for user: {Email}", user.Email);
                throw new Exception("Invalid password");
            }

            // Tắt 2FA
            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("2FA disabled for user: {Email}", user.Email);
            return true;
        }

        // ====== TESTING METHODS ======
        public async Task TestConfirmEmailAsync(string email)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            user.EmailConfirmed = true;
            user.EmailConfirmationToken = null;
            user.EmailConfirmationTokenExpiry = null;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Email confirmed for testing: {Email}", email);
        }

        public async Task<string> TestGetResetTokenAsync(string email)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            if (string.IsNullOrEmpty(user.PasswordResetToken))
            {
                throw new Exception("No reset token found. Please request password reset first.");
            }

            _logger.LogInformation("Reset token retrieved for testing: {Email}", email);
            return user.PasswordResetToken;
        }

        public async Task<string> TestGenerate2FACodeAsync(string email)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            if (string.IsNullOrEmpty(user.TwoFactorSecret))
            {
                throw new Exception("2FA not setup for this user");
            }

            var key = OtpNet.Base32Encoding.ToBytes(user.TwoFactorSecret);
            var totp = new OtpNet.Totp(key);
            var code = totp.ComputeTotp();

            _logger.LogInformation("2FA code generated for testing: {Email}", email);
            return code;
        }

        // ====== PRIVATE HELPER METHODS ======
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("level", user.Level),
                new Claim("is_active", user.IsActive.ToString().ToLower()),
                new Claim("email_confirmed", user.EmailConfirmed.ToString().ToLower())
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

        private bool VerifyTwoFactorCode(string secret, string code)
        {
            try
            {
                var key = OtpNet.Base32Encoding.ToBytes(secret);
                var totp = new OtpNet.Totp(key);
                return totp.VerifyTotp(code, out _, new OtpNet.VerificationWindow(2, 2));
            }
            catch
            {
                return false;
            }
        }

        private async Task SendEmailConfirmationAsync(string email, string token)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = request != null
                ? $"{request.Scheme}://{request.Host}"
                : (Environment.GetEnvironmentVariable("BACKEND_BASE_URL") ?? "https://localhost:7154");

            var confirmUrl = $"{baseUrl}/confirm-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

            var subject = "Confirm Your Email - Crypto Trading";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; background-color: #f5f7fa; padding: 20px;'>
                    <div style='max-width: 600px; margin: 0 auto; background: white; border-radius: 15px; padding: 40px; box-shadow: 0 10px 30px rgba(0,0,0,0.1);'>
                        <div style='text-align: center; margin-bottom: 30px;'>
                            <h1 style='color: #667eea; margin: 0; font-size: 28px;'>🔐 Crypto Trading</h1>
                        </div>
                        
                        <h2 style='color: #333; margin-bottom: 20px;'>Welcome to Crypto Trading!</h2>
                        <p style='color: #666; font-size: 16px; line-height: 1.6;'>
                            Thank you for registering. Please confirm your email address by clicking the button below:
                        </p>
                        
                        <div style='text-align: center; margin: 40px 0;'>
                            <a href='{confirmUrl}' 
                               style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                                      color: white;
                                      padding: 16px 50px;
                                      text-decoration: none;
                                      border-radius: 8px;
                                      font-weight: bold;
                                      font-size: 16px;
                                      display: inline-block;
                                      box-shadow: 0 5px 15px rgba(102, 126, 234, 0.4);'>
                                ✓ Confirm Email Address
                            </a>
                        </div>
                        
                        <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 30px 0;'>
                            <p style='color: #666; font-size: 14px; margin: 0 0 10px 0;'>
                                <strong>Or copy this link:</strong>
                            </p>
                            <code style='background: white; padding: 10px; border-radius: 5px; display: block; word-break: break-all; color: #667eea; font-size: 12px;'>
                                {confirmUrl}
                            </code>
                        </div>
                        
                        <div style='border-top: 2px solid #f0f0f0; padding-top: 20px; margin-top: 30px;'>
                            <p style='color: #999; font-size: 13px; margin: 0;'>
                                ⏰ This link will expire in <strong>24 hours</strong>.
                            </p>
                            <p style='color: #999; font-size: 13px; margin: 10px 0 0 0;'>
                                If you didn't create this account, please ignore this email.
                            </p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await _emailSender.SendEmailAsync(email, subject, body, true);
            _logger.LogInformation("Email confirmation sent to {Email}", email);
        }

        private async Task SendPasswordResetEmailAsync(string email, string token)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = request != null
                ? $"{request.Scheme}://{request.Host}"
                : (Environment.GetEnvironmentVariable("BACKEND_BASE_URL") ?? "https://localhost:7154");

            var resetUrl = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

            var subject = "Reset Your Password - Crypto Trading";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; background-color: #f5f7fa; padding: 20px;'>
                    <div style='max-width: 600px; margin: 0 auto; background: white; border-radius: 15px; padding: 40px; box-shadow: 0 10px 30px rgba(0,0,0,0.1);'>
                        <div style='text-align: center; margin-bottom: 30px;'>
                            <h1 style='color: #dc3545; margin: 0; font-size: 28px;'>🔒 Crypto Trading</h1>
                        </div>
                        
                        <h2 style='color: #333; margin-bottom: 20px;'>Password Reset Request</h2>
                        <p style='color: #666; font-size: 16px; line-height: 1.6;'>
                            We received a request to reset your password. Click the button below to proceed:
                        </p>
                        
                        <div style='text-align: center; margin: 40px 0;'>
                            <a href='{resetUrl}' 
                               style='background: #dc3545;
                                      color: white;
                                      padding: 16px 50px;
                                      text-decoration: none;
                                      border-radius: 8px;
                                      font-weight: bold;
                                      font-size: 16px;
                                      display: inline-block;
                                      box-shadow: 0 5px 15px rgba(220, 53, 69, 0.4);'>
                                🔑 Reset Password
                            </a>
                        </div>
                        
                        <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 30px 0;'>
                            <p style='color: #666; font-size: 14px; margin: 0 0 10px 0;'>
                                <strong>Or copy this link:</strong>
                            </p>
                            <code style='background: white; padding: 10px; border-radius: 5px; display: block; word-break: break-all; color: #dc3545; font-size: 12px;'>
                                {resetUrl}
                            </code>
                        </div>
                        
                        <div style='border-top: 2px solid #f0f0f0; padding-top: 20px; margin-top: 30px;'>
                            <p style='color: #999; font-size: 13px; margin: 0;'>
                                ⏰ This link will expire in <strong>1 hour</strong>.
                            </p>
                            <p style='color: #999; font-size: 13px; margin: 10px 0 0 0;'>
                                ⚠️ If you didn't request this, please <strong>secure your account immediately</strong>.
                            </p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await _emailSender.SendEmailAsync(email, subject, body, true);
            _logger.LogInformation("Password reset email sent to {Email}", email);
        }

        private async Task<User> GetCurrentUserAsync()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("GetCurrentUserAsync failed: Could not find or parse UserID from token.");
                throw new UnauthorizedAccessException("Invalid user token");
            }

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("GetCurrentUserAsync failed: User {UserId} not found in database", userId);
                throw new Exception("User not found");
            }

            return user;
        }
    }
}
