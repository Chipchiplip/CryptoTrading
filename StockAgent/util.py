"""
Configuration and utility constants for StockAgent.
API keys and database config are loaded from appsettings.Development.json
"""
import os
import json
from pathlib import Path

# Path to appsettings.Development.json (relative to project root)
CONFIG_FILE = Path(__file__).parent.parent / "appsettings.Development.json"

# Default values
OPENAI_API_KEY = ""
GOOGLE_API_KEY = ""
DB_HOST = "cryptotrading-01-phantrunghieu0000-ad84.g.aivencloud.com"
DB_PORT = 20158
DB_NAME = "crypto_trading"
DB_USER = "avnadmin"
DB_PASSWORD = "AVNS_NqzAptm4Fgqk6iny2nh"
DB_SSL_MODE = "Required"

# Try to load from appsettings.Development.json
if CONFIG_FILE.exists():
    try:
        with open(CONFIG_FILE, 'r', encoding='utf-8') as f:
            config = json.load(f)
        
        # Load API keys from config file
        if "LLMApiKeys" in config:
            OPENAI_API_KEY = config["LLMApiKeys"].get("OpenAI", "")
            GOOGLE_API_KEY = config["LLMApiKeys"].get("Google", "")
        
        # Fallback to environment variables if not in config
        if not OPENAI_API_KEY:
            OPENAI_API_KEY = os.getenv("OPENAI_API_KEY", "")
        if not GOOGLE_API_KEY:
            GOOGLE_API_KEY = os.getenv("GOOGLE_API_KEY", "")
        
        # Load database connection from ConnectionStrings
        if "ConnectionStrings" in config and "DefaultConnection" in config["ConnectionStrings"]:
            conn_string = config["ConnectionStrings"]["DefaultConnection"]
            # Parse connection string: server=host;port=port;database=db;user=user;password=pass;sslmode=mode
            parts = {}
            for part in conn_string.split(';'):
                if '=' in part:
                    key, value = part.split('=', 1)
                    parts[key.strip().lower()] = value.strip()
            
            DB_HOST = parts.get('server', DB_HOST)
            DB_PORT = int(parts.get('port', DB_PORT))
            DB_NAME = parts.get('database', DB_NAME)
            DB_USER = parts.get('user', DB_USER)
            DB_PASSWORD = parts.get('password', DB_PASSWORD)
            DB_SSL_MODE = parts.get('sslmode', DB_SSL_MODE)
        
        # Load CoinGecko API key if available
        if "CoinGecko" in config and "ApiKey" in config["CoinGecko"]:
            # Store for potential future use
            COINGECKO_API_KEY = config["CoinGecko"]["ApiKey"]
        
    except Exception as e:
        print(f"Warning: Could not load config from {CONFIG_FILE}: {e}")
        print("Using default values or environment variables")
else:
    # Fallback to environment variables if config file doesn't exist
    OPENAI_API_KEY = os.getenv("OPENAI_API_KEY", OPENAI_API_KEY)
    GOOGLE_API_KEY = os.getenv("GOOGLE_API_KEY", GOOGLE_API_KEY)
    DB_HOST = os.getenv("DB_HOST", DB_HOST)
    DB_PORT = int(os.getenv("DB_PORT", str(DB_PORT)))
    DB_NAME = os.getenv("DB_NAME", DB_NAME)
    DB_USER = os.getenv("DB_USER", DB_USER)
    DB_PASSWORD = os.getenv("DB_PASSWORD", DB_PASSWORD)
    DB_SSL_MODE = os.getenv("DB_SSL_MODE", DB_SSL_MODE)

# 基础设置
AGENTS_NUM = 50  # 交易员数量
TOTAL_DATE = 264   # 模拟时长
TOTAL_SESSION = 3   # 每日交易次数

# 股票初始价格
STOCK_A_INITIAL_PRICE = 30
STOCK_B_INITIAL_PRICE = 40
# STOCK_B_PUBLISH = 100   # 股票B发行数量

# agent初始财产
MAX_INITIAL_PROPERTY = 5000000.0
MIN_INITIAL_PROPERTY = 100000.0


# 贷款
LOAN_TYPE = ["one-month", "two-month", "three-month"]
LOAN_TYPE_DATE = [22, 44, 66]  # 贷款时长
LOAN_RATE = [0.027, 0.03, 0.033] # 贷款利率

REPAYMENT_DAYS = [22, 44, 66, 88, 110, 132, 154, 176, 198, 220, 242, 264]  # 付息日

# 财报
SEASONAL_DAYS = 66 # 一季度的时间
SEASON_REPORT_DAYS = [12, 78, 144, 210] # 财报发布时间
FINANCIAL_REPORT_A = ["Last quarter's financial report of Company A. Revenue growth rate (YoY): 9.49%, Revenue million: 4483.99, Gross margin: 41.05%, Income Tax as a percentage of Revenue: 11.31%, Selling Expense Rate:6.83%, Management Expense Rate: 3.83%, Net profit million: 856.6705, Depreciation and Amortization: 0.91%, Capital Expenditures: 2.30%, Changes in working capital: 0.82%, Cash Flow(million): 756.7537",
                      "Last quarter's financial report of Company A. Revenue growth rate (YoY): 7.38%, Revenue million: 4417.79, Gross margin: 35.68%, Income Tax as a percentage of Revenue: 11.75%, Selling Expense Rate:8.13%, Management Expense Rate: 4.62%, Net profit million: 493.9451, Depreciation and Amortization: 1.34%, Capital Expenditures: 2.68%, Changes in working capital: 0.86%, Cash Flow(million): 396.5329",
                      "Last quarter's financial report of Company A. Revenue growth rate (YoY): 8.70%, Revenue million: 4041.30, Gross margin: 37.45%, Income Tax as a percentage of Revenue: 9.34%, Selling Expense Rate:6.79%, Management Expense Rate: 3.41%, Net profit million: 724.3648, Depreciation and Amortization: 1.27%, Capital Expenditures: 2.44%, Changes in working capital: 0.94%, Cash Flow(million): 639.5329",
                      "Last quarter's financial report of Company A. Revenue growth rate (YoY): 7.75%, Revenue million: 5024.04, Gross margin: 42.47%, Income Tax as a percentage of Revenue: 10.67%, Selling Expense Rate:6.56%, Management Expense Rate: 4.72%, Net profit million: 1031.214, Depreciation and Amortization: 1.08%, Capital Expenditures: 2.71%, Changes in working capital: 0.08%, Cash Flow(million): 945.5034"] # 各个季度的财报
FINANCIAL_REPORT_B = ["Last quarter's financial report of Company B. Revenue growth rate (YoY): 19.96%, Revenue million: 1319.94, Gross margin: 31.21%, Income Tax as a percentage of Revenue: 0.70%, Selling Expense Rate:4.69%, Management Expense Rate: 8.78%, Net profit million: 224.9179, Depreciation and Amortization: 1.13%, Capital Expenditures: 1.77%, Changes in working capital: 0.59%, Cash Flow(million): 208.7266",
                      "Last quarter's financial report of Company B. Revenue growth rate (YoY): 19.86%, Revenue million: 1096.70, Gross margin: 31.26%, Income Tax as a percentage of Revenue: 0.71%, Selling Expense Rate:3.62%, Management Expense Rate: 9.90%, Net profit million: 186.7678, Depreciation and Amortization: 0.67%, Capital Expenditures: 1.44%, Changes in working capital: -0.31%, Cash Flow(million): 181.6862",
                      "Last quarter's financial report of Company B. Revenue growth rate (YoY): 18.21%, Revenue million: 1676.70, Gross margin: 31.58%, Income Tax as a percentage of Revenue: 0.92%, Selling Expense Rate:3.78%, Management Expense Rate: 10.27%, Net profit million: 278.3327, Depreciation and Amortization: 0.77%, Capital Expenditures: 1.56%, Changes in working capital: -0.06%, Cash Flow(million): 266.1486",
                      "Last quarter's financial report of Company B. Revenue growth rate (YoY): 15.98%, Revenue million: 1075.13, Gross margin: 32.41%, Income Tax as a percentage of Revenue: 1.08%, Selling Expense Rate:3.79%, Management Expense Rate: 10.70%, Net profit million: 181.1602, Depreciation and Amortization: 1.09%, Capital Expenditures: 2.28%, Changes in working capital: 0.67%, Cash Flow(million): 161.1985"]

# 特殊事件

EVENT_1_DAY = 78
EVENT_1_MESSAGE = "The government has announced a reduction in the reserve requirement ratio. " \
                  "The lending interest rates have been lowered."
EVENT_1_LOAN_RATE = [0.024, 0.027, 0.030] # 降准后的利率放在这里

EVENT_2_DAY = 144
EVENT_2_MESSAGE = "The government has announced an increase in interest rates."
EVENT_2_LOAN_RATE = [0.0255, 0.0285, 0.0315]

# ==================== CRYPTO MODE CONSTANTS ====================
# Crypto trading pair symbols (using USD instead of USDT)
CRYPTO_SYMBOL_1 = "BTCUSD"  # Primary crypto (replaces Stock A)
CRYPTO_SYMBOL_2 = "ETHUSD"  # Secondary crypto (replaces Stock B)

# Crypto initial prices (can be overridden by CSV data)
CRYPTO_BTC_INITIAL_PRICE = 50000.0
CRYPTO_ETH_INITIAL_PRICE = 3000.0

# Crypto data directory (relative to StockAgent root)
CRYPTO_DATA_DIR = "data/crypto"
CRYPTO_TIMEFRAME = "1h"  # Default timeframe: 1h, 4h, 1d, etc.

# Crypto spot trading fee (taker fee, typical for major exchanges)
CRYPTO_FEE_RATE = 0.0004  # 0.04% (4 basis points)

# On-chain metrics for BTC (replaces financial reports)
# Format: [Q1, Q2, Q3, Q4] - quarterly-like periods
ONCHAIN_METRICS_BTC = [
    "Bitcoin on-chain metrics: Active addresses (7d MA): 950K, Transaction volume (7d): $45B, Hash rate: 580 EH/s, MVRV ratio: 2.1, Exchange reserves: -12K BTC (outflow)",
    "Bitcoin on-chain metrics: Active addresses (7d MA): 1.1M, Transaction volume (7d): $52B, Hash rate: 620 EH/s, MVRV ratio: 2.3, Exchange reserves: -8K BTC (outflow)",
    "Bitcoin on-chain metrics: Active addresses (7d MA): 1.05M, Transaction volume (7d): $48B, Hash rate: 600 EH/s, MVRV ratio: 2.2, Exchange reserves: -5K BTC (outflow)",
    "Bitcoin on-chain metrics: Active addresses (7d MA): 1.2M, Transaction volume (7d): $58B, Hash rate: 650 EH/s, MVRV ratio: 2.4, Exchange reserves: -15K BTC (strong outflow)"
]

# On-chain metrics for ETH
ONCHAIN_METRICS_ETH = [
    "Ethereum on-chain metrics: Active addresses (7d MA): 420K, Transaction volume (7d): $28B, Total value staked: 32M ETH, Gas price (avg): 25 gwei, Exchange reserves: -50K ETH (outflow)",
    "Ethereum on-chain metrics: Active addresses (7d MA): 480K, Transaction volume (7d): $32B, Total value staked: 33M ETH, Gas price (avg): 30 gwei, Exchange reserves: -30K ETH (outflow)",
    "Ethereum on-chain metrics: Active addresses (7d MA): 450K, Transaction volume (7d): $30B, Total value staked: 32.5M ETH, Gas price (avg): 28 gwei, Exchange reserves: -20K ETH (outflow)",
    "Ethereum on-chain metrics: Active addresses (7d MA): 520K, Transaction volume (7d): $35B, Total value staked: 34M ETH, Gas price (avg): 35 gwei, Exchange reserves: -60K ETH (strong outflow)"
]

# Crypto-specific events (replaces government policy events)
CRYPTO_EVENT_1_DAY = 78
CRYPTO_EVENT_1_MESSAGE = "Major news: Bitcoin ETF approval by SEC. Institutional adoption expected to increase significantly."
CRYPTO_EVENT_1_LOAN_RATE = [0.024, 0.027, 0.030]  # Lower rates due to positive sentiment

CRYPTO_EVENT_2_DAY = 144
CRYPTO_EVENT_2_MESSAGE = "Major news: Major exchange hack reported. Market sentiment turns negative. Regulatory FUD increases."
CRYPTO_EVENT_2_LOAN_RATE = [0.0255, 0.0285, 0.0315]  # Slightly higher rates due to risk

# Crypto "quarterly" periods (similar to SEASON_REPORT_DAYS for stocks)
# In crypto, we use on-chain metrics updates instead of financial reports
CRYPTO_METRICS_UPDATE_DAYS = [12, 78, 144, 210]  # Similar to SEASON_REPORT_DAYS