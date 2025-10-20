using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    /// <summary>
    /// Get payment info
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPaymentInfo()
    {
        return Ok(new { message = "Payment API - Ready for implementation", endpoints = new[] { "plans", "transactions", "deposits", "withdrawals" } });
    }

    /// <summary>
    /// Get subscription plans
    /// </summary>
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
    {
        // TODO: Implement payment service
        var plans = new object[]
        {
            new { id = 0, name = "Free", price = 0, features = new[] { "Basic trading", "1 watchlist" } },
            new { id = 1, name = "Plus", price = 9.99m, features = new[] { "Advanced trading", "5 watchlists", "Basic bots" } },
            new { id = 2, name = "Pro", price = 29.99m, features = new[] { "Professional trading", "Unlimited watchlists", "Advanced bots", "Priority support" } }
        };
        
        return Ok(new { message = "Get plans endpoint - to be implemented by team member", plans });
    }

    /// <summary>
    /// Create checkout session
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutDto dto)
    {
        // TODO: Implement payment service
        return Ok(new { message = "Create checkout session endpoint - to be implemented by team member", planType = dto.PlanType });
    }

    /// <summary>
    /// Get user subscription
    /// </summary>
    [HttpGet("subscription")]
    public async Task<IActionResult> GetSubscription()
    {
        // TODO: Implement payment service
        return Ok(new { message = "Get subscription endpoint - to be implemented by team member" });
    }

    /// <summary>
    /// Stripe webhook endpoint
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        // TODO: Implement payment service
        return Ok(new { message = "Stripe webhook endpoint - to be implemented by team member" });
    }
}

// DTOs
public record CreateCheckoutDto(int PlanType, string? SuccessUrl = null, string? CancelUrl = null);
