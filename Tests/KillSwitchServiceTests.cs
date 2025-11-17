using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Data;
using CryptoTrading.Models;
using CryptoTrading.Services.Risk;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CryptoTrading.Tests;

public class KillSwitchServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly KillSwitchService _service;
    private readonly Mock<ILogger<KillSwitchService>> _loggerMock = new();

    public KillSwitchServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Trading:KillSwitch:Enabled"] = "true",
                ["Trading:KillSwitch:DefaultConsecutiveLossLimit"] = "3",
                ["Trading:KillSwitch:DefaultDailyLossLimit"] = "10",
                ["Trading:KillSwitch:DefaultMaxDrawdownPercent"] = "20"
            })
            .Build();

        _service = new KillSwitchService(_context, _loggerMock.Object, configuration);
    }

    [Fact]
    public async Task CheckKillSwitchAsync_WithEmptyBotId_ReturnsContinue()
    {
        var result = await _service.CheckKillSwitchAsync(Guid.Empty, 1);

        Assert.False(result.ShouldStop);
    }

    [Fact]
    public async Task RecordTradeResultAsync_PersistsRiskStateForNewBot()
    {
        var botId = await CreateDemoBotAsync();

        await _service.RecordTradeResultAsync(botId, -5m, isProfit: false);

        var state = await _context.BotRiskStates.FirstOrDefaultAsync(s => s.BotId == botId);
        Assert.NotNull(state);
        Assert.Equal(1, state!.ConsecutiveLosses);
        Assert.Equal(5m, state.DailyLoss);
    }

    private async Task<Guid> CreateDemoBotAsync()
    {
        var user = new User
        {
            Id = 42,
            Email = "demo@risk.test",
            PasswordHash = "hash",
            FullName = "Risk Demo",
            Role = "User",
            Level = "Beginner",
            IsActive = true
        };

        var strategy = new BotStrategyDefinition
        {
            Id = Guid.NewGuid(),
            StrategyKey = "risk-smoke",
            Version = "1.0.0",
            DisplayName = "Risk Smoke Test",
            Description = "Temporary bot for kill switch tests",
            IsActive = true
        };

        var bot = new TradingBot
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            StrategyDefinitionId = strategy.Id,
            Name = "Risk Smoke Bot",
            Status = "Running",
            BaseAsset = "BTC",
            QuoteAsset = "USDT",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.BotStrategyDefinitions.Add(strategy);
        _context.TradingBots.Add(bot);
        await _context.SaveChangesAsync();

        return bot.Id;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

