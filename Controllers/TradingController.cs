using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Services.Trading;

namespace CryptoTrading.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TradingController : ControllerBase
{
    private readonly ITradingService _tradingService;

    public TradingController(ITradingService tradingService)
    {
        _tradingService = tradingService;
    }

    /// <summary>
    /// Get user balances
    /// </summary>
    [HttpGet("balances")]
    public async Task<IActionResult> GetBalances()
    {
        var result = await _tradingService.GetBalancesAsync();
        return Ok(result);
    }

    /// <summary>
    /// Place new order
    /// </summary>
    [HttpPost("orders")]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
    {
        var result = await _tradingService.PlaceOrderAsync(dto);
        return CreatedAtAction(nameof(GetOrder), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("orders/{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var result = await _tradingService.GetOrderAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Get user orders
    /// </summary>
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders()
    {
        var result = await _tradingService.GetOrdersAsync();
        return Ok(result);
    }
}

// DTOs
public record PlaceOrderDto(string Symbol, string Side, string Type, decimal Quantity, decimal? Price = null);
public record OrderDto(Guid Id, string Symbol, string Side, string Type, decimal Quantity, decimal? Price, string Status, DateTime CreatedAtUtc);
public record BalanceDto(string Asset, decimal Amount, decimal? UsdValue);
