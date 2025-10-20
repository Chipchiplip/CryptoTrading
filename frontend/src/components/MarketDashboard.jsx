import React, { useState } from "react";
import useMarketData from "../hooks/useMarketData";
import MarketStats from "./MarketStats";
import PriceChart from "./PriceChart";

function MarketDashboard() {
  const { coins } = useMarketData();
  const [selectedCoin, setSelectedCoin] = useState(null);
  const [selectedDays, setSelectedDays] = useState(7);

  return (
    <div style={{ padding: '2rem', maxWidth: '1200px', margin: '0 auto' }}>
      <h2 style={{ textAlign: 'center', marginBottom: '2rem', color: '#333' }}>
        📊 Crypto Market Dashboard
      </h2>
      
      {/* Market Statistics */}
      <MarketStats />
      
      {/* Crypto List */}
      <div style={{ marginBottom: '2rem' }}>
        <h3>Cryptocurrency Prices</h3>
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', background: 'white', borderRadius: '8px', overflow: 'hidden', boxShadow: '0 2px 8px rgba(0,0,0,0.1)' }}>
            <thead style={{ background: '#f5f5f5' }}>
              <tr>
                <th style={{ padding: '1rem', textAlign: 'left', borderBottom: '1px solid #ddd' }}>Name</th>
                <th style={{ padding: '1rem', textAlign: 'right', borderBottom: '1px solid #ddd' }}>Price</th>
                <th style={{ padding: '1rem', textAlign: 'right', borderBottom: '1px solid #ddd' }}>24h Change</th>
                <th style={{ padding: '1rem', textAlign: 'right', borderBottom: '1px solid #ddd' }}>Market Cap</th>
                <th style={{ padding: '1rem', textAlign: 'center', borderBottom: '1px solid #ddd' }}>Chart</th>
              </tr>
            </thead>
            <tbody>
              {coins.map(c => (
                <tr key={c.id} style={{ borderBottom: '1px solid #eee' }}>
                  <td style={{ padding: '1rem' }}>
                    <div>
                      <strong>{c.name}</strong>
                      <div style={{ color: '#666', fontSize: '0.9rem' }}>{c.symbol?.toUpperCase()}</div>
                    </div>
                  </td>
                  <td style={{ padding: '1rem', textAlign: 'right', fontWeight: 'bold' }}>
                    ${c.currentPrice?.toFixed(2)}
                  </td>
                  <td style={{ 
                    padding: '1rem', 
                    textAlign: 'right', 
                    color: c.priceChangePercentage24h >= 0 ? '#2e7d32' : '#d32f2f',
                    fontWeight: 'bold'
                  }}>
                    {c.priceChangePercentage24h >= 0 ? '+' : ''}{c.priceChangePercentage24h?.toFixed(2)}%
                  </td>
                  <td style={{ padding: '1rem', textAlign: 'right' }}>
                    ${(c.marketCap / 1e9).toFixed(2)}B
                  </td>
                  <td style={{ padding: '1rem', textAlign: 'center' }}>
                    <button 
                      onClick={() => setSelectedCoin(c.id)}
                      style={{
                        background: '#1976d2',
                        color: 'white',
                        border: 'none',
                        padding: '0.5rem 1rem',
                        borderRadius: '4px',
                        cursor: 'pointer'
                      }}
                    >
                      View Chart
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Price Chart */}
      {selectedCoin && (
        <div style={{ background: 'white', padding: '2rem', borderRadius: '8px', boxShadow: '0 2px 8px rgba(0,0,0,0.1)' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
            <h3>Price History</h3>
            <div>
              <label style={{ marginRight: '1rem' }}>Time Period: </label>
              <select 
                value={selectedDays} 
                onChange={(e) => setSelectedDays(parseInt(e.target.value))}
                style={{ padding: '0.5rem', borderRadius: '4px', border: '1px solid #ddd' }}
              >
                <option value={1}>1 Day</option>
                <option value={7}>7 Days</option>
                <option value={30}>30 Days</option>
                <option value={90}>90 Days</option>
              </select>
            </div>
          </div>
          <PriceChart coinId={selectedCoin} days={selectedDays} />
        </div>
      )}
    </div>
  );
}

export default MarketDashboard;
