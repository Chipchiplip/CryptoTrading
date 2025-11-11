using CryptoTrading.Interfaces;
using CryptoTrading.Interfaces.Bot;
using CryptoTrading.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CryptoTrading.Controllers
{
    [ApiController]
    [Route("api/bots")]
    [Authorize]
    public class BotController : ControllerBase
    {
        private readonly IBotApplicationService _botService;
        private readonly ILogger<BotController> _logger;

        public BotController(
            IBotApplicationService botService,
            ILogger<BotController> logger)
        {
            _botService = botService;
            _logger = logger;
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
}

