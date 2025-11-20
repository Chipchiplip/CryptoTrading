"""
Example script to help set up crypto data for testing.

This script creates sample CSV files in the data/crypto/ directory
for testing the crypto trading simulation.
"""

import pandas as pd
import os
from datetime import datetime, timedelta

def create_sample_crypto_data():
    """
    Create sample OHLCV CSV files for BTCUSDT and ETHUSDT.
    This is for testing purposes only.
    """
    # Create data directory if it doesn't exist
    data_dir = "data/crypto"
    os.makedirs(data_dir, exist_ok=True)
    
    # Generate sample data for BTCUSDT (1h timeframe)
    # Starting from a base timestamp
    base_time = datetime(2024, 1, 1, 0, 0, 0)
    num_bars = 200  # 200 hours of data
    
    btc_data = []
    btc_price = 50000.0  # Starting price
    
    for i in range(num_bars):
        timestamp = base_time + timedelta(hours=i)
        # Simple random walk for price simulation
        import random
        change = random.uniform(-0.02, 0.02)  # ±2% change per hour
        btc_price = btc_price * (1 + change)
        
        open_price = btc_price
        high_price = btc_price * (1 + random.uniform(0, 0.01))
        low_price = btc_price * (1 - random.uniform(0, 0.01))
        close_price = btc_price * (1 + random.uniform(-0.005, 0.005))
        volume = random.uniform(100, 1000)
        
        btc_data.append({
            'timestamp': int(timestamp.timestamp()),
            'open': round(open_price, 2),
            'high': round(high_price, 2),
            'low': round(low_price, 2),
            'close': round(close_price, 2),
            'volume': round(volume, 2)
        })
    
    # Generate sample data for ETHUSDT
    eth_data = []
    eth_price = 3000.0  # Starting price
    
    for i in range(num_bars):
        timestamp = base_time + timedelta(hours=i)
        import random
        change = random.uniform(-0.02, 0.02)
        eth_price = eth_price * (1 + change)
        
        open_price = eth_price
        high_price = eth_price * (1 + random.uniform(0, 0.01))
        low_price = eth_price * (1 - random.uniform(0, 0.01))
        close_price = eth_price * (1 + random.uniform(-0.005, 0.005))
        volume = random.uniform(500, 5000)
        
        eth_data.append({
            'timestamp': int(timestamp.timestamp()),
            'open': round(open_price, 2),
            'high': round(high_price, 2),
            'low': round(low_price, 2),
            'close': round(close_price, 2),
            'volume': round(volume, 2)
        })
    
    # Create DataFrames and save to CSV
    btc_df = pd.DataFrame(btc_data)
    eth_df = pd.DataFrame(eth_data)
    
    btc_file = os.path.join(data_dir, "BTCUSDT_1h.csv")
    eth_file = os.path.join(data_dir, "ETHUSDT_1h.csv")
    
    btc_df.to_csv(btc_file, index=False)
    eth_df.to_csv(eth_file, index=False)
    
    print(f"✅ Created sample data files:")
    print(f"   - {btc_file} ({len(btc_df)} bars)")
    print(f"   - {eth_file} ({len(eth_df)} bars)")
    print(f"\nYou can now run:")
    print(f"   python main.py --model gemini-pro --market_mode crypto")


if __name__ == "__main__":
    create_sample_crypto_data()

