using CryptoTrading.Interfaces;
using CryptoTrading.Interfaces.Bot;
using CryptoTrading.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using CryptoTrading.Data;
using Microsoft.Extensions.DependencyInjection;
using CryptoTrading.Services.Trading;
using CryptoTrading.Attributes;

namespace CryptoTrading.Controllers
{
    [ApiController]
    [Route("api/bots")]
[Authorize]
[RequireProOrPremium]
public class BotController : ControllerBase
    {
        private readonly IBotApplicationService _botService;
        private readonly ILogger<BotController> _logger;
        private readonly IServiceProvider _serviceProvider;

        public BotController(
            IBotApplicationService botService,
            ILogger<BotController> logger,
            IServiceProvider serviceProvider)
        {
            _botService = botService;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? "0");
        }

        /// <summary>
        /// Get all bots for current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<TradingBotSummaryDto>>> GetBots(
            [FromQuery] string? status,
            [FromQuery] string? strategyKey,
            [FromQuery] string? baseAsset,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetUserId();
                var query = new BotListQuery
                {
                    Status = status,
                    StrategyKey = strategyKey,
                    BaseAsset = baseAsset,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _botService.GetListAsync(userId, query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bots");
                return StatusCode(500, new { error = "Failed to retrieve bots" });
            }
        }

        /// <summary>
        /// Get bot details by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TradingBotDetailDto>> GetBot(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var bot = await _botService.GetAsync(userId, id);
                return Ok(bot);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Bot not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bot {BotId}", id);
                return StatusCode(500, new { error = "Failed to retrieve bot" });
            }
        }

        /// <summary>
        /// Create a new bot
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TradingBotDetailDto>> CreateBot([FromBody] CreateBotRequest request)
        {
            try
            {
                var userId = GetUserId();
                var bot = await _botService.CreateAsync(userId, request);
                return CreatedAtAction(nameof(GetBot), new { id = bot.Id }, bot);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bot");
                return StatusCode(500, new { error = "Failed to create bot" });
            }
        }

        /// <summary>
        /// Update bot configuration (only when stopped)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<TradingBotDetailDto>> UpdateBot(Guid id, [FromBody] UpdateBotRequest request)
        {
            try
            {
                var userId = GetUserId();
                var bot = await _botService.UpdateAsync(userId, id, request);
                return Ok(bot);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Bot not found" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bot {BotId}", id);
                return StatusCode(500, new { error = "Failed to update bot" });
            }
        }

        /// <summary>
        /// Delete a bot
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBot(Guid id)
        {
            try
            {
                var userId = GetUserId();
                await _botService.DeleteAsync(userId, id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Bot not found" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting bot {BotId}", id);
                return StatusCode(500, new { error = "Failed to delete bot" });
            }
        }

        /// <summary>
        /// Start a bot
        /// </summary>
        [HttpPost("{id}/start")]
        [ProducesResponseType(typeof(BotOperationResponse), 202)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> StartBot(Guid id, [FromBody] StartBotRequest request)
        {
            try
            {
                var userId = GetUserId();
                var operationId = await _botService.StartAsync(userId, id, request);
                return Accepted(new BotOperationResponse { OperationId = operationId, Message = "Bot starting" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ErrorResponse { Error = "Bot not found" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting bot {BotId}", id);
                return StatusCode(500, new ErrorResponse { Error = "Failed to start bot" });
            }
        }

        /// <summary>
        /// Stop a bot
        /// </summary>
        [HttpPost("{id}/stop")]
        public async Task<ActionResult> StopBot(Guid id, [FromBody] StopBotRequest request)
        {
            try
            {
                var userId = GetUserId();
                await _botService.StopAsync(userId, id, request);
                return Ok(new { message = "Bot stopping" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Bot not found" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping bot {BotId}", id);
                return StatusCode(500, new { error = "Failed to stop bot" });
            }
        }

        /// <summary>
        /// Trigger immediate execution (nudge)
        /// </summary>
        [HttpPost("{id}/nudge")]
        public async Task<ActionResult> NudgeBot(Guid id)
        {
            try
            {
                var userId = GetUserId();
                await _botService.NudgeAsync(userId, id);
                return Accepted(new { message = "Bot execution triggered" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Bot not found" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error nudging bot {BotId}", id);
                return StatusCode(500, new { error = "Failed to trigger bot" });
            }
        }

        /// <summary>
        /// Get bot execution logs
        /// </summary>
        [HttpGet("{id}/logs")]
        public async Task<ActionResult<PaginatedResponse<BotLogDto>>> GetLogs(
            Guid id,
            [FromQuery] string? level,
            [FromQuery] string? category,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var userId = GetUserId();
                var query = new BotLogsQuery
                {
                    Level = level,
                    Category = category,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Page = page,
                    PageSize = pageSize
                };

                var logs = await _botService.GetLogsAsync(userId, id, query);
                return Ok(logs);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Bot not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs for bot {BotId}", id);
                return StatusCode(500, new { error = "Failed to retrieve logs" });
            }
        }

        /// <summary>
        /// Reset bot inventory based on actual filled orders
        /// </summary>
        [HttpPost("{id}/reset-inventory")]
        public async Task<ActionResult<ResetInventoryResultDto>> ResetInventory(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _botService.ResetInventoryAsync(userId, id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Bot not found" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting inventory for bot {BotId}", id);
                return StatusCode(500, new { error = "Failed to reset inventory" });
            }
        }

        /// <summary>
        /// Search bots by name (admin/debug endpoint)
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<List<BotSearchResultDto>>> SearchBots([FromQuery] string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new { error = "Name parameter is required" });
                }

                var results = await _botService.SearchBotsByNameAsync(name);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching bots");
                return StatusCode(500, new { error = "Failed to search bots" });
            }
        }

        /// <summary>
        /// Get all bots for a specific user ID (admin/debug endpoint)
        /// </summary>
        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<List<BotSearchResultDto>>> GetBotsByUser(int userId)
        {
            try
            {
                var results = await _botService.GetBotsByUserIdAsync(userId);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bots for user {UserId}", userId);
                return StatusCode(500, new { error = "Failed to get bots" });
            }
        }

        /// <summary>
        /// Reset bot inventory (admin version - no user ownership check)
        /// </summary>
        [HttpPost("{id}/reset-inventory-admin")]
        public async Task<ActionResult<ResetInventoryResultDto>> ResetInventoryAdmin(Guid id)
        {
            try
            {
                var result = await _botService.ResetInventoryAdminAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting inventory (admin) for bot {BotId}", id);
                return StatusCode(500, new { error = "Failed to reset inventory" });
            }
        }

        /// <summary>
        /// Get bot orders
        /// </summary>
        [HttpGet("{id}/orders")]
        public async Task<ActionResult<PaginatedResponse<BotOrderDto>>> GetOrders(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetUserId();
                var orders = await _botService.GetOrdersAsync(userId, id, page, pageSize);
                return Ok(orders);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Bot not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders for bot {BotId}", id);
                return StatusCode(500, new { error = "Failed to retrieve orders" });
            }
        }

        /// <summary>
        /// Run backtest simulation
        /// </summary>
        [HttpPost("{id}/simulate")]
        public async Task<ActionResult<SimulationResultDto>> Simulate(
            Guid id,
            [FromBody] SimulationRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _botService.SimulateAsync(userId, id, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (NotImplementedException)
            {
                return StatusCode(501, new { error = "Simulation feature coming soon" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Bot not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error simulating bot {BotId}", id);
                return StatusCode(500, new { error = "Failed to run simulation" });
            }
        }

        /// <summary>
        /// Test endpoint: Info about testing bot P&amp;L (DEV ONLY)
        /// Note: P&amp;L is calculated from real orders/trades. Use NUDGE to trigger bot execution.
        /// </summary>
        [HttpPost("{id}/test-pnl")]
        public async Task<ActionResult> TestUpdatePnl(
            Guid id,
            [FromBody] TestPnlRequest request)
        {
            try
            {
                var userId = GetUserId();
                
                // Only allow in Development environment
                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (env != "Development")
                {
                    return BadRequest(new { error = "This endpoint is only available in Development environment" });
                }

                var bot = await _botService.GetAsync(userId, id);
                if (bot == null)
                {
                    return NotFound(new { error = "Bot not found" });
                }

                return Ok(new { 
                    message = "P&L is calculated from real orders/trades",
                    note = "To test bot P&L:",
                    instructions = new[] {
                        "1. Use NUDGE endpoint to trigger bot execution: POST /api/bots/{id}/nudge",
                        "2. Bot will create orders and trades",
                        "3. P&L will be calculated automatically from filled orders",
                        "4. Refresh bot detail page to see updated P&L"
                    },
                    currentRuntime = bot.Runtime,
                    requestedTestValues = new {
                        realizedPnl = request.RealizedPnl,
                        unrealizedPnl = request.UnrealizedPnl,
                        totalFees = request.TotalFees
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test P&L endpoint for bot {BotId}", id);
                return StatusCode(500, new { error = "Failed to process request" });
            }
        }

        /// <summary>
        /// Test endpoint: Force fill bot orders for testing (DEV ONLY)
        /// </summary>
        [HttpPost("{id}/test-fill-orders")]
        public async Task<ActionResult> TestFillOrders(
            Guid id,
            [FromQuery] int count = 5)
        {
            try
            {
                var userId = GetUserId();
                
                // Only allow in Development environment
                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (env != "Development")
                {
                    return BadRequest(new { error = "This endpoint is only available in Development environment" });
                }

                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
                
                var bot = await db.TradingBots
                    .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
                
                if (bot == null)
                {
                    return NotFound(new { error = "Bot not found" });
                }

                // Get NEW/OPEN orders for this bot
                var botOrders = await db.TradingBotOrders
                    .Include(bo => bo.Order)
                    .Where(bo => bo.TradingBotId == id && 
                                 bo.Order != null && 
                                 (bo.Order.Status == "NEW" || bo.Order.Status == "OPEN"))
                    .OrderBy(bo => bo.CreatedAt)
                    .Take(count)
                    .ToListAsync();

                if (!botOrders.Any())
                {
                    return Ok(new { message = "No NEW/OPEN orders found to fill", filledCount = 0 });
                }

                var filledCount = 0;
                foreach (var botOrder in botOrders)
                {
                    var order = botOrder.Order!;
                    
                    // Force fill the order
                    order.Status = "FILLED";
                    order.FilledQty = order.QuantityCoin;
                    order.UpdatedAt = DateTime.UtcNow;
                    
                // Create a trade record
                var trade = new Models.Trade
                {
                    OrderId = order.Id,
                    CryptocurrencyId = order.CryptocurrencyId,
                    QuantityCoin = order.FilledQty,
                    PriceUsd = order.PriceUsd ?? 0,
                    FeeUsd = order.FilledQty * (order.PriceUsd ?? 0) * 0.001m, // 0.1% fee
                    CreatedAt = DateTime.UtcNow
                };
                    
                    db.Trades.Add(trade);
                    filledCount++;
                }

                await db.SaveChangesAsync();
                
                _logger.LogInformation("Force filled {Count} orders for bot {BotId} (TEST MODE)", filledCount, id);
                
                return Ok(new { 
                    message = $"Force filled {filledCount} orders for testing",
                    filledCount = filledCount,
                    note = "P&L will be recalculated on next bot execution or when you GET bot detail"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force filling orders for bot {BotId}", id);
                return StatusCode(500, new { error = "Failed to fill orders" });
            }
        }

        /// <summary>
        /// Close all positions (chốt lời) - Bán tất cả inventory đang hold
        /// </summary>
        [HttpPost("{id}/close-positions")]
        public async Task<ActionResult> CloseAllPositions(Guid id)
        {
            try
            {
                var userId = GetUserId();
                
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
                var tradingService = scope.ServiceProvider.GetRequiredService<ITradingService>();
                var strategyRegistry = scope.ServiceProvider.GetRequiredService<IStrategyRegistry>();
                
                var bot = await db.TradingBots
                    .Include(b => b.StrategyDefinition)
                    .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
                
                if (bot == null)
                {
                    return NotFound(new { error = "Bot not found" });
                }

                // Load bot state để lấy inventory
                var latestSnapshot = await db.TradingBotRuntimeSnapshots
                    .Where(s => s.TradingBotId == bot.Id)
                    .OrderByDescending(s => s.CapturedAt)
                    .FirstOrDefaultAsync();

                if (latestSnapshot == null || string.IsNullOrEmpty(latestSnapshot.RuntimeState))
                {
                    return Ok(new { message = "No bot state found, no positions to close", closedQuantity = 0m });
                }

                // Parse bot state để lấy inventory
                decimal inventory = 0;
                try
                {
                    var stateType = bot.StrategyDefinition?.StrategyKey == "grid-basic" 
                        ? typeof(Services.Bot.Strategies.GridRuntimeState) 
                        : null;
                    
                    if (stateType != null)
                    {
                        var state = System.Text.Json.JsonSerializer.Deserialize(latestSnapshot.RuntimeState, stateType);
                        if (state != null)
                        {
                            var inventoryProperty = stateType.GetProperty("Inventory");
                            if (inventoryProperty != null)
                            {
                                inventory = (decimal)(inventoryProperty.GetValue(state) ?? 0m);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse bot state for bot {BotId}", bot.Id);
                }

                if (inventory <= 0)
                {
                    return Ok(new { message = "No inventory to close", closedQuantity = 0m });
                }

                // Tạo SELL MARKET orders để bán hết inventory
                var orderSize = 0.1m; // Default order size, có thể lấy từ bot parameters
                var ordersCreated = 0;
                var totalSold = 0m;
                var remainingInventory = inventory;

                // Bán từng phần cho đến hết inventory
                while (remainingInventory > 0.001m) // Tolerance cho floating point
                {
                    var sellQuantity = Math.Min(orderSize, remainingInventory);
                    
                    try
                    {
                        var order = await tradingService.PlaceOrderAsync(userId, new PlaceOrderRequest
                        {
                            Symbol = $"{bot.BaseAsset}/{bot.QuoteAsset}",
                            Side = "SELL",
                            Type = "MARKET",
                            Quantity = sellQuantity
                        });

                        ordersCreated++;
                        totalSold += sellQuantity;
                        remainingInventory -= sellQuantity;

                        _logger.LogInformation("Created SELL order {OrderId} to close position: {Quantity} {Asset} for bot {BotId}",
                            order.Id, sellQuantity, bot.BaseAsset, bot.Id);

                        // Delay nhỏ giữa các orders
                        if (remainingInventory > 0.001m)
                        {
                            await Task.Delay(200); // 200ms delay
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create SELL order to close position for bot {BotId}", bot.Id);
                        break; // Dừng nếu có lỗi
                    }
                }

                return Ok(new { 
                    message = $"Closed {totalSold:F4} {bot.BaseAsset} positions",
                    closedQuantity = totalSold,
                    ordersCreated = ordersCreated,
                    remainingInventory = remainingInventory
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing positions for bot {BotId}", id);
                return StatusCode(500, new { error = "Failed to close positions" });
            }
        }
    }

    // Response DTOs for Swagger
    public class BotOperationResponse
    {
        public string OperationId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
    }

    public class TestPnlRequest
    {
        public decimal? RealizedPnl { get; set; }
        public decimal? UnrealizedPnl { get; set; }
        public decimal? TotalFees { get; set; }
    }
}

