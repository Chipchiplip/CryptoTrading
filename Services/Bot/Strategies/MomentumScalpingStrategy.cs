using System.Text.Json;
using CryptoTrading.Interfaces.Bot;
using CryptoTrading.Models;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Services.Bot.Strategies
{
    /// <summary>
    /// Momentum Scalping Strategy
    /// Detects coins with >3% price increase in 5 minutes, buys most volatile, sells at 6% profit or 3% stop loss
    /// </summary>
    public class MomentumScalpingStrategy : ITradingStrategy
    {
        public string Key => "momentum-scalping";

        public StrategyMetadata Metadata => new()
        {
            Version = "1.0.0",
            DisplayName = "Momentum Scalping",
            Description = "Detects coins with >3% spike in 5min, buys 100 USDT of most volatile, sells at 6% profit or 3% stop loss",
            MaxConcurrency = 20, // Can handle multiple coins
            ParametersSchemaJson = JsonSerializer.Serialize(new
            {
                type = "object",
                properties = new
                {
                    // Detection Settings
                    spikeThresholdPercent = new { type = "number", minimum = 1.0, maximum = 20.0, @default = 3.0 },
                    detectionWindowMinutes = new { type = "integer", minimum = 1, maximum = 60, @default = 5 },
                    
                    // Position Settings
                    positionSizeUSDT = new { type = "number", minimum = 10, maximum = 1000, @default = 100 },
                    maxConcurrentPositions = new { type = "integer", minimum = 1, maximum = 50, @default = 10 },
                    
                    // Exit Settings
                    takeProfitPercent = new { type = "number", minimum = 1.0, maximum = 50.0, @default = 6.0 },
                    stopLossPercent = new { type = "number", minimum = 0.5, maximum = 20.0, @default = 3.0 },
                    
                    // Risk Management
                    maxDailyLoss = new { type = "number", minimum = 50, maximum = 5000, @default = 500 },
                    cooldownMinutes = new { type = "integer", minimum = 1, maximum = 60, @default = 10 },
                    
                    // Market Data
                    minVolume24h = new { type = "number", minimum = 100000, maximum = 10000000, @default = 1000000 },
                    excludeStablecoins = new { type = "boolean", @default = true },
                    
                    // Execution
                    executionIntervalSeconds = new { type = "integer", minimum = 10, maximum = 300, @default = 30 }
                },
                required = new[] { "positionSizeUSDT", "takeProfitPercent", "stopLossPercent" }
            })
        };

        public async Task<StrategyValidationResult> ValidateAsync(
            BotContext context,
            BotParameters parameters,
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            
            // Basic validation
            var positionSize = parameters.GetValue("positionSizeUSDT", 100m);
            var takeProfitPercent = parameters.GetValue("takeProfitPercent", 6.0m);
            var stopLossPercent = parameters.GetValue("stopLossPercent", 3.0m);

            if (positionSize <= 0)
                return StrategyValidationResult.Fail("Position size must be greater than zero");

            if (takeProfitPercent <= 0)
                return StrategyValidationResult.Fail("Take profit percent must be greater than zero");

            if (stopLossPercent <= 0)
                return StrategyValidationResult.Fail("Stop loss percent must be greater than zero");

            return StrategyValidationResult.Ok();
        }

        public async Task<BotExecutionResult> ExecuteAsync(
            BotContext context,
            BotParameters parameters,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Load runtime state
                var state = await LoadOrInitializeStateAsync(context, parameters);
                
                // Check daily loss limit
                if (HasExceededDailyLoss(state, parameters))
                {
                    context.Logger.LogWarning("RiskManagement", "Daily loss limit exceeded, skipping execution");
                    return BotExecutionResult.SuccessResult();
                }

                // Scan for momentum opportunities
                var opportunities = await ScanForMomentumAsync(context, parameters, cancellationToken);
                
                if (opportunities.Any())
                {
                    context.Logger.LogInfo("Scanner", $"Found {opportunities.Count} momentum opportunities");
                    
                    // Execute trades on best opportunities
                    await ExecuteMomentumTradesAsync(context, state, opportunities, parameters);
                }

                // Manage existing positions
                await ManageExistingPositionsAsync(context, state, parameters);
                
                // Clean up old data
                CleanupOldData(state);
                
                // Save state
                await context.SaveStateAsyncFunc(state, cancellationToken);
                
                return BotExecutionResult.SuccessResult();
            }
            catch (Exception ex)
            {
                context.Logger.LogError("Execution", $"Strategy execution failed: {ex.Message}");
                return BotExecutionResult.Failure($"Execution failed: {ex.Message}");
            }
        }

        public async Task<StrategySimulationResult> SimulateAsync(
            BotContext context,
            SimulationRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validation
                if (request.InitialCapital <= 0)
                    return new StrategySimulationResult { Success = false, ErrorMessage = "Initial capital must be greater than zero" };

                if (request.StartDate >= request.EndDate)
                    return new StrategySimulationResult { Success = false, ErrorMessage = "Start date must be earlier than end date" };

                var parameters = new BotParameters { Values = request.Parameters ?? new Dictionary<string, object>() };
                
                // Get historical data (using 5-minute intervals for momentum detection)
                var candles = await context.MarketData.GetOhlcvAsync(
                    context.BaseAsset,
                    context.QuoteAsset,
                    request.StartDate,
                    request.EndDate,
                    "5m", // 5-minute candles for momentum detection
                    cancellationToken);

                if (candles.Count == 0)
                    return new StrategySimulationResult { Success = false, ErrorMessage = "No market data available for the requested range" };

                candles = candles.OrderBy(c => c.Timestamp).ToList();

                // Initialize simulation state
                var state = InitializeState(parameters);
                var availableCash = request.InitialCapital;
                var trades = new List<SimulationTradeDto>();
                var equityCurve = new List<SimulationEquityPoint>();
                var peakEquity = request.InitialCapital;
                var maxDrawdown = 0m;

                // Simulation parameters
                var spikeThreshold = parameters.GetValue("spikeThresholdPercent", 3.0m) / 100m;
                var positionSize = parameters.GetValue("positionSizeUSDT", 100m);
                var takeProfitPercent = parameters.GetValue("takeProfitPercent", 6.0m) / 100m;
                var stopLossPercent = parameters.GetValue("stopLossPercent", 3.0m) / 100m;
                var maxPositions = parameters.GetValue("maxConcurrentPositions", 10);

                // Run simulation
                for (int i = 1; i < candles.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var currentCandle = candles[i];
                    var previousCandle = candles[i - 1];
                    
                    // Calculate price change
                    var priceChange = (currentCandle.Close - previousCandle.Close) / previousCandle.Close;
                    
                    // Check for momentum spike
                    if (priceChange >= spikeThreshold && state.ActivePositions.Count < maxPositions && availableCash >= positionSize)
                    {
                        // Open new position
                        var quantity = positionSize / currentCandle.Close;
                        var position = new MomentumPosition
                        {
                            Id = Guid.NewGuid(),
                            Symbol = context.BaseAsset,
                            EntryPrice = currentCandle.Close,
                            Quantity = quantity,
                            EntryTime = currentCandle.Timestamp,
                            TakeProfitPrice = currentCandle.Close * (1 + takeProfitPercent),
                            StopLossPrice = currentCandle.Close * (1 - stopLossPercent)
                        };

                        state.ActivePositions.Add(position);
                        availableCash -= positionSize;
                        
                        // Log entry
                        trades.Add(new SimulationTradeDto
                        {
                            Timestamp = currentCandle.Timestamp,
                            Side = "BUY",
                            Price = currentCandle.Close,
                            Quantity = quantity,
                            Pnl = 0 // Entry trade
                        });
                    }

                    // Check exit conditions for existing positions
                    var positionsToClose = new List<MomentumPosition>();
                    
                    foreach (var position in state.ActivePositions)
                    {
                        var shouldClose = false;
                        var exitReason = "";
                        
                        // Take profit
                        if (currentCandle.High >= position.TakeProfitPrice)
                        {
                            shouldClose = true;
                            exitReason = "Take Profit";
                        }
                        // Stop loss
                        else if (currentCandle.Low <= position.StopLossPrice)
                        {
                            shouldClose = true;
                            exitReason = "Stop Loss";
                        }

                        if (shouldClose)
                        {
                            var exitPrice = exitReason == "Take Profit" ? position.TakeProfitPrice : position.StopLossPrice;
                            var pnl = (exitPrice - position.EntryPrice) * position.Quantity;
                            var proceeds = position.Quantity * exitPrice;
                            
                            availableCash += proceeds;
                            positionsToClose.Add(position);
                            
                            trades.Add(new SimulationTradeDto
                            {
                                Timestamp = currentCandle.Timestamp,
                                Side = "SELL",
                                Price = exitPrice,
                                Quantity = position.Quantity,
                                Pnl = pnl
                            });
                        }
                    }

                    // Remove closed positions
                    foreach (var position in positionsToClose)
                    {
                        state.ActivePositions.Remove(position);
                    }

                    // Calculate current equity
                    var positionValue = state.ActivePositions.Sum(p => p.Quantity * currentCandle.Close);
                    var totalValue = availableCash + positionValue;

                    // Update equity curve
                    equityCurve.Add(new SimulationEquityPoint
                    {
                        Timestamp = currentCandle.Timestamp,
                        Equity = totalValue
                    });

                    // Track drawdown
                    if (totalValue > peakEquity)
                    {
                        peakEquity = totalValue;
                    }
                    else
                    {
                        var drawdown = peakEquity - totalValue;
                        if (drawdown > maxDrawdown)
                        {
                            maxDrawdown = drawdown;
                        }
                    }
                }

                // Close remaining positions at final price
                var finalCandle = candles.Last();
                foreach (var position in state.ActivePositions)
                {
                    var pnl = (finalCandle.Close - position.EntryPrice) * position.Quantity;
                    var proceeds = position.Quantity * finalCandle.Close;
                    availableCash += proceeds;
                    
                    trades.Add(new SimulationTradeDto
                    {
                        Timestamp = finalCandle.Timestamp,
                        Side = "SELL",
                        Price = finalCandle.Close,
                        Quantity = position.Quantity,
                        Pnl = pnl
                    });
                }

                // Final calculations
                var finalCapital = availableCash;
                var totalReturn = finalCapital - request.InitialCapital;
                var returnPercentage = request.InitialCapital > 0 ? totalReturn / request.InitialCapital * 100m : 0m;

                var winningTrades = trades.Where(t => t.Side == "SELL" && t.Pnl > 0).Count();
                var losingTrades = trades.Where(t => t.Side == "SELL" && t.Pnl < 0).Count();
                var totalTrades = winningTrades + losingTrades;

                // Calculate Sharpe ratio
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

                var sharpeRatio = stdDev > 0 ? averageReturn / stdDev * (decimal)Math.Sqrt(288d) : 0m; // 288 = 24*60/5 (5-min periods per day)

                var result = new SimulationResultDto
                {
                    FinalCapital = finalCapital,
                    TotalReturn = totalReturn,
                    ReturnPercentage = returnPercentage,
                    TotalTrades = totalTrades,
                    WinningTrades = winningTrades,
                    LosingTrades = losingTrades,
                    WinRate = totalTrades > 0 ? winningTrades / (decimal)totalTrades * 100m : 0m,
                    MaxDrawdown = maxDrawdown,
                    SharpeRatio = sharpeRatio,
                    Trades = trades.Where(t => t.Side == "SELL").ToList(), // Only show exit trades
                    EquityCurve = equityCurve
                };

                return new StrategySimulationResult
                {
                    Success = true,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new StrategySimulationResult
                {
                    Success = false,
                    ErrorMessage = $"Simulation failed: {ex.Message}"
                };
            }
        }

        #region Private Methods

        private async Task<MomentumScalpingRuntimeState> LoadOrInitializeStateAsync(BotContext context, BotParameters parameters)
        {
            var state = await context.LoadStateAsyncFunc(typeof(MomentumScalpingRuntimeState), CancellationToken.None) as MomentumScalpingRuntimeState;
            return state ?? InitializeState(parameters);
        }

        private MomentumScalpingRuntimeState InitializeState(BotParameters parameters)
        {
            return new MomentumScalpingRuntimeState
            {
                ActivePositions = new List<MomentumPosition>(),
                PriceHistory = new Dictionary<string, List<PriceSnapshot>>(),
                DailyTrades = new List<TradeRecord>(),
                LastScanTime = DateTime.MinValue,
                DailyPnL = 0
            };
        }

        private bool HasExceededDailyLoss(MomentumScalpingRuntimeState state, BotParameters parameters)
        {
            var maxDailyLoss = parameters.GetValue("maxDailyLoss", 500m);
            var today = DateTime.UtcNow.Date;
            
            var todaysPnL = state.DailyTrades
                .Where(t => t.ExitTime.Date == today)
                .Sum(t => t.PnL);
                
            return todaysPnL <= -maxDailyLoss;
        }

        private async Task<List<MomentumOpportunity>> ScanForMomentumAsync(BotContext context, BotParameters parameters, CancellationToken cancellationToken)
        {
            var opportunities = new List<MomentumOpportunity>();
            var spikeThreshold = parameters.GetValue("spikeThresholdPercent", 3.0m) / 100m;
            var detectionWindow = parameters.GetValue("detectionWindowMinutes", 5);
            
            try
            {
                // For simulation, we'll use the current asset
                // In real implementation, this would scan multiple coins from Binance
                var endTime = DateTime.UtcNow;
                var startTime = endTime.AddMinutes(-detectionWindow);
                
                var recentCandles = await context.MarketData.GetOhlcvAsync(
                    context.BaseAsset,
                    context.QuoteAsset,
                    startTime,
                    endTime,
                    "1m",
                    cancellationToken);

                if (recentCandles.Count >= 2)
                {
                    var latestCandle = recentCandles.Last();
                    var earliestCandle = recentCandles.First();
                    
                    var priceChange = (latestCandle.Close - earliestCandle.Open) / earliestCandle.Open;
                    
                    if (priceChange >= spikeThreshold)
                    {
                        opportunities.Add(new MomentumOpportunity
                        {
                            Symbol = context.BaseAsset,
                            CurrentPrice = latestCandle.Close,
                            PriceChangePercent = priceChange * 100,
                            Volume24h = recentCandles.Sum(c => c.Volume),
                            DetectedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError("Scanner", $"Failed to scan for momentum: {ex.Message}");
            }
            
            return opportunities;
        }

        private async Task ExecuteMomentumTradesAsync(BotContext context, MomentumScalpingRuntimeState state, List<MomentumOpportunity> opportunities, BotParameters parameters)
        {
            await Task.CompletedTask;
            
            var positionSize = parameters.GetValue("positionSizeUSDT", 100m);
            var maxPositions = parameters.GetValue("maxConcurrentPositions", 10);
            var takeProfitPercent = parameters.GetValue("takeProfitPercent", 6.0m) / 100m;
            var stopLossPercent = parameters.GetValue("stopLossPercent", 3.0m) / 100m;
            var cooldownMinutes = parameters.GetValue("cooldownMinutes", 10);

            // Sort opportunities by momentum strength
            var sortedOpportunities = opportunities
                .OrderByDescending(o => o.PriceChangePercent)
                .Take(maxPositions - state.ActivePositions.Count)
                .ToList();

            foreach (var opportunity in sortedOpportunities)
            {
                // Check cooldown
                var lastTrade = state.DailyTrades
                    .Where(t => t.Symbol == opportunity.Symbol)
                    .OrderByDescending(t => t.ExitTime)
                    .FirstOrDefault();

                if (lastTrade != null && DateTime.UtcNow.Subtract(lastTrade.ExitTime).TotalMinutes < cooldownMinutes)
                {
                    continue; // Skip due to cooldown
                }

                // Check if we have enough capital
                if (context.AllowedCapital < positionSize)
                {
                    context.Logger.LogWarning("Trading", "Insufficient capital for new position");
                    break;
                }

                // Create new position
                var quantity = positionSize / opportunity.CurrentPrice;
                var orderRequest = new PlaceOrderRequest
                {
                    Symbol = $"{opportunity.Symbol}/{context.QuoteAsset}",
                    Side = "BUY",
                    Type = "MARKET",
                    Quantity = quantity
                };

                ulong orderId;
                try
                {
                    orderId = await context.TradingService.PlaceOrderAsync(orderRequest);
                }
                catch (Exception ex)
                {
                    context.Logger.LogError("Trading", $"Failed to place BUY order for {opportunity.Symbol}: {ex.Message}");
                    continue;
                }

                var position = new MomentumPosition
                {
                    Id = Guid.NewGuid(),
                    Symbol = opportunity.Symbol,
                    EntryPrice = opportunity.CurrentPrice,
                    Quantity = quantity,
                    EntryTime = DateTime.UtcNow,
                    TakeProfitPrice = opportunity.CurrentPrice * (1 + takeProfitPercent),
                    StopLossPrice = opportunity.CurrentPrice * (1 - stopLossPercent),
                    DetectedMomentum = opportunity.PriceChangePercent
                };

                state.ActivePositions.Add(position);
                
                context.Logger.LogInfo("Trading", 
                    $"Opened momentum position: {opportunity.Symbol} {quantity:F6} @ ${opportunity.CurrentPrice:F2} " +
                    $"(Momentum: +{opportunity.PriceChangePercent:F2}%, TP: ${position.TakeProfitPrice:F2}, SL: ${position.StopLossPrice:F2}), orderId={orderId}");
                context.Logger.LogInfo("Signal",
                    $"Signal BUY {opportunity.Symbol} at ${opportunity.CurrentPrice:F2} qty={quantity:F6} (spike {opportunity.PriceChangePercent:F2}%)",
                    new { opportunity.Symbol, quantity, opportunity.CurrentPrice, orderId });
            }
        }

        private async Task ManageExistingPositionsAsync(BotContext context, MomentumScalpingRuntimeState state, BotParameters parameters)
        {
            var positionsToClose = new List<MomentumPosition>();

            foreach (var position in state.ActivePositions)
            {
                try
                {
                    // Get current price
                    var currentPrice = await GetCurrentPriceAsync(context, position.Symbol);
                    if (currentPrice <= 0) continue;

                    var shouldClose = false;
                    var exitReason = "";
                    var exitPrice = currentPrice;

                    // Check take profit
                    if (currentPrice >= position.TakeProfitPrice)
                    {
                        shouldClose = true;
                        exitReason = "Take Profit";
                        exitPrice = position.TakeProfitPrice;
                    }
                    // Check stop loss
                    else if (currentPrice <= position.StopLossPrice)
                    {
                        shouldClose = true;
                        exitReason = "Stop Loss";
                        exitPrice = position.StopLossPrice;
                    }
                    // Check time-based exit (optional: close after X hours)
                    else if (DateTime.UtcNow.Subtract(position.EntryTime).TotalHours >= 4)
                    {
                        shouldClose = true;
                        exitReason = "Time Exit";
                        exitPrice = currentPrice;
                    }

                    if (shouldClose)
                    {
                        var pnl = (exitPrice - position.EntryPrice) * position.Quantity;
                        var pnlPercent = (exitPrice - position.EntryPrice) / position.EntryPrice * 100;

                        var side = position.Quantity > 0 ? "SELL" : "BUY";
                        var orderRequest = new PlaceOrderRequest
                        {
                            Symbol = $"{position.Symbol}/{context.QuoteAsset}",
                            Side = side,
                            Type = "MARKET",
                            Quantity = Math.Abs(position.Quantity)
                        };

                        ulong orderId;
                        try
                        {
                            orderId = await context.TradingService.PlaceOrderAsync(orderRequest);
                        }
                        catch (Exception ex)
                        {
                            context.Logger.LogError("Trading", $"Failed to close position {position.Symbol}: {ex.Message}");
                            continue;
                        }

                        // Record trade
                        state.DailyTrades.Add(new TradeRecord
                        {
                            Symbol = position.Symbol,
                            EntryPrice = position.EntryPrice,
                            ExitPrice = exitPrice,
                            Quantity = position.Quantity,
                            EntryTime = position.EntryTime,
                            ExitTime = DateTime.UtcNow,
                            PnL = pnl,
                            ExitReason = exitReason
                        });

                        state.DailyPnL += pnl;
                        positionsToClose.Add(position);

                        context.Logger.LogInfo("Trading",
                            $"Closed position: {position.Symbol} {position.Quantity:F6} @ ${exitPrice:F2} " +
                            $"({exitReason}, PnL: ${pnl:F2} / {pnlPercent:F2}%), orderId={orderId}");
                        context.Logger.LogInfo("Signal",
                            $"Exit {position.Symbol} at ${exitPrice:F2} reason={exitReason} qty={position.Quantity:F6} PnL=${pnl:F2}",
                            new { position.Symbol, position.Quantity, exitPrice, exitReason, orderId });
                    }
                }
                catch (Exception ex)
                {
                    context.Logger.LogError("Trading", $"Error managing position {position.Symbol}: {ex.Message}");
                }
            }

            // Remove closed positions
            foreach (var position in positionsToClose)
            {
                state.ActivePositions.Remove(position);
            }
        }

        private async Task<decimal> GetCurrentPriceAsync(BotContext context, string symbol)
        {
            try
            {
                var ohlcv = await context.MarketData.GetOhlcvAsync(
                    symbol,
                    context.QuoteAsset,
                    DateTime.UtcNow.AddMinutes(-5),
                    DateTime.UtcNow,
                    "1m");

                return ohlcv.LastOrDefault()?.Close ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private void CleanupOldData(MomentumScalpingRuntimeState state)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-7);
            
            // Remove old trades
            state.DailyTrades.RemoveAll(t => t.ExitTime < cutoffDate);
            
            // Remove old price history
            foreach (var symbol in state.PriceHistory.Keys.ToList())
            {
                state.PriceHistory[symbol].RemoveAll(p => p.Timestamp < cutoffDate);
                if (!state.PriceHistory[symbol].Any())
                {
                    state.PriceHistory.Remove(symbol);
                }
            }

            // Reset daily P&L if new day
            if (DateTime.UtcNow.Date > state.DailyTrades.Where(t => t.ExitTime.Date == DateTime.UtcNow.Date).LastOrDefault()?.ExitTime.Date)
            {
                state.DailyPnL = 0;
            }
        }

        #endregion

        #region Data Classes

        public class MomentumScalpingRuntimeState
        {
            public List<MomentumPosition> ActivePositions { get; set; } = new();
            public Dictionary<string, List<PriceSnapshot>> PriceHistory { get; set; } = new();
            public List<TradeRecord> DailyTrades { get; set; } = new();
            public DateTime LastScanTime { get; set; }
            public decimal DailyPnL { get; set; }
        }

        public class MomentumPosition
        {
            public Guid Id { get; set; }
            public string Symbol { get; set; } = string.Empty;
            public decimal EntryPrice { get; set; }
            public decimal Quantity { get; set; }
            public DateTime EntryTime { get; set; }
            public decimal TakeProfitPrice { get; set; }
            public decimal StopLossPrice { get; set; }
            public decimal DetectedMomentum { get; set; }
        }

        public class MomentumOpportunity
        {
            public string Symbol { get; set; } = string.Empty;
            public decimal CurrentPrice { get; set; }
            public decimal PriceChangePercent { get; set; }
            public decimal Volume24h { get; set; }
            public DateTime DetectedAt { get; set; }
        }

        public class PriceSnapshot
        {
            public DateTime Timestamp { get; set; }
            public decimal Price { get; set; }
            public decimal Volume { get; set; }
        }

        public class TradeRecord
        {
            public string Symbol { get; set; } = string.Empty;
            public decimal EntryPrice { get; set; }
            public decimal ExitPrice { get; set; }
            public decimal Quantity { get; set; }
            public DateTime EntryTime { get; set; }
            public DateTime ExitTime { get; set; }
            public decimal PnL { get; set; }
            public string ExitReason { get; set; } = string.Empty;
        }

        #endregion
    }
}
