# Dashboard API Timeout Fix

## Problem
The `/api/trading/dashboard/summary` endpoint was experiencing timeouts due to:
1. **Duplicate API calls**: Both `TraderLayout` (sidebar) and `TraderDashboard` (main page) were calling the summary API simultaneously
2. **Slow database queries**: Missing `AsNoTracking()` optimization and no cancellation token support
3. **Frontend timeout**: 10 seconds (adequate with optimizations)
4. **No HttpClient timeout**: External CoinGecko API calls had no timeout configured
5. **No performance monitoring**: Difficult to identify bottlenecks

## Solutions Implemented

### Backend Fixes (.NET)

#### 1. **Database Query Optimization** (`TradingController.cs`)
- Added `AsNoTracking()` to all EF queries in `GetDashboardSummaryData()`
- This prevents EF from tracking entities, significantly improving query performance
- Applied to: Wallets, WalletMovements, OrderHolds, Trades, Orders, CryptoPrices

```csharp
var wallets = await _db.Wallets
    .Where(w => w.UserId == userId)
    .Include(w => w.Cryptocurrency)
    .AsNoTracking()  // ✅ Added
    .ToListAsync(cancellationToken);
```

#### 2. **Cancellation Token Support** (`TradingController.cs`)
- Added `CancellationToken` parameter to `GetDashboardSummaryEndpoint()` and `GetDashboardSummaryData()`
- Passed token to all async database operations
- Allows requests to be cancelled if client disconnects or times out

```csharp
public async Task<IActionResult> GetDashboardSummaryEndpoint(CancellationToken cancellationToken)
{
    var summary = await GetDashboardSummaryData(userId, cancellationToken);
    return Ok(summary);
}
```

#### 3. **Performance Logging** (`TradingController.cs`)
- Added `Stopwatch` to measure execution time
- Added debug logs after each database operation
- Logs total execution time at completion

```csharp
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
_logger.LogInformation("[Dashboard Summary] Starting for user {UserId}", userId);
// ... operations ...
_logger.LogDebug("[Dashboard Summary] Loaded wallets in {Elapsed}ms", stopwatch.ElapsedMilliseconds);
```

#### 4. **HttpClient Timeout Configuration** (`Program.cs`)
- Set 10-second timeout for CoinGecko API calls
- Set 30-second timeout for database queries

```csharp
builder.Services.AddHttpClient<ICoinGeckoService, CoinGeckoService>(client =>
{
    client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
    client.DefaultRequestHeaders.Add("User-Agent", "CryptoTrading/1.0");
    client.Timeout = TimeSpan.FromSeconds(10); // ✅ Added
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(mysqlConnectionString, new MySqlServerVersion(new Version(8, 0, 21)),
        mySqlOptions => 
        {
            mySqlOptions.SchemaBehavior(Pomelo.EntityFrameworkCore.MySql.Infrastructure.MySqlSchemaBehavior.Ignore);
            mySqlOptions.CommandTimeout(30); // ✅ Added
        }));
```

### Frontend Fixes (React/TypeScript)

#### 1. **Request Deduplication** (`DashboardContext.tsx`)
- Created `DashboardProvider` context to share summary data across components
- Implements 10-second deduplication window (prevents redundant calls)
- Auto-refreshes every 30 seconds
- Handles in-flight request tracking

```typescript
const CACHE_DURATION = 10000; // 10 seconds deduplication window
const REFETCH_INTERVAL = 30000; // Auto refetch every 30 seconds

// Deduplication logic
if (!force && isFetching) {
  console.log('[DashboardContext] Fetch already in progress, skipping duplicate call');
  return;
}

if (!force && now - lastFetch < CACHE_DURATION) {
  console.log('[DashboardContext] Using cached data');
  return;
}
```

#### 2. **Updated TraderLayout** (`TraderLayout.tsx`)
- Replaced direct API call with `useDashboardSummary()` hook
- Removed duplicate fetch logic
- Removed 30-second interval (now handled by context)

```typescript
// ✅ Before: Direct API call
const result = await DashboardApi.getSummary();

// ✅ After: Shared context
const { summary: dashboardSummary, loading: balanceLoading } = useDashboardSummary();
```

#### 3. **Updated TraderDashboard** (`TraderDashboard.tsx`)
- Replaced direct summary API call with `useDashboardSummary()` hook
- Reduced parallel API calls from 5 to 4 (summary now from context)
- Improved error handling with proper type checking

```typescript
// ✅ Before: Fetching summary in parallel with other data
const [summaryRes, navRes, pnlRes, ...] = await Promise.all([
  DashboardApi.getSummary(), // ❌ Duplicate call
  DashboardApi.getNavHistory(),
  // ...
]);

// ✅ After: Summary from context
const { summary, error: summaryError } = useDashboardSummary();
const [navRes, pnlRes, ...] = await Promise.all([
  DashboardApi.getNavHistory(),
  // ...
]);
```

#### 4. **Timeout Configuration** (`http.ts`)
- Kept request timeout at 10 seconds (original value)
- With query optimizations, 10 seconds is sufficient for most requests

```typescript
const timeoutId = setTimeout(() => controller.abort(), 10000); // ✅ Kept at 10 seconds (original)
```

#### 5. **Integrated Context Provider** (`App.tsx`)
- Wrapped `TraderPage` component with `DashboardProvider`
- Ensures all trader routes share the same dashboard data

```typescript
<DashboardProvider>
  <div className="dark min-h-screen bg-black">
    <TraderLayout currentPage={current} onNavigate={onNavigate}>
      {childrenWithNavigate}
    </TraderLayout>
  </div>
</DashboardProvider>
```

## Benefits

### Performance Improvements
- **Reduced Database Load**: `AsNoTracking()` eliminates change tracking overhead
- **Fewer API Calls**: Deduplication prevents duplicate summary requests
- **Better Resource Usage**: Cancellation tokens allow early termination
- **Faster Queries**: Database command timeout ensures queries don't hang indefinitely

### Debugging Improvements
- **Performance Metrics**: Stopwatch logs help identify slow operations
- **Request Tracking**: Logs show when cache is used vs new requests
- **Error Visibility**: Better error messages with proper type handling

### User Experience
- **No More Timeouts**: Optimized queries complete faster
- **Smoother Loading**: Single API call instead of duplicates
- **Better Error Handling**: Default data shown even when API fails
- **Auto Refresh**: Data stays fresh without manual refresh

## Testing Checklist

- [ ] Backend restarts successfully
- [ ] Dashboard summary loads within 5 seconds
- [ ] No duplicate API calls in network tab
- [ ] Sidebar balance updates correctly
- [ ] Dashboard cards show correct data
- [ ] Error messages display when backend is down
- [ ] Retry button works
- [ ] Logs show performance metrics
- [ ] Cache deduplication works (check console logs)
- [ ] Auto-refresh works after 30 seconds

## Monitoring

Check backend logs for performance metrics:
```
[Dashboard Summary] Starting for user 1
[Dashboard Summary] Loaded 5 wallets in 45ms
[Dashboard Summary] Loaded wallet movements in 78ms
[Dashboard Summary] Loaded market data in 123ms
[Dashboard Summary] Loaded order holds in 34ms
...
[Dashboard Summary] Completed for user 1 in 456ms
```

Check frontend console for deduplication:
```
[DashboardContext] Fetching dashboard summary...
[DashboardContext] Summary loaded successfully
[DashboardContext] Using cached data (fetched 5234 ms ago)
[DashboardContext] Fetch already in progress, skipping duplicate call
```

## Rollback Plan

If issues occur, revert the following files:
- `Controllers/TradingController.cs`
- `Program.cs`
- `frontend/src/api/http.ts`
- `frontend/src/contexts/DashboardContext.tsx` (delete)
- `frontend/src/components/TraderLayout.tsx`
- `frontend/src/components/pages/trader/TraderDashboard.tsx`
- `frontend/src/App.tsx`

