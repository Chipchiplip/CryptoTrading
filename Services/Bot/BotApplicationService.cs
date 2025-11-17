using CryptoTrading.Data;
using CryptoTrading.Interfaces.Bot;
using CryptoTrading.Models;
using CryptoTrading.Models.DTOs;
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
                QuoteAsset = request.QuoteAsset,
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

            return await MapToBotDetailDto(bot, bot.StrategyDefinition!);
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

            return await MapToBotDetailDto(bot, bot.StrategyDefinition!);
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

            var botDtos = new List<TradingBotSummaryDto>();
            foreach (var bot in bots)
            {
                botDtos.Add(await MapToBotSummaryDto(bot, bot.StrategyDefinition!));
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

            // Get strategy instance
            var strategy = _strategyRegistry.GetStrategy(bot.StrategyDefinition!.StrategyKey);
            if (strategy == null)
            {
                throw new InvalidOperationException("Strategy implementation not found");
            }

            // STEP 1: Validate strategy configuration
            try
            {
                var parametersDict = string.IsNullOrEmpty(bot.Parameters)
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(bot.Parameters)
                        ?? new Dictionary<string, object>();

                // Create BotParameters and BotContext for validation
                var botParameters = new BotParameters { Values = parametersDict };
                var botContext = new BotContext
                {
                    BotId = bot.Id,
                    UserId = bot.UserId,
                    BaseAsset = bot.BaseAsset,
                    QuoteAsset = bot.QuoteAsset,
                    AllowedCapital = 0, // Not needed for validation
                    TradingService = null!, // Not needed for validation
                    MarketData = null!, // Not needed for validation
                    PortfolioService = null!, // Not needed for validation
                    RiskManager = null!, // Not needed for validation
                    Logger = null!, // Not needed for validation
                    EventCollector = null!, // Not needed for validation
                    LoadStateAsyncFunc = (_, _) => Task.FromResult<object?>(null),
                    SaveStateAsyncFunc = (_, _) => Task.CompletedTask
                };

                var validationResult = await strategy.ValidateAsync(botContext, botParameters, CancellationToken.None);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors);
                    _logger.LogError("Bot {BotId} validation failed: {Errors}", botId, errors);

                    bot.Status = "Error";
                    bot.LastStatusReason = $"Configuration validation failed: {errors}";
                    bot.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    throw new InvalidOperationException($"Strategy validation failed: {errors}");
                }

                _logger.LogDebug("Bot {BotId} configuration validated successfully", botId);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Bot {BotId} validation threw exception", botId);

                bot.Status = "Error";
                bot.LastStatusReason = $"Validation error: {ex.Message}";
                bot.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                throw new InvalidOperationException($"Strategy validation failed: {ex.Message}", ex);
            }

            // STEP 2: Check risk limits
            decimal requiredCapital = 1000m; // Default
            try
            {
                if (!string.IsNullOrEmpty(bot.PositionSizing))
                {
                    var sizing = JsonSerializer.Deserialize<Dictionary<string, object>>(bot.PositionSizing);
                    if (sizing != null && sizing.TryGetValue("capitalAllocation", out var capital))
                    {
                        requiredCapital = Convert.ToDecimal(capital);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse position sizing for bot {BotId}, using default", botId);
            }

            var limitsOk = await _serviceProvider.GetRequiredService<IRiskManager>()
                .CheckLimitsAsync(userId, requiredCapital);

            if (!limitsOk)
            {
                _logger.LogWarning("Bot {BotId} risk limits check failed", botId);

                bot.Status = "Paused";
                bot.LastStatusReason = "Risk limits exceeded. Please adjust your configuration or close other positions.";
                bot.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                throw new InvalidOperationException(
                    "Risk limits exceeded. You may have too many active bots or insufficient capital.");
            }

            _logger.LogDebug("Bot {BotId} risk limits check passed", botId);

            // STEP 3: Check cooldown (prevent rapid restart)
            if (bot.UpdatedAt.HasValue)
            {
                var timeSinceLastUpdate = DateTime.UtcNow - bot.UpdatedAt.Value;
                if (timeSinceLastUpdate < TimeSpan.FromSeconds(10))
                {
                    _logger.LogWarning("Bot {BotId} cooldown violation: {Seconds}s since last update",
                        botId, timeSinceLastUpdate.TotalSeconds);

                    throw new InvalidOperationException(
                        $"Please wait {10 - (int)timeSinceLastUpdate.TotalSeconds} seconds before restarting the bot.");
                }
            }

            // All checks passed - start the bot
            bot.Status = "Starting";
            bot.NextRunAt = DateTime.UtcNow;
            bot.LastStatusReason = "All validation checks passed, bot is starting";
            bot.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Bot {BotId} starting successfully (Strategy: {Strategy}, Capital: {Capital})",
                botId, bot.StrategyDefinition.StrategyKey, requiredCapital);

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

            _logger.LogInformation("Bot {BotId} stopping (reason: {Reason})", botId, request.Reason);
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
                TradingService = new BotTradingServiceWrapper(_tradingService, bot.UserId, bot.Id),
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

            BotRuntimeInfoDto? runtime = null;
            if (latestSnapshot != null)
            {
                runtime = new BotRuntimeInfoDto
                {
                    NextRunAt = latestSnapshot.NextTickAt,
                    LastExecutionAt = latestSnapshot.CapturedAt,
                    LastSignal = latestSnapshot.LastSignal
                    // TODO: Calculate PnL and other metrics
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

        private async Task<TradingBotSummaryDto> MapToBotSummaryDto(TradingBot bot, BotStrategyDefinition strategy)
        {
            var latestSnapshot = await _context.TradingBotRuntimeSnapshots
                .Where(s => s.TradingBotId == bot.Id)
                .OrderByDescending(s => s.CapturedAt)
                .FirstOrDefaultAsync();

            BotRuntimeInfoDto? runtime = null;
            if (latestSnapshot != null)
            {
                runtime = new BotRuntimeInfoDto
                {
                    NextRunAt = latestSnapshot.NextTickAt,
                    LastExecutionAt = latestSnapshot.CapturedAt,
                    LastSignal = latestSnapshot.LastSignal
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
    }
}

