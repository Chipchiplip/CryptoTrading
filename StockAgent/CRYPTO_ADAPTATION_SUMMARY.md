# Crypto Adaptation Summary

## Current Stock Environment Analysis

### Key Components:

1. **stock.py** - `Stock` class:
   - Manages stock price, history, and session deals
   - Updates price from session deals
   - Generates financial reports (quarterly)
   - No market open/close logic (simulated trading sessions)

2. **main.py** - Simulation loop:
   - Initializes two stocks (A and B)
   - Runs for TOTAL_DATE days with TOTAL_SESSION sessions per day
   - Handles loan repayments, interest payments, bankruptcies
   - Processes special events (EVENT_1_DAY, EVENT_2_DAY)
   - Matches buy/sell orders between agents
   - Updates stock prices after each session

3. **agent.py** - Trading agents:
   - Each agent has cash, stock_a_amount, stock_b_amount
   - Makes loan decisions and trading decisions via LLM
   - Uses prompts to decide actions
   - Tracks action history and chat history

4. **util.py** - Configuration:
   - AGENTS_NUM, TOTAL_DATE, TOTAL_SESSION
   - Initial stock prices
   - Loan types and rates
   - Financial reports (quarterly)
   - Special event days and messages

5. **prompt/agent_prompt.py** - LLM prompts:
   - References "Company A" and "Company B"
   - Uses stock/share terminology
   - Financial reports and company fundamentals

6. **secretary.py** - Action validation:
   - Validates loan JSON format
   - Validates trading action JSON (buy/sell stock A or B)
   - Checks cash and holdings constraints

## Adaptation Strategy:

- Create `crypto.py` with `Crypto` class (mirrors `Stock`)
- Create `crypto_data_loader.py` for CSV OHLCV loading
- Add `--market_mode` argument to `main.py`
- Create `prompt/crypto_agent_prompt.py` with crypto-specific prompts
- Update `util.py` with crypto constants
- Update `secretary.py` to handle crypto symbols (BTCUSDT, ETHUSDT)
- Keep stock mode intact (backward compatible)

