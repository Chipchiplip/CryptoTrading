using CryptoTrading.Data;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Services.Ai;
using CryptoTrading.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.Controllers;

/// <summary>
/// API surface for interacting with the AI trading chat agent.
/// </summary>
[ApiController]
[Route("api/ai/chat")]
[Authorize]
public class AiChatController : ControllerBase
{
    private readonly IAiTradingChatService _chatService;

    public AiChatController(IAiTradingChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// Sends a user message to the AI agent and returns the response.
    /// Requires Pro or Premium subscription.
    /// </summary>
    [HttpPost]
    [RequireProOrPremium]
    public async Task<ActionResult<AiChatResponseDto>> ChatAsync(
        [FromBody] AiChatMessageRequest request,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await _chatService.HandleMessageAsync(request, ct);
        return Ok(response);
    }

    /// <summary>
    /// Apply a bot suggestion from AI chat.
    /// Requires Pro or Premium subscription.
    /// </summary>
    [HttpPost("bot-suggestions/{id:guid}/apply")]
    [RequireProOrPremium]
    public async Task<ActionResult<TradingBotDetailDto>> ApplySuggestion(
        Guid id,
        [FromQuery] int userId,
        CancellationToken ct = default)
    {
        var bot = await _chatService.ApplySuggestionAsync(id, userId, ct);
        return Ok(bot);
    }
}

