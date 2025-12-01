# Getting Started

## Prerequisites
- .NET 9 SDK
- MySQL 8 (provide connection string)
- Node 20+ for frontend
- Optional: Redis (cache/backplane), Stripe/VNPay credentials, Cloudflare R2, Gmail/SMTP, Gemini/OpenAI keys

## 1) Clone
```bash
git clone <repository-url>
cd CryptoTrading
dotnet restore
```

## 2) Configure settings
- Copy `appsettings.Development.json` and replace secrets:
  - `ConnectionStrings:DefaultConnection` (MySQL)
  - `JwtSettings` secret/issuer/audience
  - API keys: CoinGecko, Stripe, VNPay, Cloudflare, Gmail, Gemini/OpenAI
- Keep this file out of git.

## 3) Database
```bash
dotnet ef database update
```

## 4) Run API
```bash
dotnet run
# HTTP 5186, HTTPS 7269
```
Swagger: `http://localhost:5186/swagger`  
Health: `/health`  
SignalR hubs: `/marketHub`, `/botHub`.

## 5) Run Frontend
```bash
cd frontend
npm install
npm run dev
```
Default origin http://localhost:5173 (CORS allowed in Program.cs).

## 6) Background services
Enabled by default: CryptoSync, RealtimeBroadcast, OrderMatching, BotExecution, BotMonitor, SubscriptionExpiration.

## 7) Payments/Uploads
- Stripe/VNPay: supply keys and webhook secrets before testing callbacks.
- Cloudflare R2: fill `Cloudflare` section to generate avatar upload URLs.

## 8) Common issues
- Connection errors: verify MySQL reachable and SSL mode matches host.
- Pending migrations: run `dotnet ef database update`.
- Secrets committed: rotate immediately.
