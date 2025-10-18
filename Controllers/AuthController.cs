using CryptoTrading.Models.DTOs;
using CryptoTrading.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
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
    public async Task<IActionResult> Verify2FA([FromBody] TwoFactorDto twoFactorDto)
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
