using CryptoTrading.Models.DTOs;
using CryptoTrading.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Interfaces;
using CryptoTrading.Models.DTOs.ExternalAuth;

namespace CryptoTrading.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUser _currentUser;
    private readonly IUserService _userService;

    public AuthController(IAuthService authService, ICurrentUser currentUser, IUserService userService)
    {
        _authService = authService;
        _currentUser = currentUser;
        _userService = userService;
    }

    /// <summary>
    /// Get current user's profile
    /// </summary>
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = _currentUser.UserId;
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var profile = await _authService.GetProfileAsync(userId.Value);
            if (profile == null)
            {
                return NotFound(new { message = "Profile not found" });
            }
            return Ok(profile);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    /// <summary>
    /// Login or Register with Google ID Token
    /// </summary>
    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] ExternalAuthDto dto)
    {
        try
        {
            // dto.Token ở đây là idToken
            var result = await _authService.LoginWithGoogleAsync(dto.Token);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    /// <summary>
    /// Login or Register with GitHub OAuth Code
    /// </summary>
    [HttpPost("github")]
    public async Task<IActionResult> GitHubLogin([FromBody] ExternalAuthDto dto)
    {
        try
        {
            // dto.Token ở đây là 'code'
            var result = await _authService.LoginWithGitHubAsync(dto.Token);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Change current user's password
    /// </summary>
    [Authorize]
    [HttpPost("change-password")] 
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            var userId = _currentUser.UserId;
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var success = await _authService.ChangePasswordAsync(userId.Value, dto);

            if (!success)
            {
                return BadRequest(new { message = "Invalid current password" });
            }

            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        try
        {
            var userId = _currentUser.UserId;
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Gọi UpdateProfileAsync từ IAuthService
            var profile = await _authService.UpdateProfileAsync(userId.Value, dto);
            if (profile == null)
            {
                return NotFound(new { message = "Profile update failed or user not found" });
            }

            // Trả về thông tin profile đã được cập nhật
            return Ok(profile);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    /// <summary>
    /// Get current user's login activity
    /// </summary>
    [Authorize]
    [HttpGet("activity")]
    [ProducesResponseType(typeof(List<LoginActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetLoginActivity()
    {
        var userId = _currentUser.UserId;
        if (userId == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            var activity = await _userService.GetLoginActivityAsync(userId.Value, 5);
            return Ok(activity);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// User registration
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            var result = await _authService.RegisterAsync(registerDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// User login
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            var result = await _authService.LoginAsync(loginDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Confirm email address
    /// </summary>
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto confirmEmailDto)
    {
        try
        {
            await _authService.ConfirmEmailAsync(confirmEmailDto.Email, confirmEmailDto.Token);
            return Ok(new { message = "Email confirmed successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Refresh JWT token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(refreshTokenDto.RefreshToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Revoke refresh token (logout)
    /// </summary>
    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        var result = await _authService.RevokeTokenAsync(refreshTokenDto.RefreshToken);
        if (!result)
            return BadRequest(new { message = "Invalid token" });
        return Ok(new { message = "Token revoked successfully" });
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        try
        {
            await _authService.GeneratePasswordResetTokenAsync(forgotPasswordDto.Email);
            return Ok(new
            {
                message = "Password reset instructions have been sent to your email"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
    {
        try
        {
            await _authService.ResetPasswordAsync(resetPasswordDto);
            return Ok(new { message = "Password reset successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Login with two-factor authentication
    /// </summary>
    [HttpPost("login-2fa")]
    public async Task<IActionResult> LoginWith2FA([FromBody] Login2FADto login2FADto)
    {
        try
        {
            var result = await _authService.LoginWith2FAAsync(login2FADto.Email, login2FADto.Code);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Test endpoint to confirm email directly (Development only)
    /// </summary>
    [HttpPost("test-confirm-email")]
    public async Task<IActionResult> TestConfirmEmail([FromBody] TestConfirmEmailDto testConfirmDto)
    {
        try
        {
            await _authService.TestConfirmEmailAsync(testConfirmDto.Email);
            return Ok(new { message = "Email confirmed successfully for testing" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Test endpoint to get password reset token (Development only)
    /// </summary>
    [HttpPost("test-get-reset-token")]
    public async Task<IActionResult> TestGetResetToken([FromBody] TestConfirmEmailDto testDto)
    {
        try
        {
            var token = await _authService.TestGetResetTokenAsync(testDto.Email);
            return Ok(new { resetToken = token });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Enable 2FA for user
    /// </summary>
    [Authorize]
    [HttpPost("enable-2fa")]
    public async Task<IActionResult> Enable2FA()
    {
        try
        {
            var result = await _authService.Enable2FAAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Verify 2FA setup
    /// </summary>
    [Authorize]
    [HttpPost("verify-2fa")]
    public async Task<IActionResult> Verify2FA([FromBody] VerifyTwoFactorDto twoFactorDto)
    {
        try
        {
            var result = await _authService.Verify2FAAsync(twoFactorDto.Code);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Disable 2FA for user
    /// </summary>
    [Authorize]
    [HttpPost("disable-2fa")]
    public async Task<IActionResult> Disable2FA([FromBody] DisableTwoFactorDto dto)
    {
      
   try
        {
            var result = await _authService.Disable2FAAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Test endpoint to generate 2FA code (Development only)
    /// </summary>
    [HttpPost("test-generate-2fa-code")]
    public async Task<IActionResult> TestGenerate2FACode([FromBody] TestConfirmEmailDto testDto)
    {
        try
        {
            var code = await _authService.TestGenerate2FACodeAsync(testDto.Email);
            return Ok(new { code = code });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
