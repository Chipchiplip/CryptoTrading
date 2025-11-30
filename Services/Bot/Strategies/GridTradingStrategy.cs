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
                ["gridLevels"] = 10, // ✅ Giảm từ 20 xuống 10
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
                // Get current market price first to set default parameters if needed
                var currentPriceForDefaults = await context.MarketData.GetMidPriceAsync(
                    context.BaseAsset, 
                    context.QuoteAsset, 
                    cancellationToken);

                // Get or set default parameters
                var lowerBound = parameters.GetValue<decimal>("lowerBound", 0);
                var upperBound = parameters.GetValue<decimal>("upperBound", 0);
                var gridLevels = parameters.GetValue<int>("gridLevels", 0);
                var orderSize = parameters.GetValue<decimal>("orderSize", 0);
                var capitalAllocation = parameters.GetValue<decimal>("capitalAllocation", 0);

                // Auto-fix missing parameters based on current price
                if (currentPriceForDefaults > 0)
                {
                    if (lowerBound <= 0) 
                    {
                        lowerBound = currentPriceForDefaults * 0.7m;
                        parameters.Values["lowerBound"] = Math.Round(lowerBound, 2);
                        context.Logger.LogInfo("Execution", $"Auto-set lowerBound to {lowerBound:F2} (70% of current price)");
                    }
                    if (upperBound <= 0) 
                    {
                        upperBound = currentPriceForDefaults * 1.5m;
                        parameters.Values["upperBound"] = Math.Round(upperBound, 2);
                        context.Logger.LogInfo("Execution", $"Auto-set upperBound to {upperBound:F2} (150% of current price)");
                    }
                }
                else
                {
                    // Fallback defaults if no price available
                    if (lowerBound <= 0) 
                    {
                        lowerBound = 2000m;
                        parameters.Values["lowerBound"] = lowerBound;
                    }
                    if (upperBound <= 0) 
                    {
                        upperBound = 5000m;
                        parameters.Values["upperBound"] = upperBound;
                    }
                }

                if (gridLevels < 2) 
                {
                    gridLevels = 10; // ✅ Giảm từ 20 xuống 10 để giảm số API calls
                    parameters.Values["gridLevels"] = gridLevels;
                    context.Logger.LogInfo("Execution", $"Auto-set gridLevels to {gridLevels}");
                }

                if (orderSize <= 0) 
                {
                    orderSize = 0.2m; // ✅ Tăng từ 0.1 lên 0.2 để giảm số orders và API calls
                    parameters.Values["orderSize"] = orderSize;
                    context.Logger.LogInfo("Execution", $"Auto-set orderSize to {orderSize}");
                }

                if (capitalAllocation <= 0) 
                {
                    capitalAllocation = context.AllowedCapital > 0 ? Math.Min(context.AllowedCapital, 10000m) : 10000m;
                    parameters.Values["capitalAllocation"] = capitalAllocation;
                    context.Logger.LogInfo("Execution", $"Auto-set capitalAllocation to {capitalAllocation:F2}");
                }

                // Validate final parameters
                if (upperBound <= lowerBound)
                {
                    upperBound = lowerBound * 1.5m;
                    parameters.Values["upperBound"] = Math.Round(upperBound, 2);
                    context.Logger.LogWarning("Execution", $"Auto-fixed upperBound to {upperBound:F2} (must be > lowerBound)");
                }

                // Load or initialize state
                var state = await context.LoadStateAsync<GridRuntimeState>(cancellationToken) 
                    ?? InitializeState(parameters);

                // Reconcile filled/cancelled orders to update inventory and free grid lines
                await ReconcileOrdersAsync(context, state, parameters, cancellationToken);

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
                context.Logger.LogInfo("Execution", $"Grid lines count: {state.GridLines.Count}, Inventory: {state.Inventory}, CashAvailable: {state.CashAvailable}");

                // Check orderType parameter để quyết định dùng MARKET hay LIMIT orders
                var orderType = parameters.GetValue<string>("orderType", "LIMIT")?.ToUpper() ?? "LIMIT";
                var orderSizeCheck = parameters.GetValue<decimal>("orderSize", 0);
                
                // Log orderType để debug
                context.Logger.LogInfo("Execution", $"orderType parameter: '{orderType}' (from parameters: '{parameters.GetValue<string>("orderType", "LIMIT")}')");
                
                var events = new List<BotEvent>();
                var ordersPlaced = 0;

                // Nếu orderType = "MARKET", chỉ tạo market orders, skip grid lines LIMIT orders
                if (orderType == "MARKET" && orderSizeCheck > 0)
                {
                    context.Logger.LogInfo("Execution", "Using MARKET orders mode (orderType=MARKET)");
                    
                    // QUAN TRỌNG: Cancel tất cả LIMIT orders cũ khi chuyển sang MARKET mode
                    // Vì MARKET orders execute ngay, không cần giữ LIMIT orders cũ
                    var limitOrdersToCancel = state.GridLines
                        .Where(l => l.HasPendingOrder)
                        .ToList();
                    
                    foreach (var line in limitOrdersToCancel)
                    {
                        try
                        {
                            var order = await context.TradingService.GetOrderAsync(line.OrderId!.Value, cancellationToken);
                            
                            // Chỉ cancel LIMIT orders, không cancel MARKET orders
                            if (order.Type == "LIMIT" && (order.Status == "NEW" || order.Status == "OPEN" || order.Status == "PARTIAL"))
                            {
                                await context.TradingService.CancelOrderAsync(line.OrderId!.Value, cancellationToken);
                                
                                // Refund cash cho BUY orders chưa filled
                                if (line.OrderSide == "BUY" && order.Filled < order.Quantity)
                                {
                                    var unfilledQty = order.Quantity - order.Filled;
                                    var refundAmount = (order.Price ?? line.Price) * unfilledQty;
                                    state.CashAvailable += refundAmount;
                                    context.Logger.LogInfo("OrderCancelled", 
                                        $"Cancelled LIMIT BUY order {order.Id} (refunded ${refundAmount:F2} for {unfilledQty} unfilled)");
                                }
                                
                                // Refund inventory cho SELL orders chưa filled
                                if (line.OrderSide == "SELL" && order.Filled < order.Quantity)
                                {
                                    var unfilledQty = order.Quantity - order.Filled;
                                    state.Inventory += unfilledQty;
                                    context.Logger.LogInfo("OrderCancelled", 
                                        $"Cancelled LIMIT SELL order {order.Id} (refunded {unfilledQty} inventory)");
                                }
                                
                                line.Reset();
                            }
                        }
                        catch (Exception ex)
                        {
                            context.Logger.LogWarning("CancelFailed", $"Failed to cancel LIMIT order {line.OrderId}: {ex.Message}");
                        }
                    }
                    
                    // Tạo market orders để trade trực tiếp với market ảo
                    await CreateMarketOrders(context, state, currentPrice, parameters, cancellationToken);
                    
                    // QUAN TRỌNG: Sau khi tạo MARKET orders, cần reconcile orders và update state
                    // Sau đó return sớm để KHÔNG tạo LIMIT orders
                    await ReconcileOrdersAsync(context, state, parameters, cancellationToken);
                    
                    // Update metrics
                    state.UpdateMetrics(currentPrice);
                    
                    // Save state
                    await context.SaveStateAsync(state, cancellationToken);
                    
                    context.Logger.LogInfo("Execution", "MARKET orders mode: Created market orders, skipping LIMIT orders logic");
                    
                    return BotExecutionResult.SuccessResult(DateTime.UtcNow.AddSeconds(parameters.GetValue<int>("refreshIntervalSeconds", 60)))
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
                else if (orderType == "LIMIT" || string.IsNullOrEmpty(orderType))
                {
                    context.Logger.LogInfo("Execution", "Using LIMIT orders mode (orderType=LIMIT or default)");
                    // Process grid lines với LIMIT orders
                }
                else
                {
                    context.Logger.LogWarning("Execution", $"Invalid orderType: {orderType}, defaulting to LIMIT");
                }

                // Log grid lines info for debugging
                if (state.GridLines.Any())
                {
                    var minPrice = state.GridLines.Min(l => l.Price);
                    var maxPrice = state.GridLines.Max(l => l.Price);
                    var buyableLinesCount = state.GridLines.Count(l => l.ShouldPlaceBuy(currentPrice) && !l.HasPendingOrder);
                    var sellableLinesCount = state.GridLines.Count(l => l.ShouldPlaceSell(currentPrice) && !l.HasPendingOrder);
                    var totalGridLines = state.GridLines.Count;
                    var linesBelowPrice = state.GridLines.Count(l => l.Price < currentPrice);
                    var linesWithPendingOrders = state.GridLines.Count(l => l.HasPendingOrder);
                    
                    context.Logger.LogInfo("Execution", 
                        $"Grid range: ${minPrice:F2} - ${maxPrice:F2}, Total lines: {totalGridLines}, " +
                        $"Lines below price ({currentPrice:F2}): {linesBelowPrice}, " +
                        $"Lines with pending orders: {linesWithPendingOrders}, " +
                        $"Buyable lines: {buyableLinesCount}, Sellable lines: {sellableLinesCount}");
                }
                else
                {
                    // Chỉ reinitialize nếu chưa có grid lines VÀ chưa có orders pending
                    // Nếu đã có orders pending, không reinitialize để tránh mất state
                    var hasPendingOrders = state.GridLines.Any(l => l.HasPendingOrder);
                    if (!hasPendingOrders)
                    {
                        context.Logger.LogWarning("Execution", "No grid lines initialized! Reinitializing...");
                        var newState = InitializeState(parameters);
                        // Giữ lại cash, inventory và average cost price từ state cũ nếu có
                        newState.CashAvailable = state.CashAvailable > 0 ? state.CashAvailable : newState.CashAvailable;
                        newState.Inventory = state.Inventory;
                        newState.AverageCostPrice = state.AverageCostPrice;
                        state = newState;
                    }
                    else
                    {
                        context.Logger.LogWarning("Execution", "No grid lines but has pending orders. Skipping reinitialize to preserve state.");
                    }
                }

                // Chỉ tạo LIMIT orders ở grid lines nếu orderType = "LIMIT"
                if (orderType != "MARKET")
                {
                    // Tạo BUY orders ở tất cả grid lines thấp hơn currentPrice (để match với limit orders mua của user)
                    var buyableLines = state.GridLines
                    .Where(line => line.Price < currentPrice && !line.HasPendingOrder)
                    .OrderByDescending(line => line.Price) // Ưu tiên giá gần market hơn
                    .ToList();

                if (buyableLines.Count == 0 && state.CashAvailable > 0)
                {
                    context.Logger.LogWarning("Execution", 
                        $"No buyable lines found! Current price: ${currentPrice:F2}, " +
                        $"Grid lines below price: {state.GridLines.Count(l => l.Price < currentPrice)}, " +
                        $"Lines with pending orders: {state.GridLines.Count(l => l.HasPendingOrder && l.Price < currentPrice)}");
                }

                var availableCash = state.CashAvailable;
                var maxBuyOrders = Math.Min(5, buyableLines.Count); // ✅ Giảm từ 10 xuống 5 để giảm số orders và API calls
                var buyOrderSize = parameters.GetValue<decimal>("orderSize");

                foreach (var line in buyableLines.Take(maxBuyOrders))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var orderCost = line.Price * buyOrderSize;
                    
                    // Kiểm tra có đủ vốn không
                    if (availableCash < orderCost)
                    {
                        context.Logger.LogInfo("OrderSkipped", 
                            $"Skip BUY at ${line.Price:F2}: insufficient cash (${availableCash:F2} < ${orderCost:F2})");
                        continue;
                    }

                    try
                    {
                        var orderId = await context.TradingService.PlaceOrderAsync(
                            new PlaceOrderRequest
                            {
                                Symbol = $"{context.BaseAsset}/{context.QuoteAsset}",
                                Side = "BUY",
                                Type = "LIMIT",
                                Quantity = buyOrderSize,
                                Price = line.Price
                            },
                            cancellationToken);

                        line.MarkPending(orderId, "BUY");
                        ordersPlaced++;
                        availableCash -= orderCost; // Trừ vốn đã dùng
                        state.CashAvailable = availableCash; // QUAN TRỌNG: Cập nhật state ngay để lần chạy sau không tạo thêm orders
                        
                        context.Logger.LogInfo("OrderPlaced", 
                            $"BUY order at ${line.Price:F2} qty={buyOrderSize} (cash: ${state.CashAvailable:F2} -> ${availableCash:F2})", 
                            new { orderId, line.Price, orderSize = buyOrderSize });
                        context.Logger.LogInfo("Signal", $"Grid BUY signal at ${line.Price:F2}", new { orderId, line.Price });
                        events.Add(new BotEvent
                        {
                            Type = "OrderPlaced",
                            Message = $"BUY limit order placed at ${line.Price:F2}",
                            Data = new Dictionary<string, object> { ["orderId"] = orderId, ["price"] = line.Price }
                        });
                    }
                    catch (Exception ex)
                    {
                        context.Logger.LogError("OrderFailed", $"Failed to place BUY order at ${line.Price:F2}: {ex.Message}");
                    }
                }
                } // End if orderType != "MARKET" for BUY orders

                // Process grid lines for SELL orders với giới hạn số lượng (chỉ khi dùng LIMIT)
                if (orderType != "MARKET")
                {
                    var sellableLines = state.GridLines
                        .Where(line => line.Price > currentPrice && !line.HasPendingOrder && line.ShouldPlaceSell(currentPrice))
                        .OrderBy(line => line.Price) // Ưu tiên giá thấp hơn (gần market hơn)
                        .ToList();
                    
                    var maxSellOrders = Math.Min(5, sellableLines.Count); // ✅ Giảm từ 10 xuống 5 để giảm số orders
                    var sellOrderSize = parameters.GetValue<decimal>("orderSize");
                    var availableInventory = state.Inventory;
                    
                    foreach (var line in sellableLines.Take(maxSellOrders))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (availableInventory < sellOrderSize)
                    {
                        context.Logger.LogWarning("OrderSkipped", $"Skip SELL at ${line.Price:F2}: insufficient virtual inventory ({availableInventory} < {sellOrderSize})");
                        break; // Không còn inventory, dừng luôn
                    }

                    try
                    {
                        // ✅ FIX: Check real balance trước khi SELL để tránh lỗi balance âm
                        // Nếu virtual inventory > 0 nhưng real balance = 0 hoặc âm, skip SELL
                        // TradingService sẽ throw exception nếu balance không đủ, catch và skip
                        var orderId = await context.TradingService.PlaceOrderAsync(
                            new PlaceOrderRequest
                            {
                                Symbol = $"{context.BaseAsset}/{context.QuoteAsset}",
                                Side = "SELL",
                                Type = "LIMIT",
                                Quantity = sellOrderSize,
                                Price = line.Price
                            },
                            cancellationToken);

                        line.MarkPending(orderId, "SELL");
                        ordersPlaced++;
                        availableInventory -= sellOrderSize; // Trừ inventory đã dùng
                        state.Inventory = availableInventory; // QUAN TRỌNG: Cập nhật state ngay để lần chạy sau không tạo thêm orders
                        
                        context.Logger.LogInfo("OrderPlaced", $"SELL order at ${line.Price:F2} (inventory: {state.Inventory} -> {availableInventory})", new { orderId, line.Price });
                        context.Logger.LogInfo("Signal", $"Grid SELL signal at ${line.Price:F2}", new { orderId, line.Price });
                        events.Add(new BotEvent
                        {
                            Type = "OrderPlaced",
                            Message = $"SELL limit order placed at ${line.Price:F2}",
                            Data = new Dictionary<string, object> { ["orderId"] = orderId, ["price"] = line.Price }
                        });
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Insufficient balance"))
                    {
                        // ✅ FIX: Nếu real balance không đủ, skip SELL và log warning
                        // Điều này xảy ra khi virtual inventory > 0 nhưng real balance = 0 hoặc âm
                        context.Logger.LogWarning("OrderSkipped", 
                            $"Skip SELL at ${line.Price:F2}: Real balance insufficient. " +
                            $"Virtual inventory: {availableInventory}, Error: {ex.Message}. " +
                            $"This indicates inventory/balance mismatch. Please check balance reconciliation.");
                        // Không break, tiếp tục với line tiếp theo
                        continue;
                    }
                    catch (Exception ex)
                    {
                        context.Logger.LogError("OrderFailed", $"Failed to place SELL order: {ex.Message}");
                        // Không break, tiếp tục với line tiếp theo
                        continue;
                    }
                }
                } // End if orderType != "MARKET" for SELL orders
                
                // Check for orders that should be cancelled (price moved away)
                foreach (var line in state.GridLines.Where(l => l.HasPendingOrder))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    if (line.ShouldCancel(currentPrice))
                    {
                        try
                        {
                            // Lấy thông tin order để biết đã filled bao nhiêu
                            var order = await context.TradingService.GetOrderAsync(line.OrderId!.Value, cancellationToken);
                            
                            // QUAN TRỌNG: Kiểm tra order đã filled chưa trước khi cancel
                            // Nếu order đã filled (FilledQty >= Quantity) hoặc status là FILLED, không cancel
                            var isFullyFilled = order.Filled > 0 && order.Quantity > 0 && 
                                               (order.Filled >= order.Quantity || Math.Abs(order.Filled - order.Quantity) < 0.0001m);
                            
                            if (order.Status == "FILLED" || isFullyFilled)
                            {
                                // Order đã filled, chỉ reset grid line thôi, không cancel
                                context.Logger.LogInfo("OrderFilled", 
                                    $"Order {order.Id} already filled ({order.Filled}/{order.Quantity}), skipping cancel. Resetting grid line.");
                                line.Reset();
                                continue;
                            }
                            
                            // Chỉ cancel nếu order chưa filled hoặc chỉ filled một phần
                            var cancelOrderSize = parameters.GetValue<decimal>("orderSize");
                            var unfilledQty = order.Quantity - order.Filled;
                            
                            await context.TradingService.CancelOrderAsync(line.OrderId!.Value, cancellationToken);
                            
                            // Cộng lại cash cho phần chưa filled (vì đã trừ khi tạo order)
                            if (line.OrderSide == "BUY" && unfilledQty > 0)
                            {
                                var refundAmount = line.Price * unfilledQty;
                                state.CashAvailable += refundAmount;
                                context.Logger.LogInfo("OrderCancelled", 
                                    $"Cancelled BUY order at ${line.Price:F2}, refunded ${refundAmount:F2} for {unfilledQty} unfilled (cash: {state.CashAvailable - refundAmount:F2} -> {state.CashAvailable:F2})");
                            }
                            // SELL order không cần cộng lại inventory vì inventory đã được trừ khi tạo order
                            
                            line.Reset();
                            
                            context.Logger.LogInfo("OrderCancelled", $"Cancelled {line.OrderSide} order at ${line.Price:F2}");
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

        private async Task ReconcileOrdersAsync(BotContext context, GridRuntimeState state, BotParameters parameters, CancellationToken cancellationToken)
        {
            var orderSize = parameters.GetValue<decimal>("orderSize");

            foreach (var line in state.GridLines.Where(l => l.HasPendingOrder).ToList())
            {
                try
                {
                    var order = await context.TradingService.GetOrderAsync(line.OrderId!.Value, cancellationToken);

                    // Calculate newly filled quantity (only reconcile the difference)
                    var previouslyReconciledQty = line.FilledQty;
                    var currentFilledQty = order.Filled;
                    var newlyFilledQty = currentFilledQty - previouslyReconciledQty;

                    // Update filled quantity for visibility
                    line.FilledQty = currentFilledQty;

                    // Only process if there's newly filled quantity
                    if (newlyFilledQty > 0)
                    {
                        var fillPrice = order.Price ?? line.Price;
                        if (line.OrderSide == "BUY")
                        {
                            state.Inventory += newlyFilledQty;
                            
                            // Cập nhật giá mua trung bình (weighted average)
                            if (state.Inventory > 0)
                            {
                                var totalCost = (state.Inventory - newlyFilledQty) * state.AverageCostPrice + (newlyFilledQty * fillPrice);
                                state.AverageCostPrice = totalCost / state.Inventory;
                            }
                            else
                            {
                                state.AverageCostPrice = fillPrice;
                            }
                            
                            context.Logger.LogInfo("OrderFilled", 
                                $"BUY filled {newlyFilledQty} more at ${fillPrice:F2} (total: {currentFilledQty}/{order.Quantity}, previously reconciled: {previouslyReconciledQty}, avg cost: ${state.AverageCostPrice:F2})");
                        }
                        else if (line.OrderSide == "SELL")
                        {
                            // Check if we have enough inventory before subtracting
                            if (state.Inventory < newlyFilledQty)
                            {
                                context.Logger.LogError("InventoryError", 
                                    $"Cannot reconcile SELL order: insufficient inventory. " +
                                    $"Trying to subtract {newlyFilledQty} but only have {state.Inventory}. " +
                                    $"Order: {order.Id}, Filled: {currentFilledQty}, Previously reconciled: {previouslyReconciledQty}. " +
                                    $"This may indicate a double-reconciliation issue. " +
                                    $"Skipping this reconciliation to prevent negative inventory.");
                                
                                // Don't subtract if it would make inventory negative
                                // This prevents the -971 ETH issue
                                continue;
                            }
                            
                            state.Inventory -= newlyFilledQty;
                            // KHÔNG cộng cash ở đây vì cash thực tế đã được cộng bởi trading service khi order filled
                            // Chỉ cần cập nhật inventory
                            context.Logger.LogInfo("OrderFilled", 
                                $"SELL filled {newlyFilledQty} more at ${fillPrice:F2} (total: {currentFilledQty}/{order.Quantity}, previously reconciled: {previouslyReconciledQty})");
                        }
                    }
                    // Warn if order has been filled but we haven't reconciled it yet (possible state loss)
                    // Status hợp lệ: NEW, PARTIAL, FILLED, CANCELED, REJECTED
                    else if (currentFilledQty > 0 && previouslyReconciledQty == 0 && order.Status != "NEW")
                    {
                        // Order was filled but we don't have record of reconciling it
                        // This could happen if state was reset. Log warning but don't reconcile to avoid double-counting
                        context.Logger.LogWarning("ReconciliationWarning", 
                            $"Order {order.Id} has {currentFilledQty} filled but previouslyReconciledQty is 0. " +
                            $"This may indicate state was reset. Skipping reconciliation to prevent double-counting.");
                    }

                    // Check if order is fully filled (even if status is still "NEW" or "PARTIAL")
                    // Status hợp lệ: NEW, PARTIAL, FILLED, CANCELED, REJECTED
                    // Sử dụng tolerance nhỏ để tránh floating point issues
                    var isFullyFilled = currentFilledQty > 0 && order.Quantity > 0 && 
                                       (currentFilledQty >= order.Quantity || Math.Abs(currentFilledQty - order.Quantity) < 0.0001m);
                    var effectiveStatus = isFullyFilled ? "FILLED" : order.Status;

                    if (effectiveStatus is "FILLED" or "CANCELED" or "REJECTED")
                    {
                        // Nếu order bị cancel/reject, cộng lại cash cho phần chưa filled
                        if ((effectiveStatus == "CANCELED" || effectiveStatus == "REJECTED") && line.OrderSide == "BUY")
                        {
                            var unfilledQty = order.Quantity - currentFilledQty;
                            if (unfilledQty > 0)
                            {
                                var refundAmount = (order.Price ?? line.Price) * unfilledQty;
                                state.CashAvailable += refundAmount;
                                context.Logger.LogInfo("OrderCancelled", 
                                    $"Order {order.Id} cancelled/rejected, refunded ${refundAmount:F2} for {unfilledQty} unfilled");
                            }
                        }
                        
                        // Free the grid line for next orders
                        line.Reset();
                    }
                    else if (order.Status == "PARTIAL" && currentFilledQty >= orderSize)
                    {
                        // If partially filled up to target size, treat as filled
                        line.Reset();
                    }
                }
                catch (Exception ex)
                {
                    context.Logger.LogError("OrderCheckFailed", $"Failed to check order {line.OrderId}: {ex.Message}");
                }
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
            // Lấy parameters với default values nếu không có
            var lowerBound = parameters.GetValue<decimal>("lowerBound", 0);
            var upperBound = parameters.GetValue<decimal>("upperBound", 0);
            var gridLevels = parameters.GetValue<int>("gridLevels", 0);
            var capitalAllocation = parameters.GetValue<decimal>("capitalAllocation", 0);

            // Nếu không có parameters, sử dụng default values dựa trên giá thị trường hiện tại
            // (Sẽ được set trong ExecuteAsync nếu cần)
            if (lowerBound <= 0 || upperBound <= 0 || gridLevels < 2)
            {
                // Default values nếu bot được tạo mà không có parameters
                lowerBound = lowerBound <= 0 ? 2000 : lowerBound;
                upperBound = upperBound <= 0 ? 5000 : upperBound;
                gridLevels = gridLevels < 2 ? 20 : gridLevels;
            }

            if (upperBound <= lowerBound)
            {
                upperBound = lowerBound * 1.5m; // Tự động set upperBound nếu không hợp lệ
            }

            if (gridLevels < 2)
            {
                gridLevels = 20; // Default grid levels
            }

            var step = gridLevels > 1 ? (upperBound - lowerBound) / (gridLevels - 1) : (upperBound - lowerBound);
            if (step <= 0)
            {
                step = 100; // Default step
            }

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
                CashAvailable = capitalAllocation > 0 ? capitalAllocation : 10000, // Default capital
                Inventory = 0,
                AverageCostPrice = 0,
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

        private Task ExpandGridIfNeeded(
            BotContext context,
            GridRuntimeState state,
            decimal currentPrice,
            BotParameters parameters,
            CancellationToken cancellationToken)
        {
            try
            {
                var upperBound = parameters.GetValue<decimal>("upperBound", 0);
                var lowerBound = parameters.GetValue<decimal>("lowerBound", 0);
                var gridLevels = parameters.GetValue<int>("gridLevels", 0);
                
                // Kiểm tra parameters hợp lệ
                if (upperBound <= 0 || lowerBound <= 0 || gridLevels <= 1)
                {
                    context.Logger.LogWarning("Execution", 
                        $"Invalid grid parameters: lowerBound={lowerBound}, upperBound={upperBound}, gridLevels={gridLevels}. Skipping grid expansion.");
                    return Task.CompletedTask;
                }

                if (upperBound <= lowerBound)
                {
                    context.Logger.LogWarning("Execution", 
                        $"Invalid grid range: upperBound ({upperBound}) <= lowerBound ({lowerBound}). Skipping grid expansion.");
                    return Task.CompletedTask;
                }
                
                // Nếu currentPrice gần upperBound, mở rộng grid range lên cao hơn
                // Để có thể match với limit orders ở giá cao hơn (ví dụ: $4,000)
                var priceRange = upperBound - lowerBound;
                if (priceRange <= 0)
                {
                    return Task.CompletedTask;
                }

                var expansionThreshold = upperBound - (priceRange * 0.2m); // Khi giá cách upperBound < 20% range

                if (currentPrice >= expansionThreshold && state.Inventory > 0)
                {
                    // Mở rộng grid lên cao hơn (ví dụ: thêm 50% range)
                    var newUpperBound = Math.Max(upperBound, currentPrice * 1.5m);
                    var maxGridPrice = state.GridLines.Any() ? state.GridLines.Max(l => l.Price) : upperBound;
                    
                    if (newUpperBound > maxGridPrice && gridLevels > 1)
                    {
                        // Thêm grid lines mới ở mức giá cao hơn
                        var step = (upperBound - lowerBound) / (gridLevels - 1);
                        if (step <= 0)
                        {
                            context.Logger.LogWarning("Execution", "Grid step is zero or negative. Skipping expansion.");
                            return Task.CompletedTask;
                        }

                        var newLines = new List<GridLine>();
                        var startPrice = maxGridPrice + step;
                        
                        for (var price = startPrice; price <= newUpperBound; price += step)
                        {
                            var roundedPrice = Math.Round(price, 2);
                            if (roundedPrice > maxGridPrice && !state.GridLines.Any(l => Math.Abs(l.Price - roundedPrice) < 0.01m))
                            {
                                newLines.Add(new GridLine
                                {
                                    Price = roundedPrice,
                                    Index = state.GridLines.Count + newLines.Count
                                });
                            }
                        }

                        if (newLines.Any())
                        {
                            state.GridLines.AddRange(newLines);
                            context.Logger.LogInfo("Execution", 
                                $"Expanded grid range: added {newLines.Count} new grid lines up to ${newUpperBound:F2} to match limit orders");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError("Execution", $"Error expanding grid: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        private async Task CheckAndCreateMatchingBuyOrders(
            BotContext context,
            GridRuntimeState state,
            decimal currentPrice,
            BotParameters parameters,
            CancellationToken cancellationToken)
        {
            try
            {
                var orderSize = parameters.GetValue<decimal>("orderSize", 0);
                
                // Kiểm tra orderSize hợp lệ
                if (orderSize <= 0)
                {
                    return;
                }

                // Kiểm tra có đủ vốn không
                var orderCost = currentPrice * orderSize * 0.8m; // Ước tính 80% giá hiện tại
                if (state.CashAvailable < orderCost)
                {
                    context.Logger.LogInfo("Execution", 
                        $"Insufficient cash (${state.CashAvailable:F2}) to create BUY orders. Need ~${orderCost:F2}");
                    return;
                }

                // Tạo BUY orders ở tất cả grid lines thấp hơn currentPrice nếu có vốn
                // Điều này đảm bảo bot sẽ match với limit orders mua của user (ví dụ: $2800 khi market $3000)
                var buyableLines = state.GridLines
                    .Where(line => line.Price < currentPrice && !line.HasPendingOrder)
                    .OrderByDescending(line => line.Price) // Từ cao xuống thấp (ưu tiên giá gần market hơn)
                    .ToList();

                var availableCash = state.CashAvailable;
                var ordersCreated = 0;
                var maxOrders = Math.Min(20, (int)(availableCash / (currentPrice * orderSize * 0.5m))); // Ước tính số orders có thể tạo

                foreach (var line in buyableLines)
                {
                    var lineOrderCost = line.Price * orderSize;
                    if (availableCash < lineOrderCost) break;
                    if (ordersCreated >= maxOrders) break;

                    try
                    {
                        var orderId = await context.TradingService.PlaceOrderAsync(
                            new PlaceOrderRequest
                            {
                                Symbol = $"{context.BaseAsset}/{context.QuoteAsset}",
                                Side = "BUY",
                                Type = "LIMIT",
                                Quantity = orderSize,
                                Price = line.Price
                            },
                            cancellationToken);

                        line.MarkPending(orderId, "BUY");
                        availableCash -= lineOrderCost;
                        ordersCreated++;

                        context.Logger.LogInfo("OrderPlaced", 
                            $"BUY order at ${line.Price:F2} to match limit orders (cash: ${state.CashAvailable:F2})", 
                            new { orderId, line.Price, cash = state.CashAvailable });
                        context.Logger.LogInfo("Signal", 
                            $"Grid BUY signal at ${line.Price:F2} to match limit orders", 
                            new { orderId, line.Price });
                    }
                    catch (Exception ex)
                    {
                        context.Logger.LogError("OrderFailed", $"Failed to place BUY order at ${line.Price:F2}: {ex.Message}");
                    }
                }

                if (ordersCreated > 0)
                {
                    context.Logger.LogInfo("Execution", 
                        $"Created {ordersCreated} BUY orders to match limit orders. Remaining cash: ${availableCash:F2}");
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError("Execution", $"Error checking for matching buy orders: {ex.Message}");
            }
        }

        private async Task CreateMarketOrders(
            BotContext context,
            GridRuntimeState state,
            decimal currentPrice,
            BotParameters parameters,
            CancellationToken cancellationToken)
        {
            try
            {
                var orderSize = parameters.GetValue<decimal>("orderSize", 0);
                if (orderSize <= 0) return;

                // Strategy: Bot trade trực tiếp với market ảo bằng market orders
                // Market orders sẽ execute ngay lập tức với market price hiện tại
                var maxBuyOrders = parameters.GetValue<int>("maxBuyOrders", 3);
                var maxSellOrders = parameters.GetValue<int>("maxSellOrders", 3);
                
                // Giới hạn số lượng orders để tránh database overload
                // Nếu đã có quá nhiều orders, không tạo thêm
                var maxTotalOrders = 10; // Giới hạn tổng số orders để tránh timeout
                var totalPendingOrders = state.GridLines.Count(l => l.HasPendingOrder);

                // Tạo BUY market orders (execute ngay với market price)
                var availableCash = state.CashAvailable;
                var buyOrdersCreated = 0;
                var estimatedOrderCost = currentPrice * orderSize; // Ước tính cost

                // Chỉ tạo BUY orders nếu có đủ vốn và chưa có quá nhiều orders
                var existingBuyOrders = state.GridLines.Count(l => l.HasPendingOrder && l.OrderSide == "BUY");
                
                if (availableCash >= estimatedOrderCost && existingBuyOrders < maxBuyOrders && totalPendingOrders < maxTotalOrders)
                {
                    var ordersToCreate = Math.Min(maxBuyOrders - existingBuyOrders, 
                        Math.Min(maxTotalOrders - totalPendingOrders,
                        (int)(availableCash / estimatedOrderCost)));

                    for (int i = 0; i < ordersToCreate && availableCash >= estimatedOrderCost; i++)
                    {
                        try
                        {
                            // Tạo MARKET order để trade trực tiếp với market ảo
                            // Market order sẽ execute ngay với market price hiện tại
                            var orderId = await context.TradingService.PlaceOrderAsync(
                                new PlaceOrderRequest
                                {
                                    Symbol = $"{context.BaseAsset}/{context.QuoteAsset}",
                                    Side = "BUY",
                                    Type = "MARKET", // Market order - trade trực tiếp với market ảo
                                    Quantity = orderSize
                                    // Không cần Price cho market orders - sẽ execute với market price
                                },
                                cancellationToken);

                            // Tạo grid line để track (dùng currentPrice vì market order execute với giá này)
                            var gridLine = new GridLine
                            {
                                Price = currentPrice, // Track execution price
                                Index = state.GridLines.Count,
                                OrderId = orderId,
                                OrderSide = "BUY",
                                OrderCreatedAt = DateTime.UtcNow
                            };
                            state.GridLines.Add(gridLine);

                            availableCash -= estimatedOrderCost; // Trừ vốn (ước tính)
                            state.CashAvailable = availableCash;
                            buyOrdersCreated++;

                            context.Logger.LogInfo("OrderPlaced", 
                                $"BUY market order placed (will execute at market price ~${currentPrice:F2}, qty: {orderSize}, cash: ${state.CashAvailable:F2} -> ${availableCash:F2})", 
                                new { orderId, currentPrice });
                            
                            // Thêm delay nhỏ giữa các orders để tránh database overload
                            if (i < ordersToCreate - 1) // Không delay cho order cuối cùng
                            {
                                await Task.Delay(100, cancellationToken); // 100ms delay
                            }
                        }
                        catch (Exception ex)
                        {
                            context.Logger.LogError("OrderFailed", $"Failed to place BUY market order: {ex.Message}");
                            // Nếu có lỗi database timeout, dừng tạo orders
                            if (ex.Message.Contains("Timeout") || ex.Message.Contains("timeout"))
                            {
                                context.Logger.LogWarning("Execution", "Database timeout detected, stopping order creation");
                                break;
                            }
                        }
                    }
                }
                else if (totalPendingOrders >= maxTotalOrders)
                {
                    context.Logger.LogWarning("Execution", 
                        $"Skipping BUY orders: Too many pending orders ({totalPendingOrders} >= {maxTotalOrders})");
                }

                // Tạo SELL market orders (execute ngay với market price)
                var availableInventory = state.Inventory;
                var sellOrdersCreated = 0;
                var existingSellOrders = state.GridLines.Count(l => l.HasPendingOrder && l.OrderSide == "SELL");
                
                // Kiểm tra lại total pending orders (có thể đã thay đổi sau khi tạo BUY orders)
                totalPendingOrders = state.GridLines.Count(l => l.HasPendingOrder);

                // Chỉ tạo SELL orders nếu có inventory và chưa có quá nhiều orders
                // QUAN TRỌNG: Giữ lại một phần inventory (reserve) để có thể bán tiếp
                // Chỉ bán tối đa 70% inventory, giữ lại 30% để có thể bán tiếp
                var reserveInventoryRatio = 0.3m; // Giữ lại 30% inventory
                var sellableInventory = availableInventory * (1 - reserveInventoryRatio); // Có thể bán 70%
                
                if (sellableInventory >= orderSize && availableInventory >= orderSize && existingSellOrders < maxSellOrders && totalPendingOrders < maxTotalOrders)
                {
                    // Tính số orders có thể tạo dựa trên sellable inventory (70% của total)
                    var maxSellableOrders = (int)(sellableInventory / orderSize);
                    var ordersToCreate = Math.Min(maxSellOrders - existingSellOrders, 
                        Math.Min(maxTotalOrders - totalPendingOrders,
                        maxSellableOrders));

                    for (int i = 0; i < ordersToCreate && availableInventory >= orderSize; i++)
                    {
                        try
                        {
                            // ✅ FIX: Tạo MARKET order với validation real balance
                            // TradingService sẽ check real balance và throw exception nếu không đủ
                            var orderId = await context.TradingService.PlaceOrderAsync(
                                new PlaceOrderRequest
                                {
                                    Symbol = $"{context.BaseAsset}/{context.QuoteAsset}",
                                    Side = "SELL",
                                    Type = "MARKET", // Market order - trade trực tiếp với market ảo
                                    Quantity = orderSize
                                    // Không cần Price cho market orders - sẽ execute với market price
                                },
                                cancellationToken);

                            // Tạo grid line để track
                            var gridLine = new GridLine
                            {
                                Price = currentPrice, // Track execution price
                                Index = state.GridLines.Count,
                                OrderId = orderId,
                                OrderSide = "SELL",
                                OrderCreatedAt = DateTime.UtcNow
                            };
                            state.GridLines.Add(gridLine);

                            availableInventory -= orderSize;
                            state.Inventory = availableInventory;
                            sellOrdersCreated++;

                            context.Logger.LogInfo("OrderPlaced", 
                                $"SELL market order placed (will execute at market price ~${currentPrice:F2}, qty: {orderSize}, inventory: {state.Inventory} -> {availableInventory})", 
                                new { orderId, currentPrice });
                            
                            // Thêm delay nhỏ giữa các orders để tránh database overload
                            if (i < ordersToCreate - 1) // Không delay cho order cuối cùng
                            {
                                await Task.Delay(100, cancellationToken); // 100ms delay
                            }
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("Insufficient balance"))
                        {
                            // ✅ FIX: Nếu real balance không đủ, skip SELL và log warning
                            // Điều này xảy ra khi virtual inventory > 0 nhưng real balance = 0 hoặc âm
                            context.Logger.LogWarning("OrderSkipped", 
                                $"Skip SELL market order: Real balance insufficient. " +
                                $"Virtual inventory: {availableInventory}, Error: {ex.Message}. " +
                                $"This indicates inventory/balance mismatch. Stopping SELL order creation.");
                            // Break để dừng tạo SELL orders vì real balance không đủ
                            break;
                        }
                        catch (Exception ex)
                        {
                            context.Logger.LogError("OrderFailed", $"Failed to place SELL market order: {ex.Message}");
                            // Nếu có lỗi database timeout, dừng tạo orders
                            if (ex.Message.Contains("Timeout") || ex.Message.Contains("timeout"))
                            {
                                context.Logger.LogWarning("Execution", "Database timeout detected, stopping order creation");
                                break;
                            }
                            // Nếu là lỗi balance, break để dừng tạo orders
                            if (ex.Message.Contains("Insufficient balance") || ex.Message.Contains("balance"))
                            {
                                context.Logger.LogWarning("Execution", "Balance error detected, stopping SELL order creation");
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (totalPendingOrders >= maxTotalOrders)
                    {
                        context.Logger.LogWarning("Execution", 
                            $"Skipping SELL orders: Too many pending orders ({totalPendingOrders} >= {maxTotalOrders})");
                    }
                    else if (sellableInventory < orderSize)
                    {
                        context.Logger.LogInfo("Execution", 
                            $"Skipping SELL orders: Insufficient sellable inventory ({sellableInventory:F4} < {orderSize}). " +
                            $"Total inventory: {availableInventory:F4}, Reserved: {availableInventory * reserveInventoryRatio:F4} (30%)");
                    }
                    else if (availableInventory < orderSize)
                    {
                        context.Logger.LogInfo("Execution", 
                            $"Skipping SELL orders: Insufficient inventory ({availableInventory:F4} < {orderSize})");
                    }
                }

                if (buyOrdersCreated > 0 || sellOrdersCreated > 0)
                {
                    context.Logger.LogInfo("Execution", 
                        $"Created {buyOrdersCreated} BUY and {sellOrdersCreated} SELL market orders. " +
                        $"Remaining cash: ${availableCash:F2}, inventory: {availableInventory}");
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError("Execution", $"Error creating market orders: {ex.Message}");
            }
        }

        private async Task CreateBuyOrdersFromOrderBook(
            BotContext context,
            GridRuntimeState state,
            decimal currentPrice,
            OrderBookDto orderBook,
            BotParameters parameters,
            CancellationToken cancellationToken)
        {
            try
            {
                var orderSize = parameters.GetValue<decimal>("orderSize", 0);
                if (orderSize <= 0) return;

                // Tạo BUY orders để match với SELL orders (asks) ở giá cao hơn market
                // Asks là SELL orders của users khác - bot sẽ mua ở giá đó
                var availableAsks = orderBook.Asks
                    .Where(ask => ask.Price > currentPrice) // Chỉ lấy asks cao hơn market
                    .OrderBy(ask => ask.Price) // Từ thấp đến cao (gần market nhất trước)
                    .Take(10) // Giới hạn 10 orders
                    .ToList();

                if (!availableAsks.Any())
                {
                    context.Logger.LogInfo("Execution", "No SELL orders (asks) above market price to match");
                    return;
                }

                var availableCash = state.CashAvailable;
                var ordersCreated = 0;

                foreach (var ask in availableAsks)
                {
                    if (availableCash <= 0 || ordersCreated >= 10) break;

                    var orderCost = ask.Price * orderSize;
                    if (availableCash < orderCost)
                    {
                        context.Logger.LogInfo("OrderSkipped", 
                            $"Skip BUY at ${ask.Price:F2}: insufficient cash (${availableCash:F2} < ${orderCost:F2})");
                        continue;
                    }

                    // Kiểm tra xem đã có order ở giá này chưa
                    var existingOrder = state.GridLines.FirstOrDefault(l => 
                        Math.Abs(l.Price - ask.Price) < 0.01m && l.HasPendingOrder);
                    
                    if (existingOrder != null)
                    {
                        continue; // Đã có order ở giá này rồi
                    }

                    try
                    {
                        var orderId = await context.TradingService.PlaceOrderAsync(
                            new PlaceOrderRequest
                            {
                                Symbol = $"{context.BaseAsset}/{context.QuoteAsset}",
                                Side = "BUY",
                                Type = "LIMIT",
                                Quantity = orderSize,
                                Price = ask.Price // Match với giá SELL order của user
                            },
                            cancellationToken);

                        // Tạo grid line mới hoặc tìm grid line gần nhất
                        var gridLine = state.GridLines.FirstOrDefault(l => Math.Abs(l.Price - ask.Price) < 0.01m);
                        if (gridLine == null)
                        {
                            gridLine = new GridLine
                            {
                                Price = ask.Price,
                                Index = state.GridLines.Count
                            };
                            state.GridLines.Add(gridLine);
                        }

                        gridLine.MarkPending(orderId, "BUY");
                        availableCash -= orderCost;
                        state.CashAvailable = availableCash;
                        ordersCreated++;

                        context.Logger.LogInfo("OrderPlaced", 
                            $"BUY order at ${ask.Price:F2} to match SELL order (quantity: {ask.Quantity}, cash: ${state.CashAvailable:F2} -> ${availableCash:F2})", 
                            new { orderId, ask.Price });
                    }
                    catch (Exception ex)
                    {
                        context.Logger.LogError("OrderFailed", $"Failed to place BUY order at ${ask.Price:F2}: {ex.Message}");
                    }
                }

                if (ordersCreated > 0)
                {
                    context.Logger.LogInfo("Execution", 
                        $"Created {ordersCreated} BUY orders from order book. Remaining cash: ${availableCash:F2}");
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError("Execution", $"Error creating BUY orders from order book: {ex.Message}");
            }
        }

        private async Task CreateSellOrdersFromOrderBook(
            BotContext context,
            GridRuntimeState state,
            decimal currentPrice,
            OrderBookDto orderBook,
            BotParameters parameters,
            CancellationToken cancellationToken)
        {
            try
            {
                var orderSize = parameters.GetValue<decimal>("orderSize", 0);
                if (orderSize <= 0) return;

                if (state.Inventory < orderSize)
                {
                    context.Logger.LogInfo("Execution", 
                        $"Insufficient inventory ({state.Inventory}) to create SELL orders. Need {orderSize}");
                    return;
                }

                // Tạo SELL orders để match với BUY orders (bids) ở giá thấp hơn market
                // Bids là BUY orders của users khác - bot sẽ bán ở giá đó
                var availableBids = orderBook.Bids
                    .Where(bid => bid.Price < currentPrice) // Chỉ lấy bids thấp hơn market
                    .OrderByDescending(bid => bid.Price) // Từ cao xuống thấp (gần market nhất trước)
                    .Take(10) // Giới hạn 10 orders
                    .ToList();

                if (!availableBids.Any())
                {
                    context.Logger.LogInfo("Execution", "No BUY orders (bids) below market price to match");
                    return;
                }

                var availableInventory = state.Inventory;
                var ordersCreated = 0;

                foreach (var bid in availableBids)
                {
                    if (availableInventory < orderSize || ordersCreated >= 10) break;

                    // Kiểm tra xem đã có order ở giá này chưa
                    var existingOrder = state.GridLines.FirstOrDefault(l => 
                        Math.Abs(l.Price - bid.Price) < 0.01m && l.HasPendingOrder);
                    
                    if (existingOrder != null)
                    {
                        continue; // Đã có order ở giá này rồi
                    }

                    try
                    {
                        var orderId = await context.TradingService.PlaceOrderAsync(
                            new PlaceOrderRequest
                            {
                                Symbol = $"{context.BaseAsset}/{context.QuoteAsset}",
                                Side = "SELL",
                                Type = "LIMIT",
                                Quantity = orderSize,
                                Price = bid.Price // Match với giá BUY order của user
                            },
                            cancellationToken);

                        // Tạo grid line mới hoặc tìm grid line gần nhất
                        var gridLine = state.GridLines.FirstOrDefault(l => Math.Abs(l.Price - bid.Price) < 0.01m);
                        if (gridLine == null)
                        {
                            gridLine = new GridLine
                            {
                                Price = bid.Price,
                                Index = state.GridLines.Count
                            };
                            state.GridLines.Add(gridLine);
                        }

                        gridLine.MarkPending(orderId, "SELL");
                        availableInventory -= orderSize;
                        state.Inventory = availableInventory;
                        ordersCreated++;

                        context.Logger.LogInfo("OrderPlaced", 
                            $"SELL order at ${bid.Price:F2} to match BUY order (quantity: {bid.Quantity}, inventory: {state.Inventory} -> {availableInventory})", 
                            new { orderId, bid.Price });
                    }
                    catch (Exception ex)
                    {
                        context.Logger.LogError("OrderFailed", $"Failed to place SELL order at ${bid.Price:F2}: {ex.Message}");
                    }
                }

                if (ordersCreated > 0)
                {
                    context.Logger.LogInfo("Execution", 
                        $"Created {ordersCreated} SELL orders from order book. Remaining inventory: {availableInventory}");
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError("Execution", $"Error creating SELL orders from order book: {ex.Message}");
            }
        }

        private async Task CheckAndCreateMatchingSellOrders(
            BotContext context, 
            GridRuntimeState state, 
            decimal currentPrice, 
            BotParameters parameters,
            CancellationToken cancellationToken)
        {
            try
            {
                var orderSize = parameters.GetValue<decimal>("orderSize", 0);
                
                // Kiểm tra orderSize hợp lệ
                if (orderSize <= 0)
                {
                    context.Logger.LogWarning("Execution", $"Invalid orderSize ({orderSize}). Skipping SELL order creation.");
                    return;
                }
                
                // Nếu có inventory, tạo SELL orders ở tất cả grid lines cao hơn currentPrice
                if (state.Inventory < orderSize)
                {
                    context.Logger.LogInfo("Execution", $"Insufficient inventory ({state.Inventory}) to create SELL orders. Need {orderSize}");
                    return;
                }

                // Tạo SELL orders ở tất cả grid lines cao hơn currentPrice nếu có inventory
                // Điều này đảm bảo bot sẽ match với limit orders của user (ví dụ: $4,000)
                var sellableLines = state.GridLines
                    .Where(line => line.Price > currentPrice && !line.HasPendingOrder)
                    .OrderBy(line => line.Price) // Từ thấp đến cao
                    .ToList();

                var availableInventory = state.Inventory;
                var ordersCreated = 0;
                
                // Tránh divide by zero
                var maxOrders = orderSize > 0 
                    ? Math.Min(20, (int)(availableInventory / orderSize)) 
                    : 0;

                foreach (var line in sellableLines)
                {
                    if (availableInventory < orderSize) break;
                    if (ordersCreated >= maxOrders) break;

                    try
                    {
                        var orderId = await context.TradingService.PlaceOrderAsync(
                            new PlaceOrderRequest
                            {
                                Symbol = $"{context.BaseAsset}/{context.QuoteAsset}",
                                Side = "SELL",
                                Type = "LIMIT",
                                Quantity = orderSize,
                                Price = line.Price
                            },
                            cancellationToken);

                        line.MarkPending(orderId, "SELL");
                        availableInventory -= orderSize;
                        ordersCreated++;

                        context.Logger.LogInfo("OrderPlaced", 
                            $"SELL order at ${line.Price:F2} to match limit orders (inventory: {state.Inventory})", 
                            new { orderId, line.Price, inventory = state.Inventory });
                        context.Logger.LogInfo("Signal", 
                            $"Grid SELL signal at ${line.Price:F2} to match limit orders", 
                            new { orderId, line.Price });
                    }
                    catch (Exception ex)
                    {
                        context.Logger.LogError("OrderFailed", $"Failed to place SELL order at ${line.Price:F2}: {ex.Message}");
                    }
                }

                if (ordersCreated > 0)
                {
                    context.Logger.LogInfo("Execution", 
                        $"Created {ordersCreated} SELL orders to match limit orders. Remaining inventory: {availableInventory}");
                }
                else if (sellableLines.Any())
                {
                    context.Logger.LogWarning("Execution", 
                        $"No SELL orders created despite {sellableLines.Count} available grid lines. Inventory: {state.Inventory}, OrderSize: {orderSize}");
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError("Execution", $"Error checking for matching sell orders: {ex.Message}");
            }
        }
    }

    // ========== GRID RUNTIME STATE ==========

    public class GridRuntimeState
    {
        public List<GridLine> GridLines { get; set; } = new();
        public decimal CashAvailable { get; set; }
        public decimal Inventory { get; set; }
        public decimal AverageCostPrice { get; set; } // Giá mua trung bình của inventory
        public decimal LastPrice { get; set; }
        public decimal LastPnL { get; set; }
        public decimal UnrealizedPnl { get; set; }
        public DateTime? LastTradeTime { get; set; }

        public void UpdateMetrics(decimal currentPrice)
        {
            LastPrice = currentPrice;
            // Tính UnrealizedPnl dựa trên giá mua trung bình, không phải LastPrice
            // UnrealizedPnl = (Giá hiện tại - Giá mua trung bình) * Số lượng đang giữ
            if (Inventory > 0 && AverageCostPrice > 0)
            {
                UnrealizedPnl = Inventory * (currentPrice - AverageCostPrice);
            }
            else
            {
                UnrealizedPnl = 0;
            }
        }
    }

    public class GridLine
    {
        public int Index { get; set; }
        public decimal Price { get; set; }
        public ulong? OrderId { get; set; }
        public string? OrderSide { get; set; }
        public decimal FilledQty { get; set; }
        public DateTime? OrderCreatedAt { get; set; } // Track khi order được tạo

        public bool HasPendingOrder => OrderId.HasValue;

        public bool ShouldPlaceBuy(decimal currentPrice)
        {
            // Place buy order if current price is above this grid line (waiting for price to come down)
            // Hoặc nếu giá grid line thấp hơn currentPrice một chút (để match với limit orders của user)
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

            // Grid trading strategy: Orders nên được giữ lại lâu hơn
            // Chỉ cancel trong các trường hợp đặc biệt:
            
            // 1. BUY orders: Chỉ cancel nếu giá tăng QUÁ XA (50%+) VÀ order đã cũ (> 1 giờ)
            // Grid trading cần giữ orders để chờ giá quay lại
            if (OrderSide == "BUY" && currentPrice > Price)
            {
                var distancePercent = (currentPrice - Price) / Price;
                var orderAge = OrderCreatedAt.HasValue 
                    ? DateTime.UtcNow - OrderCreatedAt.Value 
                    : TimeSpan.Zero;
                
                // Chỉ cancel nếu giá tăng > 50% VÀ order đã > 1 giờ
                // Hoặc giá tăng > 100% (quá xa, không còn hy vọng)
                if (distancePercent > 1.0m) // > 100% - quá xa
                {
                    return true;
                }
                else if (distancePercent > 0.5m && orderAge.TotalHours > 1) // > 50% và > 1 giờ
                {
                    return true;
                }
                return false; // Giữ lại orders để chờ giá quay lại
            }

            // 2. SELL orders: 
            // - Không cancel nếu giá dưới grid line (bình thường, đang chờ giá tăng)
            // - Chỉ cancel nếu giá tăng quá xa (> 50%) VÀ order đã cũ
            if (OrderSide == "SELL")
            {
                if (currentPrice < Price)
                {
                    // Giá dưới grid line - bình thường, không cancel
                    return false;
                }
                else
                {
                    // Giá trên grid line - chỉ cancel nếu quá xa
                    var distancePercent = (currentPrice - Price) / Price;
                    var orderAge = OrderCreatedAt.HasValue 
                        ? DateTime.UtcNow - OrderCreatedAt.Value 
                        : TimeSpan.Zero;
                    
                    // Chỉ cancel nếu giá tăng > 50% VÀ order đã > 1 giờ
                    // Hoặc giá tăng > 100%
                    if (distancePercent > 1.0m)
                    {
                        return true;
                    }
                    else if (distancePercent > 0.5m && orderAge.TotalHours > 1)
                    {
                        return true;
                    }
                    return false; // Giữ lại để chờ giá match
                }
            }

            // Fallback: Không cancel (giữ orders cho grid trading)
            return false;
        }

        public void MarkPending(ulong orderId, string side)
        {
            OrderId = orderId;
            OrderSide = side;
            OrderCreatedAt = DateTime.UtcNow; // Track thời gian tạo order
        }

        public void Reset()
        {
            OrderId = null;
            OrderSide = null;
            FilledQty = 0;
            OrderCreatedAt = null;
        }
    }
}

