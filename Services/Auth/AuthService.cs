using CryptoTrading.Infrastructure.Cloudflare;
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
using Google.Apis.Auth;
using CryptoTrading.Models.DTOs.ExternalAuth;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;

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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILevelService _levelService;
        private readonly CloudflareImagesOptions _cloudflareOptions;

        public AuthService(
            IUnitOfWork unitOfWork,
            IOptions<JwtSettings> jwtSettings,
            IEmailSender emailSender,
            IDateTimeProvider dateTimeProvider,
            ILogger<AuthService> logger,
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IRoleService roleService,
            ILevelService levelService,
            IOptions<CloudflareImagesOptions> cloudflareOptions)
        {
            _unitOfWork = unitOfWork;
            _jwtSettings = jwtSettings.Value;
            _emailSender = emailSender;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _roleService = roleService;
            _levelService = levelService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _cloudflareOptions = cloudflareOptions.Value;
        }

        // ====== ĐĂNG KÝ (REGISTER) ======
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

                    Role = "User",
                    Level = "Beginner",
                    IsActive = true
                    // ==============================
                };

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);

                // ✅ Gửi mail xác nhận trong background để không block response
                // Fire-and-forget pattern: không await để return response ngay lập tức
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SendEmailConfirmationAsync(user.Email, emailConfirmToken);
                        _logger.LogInformation("Confirmation email sent to {Email}", user.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
                    }
                });

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


        // ====== EXTERNAL AUTHENTICATION ======

        public async Task<AuthResponseDto> LoginWithGoogleAsync(string idToken)
        {
            var googleClientId = _configuration["ExternalAuth:Google:ClientId"];
            if (string.IsNullOrEmpty(googleClientId))
            {
                _logger.LogError("Google Client ID is not configured in appsettings.");
                throw new InvalidOperationException("Google Client ID is not configured.");
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { googleClientId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google ID Token validation failed.");
                throw new Exception("Invalid Google ID Token.");
            }

            return await HandleExternalUserLogin(payload.Email, payload.Name, "Google");
        }

        public async Task<AuthResponseDto> LoginWithGitHubAsync(string code)
        {
            var accessToken = await GetGitHubAccessTokenAsync(code);
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new Exception("Could not retrieve GitHub access token.");
            }

            var externalUser = await GetGitHubUserInfoAsync(accessToken);

            return await HandleExternalUserLogin(externalUser.Email, externalUser.Name, "GitHub");
        }

        /// <summary>
        /// Đổi code lấy Access Token từ GitHub.
        /// </summary>
        private async Task<string?> GetGitHubAccessTokenAsync(string code)
        {
            var clientId = _configuration["ExternalAuth:GitHub:ClientId"];
            var clientSecret = _configuration["ExternalAuth:GitHub:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("GitHub ClientID or ClientSecret not configured.");
            }

            var httpClient = _httpClientFactory.CreateClient();
            var tokenUrl = "https://github.com/login/oauth/access_token";

            var requestBody = new
            {
                client_id = clientId,
                client_secret = clientSecret,
                code = code
            };

            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("GitHub token exchange failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new Exception("GitHub token exchange failed.");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<GitHubTokenResponse>();
            return tokenResponse?.AccessToken;
        }

        /// <summary>
        /// Dùng Access Token để lấy thông tin User từ GitHub.
        /// </summary>
        private async Task<ExternalUser> GetGitHubUserInfoAsync(string accessToken)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CryptoTrading-App");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", accessToken);

            var userResponse = await httpClient.GetFromJsonAsync<GitHubUser>("https://api.github.com/user");

            if (userResponse == null)
            {
                throw new Exception("Could not retrieve GitHub user information.");
            }

            if (string.IsNullOrEmpty(userResponse.Email))
            {
                _logger.LogWarning("GitHub /user did not return email. Trying /user/emails.");
                var emailsResponse = await httpClient.GetFromJsonAsync<List<GitHubUserEmail>>("https://api.github.com/user/emails");
                
                var primaryEmail = emailsResponse?.FirstOrDefault(e => e.Primary && e.Verified);
                if (primaryEmail != null)
                {
                    userResponse.Email = primaryEmail.Email;
                }
            }

            if (string.IsNullOrEmpty(userResponse.Email))
            {
                throw new Exception("Could not retrieve primary email from GitHub. Ensure 'user:email' scope is granted on the client side.");
            }

            return new ExternalUser
            {
                Email = userResponse.Email,
                Name = userResponse.Name ?? userResponse.Login
            };
        }

        /// <summary>
        /// Logic chung để xử lý đăng nhập/đăng ký
        /// </summary>
        private async Task<AuthResponseDto> HandleExternalUserLogin(string email, string? fullName, string provider)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new Exception($"Could not retrieve email from {provider}.");
            }
            
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString() + "P@ssw0rd!"),
                    FullName = fullName,
                    EmailConfirmed = true,
                    CreatedAt = _dateTimeProvider.UtcNow,
                    Role = "User",
                    Level = "Beginner",
                    IsActive = true
                };
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("New user registered via {Provider}: {Email}", provider, user.Email);
            }
            else
            {
                if (!user.IsActive)
                {
                    _logger.LogWarning("External login attempt for locked account: {Email}", email);
                    throw new Exception("Account is locked. Contact admin.");
                }
                user.LastLoginAt = _dateTimeProvider.UtcNow;
                _unitOfWork.Users.Update(user);
            }

            await _unitOfWork.LoginActivities.AddAsync(new LoginActivity
            {
                UserId = user.Id,
                Ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown",
                UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString(),
                Success = true,
                CreatedAt = DateTime.UtcNow
            });
            
            // 3. Tạo JWT riêng của hệ thống
            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                User = MapToUserDto(user)
            };
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                Level = user.Level,
                TwoFactorEnabled = user.TwoFactorEnabled
            };
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
                    Timezone = user.Timezone,
                    AvatarUrl = user.AvatarUrl,
                    Bio = user.Bio
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
                    user.AvatarUrl = string.IsNullOrWhiteSpace(dto.AvatarUrl)
                        ? null
                        : ValidateAvatarUrl(dto.AvatarUrl);
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

        private string ValidateAvatarUrl(string avatarUrl)
        {
            if (!Uri.TryCreate(avatarUrl, UriKind.Absolute, out var avatarUri) ||
                avatarUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new Exception("Invalid avatar URL format");
            }

            var allowedOrigins = (_cloudflareOptions.AllowedAvatarDomains ?? Array.Empty<string>())
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .ToList();

            if (!allowedOrigins.Any() && !string.IsNullOrWhiteSpace(_cloudflareOptions.DeliveryUrl))
            {
                allowedOrigins.Add(_cloudflareOptions.DeliveryUrl);
            }

            var allowedHosts = allowedOrigins
                .Select(origin => NormalizeAllowedOrigin(origin))
                .Select(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri) ? uri.Host : null)
                .Where(host => !string.IsNullOrWhiteSpace(host))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!allowedHosts.Any())
            {
                throw new Exception("Avatar URL domain is not allowed");
            }

            var hostAllowed = allowedHosts.Any(host =>
                string.Equals(host, avatarUri.Host, StringComparison.OrdinalIgnoreCase));

            if (!hostAllowed)
            {
                throw new Exception("Avatar URL domain is not allowed");
            }

            return avatarUrl;
        }

        private static string NormalizeAllowedOrigin(string origin)
        {
            var trimmed = origin.Trim();
            if (!trimmed.Contains("://", StringComparison.Ordinal))
            {
                trimmed = $"https://{trimmed.TrimStart('/')}";
            }

            return trimmed.TrimEnd('/');
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
            var user = await GetCurrentUserAsync();

            if (!user.TwoFactorEnabled)
            {
                throw new Exception("2FA is not currently enabled");
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed 2FA disable attempt (wrong password) for user: {Email}", user.Email);
                throw new Exception("Invalid password");
            }

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

            var subject = "Confirm Your Email - CryptoTrade";
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Confirm Your Email</title>
</head>
<body style='margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; background: #000000;'>
    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background: #000000; padding: 40px 16px;'>
        <tr>
            <td align='center'>
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width: 600px;'>

                    <!-- Logo -->
                    <tr>
                        <td align='center' style='padding-bottom: 40px;'>
                            <table cellpadding='0' cellspacing='0' border='0'>
                                <tr>
                                    <td style='background: linear-gradient(135deg, #10b981 0%, #059669 100%); width: 56px; height: 56px; border-radius: 12px; text-align: center; vertical-align: middle; font-size: 28px;'>
                                        🛡️
                                    </td>
                                    <td style='padding-left: 12px; font-size: 24px; font-weight: 700; color: #10b981; vertical-align: middle;'>
                                        CryptoTrade
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Main Content Card -->
                    <tr>
                        <td style='background: #111111; border: 1px solid #1f2937; border-radius: 16px; padding: 48px 32px;'>

                            <!-- Icon Circle -->
                            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                                <tr>
                                    <td align='center' style='padding-bottom: 32px;'>
                                        <div style='width: 100px; height: 100px; background: linear-gradient(135deg, rgba(16, 185, 129, 0.1) 0%, rgba(5, 150, 105, 0.1) 100%); border: 3px solid rgba(16, 185, 129, 0.3); border-radius: 50%; display: inline-block; text-align: center; line-height: 94px; font-size: 48px;'>
                                            ✉️
                                        </div>
                                    </td>
                                </tr>
                            </table>

                            <!-- Title -->
                            <h1 style='margin: 0 0 16px 0; color: #ffffff; font-size: 28px; font-weight: 700; text-align: center; line-height: 1.3;'>
                                Welcome to CryptoTrade!
                            </h1>

                            <!-- Description -->
                            <p style='margin: 0 0 32px 0; color: #9ca3af; font-size: 16px; line-height: 1.6; text-align: center;'>
                                Thank you for registering. We're excited to have you on board!<br>
                                Please confirm your email address to get started.
                            </p>

                            <!-- CTA Button -->
                            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                                <tr>
                                    <td align='center' style='padding: 0 0 32px 0;'>
                                        <a href='{confirmUrl}' style='display: inline-block; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: #000000; padding: 18px 56px; text-decoration: none; border-radius: 12px; font-weight: 700; font-size: 16px; box-shadow: 0 8px 24px rgba(16, 185, 129, 0.35); transition: transform 0.2s;'>
                                            ✓ Confirm Email Address
                                        </a>
                                    </td>
                                </tr>
                            </table>

                            <!-- Alternative Link Box -->
                            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                                <tr>
                                    <td style='background: rgba(16, 185, 129, 0.08); border: 1px solid rgba(16, 185, 129, 0.2); border-radius: 12px; padding: 24px;'>
                                        <p style='margin: 0 0 12px 0; color: #10b981; font-size: 14px; font-weight: 600; text-align: center;'>
                                            Or copy and paste this link:
                                        </p>
                                        <div style='background: #1f2937; padding: 14px; border-radius: 8px; border: 1px solid #374151;'>
                                            <p style='margin: 0; color: #10b981; font-size: 12px; font-family: monospace; word-break: break-all; text-align: center; line-height: 1.6;'>
                                                {confirmUrl}
                                            </p>
                                        </div>
                                    </td>
                                </tr>
                            </table>

                            <!-- Info Divider -->
                            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='margin-top: 40px;'>
                                <tr>
                                    <td style='border-top: 1px solid #1f2937; padding-top: 24px;'>
                                        <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                                            <tr>
                                                <td style='padding: 8px 0;'>
                                                    <table cellpadding='0' cellspacing='0' border='0'>
                                                        <tr>
                                                            <td style='font-size: 20px; padding-right: 8px; vertical-align: middle;'>⏰</td>
                                                            <td style='color: #6b7280; font-size: 13px; line-height: 1.5; vertical-align: middle;'>
                                                                This link will expire in <strong style='color: #9ca3af;'>24 hours</strong>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 8px 0;'>
                                                    <table cellpadding='0' cellspacing='0' border='0'>
                                                        <tr>
                                                            <td style='font-size: 20px; padding-right: 8px; vertical-align: middle;'>🔒</td>
                                                            <td style='color: #6b7280; font-size: 13px; line-height: 1.5; vertical-align: middle;'>
                                                                For your security, don't share this link with anyone
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style='padding-top: 32px; text-align: center;'>
                            <p style='margin: 0 0 8px 0; color: #6b7280; font-size: 13px; line-height: 1.5;'>
                                If you didn't create this account, please ignore this email.
                            </p>
                            <p style='margin: 0; color: #4b5563; font-size: 12px;'>
                                © 2025 CryptoTrade. All rights reserved.
                            </p>
                        </td>
                    </tr>

                </table>
            </td>
        </tr>
    </table>
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

            var subject = "Reset Your Password - CryptoTrade";
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reset Your Password</title>
</head>
<body style='margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; background: #000000;'>
    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background: #000000; padding: 40px 16px;'>
        <tr>
            <td align='center'>
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width: 600px;'>

                    <!-- Logo -->
                    <tr>
                        <td align='center' style='padding-bottom: 40px;'>
                            <table cellpadding='0' cellspacing='0' border='0'>
                                <tr>
                                    <td style='background: linear-gradient(135deg, #10b981 0%, #059669 100%); width: 56px; height: 56px; border-radius: 12px; text-align: center; vertical-align: middle; font-size: 28px;'>
                                        🔐
                                    </td>
                                    <td style='padding-left: 12px; font-size: 24px; font-weight: 700; color: #10b981; vertical-align: middle;'>
                                        CryptoTrade
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Main Content Card -->
                    <tr>
                        <td style='background: #111111; border: 1px solid #1f2937; border-radius: 16px; padding: 48px 32px;'>

                            <!-- Icon Circle -->
                            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                                <tr>
                                    <td align='center' style='padding-bottom: 32px;'>
                                        <div style='width: 100px; height: 100px; background: linear-gradient(135deg, rgba(239, 68, 68, 0.1) 0%, rgba(220, 38, 38, 0.1) 100%); border: 3px solid rgba(239, 68, 68, 0.35); border-radius: 50%; display: inline-block; text-align: center; line-height: 94px; font-size: 48px;'>
                                            🔑
                                        </div>
                                    </td>
                                </tr>
                            </table>

                            <!-- Title -->
                            <h1 style='margin: 0 0 16px 0; color: #ffffff; font-size: 28px; font-weight: 700; text-align: center; line-height: 1.3;'>
                                Reset Your Password
                            </h1>

                            <!-- Description -->
                            <p style='margin: 0 0 28px 0; color: #9ca3af; font-size: 16px; line-height: 1.6; text-align: center;'>
                                We received a request to reset your password.<br>
                                Click the button below to create a new password for your account.
                            </p>

                            <!-- Warning Alert -->
                            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='margin-bottom: 32px;'>
                                <tr>
                                    <td style='background: rgba(239, 68, 68, 0.08); border: 1px solid rgba(239, 68, 68, 0.25); border-left: 4px solid #ef4444; border-radius: 12px; padding: 20px;'>
                                        <table cellpadding='0' cellspacing='0' border='0'>
                                            <tr>
                                                <td style='font-size: 20px; padding-right: 12px; vertical-align: top;'>⚠️</td>
                                                <td>
                                                    <p style='margin: 0 0 8px 0; color: #ef4444; font-size: 14px; font-weight: 700;'>
                                                        Security Notice
                                                    </p>
                                                    <p style='margin: 0; color: #9ca3af; font-size: 13px; line-height: 1.6;'>
                                                        If you didn't request this password reset, please <strong style='color: #ef4444;'>secure your account immediately</strong> and ignore this email.
                                                    </p>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- CTA Button -->
                            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                                <tr>
                                    <td align='center' style='padding: 0 0 32px 0;'>
                                        <a href='{resetUrl}' style='display: inline-block; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: #000000; padding: 18px 56px; text-decoration: none; border-radius: 12px; font-weight: 700; font-size: 16px; box-shadow: 0 8px 24px rgba(16, 185, 129, 0.35);'>
                                            🔑 Reset Password
                                        </a>
                                    </td>
                                </tr>
                            </table>

                            <!-- Alternative Link Box -->
                            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                                <tr>
                                    <td style='background: rgba(16, 185, 129, 0.08); border: 1px solid rgba(16, 185, 129, 0.2); border-radius: 12px; padding: 24px;'>
                                        <p style='margin: 0 0 12px 0; color: #10b981; font-size: 14px; font-weight: 600; text-align: center;'>
                                            Or copy and paste this link:
                                        </p>
                                        <div style='background: #1f2937; padding: 14px; border-radius: 8px; border: 1px solid #374151;'>
                                            <p style='margin: 0; color: #10b981; font-size: 12px; font-family: monospace; word-break: break-all; text-align: center; line-height: 1.6;'>
                                                {resetUrl}
                                            </p>
                                        </div>
                                    </td>
                                </tr>
                            </table>

                            <!-- Info Section -->
                            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='margin-top: 40px;'>
                                <tr>
                                    <td style='border-top: 1px solid #1f2937; padding-top: 24px;'>
                                        <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                                            <tr>
                                                <td style='padding: 8px 0;'>
                                                    <table cellpadding='0' cellspacing='0' border='0'>
                                                        <tr>
                                                            <td style='font-size: 20px; padding-right: 8px; vertical-align: middle;'>⏰</td>
                                                            <td style='color: #6b7280; font-size: 13px; line-height: 1.5; vertical-align: middle;'>
                                                                This link will expire in <strong style='color: #ef4444;'>1 hour</strong>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 8px 0;'>
                                                    <table cellpadding='0' cellspacing='0' border='0'>
                                                        <tr>
                                                            <td style='font-size: 20px; padding-right: 8px; vertical-align: middle;'>🔒</td>
                                                            <td style='color: #6b7280; font-size: 13px; line-height: 1.5; vertical-align: middle;'>
                                                                For security, this link can only be used once
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 8px 0;'>
                                                    <table cellpadding='0' cellspacing='0' border='0'>
                                                        <tr>
                                                            <td style='font-size: 20px; padding-right: 8px; vertical-align: middle;'>💡</td>
                                                            <td style='color: #6b7280; font-size: 13px; line-height: 1.5; vertical-align: middle;'>
                                                                After resetting, you'll need to log in with your new password
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                        </td>
                    </tr>

                    <!-- Support Card -->
                    <tr>
                        <td style='padding-top: 24px;'>
                            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                                <tr>
                                    <td style='background: rgba(16, 185, 129, 0.08); border: 1px solid rgba(16, 185, 129, 0.2); border-radius: 12px; padding: 24px; text-align: center;'>
                                        <p style='margin: 0 0 8px 0; color: #10b981; font-size: 14px; font-weight: 700;'>
                                            Need Help?
                                        </p>
                                        <p style='margin: 0 0 12px 0; color: #9ca3af; font-size: 13px; line-height: 1.5;'>
                                            If you're having trouble, contact our support team
                                        </p>
                                        <a href='mailto:support@cryptotrade.com' style='color: #10b981; text-decoration: none; font-weight: 600; font-size: 14px;'>
                                            support@cryptotrade.com
                                        </a>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style='padding-top: 32px; text-align: center;'>
                            <p style='margin: 0 0 8px 0; color: #6b7280; font-size: 13px; line-height: 1.5;'>
                                This is an automated security email from CryptoTrade.
                            </p>
                            <p style='margin: 0; color: #4b5563; font-size: 12px;'>
                                © 2025 CryptoTrade. All rights reserved.
                            </p>
                        </td>
                    </tr>

                </table>
            </td>
        </tr>
    </table>
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
