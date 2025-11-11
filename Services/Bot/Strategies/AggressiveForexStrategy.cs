using System.Text.Json;
using CryptoTrading.Interfaces.Bot;
using CryptoTrading.Models;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Services.Bot.Strategies
{
    /// <summary>
    /// Aggressive Forex-style strategy with EMA + Martingale + Pyramiding
    /// High risk, high reward approach similar to Forex EA
    /// </summary>
    public class AggressiveForexStrategy : ITradingStrategy
    {
        public string Key => "aggressive-forex";

        public StrategyMetadata Metadata => new()
        {
            Version = "1.0.0",
            DisplayName = "Aggressive Forex Style",
            Description = "High-risk strategy with EMA trend following, Martingale recovery, and Pyramiding. Similar to aggressive Forex EAs.",
            MaxConcurrency = 5, // Limited due to high risk
            ParametersSchemaJson = JsonSerializer.Serialize(new
            {
                type = "object",
                properties = new
                {
                    // EMA Settings
                    emaPeriod = new { type = "integer", minimum = 10, maximum = 500, @default = 200 },
                    emaShift = new { type = "integer", minimum = 0, maximum = 20, @default = 5 },
                    
                    // Initial Trade Settings
                    initialLot = new { type = "number", minimum = 0.001, maximum = 1.0, @default = 0.01 },
                    capitalAllocation = new { type = "number", minimum = 100, maximum = 100000, @default = 10000 },
                    
                    // Pyramiding Settings (when in profit)
                    pyramidEnabled = new { type = "boolean", @default = true },
                    pyramidTriggerPercent = new { type = "number", minimum = 0.1, maximum = 10.0, @default = 3.0 },
                    pyramidLotMultiplier = new { type = "number", minimum = 0.5, maximum = 2.0, @default = 1.0 },
                    maxPyramidOrders = new { type = "integer", minimum = 1, maximum = 50, @default = 10 },
                    
                    // Martingale Settings (when in loss)
                    martingaleEnabled = new { type = "boolean", @default = true },
                    martingaleDistancePercent = new { type = "number", minimum = 0.5, maximum = 10.0, @default = 4.0 },
                    martingaleMultiplier = new { type = "number", minimum = 1.0, maximum = 3.0, @default = 1.1 },
                    maxMartingaleOrders = new { type = "integer", minimum = 1, maximum = 50, @default = 10 },
                    
                    // RSI Filter
                    rsiPeriod = new { type = "integer", minimum = 2, maximum = 50, @default = 5 },
                    rsiOverbought = new { type = "number", minimum = 70, maximum = 95, @default = 80 },
                    rsiOversold = new { type = "number", minimum = 5, maximum = 30, @default = 20 },
                    
                    // Risk Management
                    cutLossUSD = new { type = "number", minimum = 100, maximum = 50000, @default = 5000 },
                    trailingEnabled = new { type = "boolean", @default = true },
                    trailingTriggerPercent = new { type = "number", minimum = 0.5, maximum = 20.0, @default = 10.0 },
                    trailingDistancePercent = new { type = "number", minimum = 0.5, maximum = 20.0, @default = 10.0 },
                    
                    // Execution
                    executionIntervalSeconds = new { type = "integer", minimum = 10, maximum = 3600, @default = 60 }
                },
                required = new[] { "initialLot", "capitalAllocation", "cutLossUSD" }
            })
        };

        public async Task<StrategyValidationResult> ValidateAsync(
            BotContext context,
            BotParameters parameters,
            CancellationToken cancellationToken = default)
        {
            // Basic validation
            var initialLot = parameters.GetValue("initialLot", 0.01m);
            var capitalAllocation = parameters.GetValue("capitalAllocation", 10000m);
            var cutLossUSD = parameters.GetValue("cutLossUSD", 5000m);

            if (initialLot <= 0)
                return StrategyValidationResult.Fail("Initial lot must be greater than zero");

            if (capitalAllocation <= 0)
                return StrategyValidationResult.Fail("Capital allocation must be greater than zero");

            if (cutLossUSD <= 0)
                return StrategyValidationResult.Fail("Cut loss must be greater than zero");

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
                
                // Get current market data
                var currentPrice = await GetCurrentPriceAsync(context);
                if (currentPrice <= 0)
                {
                    return BotExecutionResult.Failure("Unable to get current price");
                }

                context.Logger.LogInfo("Execution", $"Current price: ${currentPrice:F2}");

                // Calculate EMA
                var emaValue = await CalculateEMAAsync(context, state, currentPrice, parameters);
                
                // Calculate RSI
                var rsiValue = await CalculateRSIAsync(context, state, currentPrice, parameters);
                
                // Update price history
                state.PriceHistory.Add(new PricePoint { Timestamp = DateTime.UtcNow, Price = currentPrice });
                if (state.PriceHistory.Count > 1000) // Keep last 1000 prices
                {
                    state.PriceHistory.RemoveAt(0);
                }

                // Determine trend direction
                var trendDirection = DetermineTrend(currentPrice, emaValue, rsiValue, parameters);
                
                // Execute trading logic
                await ExecuteTradingLogicAsync(context, state, currentPrice, trendDirection, parameters);
                
                // Apply risk management
                await ApplyRiskManagementAsync(context, state, currentPrice, parameters);
                
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
                
                // Get historical data
                var candles = await context.MarketData.GetOhlcvAsync(
                    context.BaseAsset,
                    context.QuoteAsset,
                    request.StartDate,
                    request.EndDate,
                    "1h",
                    cancellationToken);

                if (candles.Count == 0)
                    return new StrategySimulationResult { Success = false, ErrorMessage = "No market data available for the requested range" };

                candles = candles.OrderBy(c => c.Timestamp).ToList();

                // Initialize simulation state
                var state = InitializeState(parameters);
                var availableCash = request.InitialCapital;
                var totalValue = request.InitialCapital;
                var trades = new List<SimulationTradeDto>();
                var equityCurve = new List<SimulationEquityPoint>();
                var peakEquity = request.InitialCapital;
                var maxDrawdown = 0m;

                // Simulation parameters
                var initialLot = parameters.GetValue("initialLot", 0.01m);
                var cutLossUSD = parameters.GetValue("cutLossUSD", 5000m);
                var pyramidEnabled = parameters.GetValue("pyramidEnabled", true);
                var martingaleEnabled = parameters.GetValue("martingaleEnabled", true);

                // Run simulation
                foreach (var candle in candles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Update price history for indicators
                    state.PriceHistory.Add(new PricePoint { Timestamp = candle.Timestamp, Price = candle.Close });
                    if (state.PriceHistory.Count > 200) // Keep enough for EMA calculation
                    {
                        state.PriceHistory.RemoveAt(0);
                    }

                    // Calculate indicators
                    var emaValue = CalculateEMA(state.PriceHistory, parameters.GetValue("emaPeriod", 200));
                    var rsiValue = CalculateRSI(state.PriceHistory, parameters.GetValue("rsiPeriod", 5));

                    // Simulate trading decisions
                    var trendDirection = DetermineTrend(candle.Close, emaValue, rsiValue, parameters);
                    
                    // Simulate position management
                    await SimulatePositionManagement(state, candle, trendDirection, parameters, trades, availableCash);

                    // Calculate current equity
                    var positionValue = state.Positions.Sum(p => p.Quantity * candle.Close);
                    totalValue = availableCash + positionValue;

                    // Update equity curve
                    equityCurve.Add(new SimulationEquityPoint
                    {
                        Timestamp = candle.Timestamp,
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

                    // Apply cut loss
                    if (request.InitialCapital - totalValue >= cutLossUSD)
                    {
                        // Close all positions at cut loss
                        foreach (var position in state.Positions.ToList())
                        {
                            var pnl = (candle.Close - position.EntryPrice) * position.Quantity;
                            trades.Add(new SimulationTradeDto
                            {
                                Timestamp = candle.Timestamp,
                                Side = position.Quantity > 0 ? "SELL" : "BUY",
                                Price = candle.Close,
                                Quantity = Math.Abs(position.Quantity),
                                Pnl = pnl
                            });
                            availableCash += position.Quantity * candle.Close;
                        }
                        state.Positions.Clear();
                        break; // Stop simulation after cut loss
                    }
                }

                // Final calculations
                var finalPrice = candles.Last().Close;
                var finalPositionValue = state.Positions.Sum(p => p.Quantity * finalPrice);
                var finalCapital = availableCash + finalPositionValue;
                var totalReturn = finalCapital - request.InitialCapital;
                var returnPercentage = request.InitialCapital > 0 ? totalReturn / request.InitialCapital * 100m : 0m;

                var winningTrades = trades.Count(t => t.Pnl > 0);
                var losingTrades = trades.Count(t => t.Pnl < 0);

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

        private async Task<AggressiveForexRuntimeState> LoadOrInitializeStateAsync(BotContext context, BotParameters parameters)
        {
            var state = await context.LoadStateAsyncFunc(typeof(AggressiveForexRuntimeState), CancellationToken.None) as AggressiveForexRuntimeState;
            return state ?? InitializeState(parameters);
        }

        private AggressiveForexRuntimeState InitializeState(BotParameters parameters)
        {
            return new AggressiveForexRuntimeState
            {
                Positions = new List<Position>(),
                PriceHistory = new List<PricePoint>(),
                LastEmaValue = 0,
                LastRsiValue = 50,
                TotalPyramidOrders = 0,
                TotalMartingaleOrders = 0,
                PeakEquity = 0,
                TrailingStopPrice = 0
            };
        }

        private async Task<decimal> GetCurrentPriceAsync(BotContext context)
        {
            try
            {
                var ohlcv = await context.MarketData.GetOhlcvAsync(
                    context.BaseAsset,
                    context.QuoteAsset,
                    DateTime.UtcNow.AddHours(-1),
                    DateTime.UtcNow,
                    "1h");

                return ohlcv.LastOrDefault()?.Close ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<decimal> CalculateEMAAsync(BotContext context, AggressiveForexRuntimeState state, decimal currentPrice, BotParameters parameters)
        {
            var period = parameters.GetValue("emaPeriod", 200);
            
            if (state.PriceHistory.Count < period)
            {
                return currentPrice; // Not enough data, return current price
            }

            return CalculateEMA(state.PriceHistory, period);
        }

        private decimal CalculateEMA(List<PricePoint> prices, int period)
        {
            if (prices.Count < period) return prices.LastOrDefault()?.Price ?? 0;

            var multiplier = 2m / (period + 1);
            var ema = prices.Take(period).Average(p => p.Price);

            for (int i = period; i < prices.Count; i++)
            {
                ema = (prices[i].Price * multiplier) + (ema * (1 - multiplier));
            }

            return ema;
        }

        private async Task<decimal> CalculateRSIAsync(BotContext context, AggressiveForexRuntimeState state, decimal currentPrice, BotParameters parameters)
        {
            var period = parameters.GetValue("rsiPeriod", 5);
            return CalculateRSI(state.PriceHistory, period);
        }

        private decimal CalculateRSI(List<PricePoint> prices, int period)
        {
            if (prices.Count < period + 1) return 50; // Neutral RSI

            var gains = new List<decimal>();
            var losses = new List<decimal>();

            for (int i = 1; i < prices.Count; i++)
            {
                var change = prices[i].Price - prices[i - 1].Price;
                gains.Add(change > 0 ? change : 0);
                losses.Add(change < 0 ? -change : 0);
            }

            if (gains.Count < period) return 50;

            var avgGain = gains.TakeLast(period).Average();
            var avgLoss = losses.TakeLast(period).Average();

            if (avgLoss == 0) return 100;

            var rs = avgGain / avgLoss;
            return 100 - (100 / (1 + rs));
        }

        private TrendDirection DetermineTrend(decimal currentPrice, decimal emaValue, decimal rsiValue, BotParameters parameters)
        {
            var rsiOverbought = parameters.GetValue("rsiOverbought", 80m);
            var rsiOversold = parameters.GetValue("rsiOversold", 20m);

            // Strong trend signals
            if (currentPrice > emaValue && rsiValue < rsiOverbought)
                return TrendDirection.Up;
            
            if (currentPrice < emaValue && rsiValue > rsiOversold)
                return TrendDirection.Down;

            return TrendDirection.Sideways;
        }

        private async Task ExecuteTradingLogicAsync(BotContext context, AggressiveForexRuntimeState state, decimal currentPrice, TrendDirection trend, BotParameters parameters)
        {
            var initialLot = parameters.GetValue("initialLot", 0.01m);
            var pyramidEnabled = parameters.GetValue("pyramidEnabled", true);
            var martingaleEnabled = parameters.GetValue("martingaleEnabled", true);

            // Check for new entry
            if (state.Positions.Count == 0 && trend != TrendDirection.Sideways)
            {
                await OpenInitialPosition(context, state, currentPrice, trend, initialLot);
            }
            // Check for pyramid opportunities (when in profit)
            else if (pyramidEnabled && CanPyramid(state, currentPrice, trend, parameters))
            {
                await AddPyramidPosition(context, state, currentPrice, trend, parameters);
            }
            // Check for martingale opportunities (when in loss)
            else if (martingaleEnabled && CanMartingale(state, currentPrice, parameters))
            {
                await AddMartingalePosition(context, state, currentPrice, parameters);
            }
        }

        private async Task OpenInitialPosition(BotContext context, AggressiveForexRuntimeState state, decimal currentPrice, TrendDirection trend, decimal lot)
        {
            var quantity = trend == TrendDirection.Up ? lot : -lot;
            
            var position = new Position
            {
                Id = Guid.NewGuid(),
                EntryPrice = currentPrice,
                Quantity = quantity,
                EntryTime = DateTime.UtcNow,
                Type = PositionType.Initial
            };

            state.Positions.Add(position);
            context.Logger.LogInfo("Trading", $"Opened initial {(trend == TrendDirection.Up ? "LONG" : "SHORT")} position: {Math.Abs(quantity)} @ ${currentPrice:F2}");
        }

        private bool CanPyramid(AggressiveForexRuntimeState state, decimal currentPrice, TrendDirection trend, BotParameters parameters)
        {
            var pyramidTriggerPercent = parameters.GetValue("pyramidTriggerPercent", 3.0m) / 100m;
            var maxPyramidOrders = parameters.GetValue("maxPyramidOrders", 10);

            if (state.TotalPyramidOrders >= maxPyramidOrders) return false;

            var totalPnL = state.Positions.Sum(p => (currentPrice - p.EntryPrice) * p.Quantity);
            var totalInvestment = state.Positions.Sum(p => Math.Abs(p.EntryPrice * p.Quantity));

            return totalInvestment > 0 && (totalPnL / totalInvestment) >= pyramidTriggerPercent;
        }

        private async Task AddPyramidPosition(BotContext context, AggressiveForexRuntimeState state, decimal currentPrice, TrendDirection trend, BotParameters parameters)
        {
            var pyramidLotMultiplier = parameters.GetValue("pyramidLotMultiplier", 1.0m);
            var lastPosition = state.Positions.LastOrDefault();
            var newLot = Math.Abs(lastPosition?.Quantity ?? 0.01m) * pyramidLotMultiplier;
            var quantity = trend == TrendDirection.Up ? newLot : -newLot;

            var position = new Position
            {
                Id = Guid.NewGuid(),
                EntryPrice = currentPrice,
                Quantity = quantity,
                EntryTime = DateTime.UtcNow,
                Type = PositionType.Pyramid
            };

            state.Positions.Add(position);
            state.TotalPyramidOrders++;
            
            context.Logger.LogInfo("Trading", $"Added pyramid position: {Math.Abs(quantity)} @ ${currentPrice:F2}");
        }

        private bool CanMartingale(AggressiveForexRuntimeState state, decimal currentPrice, BotParameters parameters)
        {
            var martingaleDistancePercent = parameters.GetValue("martingaleDistancePercent", 4.0m) / 100m;
            var maxMartingaleOrders = parameters.GetValue("maxMartingaleOrders", 10);

            if (state.TotalMartingaleOrders >= maxMartingaleOrders) return false;

            var totalPnL = state.Positions.Sum(p => (currentPrice - p.EntryPrice) * p.Quantity);
            var totalInvestment = state.Positions.Sum(p => Math.Abs(p.EntryPrice * p.Quantity));

            return totalInvestment > 0 && (totalPnL / totalInvestment) <= -martingaleDistancePercent;
        }

        private async Task AddMartingalePosition(BotContext context, AggressiveForexRuntimeState state, decimal currentPrice, BotParameters parameters)
        {
            var martingaleMultiplier = parameters.GetValue("martingaleMultiplier", 1.1m);
            var lastPosition = state.Positions.LastOrDefault();
            var newLot = Math.Abs(lastPosition?.Quantity ?? 0.01m) * martingaleMultiplier;
            
            // Martingale in same direction as losing positions
            var avgDirection = state.Positions.Sum(p => p.Quantity) > 0 ? 1 : -1;
            var quantity = newLot * avgDirection;

            var position = new Position
            {
                Id = Guid.NewGuid(),
                EntryPrice = currentPrice,
                Quantity = quantity,
                EntryTime = DateTime.UtcNow,
                Type = PositionType.Martingale
            };

            state.Positions.Add(position);
            state.TotalMartingaleOrders++;
            
            context.Logger.LogInfo("Trading", $"Added martingale position: {Math.Abs(quantity)} @ ${currentPrice:F2}");
        }

        private async Task ApplyRiskManagementAsync(BotContext context, AggressiveForexRuntimeState state, decimal currentPrice, BotParameters parameters)
        {
            var cutLossUSD = parameters.GetValue("cutLossUSD", 5000m);
            var trailingEnabled = parameters.GetValue("trailingEnabled", true);

            // Calculate current P&L
            var totalPnL = state.Positions.Sum(p => (currentPrice - p.EntryPrice) * p.Quantity);
            var totalValue = context.AllowedCapital + totalPnL;

            // Cut loss check
            if (context.AllowedCapital - totalValue >= cutLossUSD)
            {
                await CloseAllPositions(context, state, currentPrice, "Cut Loss Triggered");
                return;
            }

            // Trailing stop
            if (trailingEnabled && totalPnL > 0)
            {
                await ApplyTrailingStop(context, state, currentPrice, totalValue, parameters);
            }
        }

        private async Task ApplyTrailingStop(BotContext context, AggressiveForexRuntimeState state, decimal currentPrice, decimal totalValue, BotParameters parameters)
        {
            var trailingTriggerPercent = parameters.GetValue("trailingTriggerPercent", 10.0m) / 100m;
            var trailingDistancePercent = parameters.GetValue("trailingDistancePercent", 10.0m) / 100m;

            // Update peak equity
            if (totalValue > state.PeakEquity)
            {
                state.PeakEquity = totalValue;
                state.TrailingStopPrice = totalValue * (1 - trailingDistancePercent);
            }

            // Check if trailing stop is triggered
            if (state.PeakEquity > 0 && totalValue <= state.TrailingStopPrice)
            {
                await CloseAllPositions(context, state, currentPrice, "Trailing Stop Triggered");
            }
        }

        private async Task CloseAllPositions(BotContext context, AggressiveForexRuntimeState state, decimal currentPrice, string reason)
        {
            foreach (var position in state.Positions.ToList())
            {
                var pnl = (currentPrice - position.EntryPrice) * position.Quantity;
                context.Logger.LogInfo("Trading", $"Closed position: {Math.Abs(position.Quantity)} @ ${currentPrice:F2}, PnL: ${pnl:F2} ({reason})");
            }

            state.Positions.Clear();
            state.TotalPyramidOrders = 0;
            state.TotalMartingaleOrders = 0;
            state.PeakEquity = 0;
            state.TrailingStopPrice = 0;
        }

        private Task SimulatePositionManagement(AggressiveForexRuntimeState state, OhlcvData candle, TrendDirection trend, BotParameters parameters, List<SimulationTradeDto> trades, decimal availableCash)
        {
            var initialLot = parameters.GetValue("initialLot", 0.01m);
            var currentPrice = candle.Close;

            // Simulate new entries
            if (state.Positions.Count == 0 && trend != TrendDirection.Sideways)
            {
                var quantity = trend == TrendDirection.Up ? initialLot : -initialLot;
                var cost = Math.Abs(quantity * currentPrice);
                
                if (availableCash >= cost)
                {
                    state.Positions.Add(new Position
                    {
                        Id = Guid.NewGuid(),
                        EntryPrice = currentPrice,
                        Quantity = quantity,
                        EntryTime = candle.Timestamp,
                        Type = PositionType.Initial
                    });
                    availableCash -= cost;
                }
            }

            // Simulate pyramid/martingale logic (simplified for simulation)
            var totalPnL = state.Positions.Sum(p => (currentPrice - p.EntryPrice) * p.Quantity);
            var totalInvestment = state.Positions.Sum(p => Math.Abs(p.EntryPrice * p.Quantity));

            // Simple profit taking at 6% gain
            if (totalInvestment > 0 && (totalPnL / totalInvestment) >= 0.06m)
            {
                foreach (var position in state.Positions.ToList())
                {
                    var pnl = (currentPrice - position.EntryPrice) * position.Quantity;
                    trades.Add(new SimulationTradeDto
                    {
                        Timestamp = candle.Timestamp,
                        Side = position.Quantity > 0 ? "SELL" : "BUY",
                        Price = currentPrice,
                        Quantity = Math.Abs(position.Quantity),
                        Pnl = pnl
                    });
                }
                state.Positions.Clear();
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Data Classes

        public class AggressiveForexRuntimeState
        {
            public List<Position> Positions { get; set; } = new();
            public List<PricePoint> PriceHistory { get; set; } = new();
            public decimal LastEmaValue { get; set; }
            public decimal LastRsiValue { get; set; }
            public int TotalPyramidOrders { get; set; }
            public int TotalMartingaleOrders { get; set; }
            public decimal PeakEquity { get; set; }
            public decimal TrailingStopPrice { get; set; }
        }

        public class Position
        {
            public Guid Id { get; set; }
            public decimal EntryPrice { get; set; }
            public decimal Quantity { get; set; }
            public DateTime EntryTime { get; set; }
            public PositionType Type { get; set; }
        }

        public class PricePoint
        {
            public DateTime Timestamp { get; set; }
            public decimal Price { get; set; }
        }

        public enum PositionType
        {
            Initial,
            Pyramid,
            Martingale
        }

        public enum TrendDirection
        {
            Up,
            Down,
            Sideways
        }

        #endregion
    }
}
