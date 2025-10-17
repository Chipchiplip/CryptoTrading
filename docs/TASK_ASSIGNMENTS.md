# 👥 Team Task Assignments

**Project**: CryptoTrading API  
**Team Size**: 5 Members  
**Development Approach**: Fullstack (Each member handles complete features)  
**Updated**: October 17, 2025

---

## 🎯 Team Members & Assignments

### **Member 1 - Authentication System** 
**Branch**: `feature/auth-system`  
**Priority**: 🔴 **HIGH** (Must complete first - other features depend on this)

#### **Responsibilities:**
- ✅ User registration & login
- ✅ JWT token generation & validation
- ✅ Password hashing & security
- ✅ 2FA (TOTP) implementation
- ✅ Email verification
- ✅ Password reset flow
- ✅ Refresh token rotation

#### **Files to Implement:**
```
Services/Auth/AuthService.cs           # Main service implementation
Services/EmailService.cs               # Email functionality (if needed)
```

#### **API Endpoints Ready:**
- `POST /api/auth/login`
- `POST /api/auth/register`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`

#### **Database Models Ready:**
- `User.cs` - User entity with subscription tiers
- `RefreshToken.cs` - JWT refresh token management

#### **Dependencies:**
- BCrypt.Net (for password hashing)
- System.IdentityModel.Tokens.Jwt (for JWT)
- QRCoder (for 2FA QR codes)

---

### **Member 2 - Market Data System**
**Branch**: `feature/market-data`  
**Priority**: 🟡 **MEDIUM** (Can work in parallel with others)

#### **Responsibilities:**
- ✅ CoinGecko API integration
- ✅ Real-time price updates
- ✅ Market statistics
- ✅ Price history tracking
- ✅ Background price sync jobs
- ✅ SignalR price broadcasts
- ✅ Redis caching for performance

#### **Files to Implement:**
```
Services/MarketData/MarketDataService.cs    # Main service implementation
SignalR/MarketHub.cs                        # Real-time price updates (enhance)
```

#### **API Endpoints Ready:**
- `GET /api/market/cryptocurrencies`
- `GET /api/market/cryptocurrencies/{symbol}`
- `GET /api/market/prices`
- `GET /api/market/stats`

#### **Database Models Ready:**
- `Cryptocurrency.cs` - Crypto currency data
- `CryptoPrice.cs` - Price history tracking

#### **SignalR Hub Ready:**
- `MarketHub` - Real-time price updates to clients

#### **Dependencies:**
- HttpClient (for CoinGecko API)
- Hangfire (for background jobs)
- StackExchange.Redis (for caching)

---

### **Member 3 - Portfolio Management**
**Branch**: `feature/portfolio-management`  
**Priority**: 🟡 **MEDIUM** (Depends on Auth + Market Data)

#### **Responsibilities:**
- ✅ Watchlist CRUD operations
- ✅ Portfolio tracking & analytics
- ✅ Position management
- ✅ ROI calculations
- ✅ Performance metrics
- ✅ Portfolio charts data
- ✅ Asset allocation analysis

#### **Files to Implement:**
```
Services/Portfolio/PortfolioService.cs      # Main service implementation
```

#### **API Endpoints Ready:**
- `GET /api/portfolio/watchlists`
- `POST /api/portfolio/watchlists`
- `GET /api/portfolio/watchlists/{id}`
- `GET /api/portfolio/overview`

#### **Database Models Ready:**
- `UserWatchlist.cs` - User watchlists
- `WatchlistItem.cs` - Items in watchlists
- `Position.cs` - Portfolio positions

#### **Dependencies:**
- Authentication (Member 1)
- Market Data (Member 2) for current prices

---

### **Member 4 - Trading System**
**Branch**: `feature/trading-system`  
**Priority**: 🟡 **MEDIUM** (Depends on Auth + Market Data)

#### **Responsibilities:**
- ✅ Binance API integration (testnet)
- ✅ Order placement & management
- ✅ Balance tracking
- ✅ Trading bot logic
- ✅ Order history
- ✅ Real-time trading updates
- ✅ Risk management

#### **Files to Implement:**
```
Services/Trading/TradingService.cs          # Main service implementation
SignalR/TradingHub.cs                       # Real-time trading updates (enhance)
```

#### **API Endpoints Ready:**
- `GET /api/trading/balances`
- `POST /api/trading/orders`
- `GET /api/trading/orders`
- `GET /api/trading/orders/{id}`

#### **Database Models Ready:**
- `Balance.cs` - User trading balances
- `Order.cs` - Trading orders
- `Trade.cs` - Order executions
- `Bot.cs` - Trading bot configurations

#### **SignalR Hub Ready:**
- `TradingHub` - Real-time order updates to clients

#### **Dependencies:**
- Authentication (Member 1)
- Market Data (Member 2) for current prices
- Binance.Net (for Binance API)

---

### **Member 5 - Payment & Subscription**
**Branch**: `feature/payment-subscription`  
**Priority**: 🟢 **LOW** (Can be implemented last)

#### **Responsibilities:**
- ✅ Stripe integration
- ✅ Subscription plan management
- ✅ Payment processing
- ✅ Webhook handling
- ✅ Feature access control
- ✅ Billing history
- ✅ Subscription upgrades/downgrades

#### **Files to Implement:**
```
Services/Payment/PaymentService.cs          # Main service implementation
```

#### **API Endpoints Ready:**
- `GET /api/payment/plans`
- `POST /api/payment/checkout`
- `GET /api/payment/subscription`
- `POST /api/payment/webhook`

#### **Database Models Ready:**
- `Subscription.cs` - User subscriptions
- `PaymentHistory.cs` - Payment records

#### **Dependencies:**
- Authentication (Member 1)
- Stripe.net (for Stripe API)

---

## 📋 Development Workflow

### **Phase 1: Foundation (Days 1-2)**
**Priority Order:**
1. **Member 1** - Authentication (MUST complete first)
2. **Member 2** - Market Data (parallel with Auth testing)

### **Phase 2: Core Features (Days 3-4)**
**Parallel Development:**
3. **Member 3** - Portfolio (depends on Auth + Market)
4. **Member 4** - Trading (depends on Auth + Market)
5. **Member 5** - Payment (can start anytime)

### **Phase 3: Integration (Days 5-6)**
- Connect all services
- End-to-end testing
- SignalR real-time updates
- Performance optimization

### **Phase 4: Polish (Day 7)**
- Bug fixes
- Documentation
- Sprint demo preparation

---

## 🔄 Implementation Steps for Each Member

### **Step 1: Setup Development Environment**
```bash
git clone https://github.com/Chipchiplip/CryptoTrading.git
cd CryptoTrading
git checkout -b feature/your-feature-name
dotnet build
dotnet run
```

### **Step 2: Implement Your Service**
```csharp
// Example: Services/Auth/AuthService.cs
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
        // 1. Validate user credentials
        // 2. Generate JWT token
        // 3. Create refresh token
        // 4. Return AuthResultDto
    }
    
    // ... implement all interface methods
}
```

### **Step 3: Enable Your Service in Program.cs**
```csharp
// Uncomment your service registration:
builder.Services.AddScoped<IAuthService, AuthService>();
```

### **Step 4: Test Your Implementation**
```bash
dotnet build
dotnet run
# Open: http://localhost:5186/swagger
# Test your endpoints
```

### **Step 5: Add Database Integration**
```csharp
// Enable database context in Program.cs:
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### **Step 6: Add Authentication (if needed)**
```csharp
// Enable JWT authentication in Program.cs:
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* configuration */);
```

### **Step 7: Commit & Push**
```bash
git add .
git commit -m "feat: implement [your feature] service"
git push origin feature/your-feature-name
# Create Pull Request
```

---

## 🧪 Testing Strategy

### **Unit Testing**
Each member should write unit tests for their service:
```csharp
// Tests/Services/Auth/AuthServiceTests.cs
[Test]
public async Task LoginAsync_ValidCredentials_ReturnsAuthResult()
{
    // Arrange
    var authService = new AuthService(mockUnitOfWork, mockCurrentUser);
    var loginDto = new LoginDto("test@example.com", "password");
    
    // Act
    var result = await authService.LoginAsync(loginDto);
    
    // Assert
    Assert.IsNotNull(result);
    Assert.IsNotEmpty(result.AccessToken);
}
```

### **Integration Testing**
Test API endpoints:
```bash
# Use Swagger UI or curl
curl -X POST http://localhost:5186/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password"}'
```

---

## 📊 Progress Tracking

### **Daily Standup Questions:**
1. What did you complete yesterday?
2. What are you working on today?
3. Any blockers or dependencies?

### **Sprint Goals:**
- [ ] **Member 1**: Authentication working (login/register)
- [ ] **Member 2**: Market data syncing from CoinGecko
- [ ] **Member 3**: Watchlist CRUD operational
- [ ] **Member 4**: Basic trading orders working
- [ ] **Member 5**: Stripe checkout flow working

### **Definition of Done:**
- ✅ Service interface fully implemented
- ✅ API endpoints working
- ✅ Database integration complete
- ✅ Unit tests written
- ✅ Swagger documentation updated
- ✅ Code reviewed and merged

---

## 🚨 Important Notes

### **Dependencies:**
- **Member 1 (Auth)** must complete first - others need authentication
- **Member 2 (Market)** should complete early - others need price data
- **Members 3, 4, 5** can work in parallel once Auth is ready

### **Communication:**
- Daily standup at 9:00 AM
- Use team chat for quick questions
- Create GitHub issues for bugs/features
- Code review required before merging

### **Code Standards:**
- Follow C# naming conventions
- Add XML documentation to public methods
- Use async/await for all database operations
- Handle exceptions gracefully
- Log important events

---

**Repository**: https://github.com/Chipchiplip/CryptoTrading  
**Team Lead**: Đặng Công Vũ Hoàng  
**Sprint Duration**: 7 days  
**Demo Date**: End of Sprint 1

Let's build something amazing together! 🚀💪
