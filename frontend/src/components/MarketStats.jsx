import React, { useEffect, useState } from "react";
import axios from "axios";

function MarketStats() {
  const [stats, setStats] = useState(null);

  useEffect(() => {
    axios.get("/api/crypto/stats").then(res => setStats(res.data));
  }, []);

  if (!stats) return <div>Loading market stats...</div>;

  return (
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem', marginBottom: '2rem' }}>
      <div style={{ background: '#f0f0f0', padding: '1rem', borderRadius: '8px', textAlign: 'center' }}>
        <h3>Total Market Cap</h3>
        <p style={{ fontSize: '1.5rem', fontWeight: 'bold', color: '#2e7d32' }}>
          ${(stats.totalMarketCap / 1e12).toFixed(2)}T
        </p>
      </div>
      
      <div style={{ background: '#f0f0f0', padding: '1rem', borderRadius: '8px', textAlign: 'center' }}>
        <h3>24h Volume</h3>
        <p style={{ fontSize: '1.5rem', fontWeight: 'bold', color: '#1976d2' }}>
          ${(stats.totalVolume / 1e9).toFixed(2)}B
        </p>
      </div>
      
      <div style={{ background: '#f0f0f0', padding: '1rem', borderRadius: '8px', textAlign: 'center' }}>
        <h3>Active Cryptocurrencies</h3>
        <p style={{ fontSize: '1.5rem', fontWeight: 'bold', color: '#7b1fa2' }}>
          {stats.activeCryptocurrencies.toLocaleString()}
        </p>
      </div>
      
      <div style={{ background: '#f0f0f0', padding: '1rem', borderRadius: '8px', textAlign: 'center' }}>
        <h3>Market Cap Change 24h</h3>
        <p style={{ 
          fontSize: '1.5rem', 
          fontWeight: 'bold', 
          color: stats.marketCapChangePercentage24h >= 0 ? '#2e7d32' : '#d32f2f' 
        }}>
          {stats.marketCapChangePercentage24h >= 0 ? '+' : ''}{stats.marketCapChangePercentage24h.toFixed(2)}%
        </p>
      </div>
    </div>
  );
}

export default MarketStats;















