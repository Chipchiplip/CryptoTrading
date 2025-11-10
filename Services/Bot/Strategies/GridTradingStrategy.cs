using CryptoTrading.Interfaces.Bot;
using CryptoTrading.Models.DTOs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CryptoTrading.Services.Bot.Strategies
{
    /// <summary>
    /// Grid Trading Strategy Implementation
    /// </summary>
    public class GridTradingStrategy : ITradingStrategy
    {
        private readonly ILogger<GridTradingStrategy> _logger;

        public string Key => "grid-basic";

        public StrategyMetadata Metadata => new()
        {
            DisplayName = "Grid Trading",
            Version = "1.0.0",
            Description = "Places buy and sell orders in a grid pattern to profit from price oscillations",
            MinIntervalSeconds = 30,
            RecommendedIntervalSeconds = 60,
            MaxConcurrency = 10,
            SupportedAssets = new List<string> { "BTC", "ETH", "BNB", "SOL", "XRP" },
            DefaultParameters = new Dictionary<string, object>
            {
                ["gridLevels"] = 10,
                ["lowerBound"] = 50000m,
                ["upperBound"] = 60000m,
                ["orderSize"] = 0.01m,
                ["capitalAllocation"] = 10000m,
                ["rebalanceMode"] = "balanced"
            },
            ParametersSchemaJson = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""gridLevels"": { ""type"": ""integer"", ""minimum"": 2, ""maximum"": 50 },
                    ""lowerBound"": { ""type"": ""number"", ""minimum"": 0 },
                    ""upperBound"": { ""type"": ""number"", ""minimum"": 0 },
                    ""orderSize"": { ""type"": ""number"", ""minimum"": 0.0001 },
                    ""capitalAllocation"": { ""type"": ""number"", ""minimum"": 100 },
                    ""rebalanceMode"": { ""type"": ""string"", ""enum"": [""balanced"", ""buy-heavy"", ""sell-heavy""] },
                    ""takeProfitPercent"": { ""type"": ""number"", ""minimum"": 0 },
                    ""stopLossPercent"": { ""type"": ""number"", ""minimum"": 0 }
                },
                ""required"": [""gridLevels"", ""lowerBound"", ""upperBound"", ""orderSize"", ""capitalAllocation""]
            }"
        };

        public GridTradingStrategy(ILogger<GridTradingStrategy> logger)
        {
            _logger = logger;
        }

        public Task<StrategyValidationResult> ValidateAsync(
            BotContext context, 
            BotParameters parameters, 
            CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            var lowerBound = parameters.GetValue<decimal>("lowerBound");
            var upperBound = parameters.GetValue<decimal>("upperBound");
            var gridLevels = parameters.GetValue<int>("gridLevels");
            var orderSize = parameters.GetValue<decimal>("orderSize");
            var capitalAllocation = parameters.GetValue<decimal>("capitalAllocation");

            if (upperBound <= lowerBound)
            {
                errors.Add("Upper bound must be greater than lower bound");
            }

            if (gridLevels < 2)
            {
                errors.Add("Grid levels must be at least 2");
            }

            if (gridLevels > 50)
            {
                errors.Add("Grid levels cannot exceed 50");
            }

            if (orderSize <= 0)
            {
                errors.Add("Order size must be greater than 0");
            }

            if (capitalAllocation > context.AllowedCapital)
            {
                errors.Add($"Capital allocation ({capitalAllocation}) exceeds allowed capital ({context.AllowedCapital})");
            }

            // Estimate total required capital
            var estimatedCapital = EstimateRequiredCapital(lowerBound, upperBound, gridLevels, orderSize);
            if (estimatedCapital > capitalAllocation)
            {
                errors.Add($"Estimated required capital ({estimatedCapital:F2}) exceeds allocated capital ({capitalAllocation:F2})");
            }

            if (errors.Any())
            {
                return Task.FromResult(StrategyValidationResult.Fail(errors));
            }

            return Task.FromResult(StrategyValidationResult.Ok());
        }

        public async Task<BotExecutionResult> ExecuteAsync(
            BotContext context, 
            BotParameters parameters, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Load or initialize state
                var state = await context.LoadStateAsync<GridRuntimeState>(cancellationToken) 
                    ?? InitializeState(parameters);

                // Get current market price
                var currentPrice = await context.MarketData.GetMidPriceAsync(
                    context.BaseAsset, 
                    context.QuoteAsset, 
                    cancellationToken);

                if (currentPrice <= 0)
                {
                    return BotExecutionResult.Failure("Unable to retrieve market price", TimeSpan.FromSeconds(30));
                }

                context.Logger.LogInfo("Execution", $"Current price: ${currentPrice:F2}");

                // Process grid lines
                var events = new List<BotEvent>();
                var ordersPlaced = 0;

                foreach (var line in state.GridLines)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Check if we should place a buy order
                    if (line.ShouldPlaceBuy(currentPrice) && !line.HasPendingOrder)
                    {
                        try
                        {
                            var orderId = await context.TradingService.PlaceOrderAsync(
                                new PlaceOrderRequest
                                {
                                    Symbol = $"{context.BaseAsset}/{context.QuoteAsset}",
                                    Side = "BUY",
                                    Type = "LIMIT",
                                    Quantity = parameters.GetValue<decimal>("orderSize"),
                                    Price = line.Price
                                },
                                cancellationToken);

                            line.MarkPending(orderId, "BUY");
                            ordersPlaced++;
                            
                            context.Logger.LogInfo("OrderPlaced", $"BUY order at ${line.Price:F2}", new { orderId, line.Price });
                            events.Add(new BotEvent
                            {
                                Type = "OrderPlaced",
                                Message = $"BUY limit order placed at ${line.Price:F2}",
                                Data = new Dictionary<string, object> { ["orderId"] = orderId, ["price"] = line.Price }
                            });
                        }
                        catch (Exception ex)
                        {
                            context.Logger.LogError("OrderFailed", $"Failed to place BUY order: {ex.Message}");
                        }
                    }
                    // Check if we should place a sell order
                    else if (line.ShouldPlaceSell(currentPrice) && !line.HasPendingOrder)
                    {
                        try
                        {
                            var orderId = await context.TradingService.PlaceOrderAsync(
                                new PlaceOrderRequest
                                {
                                    Symbol = $"{context.BaseAsset}/{context.QuoteAsset}",
                                    Side = "SELL",
                                    Type = "LIMIT",
                                    Quantity = parameters.GetValue<decimal>("orderSize"),
                                    Price = line.Price
                                },
                                cancellationToken);

                            line.MarkPending(orderId, "SELL");
                            ordersPlaced++;
                            
                            context.Logger.LogInfo("OrderPlaced", $"SELL order at ${line.Price:F2}", new { orderId, line.Price });
                            events.Add(new BotEvent
                            {
                                Type = "OrderPlaced",
                                Message = $"SELL limit order placed at ${line.Price:F2}",
                                Data = new Dictionary<string, object> { ["orderId"] = orderId, ["price"] = line.Price }
                            });
                        }
                        catch (Exception ex)
                        {
                            context.Logger.LogError("OrderFailed", $"Failed to place SELL order: {ex.Message}");
                        }
                    }
                    // Check if we should cancel an order (price moved away)
                    else if (line.HasPendingOrder && line.ShouldCancel(currentPrice))
                    {
                        try
                        {
                            await context.TradingService.CancelOrderAsync(line.OrderId!.Value, cancellationToken);
                            line.Reset();
                            
                            context.Logger.LogInfo("OrderCancelled", $"Cancelled order at ${line.Price:F2}");
                            events.Add(new BotEvent
                            {
                                Type = "OrderCancelled",
                                Message = $"Order cancelled (price moved away from grid line)",
                                Data = new Dictionary<string, object> { ["price"] = line.Price }
                            });
                        }
                        catch (Exception ex)
                        {
                            context.Logger.LogWarning("CancelFailed", $"Failed to cancel order: {ex.Message}");
                        }
                    }
                }

                // Update metrics
                state.UpdateMetrics(currentPrice);

                // Save state
                await context.SaveStateAsync(state, cancellationToken);

                var nextRunAt = DateTime.UtcNow.AddSeconds(parameters.GetValue<int>("refreshIntervalSeconds", 60));

                return BotExecutionResult.SuccessResult(nextRunAt)
                    .WithMetrics(new Dictionary<string, object>
                    {
                        ["currentPrice"] = currentPrice,
                        ["unrealizedPnl"] = state.UnrealizedPnl,
                        ["inventory"] = state.Inventory,
                        ["cashAvailable"] = state.CashAvailable,
                        ["ordersPlaced"] = ordersPlaced,
                        ["activeOrders"] = state.GridLines.Count(l => l.HasPendingOrder)
                    })
                    .WithEvents(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing grid strategy for bot {BotId}", context.BotId);
                return BotExecutionResult.Failure(ex.Message, TimeSpan.FromMinutes(1));
            }
        }

        public Task<StrategySimulationResult> SimulateAsync(
            BotContext context, 
            SimulationRequest request, 
            CancellationToken cancellationToken = default)
        {
            // TODO: Implement backtesting simulation
            _logger.LogWarning("Simulation not yet implemented for grid strategy");
            
            return Task.FromResult(new StrategySimulationResult
            {
                Success = false,
                ErrorMessage = "Simulation not yet implemented"
            });
        }

        private GridRuntimeState InitializeState(BotParameters parameters)
        {
            var lowerBound = parameters.GetValue<decimal>("lowerBound");
            var upperBound = parameters.GetValue<decimal>("upperBound");
            var gridLevels = parameters.GetValue<int>("gridLevels");
            var capitalAllocation = parameters.GetValue<decimal>("capitalAllocation");

            var step = (upperBound - lowerBound) / (gridLevels - 1);
            var gridLines = new List<GridLine>();

            for (int i = 0; i < gridLevels; i++)
            {
                var price = lowerBound + (step * i);
                gridLines.Add(new GridLine
                {
                    Price = Math.Round(price, 2),
                    Index = i
                });
            }

            return new GridRuntimeState
            {
                GridLines = gridLines,
                CashAvailable = capitalAllocation,
                Inventory = 0,
                LastPrice = 0,
                LastPnL = 0,
                UnrealizedPnl = 0,
                LastTradeTime = null
            };
        }

        private decimal EstimateRequiredCapital(decimal lowerBound, decimal upperBound, int gridLevels, decimal orderSize)
        {
            // Rough estimate: average price * order size * half grid levels (for buy orders)
            var avgPrice = (lowerBound + upperBound) / 2;
            return avgPrice * orderSize * (gridLevels / 2) * 1.1m; // 10% buffer
        }
    }

    // ========== GRID RUNTIME STATE ==========

    public class GridRuntimeState
    {
        public List<GridLine> GridLines { get; set; } = new();
        public decimal CashAvailable { get; set; }
        public decimal Inventory { get; set; }
        public decimal LastPrice { get; set; }
        public decimal LastPnL { get; set; }
        public decimal UnrealizedPnl { get; set; }
        public DateTime? LastTradeTime { get; set; }

        public void UpdateMetrics(decimal currentPrice)
        {
            LastPrice = currentPrice;
            UnrealizedPnl = Inventory * (currentPrice - LastPrice);
        }
    }

    public class GridLine
    {
        public int Index { get; set; }
        public decimal Price { get; set; }
        public ulong? OrderId { get; set; }
        public string? OrderSide { get; set; }
        public decimal FilledQty { get; set; }

        public bool HasPendingOrder => OrderId.HasValue;

        public bool ShouldPlaceBuy(decimal currentPrice)
        {
            // Place buy order if current price is above this grid line (waiting for price to come down)
            return currentPrice > Price && !HasPendingOrder;
        }

        public bool ShouldPlaceSell(decimal currentPrice)
        {
            // Place sell order if current price is below this grid line (waiting for price to go up)
            return currentPrice < Price && !HasPendingOrder;
        }

        public bool ShouldCancel(decimal currentPrice)
        {
            if (!HasPendingOrder)
                return false;

            // Cancel if price moved too far away (more than 10% from grid line)
            var distancePercent = Math.Abs((currentPrice - Price) / Price);
            return distancePercent > 0.10m;
        }

        public void MarkPending(ulong orderId, string side)
        {
            OrderId = orderId;
            OrderSide = side;
        }

        public void Reset()
        {
            OrderId = null;
            OrderSide = null;
            FilledQty = 0;
        }
    }
}

