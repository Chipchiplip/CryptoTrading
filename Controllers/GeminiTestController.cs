using CryptoTrading.Services.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.Controllers;

/// <summary>
/// Test controller for Gemini API integration
/// </summary>
[ApiController]
[Route("api/test/gemini")]
[AllowAnonymous] // Allow anonymous for testing
public class GeminiTestController : ControllerBase
{
    private readonly IGeminiService _geminiService;
    private readonly ILogger<GeminiTestController> _logger;

    public GeminiTestController(
        IGeminiService geminiService,
        ILogger<GeminiTestController> logger)
    {
        _geminiService = geminiService;
        _logger = logger;
    }

    /// <summary>
    /// Test Gemini API with a simple message
    /// </summary>
    [HttpPost("chat")]
    public async Task<ActionResult> TestChat([FromBody] TestChatRequest request)
    {
        try
        {
            _logger.LogInformation("Testing Gemini API with message: {Message}", request.Message);
            
            var reply = await _geminiService.ChatAsync(request.Message, null, HttpContext.RequestAborted);
            
            return Ok(new
            {
                success = true,
                reply = reply,
                message = request.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Gemini API");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                errorType = ex.GetType().Name,
                stackTrace = ex.StackTrace
            });
        }
    }
}

public class TestChatRequest
{
    public string Message { get; set; } = string.Empty;
}

