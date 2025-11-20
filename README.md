# 🚀 CryptoTrading API

Crypto Trading Platform API được xây dựng với ASP.NET Core 9.0, theo cấu trúc đơn giản và dễ phát triển.

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/Chipchiplip/CryptoTrading)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

## 📁 Cấu Trúc Project

```
📦 CryptoTrading/
├── 📁 Controllers/          # 5 API Controllers với DTOs
│   ├── AuthController.cs
│   ├── MarketController.cs
│   ├── PortfolioController.cs
│   ├── TradingController.cs
│   └── PaymentController.cs
├── 📁 Data/                # Database Context & Seed Data
│   ├── ApplicationDbContext.cs
│   └── SeedData.cs
├── 📁 Interfaces/          # Infrastructure Interfaces (5 interfaces)
├── 📁 Models/              # 13 Entity Models
├── 📁 Repositories/        # Repository Pattern Implementation
├── 📁 Services/            # Business Logic Services (5 services)
├── 📁 SignalR/             # Real-time Communication Hubs
├── Program.cs              # Application Entry Point
└── appsettings.json        # Configuration
```

## 🛠️ Technologies

- **Framework**: ASP.NET Core 9.0
- **Database**: MySQL + Entity Framework Core (Pomelo)
- **Authentication**: JWT Bearer Token
- **Real-time**: SignalR
- **Caching**: Redis
- **Documentation**: Swagger/OpenAPI
- **Architecture**: Single Project với Repository Pattern

## 🚀 Getting Started

### Prerequisites

- .NET 9.0 SDK
- MySQL (Aiven Cloud hoặc Docker)
- Redis (optional, for caching)

### 1. Clone & Setup

```bash
git clone <repository-url>
cd CryptoTrading
```

### 2. Database Setup

**Option A: Aiven Cloud MySQL (Recommended)**
```bash
# Connection string đã được cấu hình sẵn trong appsettings.Development.json
dotnet ef database update
```

**Option B: Docker MySQL**
```bash
# Start MySQL & Redis
docker-compose up -d

# Update connection string in appsettings.json:
"DefaultConnection": "Server=localhost;Port=3306;Database=crypto_trading;User ID=root;Password=YourStrong@Passw0rd;SslMode=None;"
```

### 3. Run Application

```bash
dotnet run
```

API sẽ chạy tại:
- **HTTP**: http://localhost:5186
- **HTTPS**: https://localhost:7269
- **Swagger UI**: http://localhost:5186/swagger

## 📋 API Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration
- `POST /api/auth/refresh` - Refresh token
- `POST /api/auth/logout` - User logout

### Market Data
- `GET /api/market/cryptocurrencies` - Get all cryptocurrencies
- `GET /api/market/cryptocurrencies/{symbol}` - Get cryptocurrency by symbol
- `GET /api/market/prices` - Get current prices
- `GET /api/market/stats` - Get market statistics

### Portfolio
- `GET /api/portfolio/watchlists` - Get user watchlists
- `POST /api/portfolio/watchlists` - Create watchlist
- `GET /api/portfolio/overview` - Get portfolio overview

### Trading
- `GET /api/trading/balances` - Get user balances
- `POST /api/trading/orders` - Place new order
- `GET /api/trading/orders` - Get user orders

### Payment
- `GET /api/payment/plans` - Get subscription plans
- `POST /api/payment/checkout` - Create checkout session
- `GET /api/payment/subscription` - Get user subscription

## 🔧 Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-mysql-host;Port=20158;Database=crypto_trading;User ID=avnadmin;Password=your-password;SslMode=Required;"
  },
  "Jwt": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "CryptoTradingApi",
    "Audience": "CryptoTradingApp"
  },
  "ApiKeys": {
    "CoinGecko": "your-coingecko-api-key",
    "Binance": {
      "ApiKey": "your-binance-api-key",
      "SecretKey": "your-binance-secret-key"
    }
  }
}
```

### Cloudflare avatar uploads (R2-only)

If you enable the optional avatar upload flow, fill in the `Cloudflare`
configuration section so the backend can mint **R2 S3** presigned URLs and
validate avatars served from the configured R2 public host. See
[`docs/CLOUDFLARE_IMAGES_SETUP.md`](docs/CLOUDFLARE_IMAGES_SETUP.md) for the
required keys and how to wire up the R2 bucket/prefix into
`AllowedAvatarDomains`.

## 👥 Team Development

### Team Member Assignments

| Member | Feature | Branch | Priority | Status |
|--------|---------|--------|----------|--------|
| **Member 1** | Authentication System | `feature/auth-system` | 🔴 HIGH | Ready to implement |
| **Member 2** | Market Data System | `feature/market-data` | 🟡 MEDIUM | Ready to implement |
| **Member 3** | Portfolio Management | `feature/portfolio-management` | 🟡 MEDIUM | Ready to implement |
| **Member 4** | Trading System | `feature/trading-system` | 🟡 MEDIUM | Ready to implement |
| **Member 5** | Payment & Subscription | `feature/payment-subscription` | 🟢 LOW | Ready to implement |

### Development Dependencies

1. **Authentication** (Member 1) - 🔴 **Must complete first** - others depend on this
2. **Market Data** (Member 2) - 🟡 Can work in parallel, needed by Portfolio & Trading
3. **Portfolio** (Member 3) - Depends on Authentication + Market Data
4. **Trading** (Member 4) - Depends on Authentication + Market Data  
5. **Payment** (Member 5) - Depends on Authentication only

### Git Workflow

```bash
# Tạo feature branch
git checkout -b feature/auth-implementation

# Implement feature
# ...

# Commit và push
git add .
git commit -m "feat: implement user authentication"
git push origin feature/auth-implementation

# Tạo Pull Request
```

## 🧪 Testing

```bash
# Run tests
dotnet test

# Test API endpoints
curl -X GET "https://localhost:7269/api/health"
```

## 📦 Deployment

### Docker Build

```bash
# Build image
docker build -t cryptotrading-api .

# Run container
docker run -p 8080:80 cryptotrading-api
```

## 🔍 Monitoring

- **Health Check**: `/health`
- **Swagger UI**: `/swagger`
- **Redis Commander**: http://localhost:8081 (if using Docker)

## 📚 Documentation

- **API Documentation**: Available at `/swagger` when running
- **Getting Started**: [docs/GETTING_STARTED.md](docs/GETTING_STARTED.md)
- **Task Assignments**: [docs/TASK_ASSIGNMENTS.md](docs/TASK_ASSIGNMENTS.md)
- **Documentation Index**: [docs/README.md](docs/README.md)
- **SignalR Hubs**:
  - Market Hub: `/hubs/market` - Real-time price updates
  - Trading Hub: `/hubs/trading` - Real-time trading updates

## 🎯 Current Status

- ✅ **Project Structure**: Complete
- ✅ **API Controllers**: 5 controllers with endpoints ready
- ✅ **Entity Models**: 13 models with relationships
- ✅ **Service Interfaces**: 5 interfaces with 50+ methods defined
- ✅ **Infrastructure**: Repository pattern, SignalR, JWT auth ready
- ✅ **Configuration**: Database, JWT, CORS, Swagger configured
- ⏳ **Service Implementation**: Ready for team development
- ⏳ **Database Integration**: Ready to be enabled
- ⏳ **Testing**: Ready for implementation

## 🤝 Contributing

1. Choose your assigned feature from [Task Assignments](docs/TASK_ASSIGNMENTS.md)
2. Create feature branch: `git checkout -b feature/your-feature`
3. Implement your service interface
4. Test with Swagger UI
5. Submit Pull Request for code review

## 📞 Support

- **Repository**: https://github.com/Chipchiplip/CryptoTrading
- **Team Lead**: Đặng Công Vũ Hoàng
- **API Documentation**: http://localhost:5186/swagger (when running)

## 📄 License

This project is licensed under the MIT License.