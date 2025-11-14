using System;
using System.Collections.Generic;
using System.Linq;
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
                ["rebalanceMode"] = "balanced",
                ["minOrderCooldownSeconds"] = 30,
                ["maxOrdersPerCycle"] = 5
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
                    ""stopLossPercent"": { ""type"": ""number"", ""minimum"": 0 },
                    ""minOrderCooldownSeconds"": { ""type"": ""integer"", ""minimum"": 1, ""maximum"": 3600 },
                    ""maxOrdersPerCycle"": { ""type"": ""integer"", ""minimum"": 1, ""maximum"": 50 }
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
                // Reset order count for new cycle
                await context.RiskManager.ResetOrderCountForNewCycleAsync(context.BotId, cancellationToken);

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

                // Get rate limit parameters
                var minCooldownSeconds = parameters.GetValue("minOrderCooldownSeconds", 30);
                var maxOrdersPerCycle = parameters.GetValue("maxOrdersPerCycle", 5);

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
                        // PRE-ORDER CHECKS: Cooldown and rate limit
                        var cooldownPassed = await context.RiskManager.CheckCooldownAsync(
                            context.BotId,
                            TimeSpan.FromSeconds(minCooldownSeconds),
                            cancellationToken);

                        var withinRateLimit = await context.RiskManager.CheckRateLimitAsync(
                            context.BotId,
                            maxOrdersPerCycle,
                            cancellationToken);

                        if (!cooldownPassed)
                        {
                            context.Logger.LogDebug("OrderSkipped", $"BUY order skipped (cooldown not passed) at ${line.Price:F2}");
                            continue; // Skip this order
                        }

                        if (!withinRateLimit)
                        {
                            context.Logger.LogDebug("OrderSkipped", $"BUY order skipped (rate limit reached) at ${line.Price:F2}");
                            break; // Stop processing more grid lines this cycle
                        }

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

                            // Record order placed for rate limiting tracking
                            await context.RiskManager.RecordOrderPlacedAsync(context.BotId, cancellationToken);

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
                        // PRE-ORDER CHECKS: Cooldown and rate limit
                        var cooldownPassed = await context.RiskManager.CheckCooldownAsync(
                            context.BotId,
                            TimeSpan.FromSeconds(minCooldownSeconds),
                            cancellationToken);

                        var withinRateLimit = await context.RiskManager.CheckRateLimitAsync(
                            context.BotId,
                            maxOrdersPerCycle,
                            cancellationToken);

                        if (!cooldownPassed)
                        {
                            context.Logger.LogDebug("OrderSkipped", $"SELL order skipped (cooldown not passed) at ${line.Price:F2}");
                            continue; // Skip this order
                        }

                        if (!withinRateLimit)
                        {
                            context.Logger.LogDebug("OrderSkipped", $"SELL order skipped (rate limit reached) at ${line.Price:F2}");
                            break; // Stop processing more grid lines this cycle
                        }

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

                            // Record order placed for rate limiting tracking
                            await context.RiskManager.RecordOrderPlacedAsync(context.BotId, cancellationToken);

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

                // Track cycle-level PnL change for kill switch
                // Note: Grid strategy tracks unrealized PnL. For more accurate kill switch tracking,
                // implement order fill monitoring to track realized PnL per completed buy-sell pair.
                var cyclePnLChange = state.UnrealizedPnl - state.LastPnL;
                if (Math.Abs(cyclePnLChange) > 0.01m) // Only track significant changes (> 1 cent)
                {
                    await context.RiskManager.RecordTradeResultAsync(context.BotId, cyclePnLChange, cancellationToken);
                    state.LastPnL = state.UnrealizedPnl;

                    context.Logger.LogDebug("RiskTracking",
                        $"Recorded cycle PnL change: ${cyclePnLChange:F2} (Total unrealized: ${state.UnrealizedPnl:F2})");
                }

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

        public async Task<StrategySimulationResult> SimulateAsync(
            BotContext context,
            SimulationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.InitialCapital <= 0)
            {
                return new StrategySimulationResult
                {
                    Success = false,
                    ErrorMessage = "Initial capital must be greater than zero"
                };
            }

            if (request.StartDate >= request.EndDate)
            {
                return new StrategySimulationResult
                {
                    Success = false,
                    ErrorMessage = "Start date must be earlier than end date"
                };
            }

            var parameters = new BotParameters
            {
                Values = request.Parameters ?? new Dictionary<string, object>()
            };

            var validation = await ValidateAsync(context, parameters, cancellationToken);
            if (!validation.IsValid)
            {
                return new StrategySimulationResult
                {
                    Success = false,
                    ErrorMessage = string.Join("; ", validation.Errors)
                };
            }

            var candles = await context.MarketData.GetOhlcvAsync(
                context.BaseAsset,
                context.QuoteAsset,
                request.StartDate,
                request.EndDate,
                request.Parameters != null && request.Parameters.TryGetValue("interval", out var intervalObj)
                    ? Convert.ToString(intervalObj) ?? "1h"
                    : "1h",
                cancellationToken);

            if (candles == null || candles.Count == 0)
            {
                return new StrategySimulationResult
                {
                    Success = false,
                    ErrorMessage = "No market data available for the requested range"
                };
            }

            candles = candles.OrderBy(c => c.Timestamp).ToList();

            var state = InitializeState(parameters);
            var orderSize = parameters.GetValue("orderSize", 0.01m);
            var capitalAllocation = parameters.GetValue("capitalAllocation", request.InitialCapital);
            var lowerBound = parameters.GetValue("lowerBound", candles.Min(c => c.Low));
            var upperBound = parameters.GetValue("upperBound", candles.Max(c => c.High));

            if (orderSize <= 0)
            {
                return new StrategySimulationResult
                {
                    Success = false,
                    ErrorMessage = "Order size must be greater than zero"
                };
            }

            var step = state.GridLines.Count > 1
                ? state.GridLines[1].Price - state.GridLines[0].Price
                : Math.Max(upperBound - lowerBound, 1m) / 10m;

            var availableCash = Math.Min(request.InitialCapital, capitalAllocation);
            var inventory = 0m;
            var trades = new List<SimulationTradeDto>();
            var equityCurve = new List<SimulationEquityPoint>();

            var positions = state.GridLines.Select(gl => new GridPosition
            {
                EntryPrice = gl.Price,
                TargetPrice = Math.Min(gl.Price + step, upperBound),
                IsOpen = false
            }).ToArray();

            decimal peakEquity = availableCash;
            decimal maxDrawdown = 0m;

            foreach (var candle in candles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (var i = 0; i < positions.Length; i++)
                {
                    var position = positions[i];

                    if (!position.IsOpen && position.EntryPrice >= lowerBound && candle.Low <= position.EntryPrice)
                    {
                        var cost = position.EntryPrice * orderSize;
                        if (availableCash >= cost)
                        {
                            availableCash -= cost;
                            inventory += orderSize;
                            position.IsOpen = true;
                            position.EntryTime = candle.Timestamp;
                        }
                    }
                    else if (position.IsOpen && candle.High >= position.TargetPrice)
                    {
                        var proceeds = position.TargetPrice * orderSize;
                        availableCash += proceeds;
                        inventory -= orderSize;

                        var pnl = (position.TargetPrice - position.EntryPrice) * orderSize;
                        trades.Add(new SimulationTradeDto
                        {
                            Timestamp = candle.Timestamp,
                            Side = "SELL",
                            Price = position.TargetPrice,
                            Quantity = orderSize,
                            Pnl = pnl
                        });

                        position.IsOpen = false;
                        position.EntryTime = null;
                    }
                }

                var equity = availableCash + inventory * candle.Close;
                equityCurve.Add(new SimulationEquityPoint
                {
                    Timestamp = candle.Timestamp,
                    Equity = equity
                });

                if (equity > peakEquity)
                {
                    peakEquity = equity;
                }
                else
                {
                    var drawdown = peakEquity - equity;
                    if (drawdown > maxDrawdown)
                    {
                        maxDrawdown = drawdown;
                    }
                }
            }

            var finalPrice = candles.Last().Close;
            var finalCapital = availableCash + inventory * finalPrice;
            var totalReturn = finalCapital - request.InitialCapital;
            var returnPercentage = request.InitialCapital > 0
                ? totalReturn / request.InitialCapital * 100m
                : 0m;

            var winningTrades = trades.Count(t => t.Pnl > 0);
            var losingTrades = trades.Count(t => t.Pnl < 0);

            var equityReturns = new List<decimal>();
            for (var i = 1; i < equityCurve.Count; i++)
            {
                var prevEquity = equityCurve[i - 1].Equity;
                if (prevEquity > 0)
                {
                    var ret = (equityCurve[i].Equity - prevEquity) / prevEquity;
                    equityReturns.Add(ret);
                }
            }

            var averageReturn = equityReturns.Count > 0 ? equityReturns.Average() : 0m;
            var stdDev = equityReturns.Count > 1
                ? (decimal)Math.Sqrt((double)equityReturns.Select(r => (r - averageReturn) * (r - averageReturn)).Average())
                : 0m;

            var sharpeRatio = stdDev > 0 ? averageReturn / stdDev * (decimal)Math.Sqrt(365d) : 0m;

            var result = new SimulationResultDto
            {
                FinalCapital = finalCapital,
                TotalReturn = totalReturn,
                ReturnPercentage = returnPercentage,
                TotalTrades = trades.Count,
                WinningTrades = winningTrades,
                LosingTrades = losingTrades,
                WinRate = trades.Count > 0 ? winningTrades / (decimal)trades.Count * 100m : 0m,
                MaxDrawdown = maxDrawdown,
                SharpeRatio = sharpeRatio,
                Trades = trades,
                EquityCurve = equityCurve
            };

            return new StrategySimulationResult
            {
                Success = true,
                Result = result
            };
        }

        private class GridPosition
        {
            public decimal EntryPrice { get; set; }
            public decimal TargetPrice { get; set; }
            public bool IsOpen { get; set; }
            public DateTime? EntryTime { get; set; }
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

