# CryptoTrading Setup Guide

## 🚀 Quick Start

### 1. Clone Repository
```bash
git clone <repository-url>
cd CryptoTrading
```

### 2. Configuration Setup

#### Create `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-mysql-host;Port=20158;Database=defaultdb;User ID=avnadmin;Password=your-password;SslMode=Required;"
  },
  "JwtSettings": {
    "Secret": "your-super-secret-jwt-key-here-minimum-32-characters-long-for-security-purposes"
  },
  "CoinGecko": {
    "ApiKey": "your-coingecko-api-key"
  },
  "ApiKeys": {
    "CoinGecko": "your-coingecko-api-key",
    "Binance": {
      "ApiKey": "your-binance-api-key",
      "SecretKey": "your-binance-secret-key"
    },
    "Stripe": {
      "PublishableKey": "pk_test_your-stripe-publishable-key",
      "SecretKey": "sk_test_your-stripe-secret-key",
      "WebhookSecret": "whsec_your-stripe-webhook-secret"
    }
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-gmail-app-password",
    "FromEmail": "your-email@gmail.com",
    "FromName": "CryptoTrading"
  }
}
```

### 3. Database Setup

#### Install EF Core Tools:
```bash
dotnet tool install --global dotnet-ef
```

#### Run Migrations:
```bash
dotnet restore
dotnet ef database update
```

### 4. Run Application
```bash
dotnet run --urls="http://localhost:5000"
```

## 📋 API Endpoints

- **Swagger UI**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health
- **Auth API**: http://localhost:5000/api/auth/*
- **Market API**: http://localhost:5000/api/market/*

## 🔐 Security Notes

- Never commit `appsettings.Development.json` to git
- Use environment variables in production
- Generate strong JWT secrets (32+ characters)
- Use App Passwords for Gmail SMTP

## 🛠️ Development

### MySQL Database
- Uses Pomelo.EntityFrameworkCore.MySql (v9.0.0)
- Configured for Aiven Cloud MySQL
- InnoDB engine with utf8mb4_0900_ai_ci collation
- Auto-migrations enabled
- Optimized composite indexes for performance

### Authentication
- JWT Bearer tokens
- 2FA support with TOTP
- Email confirmation
- Password reset functionality

### Email Configuration
- Gmail SMTP supported
- Requires App Password (not regular password)
- Email sending can be disabled for testing
