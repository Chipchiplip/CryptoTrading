"""
Data loading utilities for crypto OHLCV data from CSV files.
"""

import pandas as pd
import os
from typing import Dict, List, Optional


def load_crypto_csv(path: str, symbol: str) -> pd.DataFrame:
    """
    Load crypto OHLCV data from a CSV file.
    
    Expected CSV format:
    - Columns: timestamp, open, high, low, close, volume
    - Timestamp can be Unix timestamp or ISO format
    - Data should be sorted by time (ascending)
    
    Args:
        path: Path to CSV file
        symbol: Trading pair symbol (e.g., "BTCUSD") for validation
        
    Returns:
        DataFrame with columns: timestamp, open, high, low, close, volume
        Sorted by timestamp ascending
    """
    if not os.path.exists(path):
        raise FileNotFoundError(f"CSV file not found: {path}")
    
    df = pd.read_csv(path)
    
    # Validate required columns
    required_cols = ['timestamp', 'open', 'high', 'low', 'close', 'volume']
    missing_cols = [col for col in required_cols if col not in df.columns]
    if missing_cols:
        raise ValueError(f"Missing required columns: {missing_cols}")
    
    # Convert timestamp to datetime if needed
    if df['timestamp'].dtype == 'int64' or df['timestamp'].dtype == 'float64':
        # Assume Unix timestamp
        df['timestamp'] = pd.to_datetime(df['timestamp'], unit='s')
    else:
        df['timestamp'] = pd.to_datetime(df['timestamp'])
    
    # Sort by timestamp
    df = df.sort_values('timestamp').reset_index(drop=True)
    
    # Validate data types
    for col in ['open', 'high', 'low', 'close', 'volume']:
        df[col] = pd.to_numeric(df[col], errors='coerce')
    
    # Remove rows with missing data
    df = df.dropna()
    
    # Ensure no duplicate timestamps
    df = df.drop_duplicates(subset=['timestamp'], keep='last')
    
    return df[required_cols]


def load_multiple_crypto_symbols(data_dir: str, symbols: List[str], timeframe: str = "1h") -> Dict[str, pd.DataFrame]:
    """
    Load OHLCV data for multiple crypto symbols.
    
    Expected file naming: {symbol}_{timeframe}.csv
    Example: BTCUSD_1h.csv, ETHUSD_1h.csv
    
    Args:
        data_dir: Directory containing CSV files
        symbols: List of symbols to load (e.g., ["BTCUSD", "ETHUSD"])
        timeframe: Timeframe suffix (e.g., "1h", "4h") - used in filename
        
    Returns:
        Dictionary mapping symbol to DataFrame
    """
    data = {}
    
    for symbol in symbols:
        # Construct filename: BTCUSD_1h.csv
        filename = f"{symbol}_{timeframe}.csv"
        filepath = os.path.join(data_dir, filename)
        
        try:
            df = load_crypto_csv(filepath, symbol)
            data[symbol] = df
            print(f"Loaded {len(df)} bars for {symbol}")
        except FileNotFoundError:
            print(f"Warning: File not found for {symbol}: {filepath}")
            # Create empty DataFrame with correct structure
            data[symbol] = pd.DataFrame(columns=['timestamp', 'open', 'high', 'low', 'close', 'volume'])
        except Exception as e:
            print(f"Error loading {symbol}: {e}")
            data[symbol] = pd.DataFrame(columns=['timestamp', 'open', 'high', 'low', 'close', 'volume'])
    
    return data


def get_price_at_index(df: pd.DataFrame, index: int) -> Optional[float]:
    """
    Get the close price at a specific index in the DataFrame.
    
    Args:
        df: OHLCV DataFrame
        index: Index (0-based)
        
    Returns:
        Close price or None if index out of range
    """
    if index < 0 or index >= len(df):
        return None
    return float(df.iloc[index]['close'])


def get_ohlcv_at_index(df: pd.DataFrame, index: int) -> Optional[dict]:
    """
    Get full OHLCV data at a specific index.
    
    Args:
        df: OHLCV DataFrame
        index: Index (0-based)
        
    Returns:
        Dict with keys: timestamp, open, high, low, close, volume
    """
    if index < 0 or index >= len(df):
        return None
    
    row = df.iloc[index]
    return {
        'timestamp': row['timestamp'],
        'open': float(row['open']),
        'high': float(row['high']),
        'low': float(row['low']),
        'close': float(row['close']),
        'volume': float(row['volume'])
    }

