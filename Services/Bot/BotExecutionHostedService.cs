using CryptoTrading.Data;
using CryptoTrading.Interfaces.Bot;
using CryptoTrading.Services.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Channels;

namespace CryptoTrading.Services.Bot
{
    /// <summary>
    /// Background worker pool that executes bot strategies
    /// </summary>
    public class BotExecutionHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BotExecutionHostedService> _logger;
        private readonly Channel<BotExecutionJob> _jobQueue;
        private const int MAX_CONCURRENT_WORKERS = 5;

        public BotExecutionHostedService(
            IServiceProvider serviceProvider,
            ILogger<BotExecutionHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _jobQueue = Channel.CreateUnbounded<BotExecutionJob>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Bot Execution Service started");

            // Start worker tasks
            var workers = new List<Task>();
            for (int i = 0; i < MAX_CONCURRENT_WORKERS; i++)
            {
                workers.Add(WorkerAsync(i, stoppingToken));
            }

            // Start orchestrator
            workers.Add(OrchestratorAsync(stoppingToken));

            await Task.WhenAll(workers);

            _logger.LogInformation("Bot Execution Service stopped");
        }

        private async Task OrchestratorAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // Find bots that need execution
                    var botsToRun = await context.TradingBots
                        .Where(b => 
                            (b.Status == "Running" || b.Status == "Starting") &&
                            b.NextRunAt.HasValue &&
                            b.NextRunAt.Value <= DateTime.UtcNow)
                        .Select(b => new
                        {
                            b.Id,
                            b.UserId,
                            StrategyKey = b.StrategyDefinition != null ? b.StrategyDefinition.StrategyKey : ""
                        })
                        .ToListAsync(stoppingToken);

                    foreach (var bot in botsToRun)
                    {
                        var job = new BotExecutionJob
                        {
                            BotId = bot.Id,
                            UserId = bot.UserId,
                            StrategyKey = bot.StrategyKey,
                            ScheduledAt = DateTime.UtcNow
                        };

                        await _jobQueue.Writer.WriteAsync(job, stoppingToken);
                        _logger.LogDebug("Queued bot {BotId} for execution", bot.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in orchestrator loop");
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        private async Task WorkerAsync(int workerId, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker {WorkerId} started", workerId);

            await foreach (var job in _jobQueue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ExecuteBotAsync(job, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker {WorkerId} error executing bot {BotId}", workerId, job.BotId);
                }
            }

            _logger.LogInformation("Worker {WorkerId} stopped", workerId);
        }

        private async Task ExecuteBotAsync(BotExecutionJob job, CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var strategyRegistry = scope.ServiceProvider.GetRequiredService<IStrategyRegistry>();
            var tradingService = scope.ServiceProvider.GetRequiredService<ITradingService>();
            var marketDataProvider = scope.ServiceProvider.GetRequiredService<IMarketDataProvider>();
            var portfolioService = scope.ServiceProvider.GetRequiredService<IPortfolioService>();
            var riskManager = scope.ServiceProvider.GetRequiredService<IRiskManager>();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

            // Load bot
            var bot = await context.TradingBots
                .FirstOrDefaultAsync(b => b.Id == job.BotId, stoppingToken);

            if (bot == null)
            {
                _logger.LogWarning("Bot {BotId} not found", job.BotId);
                return;
            }

            // Load strategy definition separately to avoid Include issues
            var strategyDef = await context.BotStrategyDefinitions
                .FirstOrDefaultAsync(s => s.Id == bot.StrategyDefinitionId, stoppingToken);

            if (strategyDef == null)
            {
                _logger.LogError("Strategy definition {StrategyId} not found for bot {BotId}", bot.StrategyDefinitionId, bot.Id);
                bot.Status = "Error";
                bot.LastStatusReason = "Strategy definition not found";
                await context.SaveChangesAsync(stoppingToken);
                return;
            }

            // Load strategy
            var strategy = strategyRegistry.GetStrategy(strategyDef.StrategyKey);
            if (strategy == null)
            {
                _logger.LogError("Strategy {StrategyKey} not found", strategyDef.StrategyKey);
                bot.Status = "Error";
                bot.LastStatusReason = "Strategy not found";
                await context.SaveChangesAsync(stoppingToken);
                return;
            }

            // Parse parameters
            var parameters = new BotParameters
            {
                Values = !string.IsNullOrEmpty(bot.Parameters)
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(bot.Parameters) ?? new()
                    : new()
            };

            // Add execution interval
            parameters.Values["refreshIntervalSeconds"] = bot.ExecutionIntervalSeconds;

            // Get dynamic capital allocation
            decimal allowedCapital;
            try
            {
                // Use RiskManager to get bot-specific capital limit
                allowedCapital = await riskManager.GetBotCapitalLimitAsync(bot.UserId, bot.Id, stoppingToken);
                _logger.LogDebug("Bot {BotId} allowed capital: {Capital}", bot.Id, allowedCapital);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get bot capital limit for {BotId}, using default", bot.Id);
                allowedCapital = 5000m; // Conservative default
            }

            // Create bot context
            var botLogger = new BotLogger(context, bot.Id, loggerFactory.CreateLogger<BotLogger>());
            var eventCollector = new EventCollector();

            var botContext = new BotContext
            {
                BotId = bot.Id,
                UserId = bot.UserId,
                BaseAsset = bot.BaseAsset,
                QuoteAsset = bot.QuoteAsset,
                AllowedCapital = allowedCapital, // Dynamic capital from RiskManager
                TradingService = new BotTradingServiceWrapper(tradingService, bot.UserId, bot.Id),
                MarketData = marketDataProvider,
                PortfolioService = portfolioService,
                RiskManager = riskManager,
                Logger = botLogger,
                EventCollector = eventCollector,
                LoadStateAsyncFunc = async (type, ct) => await LoadStateObjectAsync(context, bot.Id, type, ct),
                SaveStateAsyncFunc = async (state, ct) => await SaveStateObjectAsync(context, bot.Id, state, ct)
            };

            // PRE-EXECUTION GUARDRAILS
            try
            {
                // Check kill switch
                var killSwitchTriggered = await riskManager.CheckKillSwitchAsync(bot.Id, bot.UserId, stoppingToken);
                if (killSwitchTriggered)
                {
                    _logger.LogWarning("Bot {BotId} stopped by kill switch", bot.Id);
                    bot.Status = "Stopped";
                    bot.LastStatusReason = "Kill switch triggered due to risk limits";
                    await context.SaveChangesAsync(stoppingToken);
                    return;
                }

                // Check cooldown (get from config or use default)
                var minCooldownSeconds = 30; // Default 30 seconds
                var minCooldown = TimeSpan.FromSeconds(minCooldownSeconds);
                var cooldownPassed = await riskManager.CheckCooldownAsync(bot.Id, minCooldown, stoppingToken);

                if (!cooldownPassed)
                {
                    _logger.LogDebug("Bot {BotId} in cooldown period, skipping execution", bot.Id);
                    // Reschedule for after cooldown
                    bot.NextRunAt = DateTime.UtcNow.AddSeconds(minCooldownSeconds);
                    await context.SaveChangesAsync(stoppingToken);
                    return;
                }

                _logger.LogDebug("Bot {BotId} pre-execution checks passed", bot.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in pre-execution guardrails for bot {BotId}", bot.Id);
                // Continue execution if guardrails fail (fail-open for safety)
            }

            try
            {
                _logger.LogInformation(
                    "Executing bot {BotId} with strategy {StrategyKey}, capital: {Capital}",
                    bot.Id, strategy.Key, allowedCapital);

                // Execute strategy
                var result = await strategy.ExecuteAsync(botContext, parameters, stoppingToken);

                // Save logs
                await ((BotLogger)botLogger).FlushAsync(stoppingToken);

                if (result.Success)
                {
                    // Update bot status
                    if (bot.Status == "Starting")
                    {
                        bot.Status = "Running";
                    }

                    bot.NextRunAt = result.NextRunAt ?? DateTime.UtcNow.AddSeconds(bot.ExecutionIntervalSeconds);
                    bot.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation("Bot {BotId} executed successfully, next run at {NextRunAt}", 
                        bot.Id, bot.NextRunAt);
                }
                else
                {
                    _logger.LogWarning("Bot {BotId} execution failed: {Error}", bot.Id, result.ErrorMessage);

                    // Handle retry or mark as error
                    if (result.RetryDelay.HasValue)
                    {
                        bot.NextRunAt = DateTime.UtcNow.Add(result.RetryDelay.Value);
                    }
                    else
                    {
                        bot.Status = "Error";
                        bot.LastStatusReason = result.ErrorMessage;
                    }
                }

                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing bot {BotId}", bot.Id);

                bot.Status = "Error";
                bot.LastStatusReason = ex.Message;
                await context.SaveChangesAsync(stoppingToken);
            }
        }

        private async Task<object?> LoadStateObjectAsync(ApplicationDbContext context, Guid botId, Type type, CancellationToken cancellationToken)
        {
            var snapshot = await context.TradingBotRuntimeSnapshots
                .Where(s => s.TradingBotId == botId)
                .OrderByDescending(s => s.CapturedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (snapshot == null || string.IsNullOrEmpty(snapshot.RuntimeState))
                return null;

            try
            {
                return JsonSerializer.Deserialize(snapshot.RuntimeState, type);
            }
            catch
            {
                return null;
            }
        }

        private async Task SaveStateObjectAsync(ApplicationDbContext context, Guid botId, object state, CancellationToken cancellationToken)
        {
            var stateJson = JsonSerializer.Serialize(state);

            var snapshot = new Models.TradingBotRuntimeSnapshot
            {
                TradingBotId = botId,
                CapturedAt = DateTime.UtcNow,
                RuntimeState = stateJson,
                NextTickAt = DateTime.UtcNow
            };

            context.TradingBotRuntimeSnapshots.Add(snapshot);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public class BotExecutionJob
    {
        public Guid BotId { get; set; }
        public int UserId { get; set; }
        public string StrategyKey { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
    }
}

