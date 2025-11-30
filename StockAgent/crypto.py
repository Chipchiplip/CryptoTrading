"""
Crypto environment for spot-only cryptocurrency trading simulation.
Mirrors the Stock class interface but adapted for crypto markets (24/7, no market close).
"""

import util


class Crypto:
    """
    Represents a cryptocurrency trading pair (e.g., BTCUSD, ETHUSD).
    
    Unlike stocks, crypto markets are 24/7 with no market open/close.
    Each bar (candle) represents a tradable period.
    """
    
    def __init__(self, symbol, initial_price, initial_holdings=0, is_new=False):
        """
        Initialize a crypto trading pair.
        
        Args:
            symbol: Trading pair symbol (e.g., "BTCUSD", "ETHUSD")
            initial_price: Initial price in USD
            initial_holdings: Initial amount held (for simulation purposes)
            is_new: Whether this is a newly listed token (not used in spot-only mode)
        """
        self.symbol = symbol
        self.price = initial_price
        self.ideal_price = 0  # For future use (target price)
        self.initial_holdings = initial_holdings
        self.history = {}   # {date: session_deal}
        self.session_deal = []  # [{"price", "amount"}]
        
        # Crypto-specific: OHLCV data loaded from CSV
        self.ohlcv_data = None  # Will be populated by data loader
        
    def gen_onchain_metrics(self, index):
        """
        Generate on-chain metrics or protocol data (replaces financial reports).
        
        For crypto, we use on-chain metrics instead of company financials:
        - Active addresses
        - Transaction volume
        - Network hash rate (for PoW)
        - Staking metrics (for PoS)
        - TVL (Total Value Locked) for DeFi tokens
        
        Args:
            index: Period index (similar to quarterly reports)
            
        Returns:
            String description of on-chain metrics
        """
        if self.symbol == "BTCUSD":
            return util.ONCHAIN_METRICS_BTC[index] if index < len(util.ONCHAIN_METRICS_BTC) else ""
        elif self.symbol == "ETHUSD":
            return util.ONCHAIN_METRICS_ETH[index] if index < len(util.ONCHAIN_METRICS_ETH) else ""
        return ""
    
    def add_session_deal(self, price_and_amount):
        """
        Add a trade execution to the current session.
        
        Args:
            price_and_amount: Dict with "price" and "amount" keys
        """
        self.session_deal.append(price_and_amount)
    
    def update_price(self, date):
        """
        Update price from the last trade in current session.
        In crypto, price updates happen after each bar/candle closes.
        
        Args:
            date: Current date/bar index
        """
        if len(self.session_deal) == 0:
            return
        # Price is the last executed trade price
        self.price = self.session_deal[-1]["price"]
        self.history[date] = self.session_deal
        self.session_deal.clear()
    
    def get_price(self):
        """Get current price."""
        return self.price
    
    def load_ohlcv_from_dataframe(self, df):
        """
        Load OHLCV data from a pandas DataFrame.
        
        Expected columns: timestamp, open, high, low, close, volume
        
        Args:
            df: DataFrame with OHLCV data
        """
        self.ohlcv_data = df
        if len(df) > 0:
            # Set initial price from first close price
            self.price = float(df.iloc[0]['close'])
    
    def update_price_from_bar(self, bar_index):
        """
        Update price from OHLCV data at a specific bar index.
        In crypto mode, each bar represents a candle (e.g., 1h, 4h).
        
        Args:
            bar_index: Index of the bar in the OHLCV DataFrame (0-based)
            
        Returns:
            True if update successful, False otherwise
        """
        if self.ohlcv_data is None or len(self.ohlcv_data) == 0:
            return False
        
        if bar_index < 0 or bar_index >= len(self.ohlcv_data):
            return False
        
        # Update price from the close price of this bar
        self.price = float(self.ohlcv_data.iloc[bar_index]['close'])
        return True
    
    def get_ohlcv_at_bar(self, bar_index):
        """
        Get OHLCV data at a specific bar index.
        
        Args:
            bar_index: Index of the bar (0-based)
            
        Returns:
            Dict with keys: timestamp, open, high, low, close, volume
            or None if index out of range
        """
        if self.ohlcv_data is None or len(self.ohlcv_data) == 0:
            return None
        
        if bar_index < 0 or bar_index >= len(self.ohlcv_data):
            return None
        
        row = self.ohlcv_data.iloc[bar_index]
        return {
            'timestamp': row['timestamp'],
            'open': float(row['open']),
            'high': float(row['high']),
            'low': float(row['low']),
            'close': float(row['close']),
            'volume': float(row['volume'])
        }

