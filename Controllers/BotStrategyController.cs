using CryptoTrading.Data;
using CryptoTrading.Interfaces.Bot;
using CryptoTrading.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CryptoTrading.Controllers
{
    [ApiController]
    [Route("api/bot-strategies")]
    [Authorize]
    public class BotStrategyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IStrategyRegistry _strategyRegistry;
        private readonly ILogger<BotStrategyController> _logger;

        public BotStrategyController(
            ApplicationDbContext context,
            IStrategyRegistry strategyRegistry,
            ILogger<BotStrategyController> logger)
        {
            _context = context;
            _strategyRegistry = strategyRegistry;
            _logger = logger;
        }

        /// <summary>
        /// Get all available strategies
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<StrategyDefinitionDto>>> GetStrategies(
            [FromQuery] bool includeExperimental = false)
        {
            try
            {
                var strategies = await _context.BotStrategyDefinitions
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayName)
                    .ToListAsync();

                var dtos = strategies.Select(s => new StrategyDefinitionDto
                {
                    Id = s.Id,
                    StrategyKey = s.StrategyKey,
                    Version = s.Version,
                    DisplayName = s.DisplayName,
                    Description = s.Description,
                    ParametersSchema = !string.IsNullOrEmpty(s.ParametersSchema)
                        ? JsonSerializer.Deserialize<object>(s.ParametersSchema)
                        : null,
                    MaxConcurrency = s.MaxConcurrency,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt
                }).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting strategies");
                return StatusCode(500, new { error = "Failed to retrieve strategies" });
            }
        }

        /// <summary>
        /// Get strategy by key
        /// </summary>
        [HttpGet("{key}")]
        public async Task<ActionResult<StrategyDefinitionDto>> GetStrategy(string key)
        {
            try
            {
                var strategy = await _context.BotStrategyDefinitions
                    .FirstOrDefaultAsync(s => s.StrategyKey == key && s.IsActive);

                if (strategy == null)
                {
                    return NotFound(new { error = "Strategy not found" });
                }

                var dto = new StrategyDefinitionDto
                {
                    Id = strategy.Id,
                    StrategyKey = strategy.StrategyKey,
                    Version = strategy.Version,
                    DisplayName = strategy.DisplayName,
                    Description = strategy.Description,
                    ParametersSchema = !string.IsNullOrEmpty(strategy.ParametersSchema)
                        ? JsonSerializer.Deserialize<object>(strategy.ParametersSchema)
                        : null,
                    MaxConcurrency = strategy.MaxConcurrency,
                    IsActive = strategy.IsActive,
                    CreatedAt = strategy.CreatedAt
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting strategy {Key}", key);
                return StatusCode(500, new { error = "Failed to retrieve strategy" });
            }
        }

        /// <summary>
        /// Get strategy JSON schema for validation
        /// </summary>
        [HttpGet("{key}/schema")]
        [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetStrategySchema(string key)
        {
            try
            {
                var strategy = await _context.BotStrategyDefinitions
                    .FirstOrDefaultAsync(s => s.StrategyKey == key && s.IsActive);

                if (strategy == null)
                {
                    return NotFound(new { error = "Strategy not found" });
                }

                if (string.IsNullOrEmpty(strategy.ParametersSchema))
                {
                    return Ok(new Dictionary<string, object>());
                }

                var schema = JsonSerializer.Deserialize<Dictionary<string, object>>(strategy.ParametersSchema);
                return Ok(schema ?? new Dictionary<string, object>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schema for strategy {Key}", key);
                return StatusCode(500, new { error = "Failed to retrieve schema" });
            }
        }

        /// <summary>
        /// Upload plugin strategy (Admin only)
        /// </summary>
        [HttpPost("upload")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(object), 202)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<ActionResult> UploadPlugin(IFormFile file, [FromHeader(Name = "X-Strategy-Key")] string strategyKey)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                if (string.IsNullOrEmpty(strategyKey))
                {
                    return BadRequest(new { error = "Strategy key required" });
                }

                // TODO: Implement plugin upload logic
                // 1. Save file to storage
                // 2. Validate assembly
                // 3. Create BotPluginPackages record
                // 4. Load and register strategy

                _logger.LogWarning("Plugin upload not yet implemented");
                return Accepted(new { message = "Plugin upload feature coming soon" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading plugin");
                return StatusCode(500, new { error = "Failed to upload plugin" });
            }
        }
    }
}

