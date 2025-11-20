# Crypto Implementation Summary

## Overview

The StockAgent codebase has been successfully adapted to support spot-only cryptocurrency trading while maintaining full backward compatibility with the original stock trading mode.

## Key Changes

### 1. New Files Created

- **`crypto.py`**: Crypto environment class that mirrors `Stock` class interface
  - Supports OHLCV data loading from CSV
  - 24/7 trading (no market open/close)
  - On-chain metrics instead of financial reports

- **`crypto_data_loader.py`**: Utilities for loading crypto OHLCV data
  - `load_crypto_csv()`: Load single CSV file
  - `load_multiple_crypto_symbols()`: Load multiple symbols
  - Handles timestamp conversion and data validation

- **`prompt/crypto_agent_prompt.py`**: Crypto-specific prompts
  - Replaces stock terminology with crypto terminology
  - Uses on-chain metrics instead of financial reports
  - References BTCUSDT, ETHUSDT instead of Stock A/B

### 2. Modified Files

#### `main.py`
- Added `--market_mode` CLI argument (choices: "stock", "crypto")
- Added `simulation_crypto()` function for crypto mode
- Updated `handle_action()` to support both stock and crypto modes
- Crypto mode loads OHLCV data from CSV files

#### `agent.py`
- Added `is_crypto` parameter to `Agent.__init__()`
- Added `crypto_1_amount` and `crypto_2_amount` attributes
- Added `buy_crypto()` and `sell_crypto()` methods (with fee calculation)
- Added `plan_crypto()` method for crypto trading decisions
- Updated `plan_loan()`, `bankrupt_process()`, `post_message()`, `next_day_estimate()` to support crypto mode
- Added `random_init_crypto()` for fractional crypto amounts

#### `secretary.py`
- Updated `check_action()` to validate crypto symbols (BTCUSDT, ETHUSDT)
- Updated `check_estimate()` to handle crypto symbol keys
- Supports fractional amounts for crypto (float) vs integer for stocks

#### `util.py`
- Added crypto constants:
  - `CRYPTO_SYMBOL_1`, `CRYPTO_SYMBOL_2`
  - `CRYPTO_BTC_INITIAL_PRICE`, `CRYPTO_ETH_INITIAL_PRICE`
  - `CRYPTO_DATA_DIR`, `CRYPTO_TIMEFRAME`
  - `CRYPTO_FEE_RATE` (0.0004 = 0.04%)
  - `ONCHAIN_METRICS_BTC`, `ONCHAIN_METRICS_ETH`
  - `CRYPTO_EVENT_1_DAY`, `CRYPTO_EVENT_2_DAY`
  - `CRYPTO_METRICS_UPDATE_DAYS`

#### `record.py`
- Updated `AgentRecordDaily.add_estimate()` to handle crypto symbol keys

#### `crypto.py`
- Added `update_price_from_bar()` method
- Added `get_ohlcv_at_bar()` method

### 3. Architecture Decisions

1. **Backward Compatibility**: Stock mode remains fully functional. All changes are additive.

2. **Spot-Only**: No leverage, futures, margin, or derivatives. Only spot trading is supported.

3. **24/7 Trading**: Crypto markets don't have open/close times. Each bar (candle) is tradable.

4. **Fractional Amounts**: Crypto supports decimal amounts (e.g., 0.5 BTC), while stocks use integers.

5. **Fee Structure**: Realistic crypto exchange fees (0.04% taker fee) applied to all trades.

6. **Data Source**: OHLCV data loaded from CSV files with format: `timestamp, open, high, low, close, volume`

7. **Model Support**: Works with both OpenAI (GPT) and Google (Gemini) models via `--model` argument.

## Usage

### Stock Mode (Default)
```bash
python main.py --model gemini-pro
```

### Crypto Mode
```bash
python main.py --model gemini-pro --market_mode crypto
```

### Data Requirements for Crypto Mode

1. Create `data/crypto/` directory
2. Place CSV files:
   - `BTCUSDT_1h.csv`
   - `ETHUSDT_1h.csv`
3. CSV format:
   ```csv
   timestamp,open,high,low,close,volume
   1609459200,29374.15,29600.00,29300.00,29500.00,1234.56
   ...
   ```

## Testing Checklist

- [x] Crypto environment class implemented
- [x] Data loader handles CSV files correctly
- [x] Agent supports crypto symbols
- [x] Secretary validates crypto actions
- [x] Main script supports --market_mode argument
- [x] Prompts adapted for crypto context
- [x] Fee calculation implemented
- [x] Fractional amounts supported
- [x] Stock mode remains functional
- [ ] Integration testing with sample data
- [ ] End-to-end simulation test

## Notes

- The implementation assumes CSV files are in the `data/crypto/` directory
- Timeframe is configurable via `util.CRYPTO_TIMEFRAME` (default: "1h")
- Simulation length is limited by available data or `util.TOTAL_DATE`
- All crypto trading is spot-only (no leverage/margin)
- Fee rate is configurable via `util.CRYPTO_FEE_RATE`

## Future Enhancements (Not Implemented)

- Support for more crypto symbols
- Dynamic timeframe selection
- Real-time data integration
- Advanced order types (limit, stop-loss)
- Portfolio rebalancing strategies

