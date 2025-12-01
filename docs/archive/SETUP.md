# Developer Onboarding Checklist (Current)

## Tools
- .NET 9 SDK
- Node.js 20+
- MySQL 8
- Optional: Redis (for future backplane/locks), Docker

## Clone & Install
```bash
git clone <repo>
cd CryptoTrading
dotnet restore
cd frontend && npm install
```

## Configure Secrets (appsettings.Development.json)
- `ConnectionStrings:DefaultConnection` (MySQL)
- `JwtSettings` secret/issuer/audience
- API keys: CoinGecko, Stripe, VNPay, Cloudflare R2, SMTP, Gemini/OpenAI
- Keep out of git

## Migrations & Seed
```bash
dotnet ef database update
```

## Run
- Backend: `dotnet run` (HTTP 5186/HTTPS 7269)
- Frontend: `npm run dev` (http://localhost:5173)

## Payments (test mode)
- Stripe keys + webhook secret
- VNPay sandbox: TmnCode/HashSecret/BaseUrl

## Cloudflare R2
- Fill Cloudflare section for avatar presign

## Optional Services
- StockAgent: see `StockAgent/README.md`
- dashboard-api: see `dashboard-api/README.md`

## Test Flows
- Auth (register/login/2FA)
- Order placement + matcher
- Bot run + SignalR updates
- Payment deposit (Stripe/VNPay test) return/callback
- Portfolio overview/performance
