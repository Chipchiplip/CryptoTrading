import React, { useEffect, useMemo, useState } from "react";
import axios from "axios";
import SparklineChart from "./SparklineChart";

function MarketStats() {
  const [stats, setStats] = useState(null);

  useEffect(() => {
    axios.get("/api/market/stats").then(res => setStats(res.data));
  }, []);

  // Load sparkline data (BTC 24h) for visual trend
  const [btcHistory, setBtcHistory] = useState([]);
  useEffect(() => {
    axios.get("/api/market/cryptocurrencies/bitcoin/history?days=1")
      .then(res => {
        const series = (res.data || []).map(p => p.price);
        setBtcHistory(series);
      })
      .catch(() => setBtcHistory([]));
  }, []);

  const formatCurrency = (num) => {
    if (num >= 1e12) return `$${(num / 1e12).toFixed(2)}T`;
    if (num >= 1e9) return `$${(num / 1e9).toFixed(2)}B`;
    if (num >= 1e6) return `$${(num / 1e6).toFixed(2)}M`;
    return `$${num.toLocaleString()}`;
  };

  // Fake sparkline trend (visual only) derived from the current value
  const marketCapTrend = useMemo(() => {
    if (btcHistory.length > 0) return btcHistory;
    if (!stats) return [];
    const base = stats.total_market_cap / 1e12;
    return Array.from({ length: 24 }, (_, i) => base * (0.95 + 0.1 * Math.sin(i / 3)));
  }, [stats, btcHistory]);

  const volumeTrend = useMemo(() => {
    if (btcHistory.length > 0) return btcHistory;
    if (!stats) return [];
    const base = stats.total_volume / 1e9;
    return Array.from({ length: 24 }, (_, i) => base * (0.92 + 0.12 * Math.sin(i / 2.5)));
  }, [stats, btcHistory]);

  if (!stats) return <div>Loading market stats...</div>;

  return (
    <div style={{ display: 'grid', gridTemplateColumns: '1fr', gap: '1rem', marginBottom: '2rem' }}>
      {/* Market Cap Card */}
      <div style={{ background: '#fff', borderRadius: '12px', padding: '20px', boxShadow: '0 2px 12px rgba(0,0,0,0.08)', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <div>
          <div style={{ fontSize: '28px', fontWeight: 800, color: '#0f172a' }}>{formatCurrency(stats.total_market_cap)}</div>
          <div style={{ color: '#64748b', marginTop: '6px', fontWeight: 600 }}>Market Cap&nbsp;
            <span style={{ color: stats.market_cap_change_percentage_24h_usd >= 0 ? '#16a34a' : '#dc2626', fontWeight: 700 }}>
              {stats.market_cap_change_percentage_24h_usd >= 0 ? '▲ ' : '▼ '}
              {Math.abs(stats.market_cap_change_percentage_24h_usd).toFixed(1)}%
            </span>
          </div>
        </div>
        <SparklineChart data={marketCapTrend} width={200} height={70} color="#10b981" />
      </div>

      {/* 24h Trading Volume Card */}
      <div style={{ background: '#fff', borderRadius: '12px', padding: '20px', boxShadow: '0 2px 12px rgba(0,0,0,0.08)', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <div>
          <div style={{ fontSize: '28px', fontWeight: 800, color: '#0f172a' }}>{formatCurrency(stats.total_volume)}</div>
          <div style={{ color: '#64748b', marginTop: '6px', fontWeight: 600 }}>24h Trading Volume</div>
        </div>
        <SparklineChart data={volumeTrend} width={200} height={70} color="#10b981" />
      </div>
    </div>
  );
}

export default MarketStats;















