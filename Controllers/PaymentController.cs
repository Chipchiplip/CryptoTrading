using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Services.Payment;

namespace CryptoTrading.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Get subscription plans
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans()
    {
        var result = await _paymentService.GetPlansAsync();
        return Ok(result);
    }

    /// <summary>
    /// Create checkout session
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutDto dto)
    {
        var result = await _paymentService.CreateCheckoutSessionAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Get user subscription
    /// </summary>
    [HttpGet("subscription")]
    public async Task<IActionResult> GetSubscription()
    {
        var result = await _paymentService.GetSubscriptionAsync();
        return Ok(result);
    }

    /// <summary>
    /// Stripe webhook endpoint
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        await _paymentService.HandleWebhookAsync(Request);
        return Ok();
    }
}

// DTOs
public record CreateCheckoutDto(int PlanType, string? SuccessUrl = null, string? CancelUrl = null);
public record CheckoutSessionDto(string SessionId, string CheckoutUrl);
public record SubscriptionDto(Guid Id, int PlanType, string Status, DateTime CurrentPeriodEndUtc);
