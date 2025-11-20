"""
Crypto-specific prompts for agent trading decisions.
Adapted from stock prompts but using crypto terminology and context.
"""

from procoder.prompt import *

# Background prompt for crypto traders
CRYPTO_BACKGROUND_PROMPT = NamedBlock(
    name="Background",
    content=""" 
        You are a cryptocurrency spot trader, and next you will simulate interactions with other traders in the market.
        There are two trading pairs in the market: {crypto_symbol_1} and {crypto_symbol_2}.
        You trade spot only (no leverage, no futures, no margin). Next, please complete your trading actions according to the order.
    """
)

# Last day forum and crypto prices
CRYPTO_LASTDAY_FORUM_AND_PRICE_PROMPT = NamedBlock(
    name="Last Day Forum and Crypto Prices",
    content="""
        After the previous trading period, the prices of {crypto_symbol_1} and {crypto_symbol_2} 
        were {crypto_price_1} USDT and {crypto_price_2} USDT, respectively. 
        Posts by other traders on the forum are as follows: {lastday_forum_message}
    """
)

# Loan type prompt (same structure, but context is crypto)
CRYPTO_LOAN_TYPE_PROMPT = NamedVariable(
    refname="crypto_loan_type_prompt",
    name="Loan Type",
    content="""
    0. 22days, the benchmark interest rate {loan_rate1}
    1. 44days, the benchmark interest rate {loan_rate2}
    2. 66days, the benchmark interest rate {loan_rate3}
    """
)

# Decide if loan (crypto context)
CRYPTO_DECIDE_IF_LOAN_PROMPT = NamedBlock(
    name="Instruction",
    content="""
    It is the {date} day, and your current character is {character}. 
    You hold {crypto_amount_1} {crypto_symbol_1}, {crypto_amount_2} {crypto_symbol_2},
    Now you have {cash} USDT in cash and {debt} in your loan situation.
    You need to decide whether to continue the loan and the amount of the loan.
    The alternative type is {crypto_loan_type_prompt}, and you should use the number to select a loan type. 
    The loan amount shall not exceed {max_loan}.

    Return the result as json, for example:
    {{"loan": "yes", "loan_type": 0, "amount": 1000}}

    If no loan is required, return:
    {{"loan" : "no"}}
    """
)

CRYPTO_LOAN_RETRY_PROMPT = NamedBlock(
    name="Instruction",
    content="""
    The following questions appeared in the loan format you last answered: {fail_response}.
    You should return the results as json, for example:
    {{"loan": "yes", "loan_type": 2, "amount": 1000}}
    If no loan is required, return:
    {{"loan" : "no"}}
    Please answer again."""
)

# Decide buy/sell crypto
CRYPTO_DECIDE_BUY_SELL_PROMPT = NamedBlock(
    name="Instruction",
    content="""
    It is the {time} trading session on the {date} day, and after the previous session, 
    the price of {crypto_symbol_1} is {crypto_price_1} USDT and the price of {crypto_symbol_2} is {crypto_price_2} USDT.
    In the current session, the buy and sell order book of {crypto_symbol_1} is {crypto_deals_1}, 
    and the buy and sell order book of {crypto_symbol_2} is {crypto_deals_2}
    You currently hold {crypto_amount_1} {crypto_symbol_1}, {crypto_amount_2} {crypto_symbol_2}, and {cash} USDT in cash.
    You need to decide whether to buy/sell {crypto_symbol_1} or {crypto_symbol_2}, and how much to buy/sell and at what price.
    You can refer to the current price and the market to determine the price yourself, not necessarily the current price. 
    The quantity must be a positive number (can be fractional for crypto).
    We encourage you to trade actively. You can only answer one json action.
    Return the result as json, for example:
    {{"action_type":"buy", "symbol":"{crypto_symbol_1}", "amount": 0.5, "price": 50000.5}}
    If neither buy nor sell, return:
    {{"action_type" : "no"}}
    """
)

CRYPTO_BUY_SELL_RETRY_PROMPT = NamedBlock(
    name="Instruction",
    content="""
    The following questions appeared in the action format you last answered: {fail_response}.
    You should return the result as json, for example:
    {{"action_type":"buy", "symbol":"{crypto_symbol_1}", "amount": 0.5, "price": 50000.5}}
    If neither buy nor sell, return:
    {{"action_type" : "no"}}
    Please answer again. You can only answer one json action.
    """
)

# First day on-chain metrics (replaces financial reports)
CRYPTO_FIRST_DAY_ONCHAIN_METRICS = NamedVariable(
    refname="first_day_onchain_metrics",
    name="The last 3 years on-chain metrics of {crypto_symbol_1} and {crypto_symbol_2}",
    content="""
    The following lists the on-chain metrics for the past three years, covering a total of twelve periods.
    {crypto_symbol_1} (Bitcoin):
    Active addresses (7d MA): 850K, 920K, 880K, 950K, 1.0M, 1.05M, 980K, 1.1M, 1.08M, 1.12M, 1.05M, 1.2M
    Transaction volume (7d, $B): 35B, 42B, 38B, 45B, 48B, 50B, 46B, 52B, 49B, 55B, 48B, 58B
    Hash rate (EH/s): 520, 560, 540, 580, 590, 600, 580, 620, 600, 630, 600, 650
    MVRV ratio: 1.8, 2.0, 1.9, 2.1, 2.2, 2.25, 2.1, 2.3, 2.2, 2.35, 2.2, 2.4
    
    {crypto_symbol_2} (Ethereum):
    Active addresses (7d MA): 380K, 400K, 390K, 420K, 440K, 460K, 430K, 480K, 450K, 500K, 450K, 520K
    Transaction volume (7d, $B): 22B, 25B, 24B, 28B, 29B, 31B, 28B, 32B, 30B, 33B, 30B, 35B
    Total value staked (M ETH): 30M, 31M, 30.5M, 32M, 32.5M, 33M, 32.2M, 33M, 32.5M, 33.5M, 32.5M, 34M
    Gas price (avg, gwei): 20, 22, 21, 25, 26, 28, 25, 30, 28, 32, 28, 35
    """
)

CRYPTO_FIRST_DAY_BACKGROUND_KNOWLEDGE = NamedBlock(
    name="The initial on-chain situation of {crypto_symbol_1} and {crypto_symbol_2}",
    content="""
    
    {crypto_symbol_1} (Bitcoin) has been trading for over 15 years, deeply rooted as digital gold and store of value. 
    However, the network has experienced periods of high volatility and regulatory uncertainty. 
    Although Bitcoin's adoption has fluctuated over the past few years, the overall trend shows increasing institutional interest. 
    With recent ETF approvals and growing acceptance by corporations, the future outlook appears positive. 
    The network's hash rate continues to grow, indicating strong miner confidence.

    {crypto_symbol_2} (Ethereum), as a smart contract platform, has been operational for about 9 years and is in a period of active development. 
    The transition to Proof-of-Stake (The Merge) was completed successfully, reducing energy consumption significantly. 
    According to the latest on-chain data, network activity remains robust with growing DeFi and NFT ecosystems. 
    In the short term, the price is expected to be influenced by network upgrades and adoption trends.
    While Ethereum's fundamentals are strong, there are concerns about scalability and high gas fees during peak usage, 
    though Layer 2 solutions are addressing these issues.
    Ethereum recently received regulatory attention regarding its classification, and the community is monitoring 
    developments closely while continuing to build decentralized applications.
    
    The last 3 years on-chain metrics of {crypto_symbol_1} and {crypto_symbol_2} are listed in {first_day_onchain_metrics}.
    """
)

# Seasonal on-chain metrics update (replaces quarterly financial reports)
CRYPTO_SEASONAL_ONCHAIN_METRICS = NamedVariable(
    refname="seasonal_onchain_metrics",
    name="The periodic on-chain metrics update of {crypto_symbol_1} and {crypto_symbol_2}",
    content="""
        {crypto_symbol_1}: {onchain_metrics_1}
        {crypto_symbol_2}: {onchain_metrics_2}
    """
)

# Post message prompt (same structure, crypto context)
CRYPTO_POST_MESSAGE_PROMPT = NamedBlock(
    refname="crypto_post_message",
    name="Instruction",
    content="""
    The current trading period is over, please briefly post your trading insights on the forum and post them on the forum.
    What you post will be publicly visible to all traders. The responses contain only what needs to be posted.
    Focus on crypto market dynamics, on-chain metrics, and spot trading strategies.
    """
)

# Next day estimate (crypto symbols)
CRYPTO_NEXT_DAY_ESTIMATE_PROMPT = NamedBlock(
    refname="crypto_next_day_estimate",
    name="Instruction",
    content="""
    Based on the market information and forum information of the current trading period, 
    please estimate whether you will buy and sell {crypto_symbol_1} and {crypto_symbol_2} in the next period, and whether you will choose loan.
    Actions that are expected to take place are marked yes, and actions that will not take place are marked no. 
    Return the result in json format, for example:
    {{"buy_{crypto_symbol_1}":"yes", "buy_{crypto_symbol_2}":"no", "sell_{crypto_symbol_1}":"yes", "sell_{crypto_symbol_2}": "no", "loan": "yes"}}
    """
)

CRYPTO_NEXT_DAY_ESTIMATE_RETRY = NamedBlock(
    refname="crypto_next_day_estimate_retry",
    name="Instruction",
    content="""
    The following questions appeared in the JSON format you last answered: {fail_response}.
    Return the result in json format, for example:
    {{"buy_{crypto_symbol_1}":"yes", "buy_{crypto_symbol_2}":"no", "sell_{crypto_symbol_1}":"yes", "sell_{crypto_symbol_2}": "no", "loan": "yes"}}
    """
)

