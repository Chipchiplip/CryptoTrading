using CryptoTrading.Core.Interfaces;
using CryptoTrading.Core.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace CryptoTrading.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TwoFactorController : ControllerBase
    {
        private readonly ITwoFactorService _twoFactorService;
        public TwoFactorController(ITwoFactorService twoFactorService)
        {
            _twoFactorService = twoFactorService;
        }

        [HttpPost("setup")]
        public async Task<IActionResult> Setup()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var setup = await _twoFactorService.GenerateSetupAsync(userId);
                return Ok(setup);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("enable")]
        public async Task<IActionResult> Enable([FromBody] EnableTwoFactorDto enableDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _twoFactorService.EnableTwoFactorAsync(userId, enableDto.Code);
                return Ok(new { message = "2FA enabled successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("disable")]
        public async Task<IActionResult> Disable([FromBody] DisableTwoFactorDto disableDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _twoFactorService.DisableTwoFactorAsync(userId, disableDto.Password);
                return Ok(new { message = "2FA disabled successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] VerifyTwoFactorDto verifyDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var isValid = await _twoFactorService.VerifyTwoFactorCodeAsync(userId, verifyDto.Code);

                if (!isValid)
                    return BadRequest(new { message = "Invalid code" });
                return Ok(new { message = "Code verified" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}