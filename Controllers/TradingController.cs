using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TradingController : ControllerBase
{
    /// <summary>
    /// Get user balances
    /// </summary>
    [HttpGet("balances")]
    public async Task<IActionResult> GetBalances()
    {
        // TODO: Implement trading service
        return Ok(new { message = "Get balances endpoint - to be implemented by team member" });
    }

    /// <summary>
    /// Place new order
    /// </summary>
    [HttpPost("orders")]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
    {
        // TODO: Implement trading service
        return Ok(new { message = "Place order endpoint - to be implemented by team member", order = dto });
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("orders/{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        // TODO: Implement trading service
        return Ok(new { message = $"Get order {id} endpoint - to be implemented by team member" });
    }

    /// <summary>
    /// Get user orders
    /// </summary>
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders()
    {
        // TODO: Implement trading service
        return Ok(new { message = "Get orders endpoint - to be implemented by team member" });
    }
}

// DTOs
public record PlaceOrderDto(string Symbol, string Side, string Type, decimal Quantity, decimal? Price = null);
