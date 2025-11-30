"""
Database loader for crypto OHLCV data from MySQL database.
Retrieves historical price data from the crypto_trading database.
"""

import pandas as pd
import pymysql
from typing import Dict, List, Optional
from datetime import datetime, timedelta
import util
from log.custom_logger import log


def get_db_connection():
    """
    Create a connection to the MySQL database.
    
    Returns:
        pymysql.Connection object
    """
    try:
        connection = pymysql.connect(
            host=util.DB_HOST,
            port=util.DB_PORT,
            user=util.DB_USER,
            password=util.DB_PASSWORD,
            database=util.DB_NAME,
            ssl={'ssl': {'ca': None}} if util.DB_SSL_MODE == "Required" else None,
            charset='utf8mb4',
            cursorclass=pymysql.cursors.DictCursor
        )
        return connection
    except Exception as e:
        raise ConnectionError(f"Failed to connect to database: {e}")


def load_crypto_from_database(symbol: str, timeframe: str = "1h", limit: Optional[int] = None) -> pd.DataFrame:
    """
    Load crypto OHLCV data from database for a specific symbol.
    
    The function aggregates CryptoPrices data into OHLCV candles based on timeframe.
    Since the database stores individual price points, we need to aggregate them.
    
    Args:
        symbol: Trading pair symbol (e.g., "BTCUSD", "ETHUSD")
        timeframe: Timeframe for aggregation ("1h", "4h", "1d")
        limit: Maximum number of bars to return (None = all available)
        
    Returns:
        DataFrame with columns: timestamp, open, high, low, close, volume
        Sorted by timestamp ascending
    """
    connection = None
    try:
        connection = get_db_connection()
        
        # Map timeframe to SQL time interval
        timeframe_map = {
            "1h": "HOUR",
            "4h": "HOUR",
            "1d": "DAY"
        }
        
        if timeframe not in timeframe_map:
            raise ValueError(f"Unsupported timeframe: {timeframe}. Supported: {list(timeframe_map.keys())}")
        
        # Get cryptocurrency ID from symbol
        with connection.cursor() as cursor:
            # Try to find by symbol (e.g., "BTCUSD" -> "BTC")
            base_symbol = symbol.replace("USD", "").replace("USDT", "").upper()
            
            # Try exact match first, then base symbol
            # Tables are in crypto_trading database, not in a separate 'market' schema
            cursor.execute("""
                SELECT Id, Symbol, Name 
                FROM Cryptocurrencies 
                WHERE Symbol = %s OR Symbol = %s
                ORDER BY CASE WHEN Symbol = %s THEN 1 ELSE 2 END
                LIMIT 1
            """, (symbol, base_symbol, symbol))
            
            crypto = cursor.fetchone()
            if not crypto:
                raise ValueError(f"Cryptocurrency not found in database: {symbol} (tried: {symbol}, {base_symbol})")
            
            crypto_id = crypto['Id']
            log.logger.debug(f"Found cryptocurrency: {crypto['Name']} (ID: {crypto_id}) for symbol {symbol}")
        
        # Build aggregation query based on timeframe
        if timeframe == "1h":
            # Aggregate by hour
            group_by_expr = """
                DATE_FORMAT(CollectedAtUtc, '%%Y-%%m-%%d %%H:00:00')
            """
            order_by_expr = group_by_expr  # Use same expression for ORDER BY
            limit_clause = f"LIMIT {limit}" if limit else ""
        elif timeframe == "4h":
            # Aggregate by 4-hour intervals
            group_by_expr = """
                DATE_FORMAT(
                    DATE_SUB(CollectedAtUtc, INTERVAL HOUR(CollectedAtUtc) %% 4 HOUR),
                    '%%Y-%%m-%%d %%H:00:00'
                )
            """
            order_by_expr = group_by_expr
            limit_clause = f"LIMIT {limit * 4}" if limit else ""
        elif timeframe == "1d":
            # Aggregate by day
            group_by_expr = "DATE(CollectedAtUtc)"
            order_by_expr = group_by_expr
            limit_clause = f"LIMIT {limit}" if limit else ""
        else:
            raise ValueError(f"Unsupported timeframe: {timeframe}")
        
        # Query to aggregate OHLCV data
        # Tables are in crypto_trading database directly
        # Use subquery to handle ORDER BY with GROUP BY properly
        query = f"""
            SELECT 
                time_bucket,
                low,
                high,
                open,
                close,
                volume,
                timestamp
            FROM (
                SELECT 
                    {group_by_expr} as time_bucket,
                    MIN(PriceUsd) as low,
                    MAX(PriceUsd) as high,
                    SUBSTRING_INDEX(GROUP_CONCAT(PriceUsd ORDER BY CollectedAtUtc), ',', 1) as open,
                    SUBSTRING_INDEX(GROUP_CONCAT(PriceUsd ORDER BY CollectedAtUtc DESC), ',', 1) as close,
                    AVG(Volume24h) as volume,
                    MIN(CollectedAtUtc) as timestamp
                FROM CryptoPrices
                WHERE CryptocurrencyId = %s
                GROUP BY {group_by_expr}
            ) as aggregated
            ORDER BY time_bucket ASC
            {limit_clause}
        """
        
        with connection.cursor() as cursor:
            cursor.execute(query, (crypto_id,))
            results = cursor.fetchall()
        
        if not results:
            raise ValueError(f"No price data found for {symbol} in database")
        
        # Convert to DataFrame
        data = []
        for row in results:
            # Convert timestamp to Unix timestamp
            if isinstance(row['timestamp'], datetime):
                timestamp = int(row['timestamp'].timestamp())
            else:
                # If time_bucket is used, parse it
                try:
                    dt = datetime.strptime(str(row['time_bucket']), '%Y-%m-%d %H:%M:%S')
                    timestamp = int(dt.timestamp())
                except:
                    dt = datetime.strptime(str(row['time_bucket']), '%Y-%m-%d')
                    timestamp = int(dt.timestamp())
            
            data.append({
                'timestamp': timestamp,
                'open': float(row['open']) if row['open'] else 0.0,
                'high': float(row['high']) if row['high'] else 0.0,
                'low': float(row['low']) if row['low'] else 0.0,
                'close': float(row['close']) if row['close'] else 0.0,
                'volume': float(row['volume']) if row['volume'] else 0.0
            })
        
        df = pd.DataFrame(data)
        
        # Sort by timestamp ascending (oldest first)
        df = df.sort_values('timestamp').reset_index(drop=True)
        
        return df
        
    except Exception as e:
        raise RuntimeError(f"Error loading crypto data from database: {e}")
    finally:
        if connection:
            connection.close()


def load_multiple_crypto_from_database(symbols: List[str], timeframe: str = "1h", limit: Optional[int] = None) -> Dict[str, pd.DataFrame]:
    """
    Load OHLCV data for multiple crypto symbols from database.
    
    Args:
        symbols: List of symbols to load (e.g., ["BTCUSD", "ETHUSD"])
        timeframe: Timeframe for aggregation (e.g., "1h", "4h", "1d")
        limit: Maximum number of bars per symbol
        
    Returns:
        Dictionary mapping symbol to DataFrame
    """
    data = {}
    
    for symbol in symbols:
        try:
            df = load_crypto_from_database(symbol, timeframe, limit)
            data[symbol] = df
            print(f"Loaded {len(df)} bars for {symbol} from database")
        except Exception as e:
            print(f"Error loading {symbol}: {e}")
            # Create empty DataFrame with correct structure
            data[symbol] = pd.DataFrame(columns=['timestamp', 'open', 'high', 'low', 'close', 'volume'])
    
    return data

