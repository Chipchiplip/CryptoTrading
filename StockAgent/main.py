import argparse
import random
import pandas as pd
import openai
import tiktoken
import os

import util
from agent import Agent
from secretary import Secretary
from stock import Stock
from crypto import Crypto
from crypto_data_loader import load_multiple_crypto_symbols
from database_loader import load_multiple_crypto_from_database
from log.custom_logger import log
from record import create_stock_record, create_trade_record, AgentRecordDaily, create_agentses_record

def get_agent(all_agents, order):
    for agent in all_agents:
        if agent.order == order:
            return agent
    return None

def handle_action(action, stock_deals, all_agents, stock, session, is_crypto=False):
    """
    Handle trading action (buy/sell) for both stock and crypto modes.
    
    Args:
        action: JSON dict with agent, action_type, stock/symbol, amount, price
        stock_deals: Order book (buy/sell lists)
        all_agents: List of all agents
        stock: Stock or Crypto object
        session: Current session number
        is_crypto: Whether we're in crypto mode
    """
    # For stock mode: action["stock"] = "A" or "B"
    # For crypto mode: action["symbol"] = "BTCUSDT" or "ETHUSDT"
    try:
        # Determine asset identifier
        asset_id = action.get("symbol") if is_crypto else action.get("stock")
        
        if action["action_type"] == "buy":
            for sell_action in stock_deals["sell"][:]:
                if action["price"] == sell_action["price"]:
                    # Trade execution
                    close_amount = min(action["amount"], sell_action["amount"])
                    
                    # Execute buy
                    if is_crypto:
                        get_agent(all_agents, action["agent"]).buy_crypto(asset_id, close_amount, action["price"])
                    else:
                        get_agent(all_agents, action["agent"]).buy_stock(asset_id, close_amount, action["price"])
                    
                    # Execute sell (if not admin/system)
                    if not sell_action["agent"] == -1:
                        if is_crypto:
                            get_agent(all_agents, sell_action["agent"]).sell_crypto(asset_id, close_amount, action["price"])
                        else:
                            get_agent(all_agents, sell_action["agent"]).sell_stock(asset_id, close_amount, action["price"])
                    
                    stock.add_session_deal({"price": action["price"], "amount": close_amount})
                    create_trade_record(action["date"], session, asset_id, action["agent"], sell_action["agent"],
                                        close_amount, action["price"])

                    if action["amount"] > close_amount:  # Buy order not fully filled, sell order filled
                        log.logger.info(f"ACTION - BUY:{action['agent']}, SELL:{sell_action['agent']}, "
                                        f"ASSET:{asset_id}, PRICE:{action['price']}, AMOUNT:{close_amount}")
                        stock_deals["sell"].remove(sell_action)
                        action["amount"] -= close_amount
                    else:  # Sell order not fully filled, buy order filled
                        log.logger.info(f"ACTION - BUY:{action['agent']}, SELL:{sell_action['agent']}, "
                                        f"ASSET:{asset_id}, PRICE:{action['price']}, AMOUNT:{close_amount}")
                        sell_action["amount"] -= close_amount
                        return
            # Remaining buy order added to order book
            stock_deals["buy"].append(action)

        else:  # sell
            for buy_action in stock_deals["buy"][:]:
                if action["price"] == buy_action["price"]:
                    # Trade execution
                    close_amount = min(action["amount"], buy_action["amount"])
                    
                    # Execute sell
                    if is_crypto:
                        get_agent(all_agents, action["agent"]).sell_crypto(asset_id, close_amount, action["price"])
                    else:
                        get_agent(all_agents, action["agent"]).sell_stock(asset_id, close_amount, action["price"])
                    
                    # Execute buy
                    if is_crypto:
                        get_agent(all_agents, buy_action["agent"]).buy_crypto(asset_id, close_amount, action["price"])
                    else:
                        get_agent(all_agents, buy_action["agent"]).buy_stock(asset_id, close_amount, action["price"])
                    
                    stock.add_session_deal({"price": action["price"], "amount": close_amount})
                    create_trade_record(action["date"], session, asset_id, buy_action["agent"], action["agent"],
                                        close_amount, action["price"])

                    if action["amount"] > close_amount:  # Sell order not fully filled, buy order filled
                        log.logger.info(f"ACTION - BUY:{buy_action['agent']}, SELL:{action['agent']}, "
                                        f"ASSET:{asset_id}, PRICE:{action['price']}, AMOUNT:{close_amount}")
                        stock_deals["buy"].remove(buy_action)
                        action["amount"] -= close_amount
                    else:  # Buy order not fully filled, sell order filled
                        log.logger.info(f"ACTION - BUY:{buy_action['agent']}, SELL:{action['agent']}, "
                                        f"ASSET:{asset_id}, PRICE:{action['price']}, AMOUNT:{close_amount}")
                        buy_action["amount"] -= close_amount
                        return
            stock_deals["sell"].append(action)
    except Exception as e:
        log.logger.error(f"handle_action error: {e}")
        return


def simulation(args):
    # init
    secretary = Secretary(args.model)
    stock_a = Stock("A", util.STOCK_A_INITIAL_PRICE, 0, is_new=False)
    #stock_b = Stock("B", util.STOCK_B_INITIAL_PRICE, util.STOCK_B_PUBLISH, is_new=True)
    stock_b = Stock("B", util.STOCK_B_INITIAL_PRICE, 0, is_new=False)
    all_agents = []
    log.logger.debug("Agents initial...")
    for i in range(0, util.AGENTS_NUM):  # agents start from 0, -1 refers to admin
        agent = Agent(i, stock_a.get_price(), stock_b.get_price(), secretary, args.model)
        all_agents.append(agent)
        log.logger.debug("cash: {}, stock a: {}, stock b:{}, debt: {}".format(agent.cash, agent.stock_a_amount,
                                                                              agent.stock_b_amount, agent.loans))

    # start simulation
    last_day_forum_message = []
    stock_a_deals = {"sell": [], "buy": []}
    stock_b_deals = {"sell": [], "buy": []}
    # stock b publish
    # stock_b_deals["sell"].append({"agent": -1, "amount": util.STOCK_B_PUBLISH, "price": util.STOCK_B_INITIAL_PRICE})

    log.logger.debug("--------Simulation Start!--------")
    for date in range(1, util.TOTAL_DATE + 1):

        log.logger.debug(f"--------DAY {date}---------")
        # 除b发行外，删除前一天的所有交易
        stock_a_deals["sell"].clear()
        stock_a_deals["buy"].clear()
        stock_b_deals["buy"].clear()

        # tmp_action = next((action for action in stock_b_deals["sell"] if action["agent"] == -1), None)
        stock_b_deals["sell"].clear()
        # if tmp_action:
        #     tmp_action["price"] *= 0.9  # B发行折价
        #     if tmp_action["price"] < 1:
        #         log.logger.warning("WARNING: STOCK B WITHDRAW FROM MARKET!!!")
        #     stock_b_deals["sell"].append(tmp_action)

        # check if an agent needs to repay loans
        for agent in all_agents[:]:
            agent.chat_history.clear()  # 只保存当天的聊天记录
            agent.loan_repayment(date)

        # repayment days
        if date in util.REPAYMENT_DAYS:
            for agent in all_agents[:]:
                agent.interest_payment()

        # deal with cash<0 agents
        for agent in all_agents[:]:
            if agent.is_bankrupt:
                quit_sig = agent.bankrupt_process(stock_a.get_price(), stock_b.get_price())
                if quit_sig:
                    agent.quit = True
                    all_agents.remove(agent)

        # special events
        if date == util.EVENT_1_DAY:
            util.LOAN_RATE = util.EVENT_1_LOAN_RATE
            last_day_forum_message.append({"name": -1, "message": util.EVENT_1_MESSAGE})
        if date == util.EVENT_2_DAY:
            util.LOAN_RATE = util.EVENT_2_LOAN_RATE
            last_day_forum_message.append({"name": -1, "message": util.EVENT_2_MESSAGE})

        # agent decide whether to loan
        daily_agent_records = []
        for agent in all_agents:
            loan = agent.plan_loan(date, stock_a.get_price(), stock_b.get_price(), last_day_forum_message)
            daily_agent_records.append(AgentRecordDaily(agent.order, date, loan))

        for session in range(1, util.TOTAL_SESSION + 1):
            log.logger.debug(f"SESSION {session}")
            # 随机定义交易顺序
            sequence = list(range(len(all_agents)))
            random.shuffle(sequence)
            for i in sequence:
                agent = all_agents[i]
                # if agent.is_bankrupt:  # cash<0的当天停止交易，交易时段结束后贩卖股票
                #     continue

                action = agent.plan_stock(date, session, stock_a, stock_b, stock_a_deals, stock_b_deals)
                proper, cash, valua_a, value_b = agent.get_proper_cash_value(stock_a.get_price(), stock_b.get_price())
                create_agentses_record(agent.order, date, session, proper, cash, valua_a, value_b, action)
                action["agent"] = agent.order
                action["date"] = date
                if not action["action_type"] == "no":
                    if action["stock"] == 'A':
                        handle_action(action, stock_a_deals, all_agents, stock_a, session)
                    else:
                        handle_action(action, stock_b_deals, all_agents, stock_b, session)

            # 交易时段结束，更新股票价格
            stock_a.update_price(date)
            stock_b.update_price(date)
            create_stock_record(date, session, stock_a.get_price(), stock_b.get_price())


        # agent预测明天行动
        for idx, agent in enumerate(all_agents):
            estimation = agent.next_day_estimate()
            log.logger.info("Agent {} tomorrow estimation: {}".format(agent.order, estimation))
            if idx >= len(daily_agent_records):
                break
            daily_agent_records[idx].add_estimate(estimation, is_crypto=False)
            daily_agent_records[idx].write_to_excel()
        daily_agent_records.clear()

        # 交易日结束，论坛信息更新
        last_day_forum_message.clear()
        log.logger.debug(f"DAY {date} ends, display forum messages...")
        for agent in all_agents:
            chat_history = agent.chat_history
            message = agent.post_message()
            log.logger.info("Agent {} says: {}".format(agent.order, message))
            last_day_forum_message.append({"name": agent.order, "message": message})



    log.logger.debug("--------Simulation finished!--------")
    log.logger.debug("--------Agents action history--------")
    # for agent in all_agents:
    #     log.logger.debug(f"Agent {agent.order} action history:")
    #     log.logger.info(agent.action_history)
    # log.logger.debug("--------Stock deal history--------")
    # for stock in [stock_a, stock_b]:
    #     log.logger.debug(f"Stock {stock.name} deal history:")
    #     log.logger.info(stock.history)


def simulation_crypto(args):
    """
    Crypto trading simulation using OHLCV data from database.
    Spot-only trading (no leverage, no futures, no margin).
    """
    # Load crypto data from database
    symbols = [util.CRYPTO_SYMBOL_1, util.CRYPTO_SYMBOL_2]
    timeframe = util.CRYPTO_TIMEFRAME
    
    log.logger.info(f"Loading crypto data from database with timeframe {timeframe}")
    try:
        # Try to load from database first
        crypto_data = load_multiple_crypto_from_database(symbols, timeframe, limit=util.TOTAL_DATE)
        log.logger.info("Successfully loaded data from database")
    except Exception as e:
        log.logger.warning(f"Failed to load from database: {e}")
        log.logger.info("Falling back to CSV files...")
        # Fallback to CSV files if database fails
        data_dir = util.CRYPTO_DATA_DIR
        crypto_data = load_multiple_crypto_symbols(data_dir, symbols, timeframe)
    
    # Determine simulation length from data
    available_data = [len(crypto_data[s]) for s in symbols if len(crypto_data[s]) > 0]
    if not available_data:
        log.logger.error("No crypto data loaded! Please check database connection or CSV files.")
        return
    
    min_bars = min(available_data)
    
    # Limit simulation to available data or TOTAL_DATE (whichever is smaller)
    total_bars = min(util.TOTAL_DATE, min_bars)
    log.logger.info(f"Running crypto simulation for {total_bars} bars")
    
    # Initialize crypto trading pairs
    secretary = Secretary(args.model)
    crypto_1 = Crypto(util.CRYPTO_SYMBOL_1, util.CRYPTO_BTC_INITIAL_PRICE, 0, is_new=False)
    crypto_2 = Crypto(util.CRYPTO_SYMBOL_2, util.CRYPTO_ETH_INITIAL_PRICE, 0, is_new=False)
    
    # Load OHLCV data
    if len(crypto_data[util.CRYPTO_SYMBOL_1]) > 0:
        crypto_1.load_ohlcv_from_dataframe(crypto_data[util.CRYPTO_SYMBOL_1])
    if len(crypto_data[util.CRYPTO_SYMBOL_2]) > 0:
        crypto_2.load_ohlcv_from_dataframe(crypto_data[util.CRYPTO_SYMBOL_2])
    
    # Initialize agents with crypto prices
    all_agents = []
    log.logger.debug("Agents initializing...")
    for i in range(0, util.AGENTS_NUM):
        agent = Agent(i, crypto_1.get_price(), crypto_2.get_price(), secretary, args.model, is_crypto=True)
        all_agents.append(agent)
        log.logger.debug(f"Agent {i}: cash={agent.cash}, {util.CRYPTO_SYMBOL_1}={agent.crypto_1_amount}, "
                        f"{util.CRYPTO_SYMBOL_2}={agent.crypto_2_amount}, debt={agent.loans}")
    
    # Start simulation
    last_day_forum_message = []
    crypto_1_deals = {"sell": [], "buy": []}
    crypto_2_deals = {"sell": [], "buy": []}
    
    log.logger.debug("--------Crypto Simulation Start!--------")
    for bar_index in range(total_bars):
        date = bar_index + 1  # 1-indexed for compatibility
        
        log.logger.debug(f"--------BAR {date} (Index {bar_index})---------")
        
        # Update prices from OHLCV data
        crypto_1.update_price_from_bar(bar_index)
        crypto_2.update_price_from_bar(bar_index)
        
        # Clear previous session orders
        crypto_1_deals["sell"].clear()
        crypto_1_deals["buy"].clear()
        crypto_2_deals["sell"].clear()
        crypto_2_deals["buy"].clear()
        
        # Check if agents need to repay loans
        for agent in all_agents[:]:
            agent.chat_history.clear()  # Clear daily chat history
            agent.loan_repayment(date)
        
        # Repayment days
        if date in util.REPAYMENT_DAYS:
            for agent in all_agents[:]:
                agent.interest_payment()
        
        # Handle bankrupt agents
        for agent in all_agents[:]:
            if agent.is_bankrupt:
                quit_sig = agent.bankrupt_process(crypto_1.get_price(), crypto_2.get_price(), is_crypto=True)
                if quit_sig:
                    agent.quit = True
                    all_agents.remove(agent)
        
        # Special events (crypto-specific)
        if date == util.CRYPTO_EVENT_1_DAY:
            util.LOAN_RATE = util.CRYPTO_EVENT_1_LOAN_RATE
            last_day_forum_message.append({"name": -1, "message": util.CRYPTO_EVENT_1_MESSAGE})
        if date == util.CRYPTO_EVENT_2_DAY:
            util.LOAN_RATE = util.CRYPTO_EVENT_2_LOAN_RATE
            last_day_forum_message.append({"name": -1, "message": util.CRYPTO_EVENT_2_MESSAGE})
        
        # Agents decide on loans
        daily_agent_records = []
        for agent in all_agents:
            loan = agent.plan_loan(date, crypto_1.get_price(), crypto_2.get_price(), last_day_forum_message, is_crypto=True)
            daily_agent_records.append(AgentRecordDaily(agent.order, date, loan))
        
        # Trading sessions (multiple sessions per bar, similar to stock mode)
        for session in range(1, util.TOTAL_SESSION + 1):
            log.logger.debug(f"SESSION {session}")
            # Randomize trading order
            sequence = list(range(len(all_agents)))
            random.shuffle(sequence)
            
            for i in sequence:
                agent = all_agents[i]
                
                action = agent.plan_crypto(date, session, crypto_1, crypto_2, crypto_1_deals, crypto_2_deals)
                proper, cash, value_1, value_2 = agent.get_proper_cash_value(crypto_1.get_price(), crypto_2.get_price(), is_crypto=True)
                create_agentses_record(agent.order, date, session, proper, cash, value_1, value_2, action)
                action["agent"] = agent.order
                action["date"] = date
                
                if not action["action_type"] == "no":
                    # Determine which crypto pair
                    symbol = action.get("symbol", "")
                    if symbol == util.CRYPTO_SYMBOL_1:
                        handle_action(action, crypto_1_deals, all_agents, crypto_1, session, is_crypto=True)
                    elif symbol == util.CRYPTO_SYMBOL_2:
                        handle_action(action, crypto_2_deals, all_agents, crypto_2, session, is_crypto=True)
            
            # Session ends, update prices from trades (or keep OHLCV close price)
            crypto_1.update_price(date)
            crypto_2.update_price(date)
            create_stock_record(date, session, crypto_1.get_price(), crypto_2.get_price())
        
        # Agents predict next period actions
        for idx, agent in enumerate(all_agents):
            estimation = agent.next_day_estimate(is_crypto=True)
            log.logger.info(f"Agent {agent.order} next period estimation: {estimation}")
            if idx >= len(daily_agent_records):
                break
            daily_agent_records[idx].add_estimate(estimation, is_crypto=True)
            daily_agent_records[idx].write_to_excel()
        daily_agent_records.clear()
        
        # Period ends, update forum messages
        last_day_forum_message.clear()
        log.logger.debug(f"BAR {date} ends, displaying forum messages...")
        for agent in all_agents:
            message = agent.post_message(is_crypto=True)
            log.logger.info(f"Agent {agent.order} says: {message}")
            last_day_forum_message.append({"name": agent.order, "message": message})
    
    log.logger.debug("--------Crypto Simulation finished!--------")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", type=str, default="gemini-pro", help="model name (e.g., gemini-pro, gpt-4)")
    parser.add_argument("--market_mode", type=str, default="stock", choices=["stock", "crypto"],
                        help="Market mode: 'stock' for stock trading, 'crypto' for spot crypto trading")
    args = parser.parse_args()
    
    if args.market_mode == "crypto":
        log.logger.info("Starting crypto spot trading simulation...")
        simulation_crypto(args)
    else:
        log.logger.info("Starting stock trading simulation...")
        simulation(args)
