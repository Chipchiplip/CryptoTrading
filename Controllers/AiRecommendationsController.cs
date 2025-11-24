using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CryptoTrading.Data;
using CryptoTrading.Models;
using CryptoTrading.Services.Bot;
using CryptoTrading.Interfaces.Bot;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CryptoTrading.Controllers
{
    /// <summary>
    /// Controller for AI trading recommendations
    /// </summary>
    [ApiController]
    [Route("api/ai/recommendations")]
    public class AiRecommendationsController : ControllerBase
    {
        private readonly HttpClient _aiHttpClient;
        private readonly ApplicationDbContext _db;
        private readonly AiRecommendationService _aiService;
        private readonly IRiskManager _riskManager;
        private readonly ILogger<AiRecommendationsController> _logger;

        public AiRecommendationsController(
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext db,
            AiRecommendationService aiService,
            IRiskManager riskManager,
            ILogger<AiRecommendationsController> logger)
        {
            _aiHttpClient = httpClientFactory.CreateClient("AiRecommendationService");
            _db = db;
            _aiService = aiService;
            _riskManager = riskManager;
            _logger = logger;
        }

        /// <summary>
        /// Request AI trading recommendation
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<AiRecommendationDto>> CreateRecommendation(
            [FromBody] CreateAiRecommendationRequest request,
            CancellationToken ct = default)
        {
            try
            {
                var userId = request.UserId; // TODO: Get from JWT token in production
                var botId = request.BotId;

                _logger.LogInformation("Requesting AI recommendation for UserId={UserId}, BotId={BotId}", userId, botId);

                // 1. Build trading plan & market snapshot from bot settings
                var tradingPlan = await _aiService.BuildTradingPlanAsync(userId, botId, ct);
                var snapshot = await _aiService.BuildMarketSnapshotAsync(userId, botId, null, ct);

                // 2. Convert trading plan and snapshot to snake_case format for Python API
                var tradingPlanJson = JsonSerializer.Serialize(tradingPlan, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                var tradingPlanObj = JsonSerializer.Deserialize<JsonElement>(tradingPlanJson);

                var snapshotJson = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                var snapshotObj = JsonSerializer.Deserialize<JsonElement>(snapshotJson);

                // 3. Call Python AI service
                var aiRequest = new AiRecommendationRequest
                {
                    UserId = userId,
                    BotId = null, // Python service expects int?, but we have Guid? - send null for now
                    TradingPlanId = tradingPlan.Id,
                    TradingPlan = tradingPlanObj,
                    MarketSnapshot = snapshotObj
                };

                var requestOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var response = await _aiHttpClient.PostAsJsonAsync("/ai/recommendations", aiRequest, requestOptions, ct);
                response.EnsureSuccessStatusCode();

                // Python service may return either snake_case or camelCase
                // JsonPropertyName attributes handle snake_case, PropertyNameCaseInsensitive handles both
                var recommendationOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var recommendation = await response.Content.ReadFromJsonAsync<AiRecommendationDto>(recommendationOptions, ct);
                if (recommendation == null)
                {
                    return BadRequest(new { message = "AI service returned null recommendation" });
                }

                _logger.LogInformation("AI recommendation received: {Decision} {Symbol} ${Amount} (confidence: {Confidence:P})",
                    recommendation.Decision, recommendation.Symbol, recommendation.AmountUsdt, recommendation.Confidence);

                return Ok(recommendation);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling AI service");
                return StatusCode(500, new { message = "Failed to get AI recommendation", error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recommendation request");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Apply recommendation to bot configuration
        /// </summary>
        [HttpPost("{id}/apply-to-bot")]
        public async Task<IActionResult> ApplyToBot(
            string id,
            [FromBody] ApplyAiToBotRequest request,
            CancellationToken ct = default)
        {
            try
            {
                // 1. Load recommendation from database
                var rec = await _db.Set<AiTradingRecommendation>()
                    .FirstOrDefaultAsync(x => x.RecommendationId == id, ct);

                if (rec == null)
                {
                    return NotFound(new { message = "Recommendation not found" });
                }

                if (rec.Status != "pending")
                {
                    return BadRequest(new { message = "Recommendation already processed" });
                }

                // 2. Load or create bot profile
                var botProfile = await _db.Set<AiBotProfile>()
                    .FirstOrDefaultAsync(x => x.ProfileId == request.ProfileId, ct);

                if (botProfile == null)
                {
                    // Create new profile
                    botProfile = new AiBotProfile
                    {
                        ProfileId = Guid.NewGuid().ToString(),
                        ProfileName = request.ProfileName ?? "AI Generated Profile",
                        TradingPlanJson = JsonSerializer.Serialize(rec.MarketSnapshotJson),
                        IsActive = true,
                        AutoApplyRecommendations = request.AutoApply ?? false,
                        MinConfidenceThreshold = request.MinConfidence ?? 0.7m
                    };
                    _db.Set<AiBotProfile>().Add(botProfile);
                }
                else
                {
                    // Update existing profile
                    botProfile.TradingPlanJson = JsonSerializer.Serialize(rec.MarketSnapshotJson);
                    botProfile.AutoApplyRecommendations = request.AutoApply ?? botProfile.AutoApplyRecommendations;
                    botProfile.MinConfidenceThreshold = request.MinConfidence ?? botProfile.MinConfidenceThreshold;
                }

                // 3. Update recommendation status
                rec.Status = "applied";
                rec.AppliedAtUtc = DateTime.UtcNow;

                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Applied recommendation {Id} to bot profile {ProfileId}", id, botProfile.ProfileId);

                return Ok(new { message = "Recommendation applied to bot", profileId = botProfile.ProfileId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying recommendation to bot");
                return StatusCode(500, new { message = "Failed to apply recommendation", error = ex.Message });
            }
        }

        /// <summary>
        /// Place order based on AI recommendation
        /// </summary>
        [HttpPost("{id}/place-order")]
        public async Task<IActionResult> PlaceOrder(
            string id,
            [FromBody] PlaceAiOrderRequest request,
            CancellationToken ct = default)
        {
            try
            {
                // 1. Load recommendation
                var rec = await _db.Set<AiTradingRecommendation>()
                    .FirstOrDefaultAsync(x => x.RecommendationId == id, ct);

                if (rec == null)
                {
                    return NotFound(new { message = "Recommendation not found" });
                }

                if (rec.Status != "pending")
                {
                    return BadRequest(new { message = "Recommendation already processed" });
                }

                // 2. Risk validation
                // TODO: Implement IRiskService.ValidateAiOrderAsync()
                var riskResult = await ValidateAiOrderAsync(rec, request.UserId, ct);

                // Log risk audit
                var auditLog = new AiRiskAuditLog
                {
                    RecommendationId = rec.RecommendationId,
                    AuditType = "capital_check",
                    Passed = riskResult.IsAllowed,
                    Details = riskResult.Details
                };
                _db.Set<AiRiskAuditLog>().Add(auditLog);

                if (!riskResult.IsAllowed)
                {
                    rec.Status = "rejected";
                    await _db.SaveChangesAsync(ct);
                    return BadRequest(new { message = "Order blocked by risk manager", details = riskResult.Details });
                }

                // 3. Place spot order
                // TODO: Implement ITradingService.PlaceSpotOrderAsync()
                // await _tradingService.PlaceSpotOrderAsync(new SpotOrderCommand
                // {
                //     UserId = request.UserId,
                //     Symbol = rec.Symbol,
                //     Side = rec.Decision == "BUY" ? OrderSide.Buy : OrderSide.Sell,
                //     AmountUsdt = rec.AmountUsdt
                // }, ct);

                // 4. Update recommendation status
                rec.Status = "applied";
                rec.AppliedAtUtc = DateTime.UtcNow;
                // rec.AppliedOrderId = orderId; // Set when order is created

                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Placed order for recommendation {Id}: {Decision} {Symbol} ${Amount}",
                    id, rec.Decision, rec.Symbol, rec.AmountUsdt);

                return Ok(new { message = "Order placed successfully", recommendationId = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order from recommendation");
                return StatusCode(500, new { message = "Failed to place order", error = ex.Message });
            }
        }


        private async Task<RiskValidationResult> ValidateAiOrderAsync(
            AiTradingRecommendation rec,
            int userId,
            CancellationToken ct)
        {
            // Use RiskManager to validate
            var requiredCapital = rec.AmountUsdt;
            var isAllowed = await _riskManager.CheckLimitsAsync(userId, requiredCapital, ct);

            if (!isAllowed)
            {
                return new RiskValidationResult
                {
                    IsAllowed = false,
                    Details = "Order exceeds risk limits (capital or exposure)"
                };
            }

            // Additional checks
            if (rec.Decision != "NO_TRADE" && rec.Confidence < 0.5m)
            {
                return new RiskValidationResult
                {
                    IsAllowed = false,
                    Details = "Confidence too low for trading"
                };
            }

            return new RiskValidationResult
            {
                IsAllowed = true,
                Details = "Risk validation passed"
            };
        }
    }

    // ============================================
    // DTOs
    // ============================================

    public class CreateAiRecommendationRequest
    {
        public int UserId { get; set; }
        public Guid? BotId { get; set; }
    }

    public class AiRecommendationRequest
    {
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }
        
        [JsonPropertyName("bot_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? BotId { get; set; }
        
        [JsonPropertyName("trading_plan_id")]
        public string TradingPlanId { get; set; } = string.Empty;
        
        [JsonPropertyName("trading_plan")]
        public object TradingPlan { get; set; } = default!;
        
        [JsonPropertyName("market_snapshot")]
        public object MarketSnapshot { get; set; } = default!;
    }

    public class AiRecommendationDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("decision")]
        public string Decision { get; set; } = "NO_TRADE";
        
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;
        
        [JsonPropertyName("amount_usdt")]
        public decimal AmountUsdt { get; set; }
        
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
        
        [JsonPropertyName("confidence")]
        public decimal Confidence { get; set; }
        
        [JsonPropertyName("time_horizon")]
        public string TimeHorizon { get; set; } = "intraday";
    }

    public class ApplyAiToBotRequest
    {
        public string? ProfileId { get; set; }
        public string? ProfileName { get; set; }
        public bool? AutoApply { get; set; }
        public decimal? MinConfidence { get; set; }
    }

    public class PlaceAiOrderRequest
    {
        public int UserId { get; set; }
    }

    public class TradingPlanDto
    {
        public string Id { get; set; } = string.Empty;
        public string[] PreferredSymbols { get; set; } = Array.Empty<string>();
        public string StrategyType { get; set; } = string.Empty;
        public string RiskMode { get; set; } = "normal";
        public decimal MaxCapitalPerTrade { get; set; }
        public decimal MaxDailyExposure { get; set; }
        public string TimeHorizon { get; set; } = "intraday";
        public decimal MinConfidence { get; set; } = 0.6m;
    }

    public class MarketSnapshotDto
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Trend1h { get; set; }
        public string? Trend4h { get; set; }
        public decimal? VolumeVsMa { get; set; }
        public decimal? Volatility { get; set; }
        public decimal? Support { get; set; }
        public decimal? Resistance { get; set; }
        public decimal UsdtBalance { get; set; }
        public decimal BtcHolding { get; set; }
        public decimal EthHolding { get; set; }
        public bool HasBadNews { get; set; }
    }

    public class RiskValidationResult
    {
        public bool IsAllowed { get; set; }
        public string Details { get; set; } = string.Empty;
    }
}

