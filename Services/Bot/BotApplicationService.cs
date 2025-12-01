using CryptoTrading.Data;
using CryptoTrading.Interfaces.Bot;
using CryptoTrading.Models;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Services.Bot.Strategies;
using CryptoTrading.Services.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Collections.Generic;

namespace CryptoTrading.Services.Bot
{
    /// <summary>
    /// Application service for bot management
    /// </summary>
    public class BotApplicationService : IBotApplicationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStrategyRegistry _strategyRegistry;
        private readonly ITradingService _tradingService;
        private readonly ILogger<BotApplicationService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public BotApplicationService(
            ApplicationDbContext context,
            IStrategyRegistry strategyRegistry,
            ITradingService tradingService,
            ILogger<BotApplicationService> logger,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _strategyRegistry = strategyRegistry;
            _tradingService = tradingService;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<TradingBotDetailDto> CreateAsync(int userId, CreateBotRequest request)
        {
            // Validate strategy exists
            var strategyDef = await _context.BotStrategyDefinitions
                .FirstOrDefaultAsync(s => s.Id == request.StrategyDefinitionId && s.IsActive);

            if (strategyDef == null)
            {
                throw new InvalidOperationException("Strategy not found or inactive");
            }

            // Validate strategy implementation exists
            if (!_strategyRegistry.HasStrategy(strategyDef.StrategyKey))
            {
                throw new InvalidOperationException($"Strategy implementation '{strategyDef.StrategyKey}' not registered");
            }

            // Normalize QuoteAsset to USD (convert USDT to USD)
            var normalizedQuoteAsset = string.IsNullOrWhiteSpace(request.QuoteAsset) || 
                request.QuoteAsset.Equals("USDT", StringComparison.OrdinalIgnoreCase)
                ? "USD"
                : request.QuoteAsset;

            // Create bot
            var bot = new TradingBot
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                StrategyDefinitionId = request.StrategyDefinitionId,
                Name = request.Name,
                Status = "Draft",
                RiskProfile = request.RiskProfile,
                BaseAsset = request.BaseAsset,
                QuoteAsset = normalizedQuoteAsset,
                Parameters = JsonSerializer.Serialize(request.Parameters),
                PositionSizing = request.PositionSizing != null 
                    ? JsonSerializer.Serialize(request.PositionSizing) 
                    : null,
                ExecutionIntervalSeconds = request.ExecutionIntervalSeconds,
                CreatedAt = DateTime.UtcNow
            };

            _context.TradingBots.Add(bot);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created bot {BotId} for user {UserId}", bot.Id, userId);

            return await MapToBotDetailDto(bot, strategyDef);
        }

        public async Task<TradingBotDetailDto> UpdateAsync(int userId, Guid botId, UpdateBotRequest request)
        {
            var bot = await _context.TradingBots
                .Include(b => b.StrategyDefinition)
                .FirstOrDefaultAsync(b => b.Id == botId && b.UserId == userId);

            if (bot == null)
            {
                throw new KeyNotFoundException("Bot not found");
            }

            // Only allow updates when Draft or Stopped
            if (bot.Status != "Draft" && bot.Status != "Stopped")
            {
                throw new InvalidOperationException($"Cannot update bot in status: {bot.Status}");
            }

            // Update fields
            if (request.Name != null)
                bot.Name = request.Name;

            if (request.RiskProfile != null)
                bot.RiskProfile = request.RiskProfile;

            if (request.Parameters != null)
                bot.Parameters = JsonSerializer.Serialize(request.Parameters);

            if (request.PositionSizing != null)
                bot.PositionSizing = JsonSerializer.Serialize(request.PositionSizing);

            if (request.ExecutionIntervalSeconds.HasValue)
                bot.ExecutionIntervalSeconds = request.ExecutionIntervalSeconds.Value;

            bot.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated bot {BotId}", botId);

            if (bot.StrategyDefinition == null)
            {
                throw new InvalidOperationException($"Bot {botId} has no strategy definition");
            }

            return await MapToBotDetailDto(bot, bot.StrategyDefinition);
        }

        public async Task DeleteAsync(int userId, Guid botId)
        {
            var bot = await _context.TradingBots
                .FirstOrDefaultAsync(b => b.Id == botId && b.UserId == userId);

            if (bot == null)
            {
                throw new KeyNotFoundException("Bot not found");
            }

            // Only allow deletion when Draft or Stopped
            if (bot.Status != "Draft" && bot.Status != "Stopped")
            {
                throw new InvalidOperationException($"Cannot delete bot in status: {bot.Status}. Stop it first.");
            }

            _context.TradingBots.Remove(bot);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted bot {BotId}", botId);
        }

        public async Task<TradingBotDetailDto> GetAsync(int userId, Guid botId)
        {
            var bot = await _context.TradingBots
                .Include(b => b.StrategyDefinition)
                .FirstOrDefaultAsync(b => b.Id == botId && b.UserId == userId);

            if (bot == null)
            {
                throw new KeyNotFoundException("Bot not found");
            }

            if (bot.StrategyDefinition == null)
            {
                throw new InvalidOperationException($"Bot {botId} has no strategy definition");
            }

            return await MapToBotDetailDto(bot, bot.StrategyDefinition);
        }

        public async Task<PaginatedResponse<TradingBotSummaryDto>> GetListAsync(int userId, BotListQuery query)
        {
            var botsQuery = _context.TradingBots
                .Include(b => b.StrategyDefinition)
                .Where(b => b.UserId == userId);

            // Apply filters
            if (!string.IsNullOrEmpty(query.Status))
            {
                botsQuery = botsQuery.Where(b => b.Status == query.Status);
            }

            if (!string.IsNullOrEmpty(query.StrategyKey))
            {
                botsQuery = botsQuery.Where(b => b.StrategyDefinition!.StrategyKey == query.StrategyKey);
            }

            if (!string.IsNullOrEmpty(query.BaseAsset))
            {
                botsQuery = botsQuery.Where(b => b.BaseAsset == query.BaseAsset);
            }

            var totalItems = await botsQuery.CountAsync();

            var bots = await botsQuery
                .OrderByDescending(b => b.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            // Batch load runtime data for all bots to avoid N+1 queries
            var botIds = bots.Select(b => b.Id).ToList();
            
            // Load all runtime snapshots at once
            var snapshots = await _context.TradingBotRuntimeSnapshots
                .Where(s => botIds.Contains(s.TradingBotId))
                .GroupBy(s => s.TradingBotId)
                .Select(g => g.OrderByDescending(s => s.CapturedAt).First())
                .ToListAsync();
            var snapshotDict = snapshots.ToDictionary(s => s.TradingBotId, s => s);

            // Load all bot orders at once
            var botOrders = await _context.TradingBotOrders
                .Include(bo => bo.Order)
                .Where(bo => botIds.Contains(bo.TradingBotId))
                .ToListAsync();
            var ordersByBot = botOrders.GroupBy(bo => bo.TradingBotId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Map to DTOs using pre-loaded data
            var botDtos = new List<TradingBotSummaryDto>();
            foreach (var bot in bots)
            {
                var snapshot = snapshotDict.GetValueOrDefault(bot.Id);
                var orders = ordersByBot.GetValueOrDefault(bot.Id, new List<TradingBotOrder>());
                botDtos.Add(MapToBotSummaryDtoSync(bot, bot.StrategyDefinition!, snapshot, orders));
            }

            return new PaginatedResponse<TradingBotSummaryDto>
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize),
                Data = botDtos
            };
        }

        public async Task<string> StartAsync(int userId, Guid botId, StartBotRequest request)
        {
            var bot = await _context.TradingBots
                .Include(b => b.StrategyDefinition)
                .FirstOrDefaultAsync(b => b.Id == botId && b.UserId == userId);

            if (bot == null)
            {
                throw new KeyNotFoundException("Bot not found");
            }

            if (bot.Status != "Draft" && bot.Status != "Stopped")
            {
                throw new InvalidOperationException($"Cannot start bot in status: {bot.Status}");
            }

            // Validate configuration
            var strategy = _strategyRegistry.GetStrategy(bot.StrategyDefinition!.StrategyKey);
            if (strategy == null)
            {
                throw new InvalidOperationException("Strategy implementation not found");
            }

            // TODO: Perform strategy validation
            // For now, just update status
            bot.Status = "Starting";
            bot.NextRunAt = DateTime.UtcNow;
            bot.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Bot {BotId} starting", botId);

            // Return operation ID (for now, just return bot ID)
            return botId.ToString();
        }

        public async Task StopAsync(int userId, Guid botId, StopBotRequest request)
        {
            var bot = await _context.TradingBots
                .FirstOrDefaultAsync(b => b.Id == botId && b.UserId == userId);

            if (bot == null)
            {
                throw new KeyNotFoundException("Bot not found");
            }

            if (bot.Status == "Stopped" || bot.Status == "Draft")
            {
                throw new InvalidOperationException($"Bot is already stopped");
            }

            bot.Status = "Stopping";
            bot.LastStatusReason = request.Reason;
            bot.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Cancel all pending orders created by this bot
            var botOrders = await _context.TradingBotOrders
                .Include(bo => bo.Order)
                .Where(bo => bo.TradingBotId == botId && bo.Order != null)
                .ToListAsync();

            var cancelledCount = 0;
            foreach (var botOrder in botOrders)
            {
                var order = botOrder.Order!;
                // Chỉ cancel orders còn pending (NEW, PARTIAL)
                // Status hợp lệ: NEW, PARTIAL, FILLED, CANCELED, REJECTED
                if (order.Status == "NEW" || order.Status == "PARTIAL")
                {
                    try
                    {
                        await _tradingService.CancelOrderAsync(userId, order.Id);
                        cancelledCount++;
                        _logger.LogInformation("Cancelled order {OrderId} (status: {Status}) when stopping bot {BotId}", order.Id, order.Status, botId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cancel order {OrderId} when stopping bot {BotId}: {Error}", order.Id, botId, ex.Message);
                    }
                }
                else
                {
                    _logger.LogDebug("Skipping order {OrderId} with status {Status} when stopping bot {BotId}", order.Id, order.Status, botId);
                }
            }

            _logger.LogInformation("Bot {BotId} stopping (reason: {Reason}), cancelled {Count} pending orders", botId, request.Reason, cancelledCount);
        }

        public async Task NudgeAsync(int userId, Guid botId)
        {
            var bot = await _context.TradingBots
                .FirstOrDefaultAsync(b => b.Id == botId && b.UserId == userId);

            if (bot == null)
            {
                throw new KeyNotFoundException("Bot not found");
            }

            if (bot.Status != "Running")
            {
                throw new InvalidOperationException($"Can only nudge running bots");
            }

            // Force immediate execution
            bot.NextRunAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bot {BotId} nudged for immediate execution", botId);
        }

        public async Task<PaginatedResponse<BotLogDto>> GetLogsAsync(int userId, Guid botId, BotLogsQuery query)
        {
            // Verify ownership
            var botExists = await _context.TradingBots
                .AnyAsync(b => b.Id == botId && b.UserId == userId);

            if (!botExists)
            {
                throw new KeyNotFoundException("Bot not found");
            }

            var logsQuery = _context.TradingBotLogs
                .Where(l => l.TradingBotId == botId);

            // Apply filters
            if (!string.IsNullOrEmpty(query.Level))
            {
                logsQuery = logsQuery.Where(l => l.Level == query.Level);
            }

            if (!string.IsNullOrEmpty(query.Category))
            {
                logsQuery = logsQuery.Where(l => l.Category == query.Category);
            }

            if (query.FromDate.HasValue)
            {
                logsQuery = logsQuery.Where(l => l.CreatedAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                logsQuery = logsQuery.Where(l => l.CreatedAt <= query.ToDate.Value);
            }

            var totalItems = await logsQuery.CountAsync();

            var logs = await logsQuery
                .OrderByDescending(l => l.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var logDtos = logs.Select(l => new BotLogDto
            {
                Id = l.Id,
                BotId = l.TradingBotId,
                Level = l.Level,
                Category = l.Category,
                Message = l.Message,
                Payload = !string.IsNullOrEmpty(l.Payload) 
                    ? JsonSerializer.Deserialize<object>(l.Payload) 
                    : null,
                CreatedAt = l.CreatedAt
            }).ToList();

            return new PaginatedResponse<BotLogDto>
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize),
                Data = logDtos
            };
        }

        public async Task<PaginatedResponse<BotOrderDto>> GetOrdersAsync(int userId, Guid botId, int page = 1, int pageSize = 20)
        {
            // Verify ownership
            var botExists = await _context.TradingBots
                .AnyAsync(b => b.Id == botId && b.UserId == userId);

            if (!botExists)
            {
                throw new KeyNotFoundException("Bot not found");
            }

            var ordersQuery = _context.TradingBotOrders
                .Include(bo => bo.Order)
                    .ThenInclude(o => o!.Cryptocurrency)
                .Where(bo => bo.TradingBotId == botId);

            var totalItems = await ordersQuery.CountAsync();

            var orders = await ordersQuery
                .OrderByDescending(bo => bo.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var orderDtos = orders.Select(bo => new BotOrderDto
            {
                Id = bo.Id,
                BotId = bo.TradingBotId,
                OrderId = bo.OrderId,
                Intent = bo.Intent,
                SignalId = bo.SignalId,
                CreatedAt = bo.CreatedAt,
                OrderDetails = bo.Order != null ? new OrderDto
                {
                    Id = bo.Order.Id.ToString(),
                    Symbol = $"{bo.Order.Cryptocurrency?.Symbol}/USD",
                    Side = bo.Order.Side,
                    Type = bo.Order.Type,
                    Quantity = bo.Order.QuantityCoin,
                    Price = bo.Order.PriceUsd,
                    Filled = bo.Order.FilledQty,
                    Remaining = bo.Order.QuantityCoin - bo.Order.FilledQty,
                    Status = bo.Order.Status,
                    CreatedAt = bo.Order.CreatedAt,
                    UpdatedAt = bo.Order.UpdatedAt ?? bo.Order.CreatedAt
                } : null
            }).ToList();

            return new PaginatedResponse<BotOrderDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Data = orderDtos
            };
        }

        public async Task<SimulationResultDto> SimulateAsync(int userId, Guid botId, SimulationRequest request)
        {
            var bot = await _context.TradingBots
                .Include(b => b.StrategyDefinition)
                .FirstOrDefaultAsync(b => b.Id == botId && b.UserId == userId);

            if (bot == null)
            {
                throw new KeyNotFoundException("Bot not found");
            }

            if (bot.StrategyDefinition == null)
            {
                throw new InvalidOperationException("Bot strategy definition not loaded");
            }

            var strategy = _strategyRegistry.GetStrategy(bot.StrategyDefinition.StrategyKey);
            if (strategy == null)
            {
                throw new InvalidOperationException("Strategy implementation not registered");
            }

            var parameterValues = !string.IsNullOrEmpty(bot.Parameters)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(bot.Parameters) ?? new Dictionary<string, object>()
                : new Dictionary<string, object>();

            if (request.Parameters != null)
            {
                foreach (var entry in request.Parameters)
                {
                    parameterValues[entry.Key] = entry.Value;
                }
            }

            var simulationRequest = new SimulationRequest
            {
                StrategyDefinitionId = bot.StrategyDefinitionId,
                Parameters = parameterValues,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                InitialCapital = request.InitialCapital > 0
                    ? request.InitialCapital
                    : parameterValues.TryGetValue("capitalAllocation", out var cap) && decimal.TryParse(cap?.ToString(), out var capValue)
                        ? capValue
                        : 10000m
            };

            using var scope = _serviceProvider.CreateScope();
            var marketData = scope.ServiceProvider.GetRequiredService<IMarketDataProvider>();
            var portfolioService = scope.ServiceProvider.GetRequiredService<IPortfolioService>();
            var riskManager = scope.ServiceProvider.GetRequiredService<IRiskManager>();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

            object? cachedState = null;

            var botContext = new BotContext
            {
                BotId = bot.Id,
                UserId = bot.UserId,
                BaseAsset = bot.BaseAsset,
                QuoteAsset = string.IsNullOrWhiteSpace(bot.QuoteAsset) ? "USD" : bot.QuoteAsset,
                AllowedCapital = simulationRequest.InitialCapital,
                TradingService = new BotTradingServiceWrapper(_tradingService, _context, bot.UserId, bot.Id),
                MarketData = marketData,
                PortfolioService = portfolioService,
                RiskManager = riskManager,
                Logger = new BotLogger(_context, bot.Id, loggerFactory.CreateLogger<BotLogger>()),
                EventCollector = new EventCollector(),
                LoadStateAsyncFunc = (type, ct) => Task.FromResult(cachedState),
                SaveStateAsyncFunc = (state, ct) =>
                {
                    cachedState = state;
                    return Task.CompletedTask;
                }
            };

            var simulationParameters = new BotParameters { Values = parameterValues };
            botContext.AllowedCapital = Math.Max(simulationRequest.InitialCapital, simulationParameters.GetValue("capitalAllocation", simulationRequest.InitialCapital));

            var result = await strategy.SimulateAsync(botContext, simulationRequest, CancellationToken.None);

            if (!result.Success || result.Result == null)
            {
                throw new InvalidOperationException(result.ErrorMessage ?? "Simulation failed");
            }

            return result.Result;
        }

        private async Task<TradingBotDetailDto> MapToBotDetailDto(TradingBot bot, BotStrategyDefinition strategy)
        {
            var parameters = !string.IsNullOrEmpty(bot.Parameters)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(bot.Parameters)
                : null;

            var positionSizing = !string.IsNullOrEmpty(bot.PositionSizing)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(bot.PositionSizing)
                : null;

            // Get runtime info if exists
            var latestSnapshot = await _context.TradingBotRuntimeSnapshots
                .Where(s => s.TradingBotId == bot.Id)
                .OrderByDescending(s => s.CapturedAt)
                .FirstOrDefaultAsync();

            // Calculate metrics from orders
            var botOrders = await _context.TradingBotOrders
                .Include(bo => bo.Order)
                .Where(bo => bo.TradingBotId == bot.Id)
                .ToListAsync();

            var totalOrders = botOrders.Count;
            var filledOrders = botOrders.Count(bo => bo.Order != null && 
                (bo.Order.Status == "FILLED" || bo.Order.Status == "PARTIAL"));
            
            // Calculate PnL from TRADES (not orders) - this is the correct way
            var orderIds = botOrders
                .Where(bo => bo.Order != null)
                .Select(bo => bo.Order!.Id)
                .ToList();
            
            var trades = await _context.Trades
                .Where(t => orderIds.Contains(t.OrderId))
                .Include(t => t.Order)
                .ToListAsync();
            
            // Calculate realized P&L: Sum of (SELL trades revenue - BUY trades cost - fees)
            var realizedPnl = trades
                .Where(t => t.Order != null)
                .Sum(t => 
                {
                    var tradeValue = t.PriceUsd * t.QuantityCoin;
                    var fee = t.FeeUsd;
                    
                    if (t.Order.Side == "BUY")
                    {
                        // BUY: negative (cost + fee)
                        return -(tradeValue + fee);
                    }
                    else // SELL
                    {
                        // SELL: positive (revenue - fee)
                        return tradeValue - fee;
                    }
                });
            
            // Calculate total fees
            var totalFees = trades.Sum(t => t.FeeUsd);
            
            // Calculate unrealized P&L from bot state (inventory đang hold)
            decimal unrealizedPnl = 0;
            decimal openPositions = 0;
            
            if (latestSnapshot != null && !string.IsNullOrEmpty(latestSnapshot.RuntimeState))
            {
                try
                {
                    // Load bot state để lấy inventory và average cost price
                    var stateJson = latestSnapshot.RuntimeState;
                    var stateType = bot.StrategyDefinition?.StrategyKey == "grid-basic" 
                        ? typeof(GridRuntimeState) 
                        : null;
                    
                    if (stateType != null)
                    {
                        var state = JsonSerializer.Deserialize(stateJson, stateType);
                        if (state != null)
                        {
                            // Lấy inventory và average cost price từ state
                            var inventoryProperty = stateType.GetProperty("Inventory");
                            var averageCostProperty = stateType.GetProperty("AverageCostPrice");
                            
                            if (inventoryProperty != null && averageCostProperty != null)
                            {
                                var inventory = (decimal)(inventoryProperty.GetValue(state) ?? 0m);
                                var averageCost = (decimal)(averageCostProperty.GetValue(state) ?? 0m);
                                
                                openPositions = inventory;
                                
                                // Tính unrealized P&L = (current price - average cost) * inventory
                                if (inventory > 0 && averageCost > 0)
                                {
                                    // Lấy current price từ market data
                                    using var scope = _serviceProvider.CreateScope();
                                    var marketDataProvider = scope.ServiceProvider.GetRequiredService<IMarketDataProvider>();
                                    
                                    try
                                    {
                                        var currentPrice = await marketDataProvider.GetMidPriceAsync(
                                            bot.BaseAsset, 
                                            bot.QuoteAsset, 
                                            CancellationToken.None);
                                        
                                        if (currentPrice > 0)
                                        {
                                            unrealizedPnl = inventory * (currentPrice - averageCost);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to get current price for unrealized P&L calculation for bot {BotId}", bot.Id);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse bot state for unrealized P&L calculation for bot {BotId}", bot.Id);
                }
            }

            BotRuntimeInfoDto? runtime = null;
            if (latestSnapshot != null || totalOrders > 0)
            {
                runtime = new BotRuntimeInfoDto
                {
                    NextRunAt = latestSnapshot?.NextTickAt ?? bot.NextRunAt,
                    LastExecutionAt = latestSnapshot?.CapturedAt ?? bot.UpdatedAt,
                    LastSignal = latestSnapshot?.LastSignal,
                    TotalOrders = totalOrders,
                    FilledOrders = filledOrders,
                    RealizedPnl = realizedPnl,
                    OpenPositions = openPositions,
                    UnrealizedPnl = unrealizedPnl,
                    TotalFees = totalFees
                };
            }

            return new TradingBotDetailDto
            {
                Id = bot.Id,
                UserId = bot.UserId,
                Name = bot.Name,
                Status = bot.Status,
                RiskProfile = bot.RiskProfile,
                BaseAsset = bot.BaseAsset,
                QuoteAsset = bot.QuoteAsset,
                Strategy = new StrategyInfoDto
                {
                    Id = strategy.Id,
                    Key = strategy.StrategyKey,
                    Version = strategy.Version,
                    DisplayName = strategy.DisplayName
                },
                Parameters = parameters,
                PositionSizing = positionSizing,
                ExecutionIntervalSeconds = bot.ExecutionIntervalSeconds,
                NextRunAt = bot.NextRunAt,
                LastStatusReason = bot.LastStatusReason,
                Runtime = runtime,
                CreatedAt = bot.CreatedAt,
                UpdatedAt = bot.UpdatedAt
            };
        }

        private TradingBotSummaryDto MapToBotSummaryDtoSync(TradingBot bot, BotStrategyDefinition strategy, TradingBotRuntimeSnapshot? latestSnapshot, List<TradingBotOrder> botOrders)
        {
            var totalOrders = botOrders.Count;
            var filledOrders = botOrders.Count(bo => bo.Order != null && 
                (bo.Order.Status == "FILLED" || bo.Order.Status == "PARTIAL"));
            
            // For the optimized version, we'll use a simplified calculation
            // The full calculation with trades would require additional queries
            var realizedPnl = 0.0m;
            var totalFees = 0.0m;
            var openPositions = 0.0m;
            var unrealizedPnl = 0.0m;

            // Calculate open positions from bot state
            if (latestSnapshot != null && !string.IsNullOrEmpty(latestSnapshot.RuntimeState))
            {
                try
                {
                    var stateJson = JsonSerializer.Deserialize<JsonElement>(latestSnapshot.RuntimeState);
                    if (stateJson.TryGetProperty("inventory", out var inventoryElement))
                    {
                        if (inventoryElement.ValueKind == JsonValueKind.Number)
                        {
                            openPositions = inventoryElement.GetDecimal();
                        }
                        else if (inventoryElement.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var prop in inventoryElement.EnumerateObject())
                            {
                                if (prop.Value.ValueKind == JsonValueKind.Number)
                                {
                                    openPositions += prop.Value.GetDecimal();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse bot state for unrealized P&L calculation for bot {BotId}", bot.Id);
                }
            }

            BotRuntimeInfoDto? runtime = null;
            if (latestSnapshot != null || totalOrders > 0)
            {
                runtime = new BotRuntimeInfoDto
                {
                    NextRunAt = latestSnapshot?.NextTickAt ?? bot.NextRunAt,
                    LastExecutionAt = latestSnapshot?.CapturedAt ?? bot.UpdatedAt,
                    LastSignal = latestSnapshot?.LastSignal,
                    TotalOrders = totalOrders,
                    FilledOrders = filledOrders,
                    RealizedPnl = realizedPnl,
                    OpenPositions = openPositions,
                    UnrealizedPnl = unrealizedPnl,
                    TotalFees = totalFees
                };
            }

            return new TradingBotSummaryDto
            {
                Id = bot.Id,
                Name = bot.Name,
                Status = bot.Status,
                BaseAsset = bot.BaseAsset,
                QuoteAsset = bot.QuoteAsset,
                Strategy = new StrategyInfoDto
                {
                    Id = strategy.Id,
                    Key = strategy.StrategyKey,
                    Version = strategy.Version,
                    DisplayName = strategy.DisplayName
                },
                Runtime = runtime,
                CreatedAt = bot.CreatedAt,
                UpdatedAt = bot.UpdatedAt
            };
        }

        private async Task<TradingBotSummaryDto> MapToBotSummaryDto(TradingBot bot, BotStrategyDefinition strategy)
        {
            var latestSnapshot = await _context.TradingBotRuntimeSnapshots
                .Where(s => s.TradingBotId == bot.Id)
                .OrderByDescending(s => s.CapturedAt)
                .FirstOrDefaultAsync();

            // Calculate metrics from orders
            var botOrders = await _context.TradingBotOrders
                .Include(bo => bo.Order)
                .Where(bo => bo.TradingBotId == bot.Id)
                .ToListAsync();

            var totalOrders = botOrders.Count;
            var filledOrders = botOrders.Count(bo => bo.Order != null && 
                (bo.Order.Status == "FILLED" || bo.Order.Status == "PARTIAL"));
            
            // Calculate PnL from TRADES (not orders) - this is the correct way
            var orderIds = botOrders
                .Where(bo => bo.Order != null)
                .Select(bo => bo.Order!.Id)
                .ToList();
            
            var trades = await _context.Trades
                .Where(t => orderIds.Contains(t.OrderId))
                .Include(t => t.Order)
                .ToListAsync();
            
            // Calculate realized P&L: Sum of (SELL trades revenue - BUY trades cost - fees)
            var realizedPnl = trades
                .Where(t => t.Order != null)
                .Sum(t => 
                {
                    var tradeValue = t.PriceUsd * t.QuantityCoin;
                    var fee = t.FeeUsd;
                    
                    if (t.Order.Side == "BUY")
                    {
                        // BUY: negative (cost + fee)
                        return -(tradeValue + fee);
                    }
                    else // SELL
                    {
                        // SELL: positive (revenue - fee)
                        return tradeValue - fee;
                    }
                });
            
            // Calculate total fees
            var totalFees = trades.Sum(t => t.FeeUsd);
            
            // Calculate unrealized P&L from bot state (inventory đang hold)
            decimal unrealizedPnl = 0;
            decimal openPositions = 0;
            
            if (latestSnapshot != null && !string.IsNullOrEmpty(latestSnapshot.RuntimeState))
            {
                try
                {
                    // Load bot state để lấy inventory và average cost price
                    var stateJson = latestSnapshot.RuntimeState;
                    var stateType = strategy.StrategyKey == "grid-basic" 
                        ? typeof(GridRuntimeState) 
                        : null;
                    
                    if (stateType != null)
                    {
                        var state = JsonSerializer.Deserialize(stateJson, stateType);
                        if (state != null)
                        {
                            // Lấy inventory và average cost price từ state
                            var inventoryProperty = stateType.GetProperty("Inventory");
                            var averageCostProperty = stateType.GetProperty("AverageCostPrice");
                            
                            if (inventoryProperty != null && averageCostProperty != null)
                            {
                                var inventory = (decimal)(inventoryProperty.GetValue(state) ?? 0m);
                                var averageCost = (decimal)(averageCostProperty.GetValue(state) ?? 0m);
                                
                                openPositions = inventory;
                                
                                // Tính unrealized P&L = (current price - average cost) * inventory
                                if (inventory > 0 && averageCost > 0)
                                {
                                    // Lấy current price từ market data
                                    using var scope = _serviceProvider.CreateScope();
                                    var marketDataProvider = scope.ServiceProvider.GetRequiredService<IMarketDataProvider>();
                                    
                                    try
                                    {
                                        var currentPrice = await marketDataProvider.GetMidPriceAsync(
                                            bot.BaseAsset, 
                                            bot.QuoteAsset, 
                                            CancellationToken.None);
                                        
                                        if (currentPrice > 0)
                                        {
                                            unrealizedPnl = inventory * (currentPrice - averageCost);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to get current price for unrealized P&L calculation for bot {BotId}", bot.Id);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse bot state for unrealized P&L calculation for bot {BotId}", bot.Id);
                }
            }

            BotRuntimeInfoDto? runtime = null;
            if (latestSnapshot != null || totalOrders > 0)
            {
                runtime = new BotRuntimeInfoDto
                {
                    NextRunAt = latestSnapshot?.NextTickAt ?? bot.NextRunAt,
                    LastExecutionAt = latestSnapshot?.CapturedAt ?? bot.UpdatedAt,
                    LastSignal = latestSnapshot?.LastSignal,
                    TotalOrders = totalOrders,
                    FilledOrders = filledOrders,
                    RealizedPnl = realizedPnl,
                    OpenPositions = openPositions,
                    UnrealizedPnl = unrealizedPnl,
                    TotalFees = totalFees
                };
            }

            return new TradingBotSummaryDto
            {
                Id = bot.Id,
                Name = bot.Name,
                Status = bot.Status,
                BaseAsset = bot.BaseAsset,
                QuoteAsset = bot.QuoteAsset,
                Strategy = new StrategyInfoDto
                {
                    Id = strategy.Id,
                    Key = strategy.StrategyKey,
                    Version = strategy.Version,
                    DisplayName = strategy.DisplayName
                },
                Runtime = runtime,
                CreatedAt = bot.CreatedAt,
                UpdatedAt = bot.UpdatedAt
            };
        }

        public async Task<ResetInventoryResultDto> ResetInventoryAsync(int userId, Guid botId)
        {
            // Verify bot exists and belongs to user
            var bot = await _context.TradingBots
                .Include(b => b.StrategyDefinition)
                .FirstOrDefaultAsync(b => b.Id == botId && b.UserId == userId);

            if (bot == null)
            {
                throw new KeyNotFoundException("Bot not found");
            }

            // Only support Grid Trading strategy for now
            if (bot.StrategyDefinition?.StrategyKey != "grid-basic")
            {
                throw new InvalidOperationException("Inventory reset is only supported for Grid Trading strategy");
            }

            // Get all filled orders for this bot
            var botOrders = await _context.TradingBotOrders
                .Include(bo => bo.Order)
                .ThenInclude(o => o!.Cryptocurrency)
                .Where(bo => bo.TradingBotId == botId && bo.Order != null)
                .OrderBy(bo => bo.Order!.CreatedAt)
                .ToListAsync();

            // Calculate inventory and cash from orders
            decimal inventory = 0m;
            decimal cashSpent = 0m;
            decimal cashReceived = 0m;
            int ordersProcessed = 0;

            foreach (var botOrder in botOrders)
            {
                var order = botOrder.Order;
                if (order == null) continue;

                // Only count filled orders
                var isFilled = order.Status == "FILLED" || 
                              (order.Status == "PARTIAL" && order.FilledQty > 0) ||
                              (order.FilledQty > 0 && order.FilledQty >= order.QuantityCoin);

                if (!isFilled || order.FilledQty <= 0) continue;

                ordersProcessed++;
                var fillPrice = order.PriceUsd ?? 0m;
                var filledQty = order.FilledQty;

                if (order.Side == "BUY")
                {
                    inventory += filledQty;
                    cashSpent += fillPrice * filledQty;
                }
                else if (order.Side == "SELL")
                {
                    inventory -= filledQty;
                    cashReceived += fillPrice * filledQty;
                }
            }

            // Get initial capital from bot parameters
            var parameters = !string.IsNullOrEmpty(bot.Parameters)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(bot.Parameters)
                : new Dictionary<string, object>();

            var capitalAllocation = 0m;
            if (parameters != null && parameters.ContainsKey("capitalAllocation"))
            {
                if (parameters["capitalAllocation"] is JsonElement jsonElement)
                {
                    capitalAllocation = jsonElement.GetDecimal();
                }
                else if (decimal.TryParse(parameters["capitalAllocation"]?.ToString(), out var parsed))
                {
                    capitalAllocation = parsed;
                }
            }

            // If no capital allocation in parameters, try to get from position sizing
            if (capitalAllocation <= 0)
            {
                var positionSizing = !string.IsNullOrEmpty(bot.PositionSizing)
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(bot.PositionSizing)
                    : null;

                if (positionSizing != null && positionSizing.ContainsKey("maxDailyExposure"))
                {
                    if (positionSizing["maxDailyExposure"] is JsonElement jsonElement)
                    {
                        capitalAllocation = jsonElement.GetDecimal();
                    }
                    else if (decimal.TryParse(positionSizing["maxDailyExposure"]?.ToString(), out var parsed))
                    {
                        capitalAllocation = parsed;
                    }
                }
            }

            // Default to 10000 if still no capital found
            if (capitalAllocation <= 0)
            {
                capitalAllocation = 10000m;
            }

            // Calculate cash available = initial capital - spent + received
            var cashAvailable = capitalAllocation - cashSpent + cashReceived;

            // Load current state
            var latestSnapshot = await _context.TradingBotRuntimeSnapshots
                .Where(s => s.TradingBotId == botId)
                .OrderByDescending(s => s.CapturedAt)
                .FirstOrDefaultAsync();

            decimal previousInventory = 0m;
            decimal previousCash = 0m;

            if (latestSnapshot != null && !string.IsNullOrEmpty(latestSnapshot.RuntimeState))
            {
                try
                {
                    // Try to deserialize as GridRuntimeState
                    var state = JsonSerializer.Deserialize<GridRuntimeState>(latestSnapshot.RuntimeState);
                    
                    if (state != null)
                    {
                        previousInventory = state.Inventory;
                        previousCash = state.CashAvailable;

                        // Update values
                        state.Inventory = inventory;
                        state.CashAvailable = cashAvailable;

                        // Save updated state
                        var updatedStateJson = JsonSerializer.Serialize(state);
                        latestSnapshot.RuntimeState = updatedStateJson;
                        latestSnapshot.CapturedAt = DateTime.UtcNow;
                        
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating state for bot {BotId}", botId);
                    // If deserialization fails, create new state
                }
            }

            // If no snapshot exists or update failed, create new one
            if (latestSnapshot == null || string.IsNullOrEmpty(latestSnapshot.RuntimeState))
            {
                // Create minimal state with correct inventory and cash
                var newState = new GridRuntimeState
                {
                    GridLines = new List<GridLine>(),
                    CashAvailable = cashAvailable,
                    Inventory = inventory,
                    LastPrice = 0m,
                    LastPnL = 0m,
                    UnrealizedPnl = 0m,
                    LastTradeTime = null
                };

                var newStateJson = JsonSerializer.Serialize(newState);
                var newSnapshot = new TradingBotRuntimeSnapshot
                {
                    TradingBotId = botId,
                    CapturedAt = DateTime.UtcNow,
                    RuntimeState = newStateJson,
                    NextTickAt = DateTime.UtcNow
                };

                _context.TradingBotRuntimeSnapshots.Add(newSnapshot);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation(
                "Reset inventory for bot {BotId}: Inventory {PreviousInventory} -> {NewInventory}, Cash {PreviousCash} -> {NewCash}",
                botId, previousInventory, inventory, previousCash, cashAvailable);

            return new ResetInventoryResultDto
            {
                Success = true,
                Message = $"Inventory reset successfully. Processed {ordersProcessed} filled orders.",
                PreviousInventory = previousInventory,
                NewInventory = inventory,
                PreviousCash = previousCash,
                NewCash = cashAvailable,
                OrdersProcessed = ordersProcessed
            };
        }

        public async Task<List<BotSearchResultDto>> SearchBotsByNameAsync(string namePattern)
        {
            var bots = await _context.TradingBots
                .Include(b => b.StrategyDefinition)
                .Where(b => b.Name.Contains(namePattern))
                .OrderByDescending(b => b.CreatedAt)
                .Take(50)
                .ToListAsync();

            return bots.Select(b => new BotSearchResultDto
            {
                Id = b.Id,
                Name = b.Name,
                UserId = b.UserId,
                Status = b.Status,
                BaseAsset = b.BaseAsset,
                StrategyKey = b.StrategyDefinition?.StrategyKey ?? "unknown"
            }).ToList();
        }

        public async Task<List<BotSearchResultDto>> GetBotsByUserIdAsync(int userId)
        {
            var bots = await _context.TradingBots
                .Include(b => b.StrategyDefinition)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return bots.Select(b => new BotSearchResultDto
            {
                Id = b.Id,
                Name = b.Name,
                UserId = b.UserId,
                Status = b.Status,
                BaseAsset = b.BaseAsset,
                StrategyKey = b.StrategyDefinition?.StrategyKey ?? "unknown"
            }).ToList();
        }

        public async Task<ResetInventoryResultDto> ResetInventoryAdminAsync(Guid botId)
        {
            // Admin version - no userId check, just find bot by ID
            var bot = await _context.TradingBots
                .Include(b => b.StrategyDefinition)
                .FirstOrDefaultAsync(b => b.Id == botId);

            if (bot == null)
            {
                throw new KeyNotFoundException($"Bot with ID {botId} not found");
            }

            // Only support Grid Trading strategy for now
            if (bot.StrategyDefinition?.StrategyKey != "grid-basic")
            {
                throw new InvalidOperationException("Inventory reset is only supported for Grid Trading strategy");
            }

            // Reuse the same calculation logic but without userId check
            // Get all filled orders for this bot
            var botOrders = await _context.TradingBotOrders
                .Include(bo => bo.Order)
                .ThenInclude(o => o!.Cryptocurrency)
                .Where(bo => bo.TradingBotId == botId && bo.Order != null)
                .OrderBy(bo => bo.Order!.CreatedAt)
                .ToListAsync();

            // Calculate inventory and cash from orders
            decimal inventory = 0m;
            decimal cashSpent = 0m;
            decimal cashReceived = 0m;
            int ordersProcessed = 0;

            foreach (var botOrder in botOrders)
            {
                var order = botOrder.Order;
                if (order == null) continue;

                // Only count filled orders
                var isFilled = order.Status == "FILLED" || 
                              (order.Status == "PARTIAL" && order.FilledQty > 0) ||
                              (order.FilledQty > 0 && order.FilledQty >= order.QuantityCoin);

                if (!isFilled || order.FilledQty <= 0) continue;

                ordersProcessed++;
                var fillPrice = order.PriceUsd ?? 0m;
                var filledQty = order.FilledQty;

                if (order.Side == "BUY")
                {
                    inventory += filledQty;
                    cashSpent += fillPrice * filledQty;
                }
                else if (order.Side == "SELL")
                {
                    inventory -= filledQty;
                    cashReceived += fillPrice * filledQty;
                }
            }

            // Get initial capital from bot parameters
            var parameters = !string.IsNullOrEmpty(bot.Parameters)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(bot.Parameters)
                : new Dictionary<string, object>();

            var capitalAllocation = 0m;
            if (parameters != null && parameters.ContainsKey("capitalAllocation"))
            {
                if (parameters["capitalAllocation"] is JsonElement jsonElement)
                {
                    capitalAllocation = jsonElement.GetDecimal();
                }
                else if (decimal.TryParse(parameters["capitalAllocation"]?.ToString(), out var parsed))
                {
                    capitalAllocation = parsed;
                }
            }

            // If no capital allocation in parameters, try to get from position sizing
            if (capitalAllocation <= 0)
            {
                var positionSizing = !string.IsNullOrEmpty(bot.PositionSizing)
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(bot.PositionSizing)
                    : null;

                if (positionSizing != null && positionSizing.ContainsKey("maxDailyExposure"))
                {
                    if (positionSizing["maxDailyExposure"] is JsonElement jsonElement)
                    {
                        capitalAllocation = jsonElement.GetDecimal();
                    }
                    else if (decimal.TryParse(positionSizing["maxDailyExposure"]?.ToString(), out var parsed))
                    {
                        capitalAllocation = parsed;
                    }
                }
            }

            // Default to 10000 if still no capital found
            if (capitalAllocation <= 0)
            {
                capitalAllocation = 10000m;
            }

            // Calculate cash available = initial capital - spent + received
            var cashAvailable = capitalAllocation - cashSpent + cashReceived;

            // Load current state
            var latestSnapshot = await _context.TradingBotRuntimeSnapshots
                .Where(s => s.TradingBotId == botId)
                .OrderByDescending(s => s.CapturedAt)
                .FirstOrDefaultAsync();

            decimal previousInventory = 0m;
            decimal previousCash = 0m;

            if (latestSnapshot != null && !string.IsNullOrEmpty(latestSnapshot.RuntimeState))
            {
                try
                {
                    // Try to deserialize as GridRuntimeState
                    var state = JsonSerializer.Deserialize<GridRuntimeState>(latestSnapshot.RuntimeState);
                    
                    if (state != null)
                    {
                        previousInventory = state.Inventory;
                        previousCash = state.CashAvailable;

                        // Update values
                        state.Inventory = inventory;
                        state.CashAvailable = cashAvailable;

                        // Save updated state
                        var updatedStateJson = JsonSerializer.Serialize(state);
                        latestSnapshot.RuntimeState = updatedStateJson;
                        latestSnapshot.CapturedAt = DateTime.UtcNow;
                        
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating state for bot {BotId}", botId);
                    // If deserialization fails, create new state
                }
            }

            // If no snapshot exists or update failed, create new one
            if (latestSnapshot == null || string.IsNullOrEmpty(latestSnapshot.RuntimeState))
            {
                // Create minimal state with correct inventory and cash
                var newState = new GridRuntimeState
                {
                    GridLines = new List<GridLine>(),
                    CashAvailable = cashAvailable,
                    Inventory = inventory,
                    LastPrice = 0m,
                    LastPnL = 0m,
                    UnrealizedPnl = 0m,
                    LastTradeTime = null
                };

                var newStateJson = JsonSerializer.Serialize(newState);
                var newSnapshot = new TradingBotRuntimeSnapshot
                {
                    TradingBotId = botId,
                    CapturedAt = DateTime.UtcNow,
                    RuntimeState = newStateJson,
                    NextTickAt = DateTime.UtcNow
                };

                _context.TradingBotRuntimeSnapshots.Add(newSnapshot);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation(
                "Reset inventory (admin) for bot {BotId} (userId={UserId}): Inventory {PreviousInventory} -> {NewInventory}, Cash {PreviousCash} -> {NewCash}",
                botId, bot.UserId, previousInventory, inventory, previousCash, cashAvailable);

            return new ResetInventoryResultDto
            {
                Success = true,
                Message = $"Inventory reset successfully. Processed {ordersProcessed} filled orders. Bot belongs to userId={bot.UserId}.",
                PreviousInventory = previousInventory,
                NewInventory = inventory,
                PreviousCash = previousCash,
                NewCash = cashAvailable,
                OrdersProcessed = ordersProcessed
            };
        }
    }
}

