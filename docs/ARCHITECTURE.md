# Architecture Documentation

## Overview

The Crypto Trading Web platform follows **Clean Architecture** principles with clear separation of concerns across layers.

## Project Structure

```
src/
├── CryptoTrading.Api/              # Presentation Layer
│   ├── Controllers/                # REST API endpoints
│   ├── Middleware/                 # Custom middleware (idempotency, rate limiting)
│   ├── Hubs/                       # SignalR hubs (real-time updates)
│   └── Program.cs                  # Application entry point
│
├── CryptoTrading.Application/      # Application Layer
│   ├── Services/                   # Business logic services
│   ├── DTOs/                       # Data Transfer Objects
│   ├── Validators/                 # FluentValidation validators
│   ├── Mappings/                   # AutoMapper profiles
│   └── Interfaces/                 # Service contracts
│
├── CryptoTrading.Domain/           # Domain Layer
│   ├── Entities/                   # Domain entities
│   ├── Enums/                      # Domain enumerations
│   ├── ValueObjects/               # Value objects (immutable)
│   └── Interfaces/                 # Repository contracts
│
├── CryptoTrading.Infrastructure/   # Infrastructure Layer
│   ├── Data/                       # EF Core DbContext & configurations
│   ├── Repositories/               # Repository implementations
│   ├── ExternalApis/               # Third-party API clients
│   │   ├── CoinGecko/             # Market data
│   │   ├── Binance/               # Trading
│   │   └── Stripe/                # Payments
│   ├── Redis/                      # Redis cache & distributed lock
│   ├── BackgroundJobs/             # Hangfire job definitions
│   └── Encryption/                 # Encryption services
│
└── CryptoTrading.Shared/           # Shared Library
    ├── Constants/                  # App-wide constants
    ├── Extensions/                 # Extension methods
    ├── Helpers/                    # Utility helpers
    └── Exceptions/                 # Custom exceptions
```

## Dependency Flow

```
Api → Application → Domain
  ↓         ↓
Infrastructure
```

**Rules:**
- `Domain` has NO dependencies (pure C# classes)
- `Application` depends only on `Domain`
- `Infrastructure` depends on `Domain` and `Application`
- `Api` depends on `Application` and `Infrastructure` (composition root)

## Key Design Patterns

### 1. Repository Pattern
```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}
```

### 2. Unit of Work Pattern
```csharp
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IOrderRepository Orders { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
```

### 3. CQRS (Light)
Separate read and write operations at the service layer:
```csharp
// Queries (read-only, optimized)
public interface IMarketDataQueryService
{
    Task<IEnumerable<CryptoDto>> GetTop100Async();
}

// Commands (write operations)
public interface IMarketDataCommandService
{
    Task UpdatePricesAsync();
}
```

### 4. Outbox Pattern
Ensures exactly-once event publishing:
```csharp
// 1. Save entity + outbox event in same transaction
await _uow.Orders.AddAsync(order);
await _uow.Outbox.AddAsync(new OutboxEvent { /* ... */ });
await _uow.SaveChangesAsync();

// 2. Background job processes outbox
var events = await _uow.Outbox.GetUnprocessedAsync();
foreach (var evt in events)
{
    await _eventBus.PublishAsync(evt);
    evt.ProcessedAtUtc = DateTime.UtcNow;
}
```

### 5. Idempotency Middleware
Prevents duplicate requests:
```csharp
[Idempotent]
[HttpPost("orders")]
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
{
    // Idempotency middleware checks for duplicate request key
    var order = await _orderService.CreateAsync(dto);
    return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
}
```

## Concurrency Control

### Optimistic Concurrency (EF Core)
```csharp
public class Order
{
    public Guid Id { get; set; }
    [Timestamp]
    public byte[] RowVersion { get; set; }
}

try
{
    await _uow.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    throw new ConflictException("Order was modified by another user.");
}
```

### Distributed Lock (Redis)
```csharp
public async Task PlaceOrderAsync(PlaceOrderDto dto)
{
    var lockKey = $"lock:{dto.UserId}:{dto.Symbol}";
    await _redisLock.AcquireAsync(lockKey, TimeSpan.FromSeconds(10), async () =>
    {
        // Critical section: check balance, place order
        await _orderService.ExecuteAsync(dto);
    });
}
```

## Security Layers

### 1. Authentication (JWT)
```csharp
[Authorize]
[HttpGet("profile")]
public async Task<IActionResult> GetProfile()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    // ...
}
```

### 2. Authorization (Policy-Based)
```csharp
[Authorize(Policy = "RequireProTier")]
[HttpPost("bots")]
public async Task<IActionResult> CreateBot([FromBody] CreateBotDto dto)
{
    // Only Pro users can create bots
}
```

### 3. Rate Limiting (Per-User)
```csharp
app.UseRateLimiter();

[EnableRateLimiting("perUser")]
[HttpPost("orders")]
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
{
    // Limited to 10 requests/second per user
}
```

## Real-Time Communication (SignalR)

### Server-Side Hub
```csharp
public class MarketDataHub : Hub
{
    public async Task SubscribeToSymbol(string symbol)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"market:{symbol}");
    }

    public async Task UnsubscribeFromSymbol(string symbol)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"market:{symbol}");
    }
}

// Background job pushes updates
await _hubContext.Clients.Group($"market:{symbol}")
    .SendAsync("PriceUpdated", priceDto);
```

### Client-Side (React)
```typescript
const connection = new HubConnectionBuilder()
  .withUrl("/hubs/market-data")
  .build();

connection.on("PriceUpdated", (data) => {
  console.log("Price updated:", data);
});

await connection.start();
await connection.invoke("SubscribeToSymbol", "BTCUSDT");
```

## External API Integration

### 1. CoinGecko (Market Data)
```csharp
public class CoinGeckoService : ICoinGeckoService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;

    public async Task<IEnumerable<CoinDto>> GetTopCoinsAsync(int limit = 100)
    {
        var cacheKey = $"coingecko:top:{limit}";
        if (_cache.TryGetValue(cacheKey, out IEnumerable<CoinDto> cached))
            return cached;

        var response = await _http.GetAsync($"/coins/markets?vs_currency=usd&per_page={limit}");
        var coins = await response.Content.ReadFromJsonAsync<IEnumerable<CoinDto>>();
        _cache.Set(cacheKey, coins, TimeSpan.FromMinutes(5));
        return coins;
    }
}
```

### 2. Binance (Trading)
```csharp
public class BinanceService : IBinanceService
{
    public async Task<OrderResult> PlaceOrderAsync(PlaceOrderRequest req)
    {
        // Sign request with HMAC-SHA256
        var signature = _crypto.SignHmacSha256(req.ToQueryString(), apiSecret);
        var url = $"{_baseUrl}/order?{req.ToQueryString()}&signature={signature}";
        
        var response = await _http.PostAsync(url, null);
        return await response.Content.ReadFromJsonAsync<OrderResult>();
    }
}
```

### 3. Stripe (Payments)
```csharp
public class StripeService : IStripeService
{
    public async Task<Subscription> CreateSubscriptionAsync(string userId, string priceId)
    {
        var options = new SubscriptionCreateOptions
        {
            Customer = customerId,
            Items = new List<SubscriptionItemOptions>
            {
                new() { Price = priceId }
            }
        };
        
        return await _subscriptionService.CreateAsync(options);
    }
}
```

## Background Jobs (Hangfire)

```csharp
// Job definition
public class UpdateMarketDataJob
{
    [DisableConcurrentExecution(60)]
    public async Task ExecuteAsync()
    {
        var coins = await _coinGeckoService.GetTopCoinsAsync();
        await _marketDataService.UpdatePricesAsync(coins);
    }
}

// Job scheduling
RecurringJob.AddOrUpdate<UpdateMarketDataJob>(
    "update-market-data",
    job => job.ExecuteAsync(),
    Cron.Minutely
);
```

## Error Handling

### Global Exception Handler
```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>();
        var response = error?.Error switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };
        
        context.Response.StatusCode = response.Item1;
        await context.Response.WriteAsJsonAsync(new { error = response.Item2 });
    });
});
```

## Testing Strategy

### Unit Tests
```csharp
[Fact]
public async Task CreateOrder_WhenBalanceInsufficient_ThrowsException()
{
    // Arrange
    var service = new OrderService(_mockRepo.Object, _mockBinance.Object);
    
    // Act & Assert
    await Assert.ThrowsAsync<InsufficientBalanceException>(
        () => service.CreateOrderAsync(dto)
    );
}
```

### Integration Tests
```csharp
public class OrdersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task POST_Orders_Returns201Created()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.PostAsJsonAsync("/api/orders", dto);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

## Deployment

### Docker Compose (Development)
```yaml
services:
  api:
    image: cryptotrading-api:latest
    ports:
      - "7001:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=...
```

### Production (Azure/AWS)
- **API**: App Service / EC2 + Auto Scaling
- **Database**: Azure SQL / RDS SQL Server
- **Cache**: Azure Redis / ElastiCache
- **Storage**: Azure Blob / S3 (for backups)
- **CDN**: Azure CDN / CloudFront (for frontend)

## Monitoring & Observability

### Structured Logging (Serilog)
```csharp
Log.Information("Order created: {OrderId} for User: {UserId}", orderId, userId);
```

### Application Insights / OpenTelemetry
```csharp
using var activity = ActivitySource.StartActivity("PlaceOrder");
activity?.SetTag("user.id", userId);
activity?.SetTag("order.symbol", symbol);
```

---

**Next Steps:**
1. Implement authentication service
2. Create DbContext and configure EF Core
3. Build market data sync job
4. Implement trading logic with Binance
5. Add comprehensive tests

