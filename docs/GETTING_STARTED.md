# 🚀 Getting Started Guide

## Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 9 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Docker Desktop** - [Download here](https://www.docker.com/products/docker-desktop)
- **Git** - [Download here](https://git-scm.com/downloads)
- **Visual Studio Code** or **Visual Studio 2022** (recommended)

### Recommended VS Code Extensions
- C# Dev Kit
- Docker
- SQL Server (mssql)
- GitLens

## Step 1: Clone the Repository

```bash
git clone https://github.com/Chipchiplip/CryptoTrading.git
cd CryptoTrading
```

## Step 2: Start Infrastructure (Optional)

If you want to use Docker for SQL Server and Redis:

```bash
docker-compose up -d
```

Verify services are running:
```bash
docker ps
```

You should see:
- `cryptotrading-sqlserver` (port 1433)
- `cryptotrading-redis` (port 6379)
- `cryptotrading-redis-commander` (port 8081) - Redis UI

## Step 3: Database Setup

### Option A: LocalDB (Recommended for Development)

The project is configured to use SQL Server LocalDB by default. No additional setup required.

Connection string in `appsettings.json`:
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CryptoTradingDb;Trusted_Connection=true;MultipleActiveResultSets=true"
```

### Option B: Docker SQL Server

If using Docker, update the connection string in `appsettings.json`:
```json
"DefaultConnection": "Server=localhost,1433;Database=CryptoTradingDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true"
```

## Step 4: Configure API Settings

The default `appsettings.json` is ready for development. For production, create `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CryptoTradingDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "SecretKey": "your-super-secret-jwt-key-here-minimum-32-characters-long-for-security",
    "Issuer": "CryptoTradingApi",
    "Audience": "CryptoTradingApp",
    "ExpiryInMinutes": 60,
    "RefreshTokenExpiryInDays": 7
  },
  "ApiKeys": {
    "CoinGecko": "your-coingecko-api-key-here",
    "Binance": {
      "ApiKey": "your-binance-api-key-here",
      "SecretKey": "your-binance-secret-key-here"
    },
    "Stripe": {
      "PublishableKey": "pk_test_your-stripe-publishable-key-here",
      "SecretKey": "sk_test_your-stripe-secret-key-here",
      "WebhookSecret": "whsec_your-stripe-webhook-secret-here"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

**Important**: Add `appsettings.Development.json` to `.gitignore` (already done)

## Step 5: Build the Project

```bash
dotnet build
```

Fix any build errors before proceeding.

## Step 6: Run the API

```bash
dotnet run
```

The API should start on:
- **HTTPS**: https://localhost:7269
- **HTTP**: http://localhost:5186

## Step 7: Test the API

### Using Swagger UI

Open your browser and navigate to:
```
http://localhost:5186/swagger
```

You should see the Swagger UI with all available endpoints:

- **Auth**: `/api/auth/login`, `/api/auth/register`, etc.
- **Market**: `/api/market/cryptocurrencies`, `/api/market/prices`, etc.
- **Portfolio**: `/api/portfolio/watchlists`, `/api/portfolio/overview`, etc.
- **Trading**: `/api/trading/balances`, `/api/trading/orders`, etc.
- **Payment**: `/api/payment/plans`, `/api/payment/checkout`, etc.

### Using curl

```bash
# Health check
curl http://localhost:5186/health

# Get cryptocurrencies (will return NotImplementedException until implemented)
curl http://localhost:5186/api/market/cryptocurrencies
```

## Step 8: Development Workflow

### Hot Reload

The API supports hot reload. Make changes to your code and the app will automatically restart:

```bash
dotnet watch run
```

### Implementing Services

Each team member should implement their assigned service. For example:

```csharp
// Services/Auth/AuthService.cs
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AuthService(IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<AuthResultDto> LoginAsync(LoginDto dto)
    {
        // TODO: Implement login logic
        // 1. Validate credentials
        // 2. Generate JWT token
        // 3. Return AuthResultDto
        
        throw new NotImplementedException("Login functionality will be implemented by team member");
    }
    
    // ... implement other methods
}
```

### Enabling Services in Program.cs

As you implement services, uncomment the corresponding lines in `Program.cs`:

```csharp
// Uncomment as services are implemented:
// builder.Services.AddScoped<IAuthService, AuthService>();
// builder.Services.AddScoped<IMarketDataService, MarketDataService>();
// builder.Services.AddScoped<IPortfolioService, PortfolioService>();
// builder.Services.AddScoped<ITradingService, TradingService>();
// builder.Services.AddScoped<IPaymentService, PaymentService>();
```

Also uncomment database and authentication configuration:

```csharp
// Database
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
// var jwtSettings = builder.Configuration.GetSection("Jwt");
// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(/* JWT configuration */);
```

## Step 9: Database Migrations (When Ready)

When you're ready to use the database:

```bash
# Add migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

## Common Issues

### Issue: Can't connect to LocalDB

**Solution**:
```bash
# Install SQL Server LocalDB if not already installed
# Windows: Download from Microsoft website
# Or use Docker SQL Server instead
```

### Issue: Port 5186 already in use

**Solution**:
```bash
# Check what's using the port
netstat -ano | findstr :5186

# Kill the process or change port in launchSettings.json
```

### Issue: Build errors after pulling latest changes

**Solution**:
```bash
# Clean solution
dotnet clean

# Restore and rebuild
dotnet restore
dotnet build
```

## Team Development

### Feature Branch Workflow

```bash
# 1. Create feature branch
git checkout -b feature/auth-implementation

# 2. Implement your service
# Edit Services/Auth/AuthService.cs

# 3. Test your implementation
dotnet build
dotnet run

# 4. Commit and push
git add .
git commit -m "feat: implement user authentication service"
git push origin feature/auth-implementation

# 5. Create Pull Request on GitHub
```

### Team Member Assignments

#### **Member 1 - Authentication** `feature/auth-system`
- Implement `Services/Auth/AuthService.cs`
- Add password hashing (BCrypt)
- JWT token generation and validation
- 2FA support (TOTP)
- Email verification

#### **Member 2 - Market Data** `feature/market-data`
- Implement `Services/MarketData/MarketDataService.cs`
- CoinGecko API integration
- Real-time price updates via SignalR
- Background jobs for price sync
- Caching with Redis

#### **Member 3 - Portfolio** `feature/watchlist-portfolio`
- Implement `Services/Portfolio/PortfolioService.cs`
- Watchlist CRUD operations
- Portfolio tracking and analytics
- ROI calculations
- Performance metrics

#### **Member 4 - Trading** `feature/trading-system`
- Implement `Services/Trading/TradingService.cs`
- Binance API integration (testnet)
- Order management
- Balance tracking
- Trading bot logic

#### **Member 5 - Payment** `feature/payments-subscription`
- Implement `Services/Payment/PaymentService.cs`
- Stripe integration
- Subscription management
- Webhook handling
- Feature access control

## Next Steps

1. **Choose Your Feature**: Pick your assigned feature from the list above
2. **Create Branch**: `git checkout -b feature/your-feature`
3. **Implement Service**: Start with the service interface
4. **Test Endpoints**: Use Swagger UI to test your API
5. **Add Database Logic**: Implement data persistence
6. **Add Real-time Updates**: Use SignalR for live updates
7. **Write Tests**: Add unit tests for your service
8. **Create Pull Request**: Submit for code review

## Useful Commands

```bash
# View project structure
tree /f

# Build specific project
dotnet build CryptoTrading.csproj

# Run with specific environment
dotnet run --environment Development

# Watch for changes
dotnet watch run

# Format code
dotnet format
```

## Development Tools

### Swagger UI
Access API documentation at: http://localhost:5186/swagger

### Redis Commander (if using Docker)
Access Redis UI at: http://localhost:8081

### Health Checks
http://localhost:5186/health

## Support

If you encounter any issues:

1. Check this guide first
2. Search existing GitHub issues
3. Ask in team chat
4. Contact team lead

## Success Criteria

By the end of Sprint 1, we should have:

- [ ] All 5 services implemented
- [ ] API endpoints working
- [ ] Database integration complete
- [ ] Basic authentication working
- [ ] Real-time updates via SignalR
- [ ] Swagger documentation complete
- [ ] Ready for frontend integration

Happy coding! 🚀

---

**Repository**: https://github.com/Chipchiplip/CryptoTrading  
**API URL**: http://localhost:5186/swagger  
**Team Lead**: Đặng Công Vũ Hoàng