# 📚 CryptoTrading Documentation

> ⚠️ **DEMO PROJECT DISCLAIMER**  
> This is a **simplified educational demo project** for learning purposes.  
> Market data, order fills, and PnL are **simulated**.  
> **NOT suitable for real trading or production use.**

Welcome to the CryptoTrading platform documentation. This directory contains all project documentation organized by category.

**This is a DEMO project** - designed to demonstrate trading bot architecture and concepts, not for real trading.

## 🚀 Quick Start

| Document | Description |
|----------|-------------|
| **[DEMO_PROJECT_DISCLAIMER.md](DEMO_PROJECT_DISCLAIMER.md)** | **⚠️ READ FIRST - Important demo project information** |
| [GETTING_STARTED.md](GETTING_STARTED.md) | Complete setup guide for developers |
| [SETUP.md](SETUP.md) | Quick setup instructions |
| [ARCHITECTURE.md](ARCHITECTURE.md) | System architecture overview |

## 🗄️ Database Documentation

| Document | Description |
|----------|-------------|
| [DATABASE_README.md](DATABASE_README.md) | Database schema and structure overview |
| [DATABASE_MYSQL_GUIDELINES.md](DATABASE_MYSQL_GUIDELINES.md) | MySQL configuration and best practices |
| [DATABASE_MYSQL_SETUP.md](DATABASE_MYSQL_SETUP.md) | MySQL setup instructions |
| [DATABASE_SEED_README.md](DATABASE_SEED_README.md) | Database seeding information |

## 👥 Team & Project Management

| Document | Description |
|----------|-------------|
| [TASK_ASSIGNMENTS.md](TASK_ASSIGNMENTS.md) | Team member task assignments |
| [TEAM_WORKFLOW.md](TEAM_WORKFLOW.md) | Git workflow and team processes |
| [COMPLETED_TASKS.md](COMPLETED_TASKS.md) | Completed tasks tracking |

## 💼 Portfolio System

| Document | Description |
|----------|-------------|
| [PORTFOLIO_ANALYSIS_AND_RECOMMENDATIONS.md](PORTFOLIO_ANALYSIS_AND_RECOMMENDATIONS.md) | Portfolio analysis and recommendations |
| [PORTFOLIO_CALCULATION_VERIFICATION.md](PORTFOLIO_CALCULATION_VERIFICATION.md) | Portfolio calculation verification |
| [PORTFOLIO_DEBUGGING_GUIDE.md](PORTFOLIO_DEBUGGING_GUIDE.md) | Portfolio debugging guide |
| [PORTFOLIO_TEST_CASES.md](PORTFOLIO_TEST_CASES.md) | Portfolio test cases |
| [HOLDINGS_VS_OPEN_ORDERS.md](HOLDINGS_VS_OPEN_ORDERS.md) | Holdings vs open orders explanation |

## 📈 Trading System

| Document | Description |
|----------|-------------|
| [TRADING_SYSTEM_IMPLEMENTATION.md](TRADING_SYSTEM_IMPLEMENTATION.md) | Trading system implementation details |
| [TRADING_MODULE_TASKS.md](TRADING_MODULE_TASKS.md) | Trading module tasks and requirements |

## 🎨 Frontend Documentation

| Document | Description |
|----------|-------------|
| [FRONTEND_UPGRADE_NODE.md](FRONTEND_UPGRADE_NODE.md) | Node.js upgrade guide for frontend |
| [FRONTEND_TRADING_MODULE_FIXES.md](FRONTEND_TRADING_MODULE_FIXES.md) | Frontend trading module fixes |

## 🛠️ Technology Stack

### Backend
- **Framework**: ASP.NET Core 9.0
- **Database**: MySQL 8.0 with Pomelo Entity Framework Core
- **Authentication**: JWT Bearer Token with 2FA
- **Caching**: Redis
- **Real-time**: SignalR
- **Cloud**: Aiven Cloud MySQL

### Frontend
- **Framework**: React 18 with TypeScript
- **Styling**: Tailwind CSS
- **State Management**: Context API
- **Charts**: Chart.js
- **HTTP Client**: Axios

### Infrastructure
- **Containerization**: Docker & Docker Compose
- **Database**: MySQL 8.0 (InnoDB, utf8mb4_0900_ai_ci)
- **Cache**: Redis 7
- **Monitoring**: Built-in health checks

## 📁 Project Structure

```
CryptoTrading/
├── 📁 src/                     # Backend source code
├── 📁 frontend/                # React frontend
├── 📁 database/               # Database scripts and migrations
├── 📁 docs/                   # Documentation (this folder)
├── 📁 tests/                  # Test projects
├── README.md                  # Project overview
└── docker-compose.yml         # Docker configuration
```

## 🔧 Development Workflow

1. **Setup**: Follow [GETTING_STARTED.md](GETTING_STARTED.md)
2. **Database**: Configure MySQL using [DATABASE_MYSQL_SETUP.md](DATABASE_MYSQL_SETUP.md)
3. **Development**: Check [TEAM_WORKFLOW.md](TEAM_WORKFLOW.md) for Git workflow
4. **Architecture**: Review [ARCHITECTURE.md](ARCHITECTURE.md) for system design

## 📊 Key Features (DEMO MODE)

- **Authentication**: JWT with 2FA, email verification
- **Trading**: **DEMO** - Simulated order execution (no real exchange)
- **Portfolio**: Portfolio tracking and analysis (demo data)
- **Market Data**: **DEMO** - Simulated price feeds (not real exchange data)
- **Payments**: VNPay integration for deposits
- **Real-time**: WebSocket connections for live updates (demo data)
- **Bot Strategies**: Grid trading and momentum scalping (demo execution)

**Note**: All trading features run in demo mode with simulated data. No real money or real exchange integration.

## 🔗 Quick Links

- **API Documentation**: Available at `/swagger` when running
- **Health Check**: `/health`
- **Database**: Aiven Cloud MySQL
- **Frontend**: React with TypeScript
- **Real-time**: SignalR hubs for market and trading data

## 📞 Support

- **Repository**: https://github.com/Chipchiplip/CryptoTrading
- **Team Lead**: Đặng Công Vũ Hoàng
- **Documentation Issues**: Create an issue in the repository

---

**Last Updated**: November 12, 2025  
**Version**: 2.0 (MySQL Migration Complete)
