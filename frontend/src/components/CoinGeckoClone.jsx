import React, { useState, useEffect } from 'react';
import useMarketData from '../hooks/useMarketData';
import SparklineChart from './SparklineChart';
import './CoinGeckoClone.css';

const CoinGeckoClone = () => {
  const { coins, marketStats } = useMarketData();
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('all');
  const [sortBy, setSortBy] = useState('market_cap');
  const [sortOrder, setSortOrder] = useState('desc');
  const [darkMode, setDarkMode] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [priceHistory, setPriceHistory] = useState({});

  // Debug logging
  console.log("CoinGeckoClone - coins:", coins);
  console.log("CoinGeckoClone - marketStats:", marketStats);

  // Generate mock price history to avoid rate limiting
  useEffect(() => {
    if (coins.length > 0) {
      const historyData = {};
      for (const coin of coins.slice(0, 10)) {
        // Generate mock data for demo to avoid rate limiting
        const mockData = Array.from({ length: 7 }, (_, i) => {
          const basePrice = coin.currentPrice || 100;
          const variation = (Math.random() - 0.5) * 0.1; // ±5% variation
          return basePrice * (1 + variation * (i / 6));
        });
        historyData[coin.id] = mockData;
      }
      console.log("Setting mock price history:", historyData);
      setPriceHistory(historyData);
    }
  }, [coins]);

  // Debug price history
  console.log("Current priceHistory state:", priceHistory);

  // Filter and sort coins
  const filteredCoins = coins
    .filter(coin => 
      coin.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      coin.symbol.toLowerCase().includes(searchTerm.toLowerCase())
    )
    .sort((a, b) => {
      let aValue, bValue;
      switch(sortBy) {
        case 'market_cap':
          aValue = a.marketCap || 0;
          bValue = b.marketCap || 0;
          break;
        case 'price':
          aValue = a.currentPrice || 0;
          bValue = b.currentPrice || 0;
          break;
        case 'change_24h':
          aValue = a.priceChangePercentage24h || 0;
          bValue = b.priceChangePercentage24h || 0;
          break;
        default:
          aValue = a.marketCap || 0;
          bValue = b.marketCap || 0;
      }
      return sortOrder === 'desc' ? bValue - aValue : aValue - bValue;
    });

  const formatNumber = (num) => {
    if (num === null || num === undefined || isNaN(num)) return '0';
    if (num >= 1e12) return (num / 1e12).toFixed(2) + 'T';
    if (num >= 1e9) return (num / 1e9).toFixed(2) + 'B';
    if (num >= 1e6) return (num / 1e6).toFixed(2) + 'M';
    if (num >= 1e3) return (num / 1e3).toFixed(2) + 'K';
    return num.toFixed(2);
  };

  const formatPrice = (price) => {
    if (price === null || price === undefined || isNaN(price)) return '$0.00';
    if (price >= 1) return '$' + price.toFixed(2);
    if (price >= 0.01) return '$' + price.toFixed(4);
    return '$' + price.toFixed(8);
  };

  useEffect(() => {
    if (coins.length > 0) {
      setIsLoading(false);
    }
  }, [coins]);

  return (
    <div className={`coingecko-clone ${darkMode ? 'dark-mode' : ''}`}>
      {/* Header */}
      <header className="header">
        <div className="header-top">
          <div className="announcement">
            📊 Now LIVE: <strong>2025 Q3 Crypto Industry Report</strong>
          </div>
        </div>
        
        <div className="header-main">
          <div className="container">
            <div className="header-content">
              <div className="logo">
                <div className="logo-icon">🦎</div>
                <span className="logo-text">CoinGecko</span>
              </div>
              
              <nav className="nav">
                <a href="#" className="nav-link active">Cryptocurrencies</a>
                <a href="#" className="nav-link">Exchanges</a>
                <a href="#" className="nav-link">NFT</a>
                <a href="#" className="nav-link">Learn</a>
                <a href="#" className="nav-link">Products</a>
                <a href="#" className="nav-link">API</a>
              </nav>
              
              <div className="header-actions">
                <div className="search-box">
                  <input
                    type="text"
                    placeholder="Search"
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="search-input"
                  />
                </div>
                <button 
                  className="dark-mode-toggle"
                  onClick={() => setDarkMode(!darkMode)}
                  title="Toggle Dark Mode"
                >
                  {darkMode ? '☀️' : '🌙'}
                </button>
                <button className="btn-login">Login</button>
                <button className="btn-signup">Sign up</button>
              </div>
            </div>
          </div>
        </div>
      </header>

          {/* Market Stats Bar */}
      <div className="market-stats-bar">
        <div className="container">
          <div className="stats-grid">
            <div className="stat-item">
              <span className="stat-label">Coins:</span>
              <span className="stat-value">{coins.length.toLocaleString()}</span>
            </div>
            <div className="stat-item">
              <span className="stat-label">Market Cap:</span>
              <span className="stat-value">
                ${formatNumber(marketStats?.totalMarketCap || 0)}
                <span className="stat-change positive">+2.8%</span>
              </span>
            </div>
            <div className="stat-item">
              <span className="stat-label">24h Vol:</span>
              <span className="stat-value">${formatNumber(marketStats?.totalVolume || 0)}</span>
            </div>
            <div className="stat-item">
              <span className="stat-label">Dominance:</span>
              <span className="stat-value">BTC 57.3% ETH 12.5%</span>
            </div>
          </div>
          <div className="attribution">
            <span className="attribution-text">Price data by <a href="https://www.coingecko.com" target="_blank" rel="noopener noreferrer" className="attribution-link">CoinGecko</a></span>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <main className="main-content">
        <div className="container">
          {/* Page Title */}
          <div className="page-header">
            <h1 className="page-title">Cryptocurrency Prices by Market Cap</h1>
            <p className="page-subtitle">
              The global cryptocurrency market cap today is <strong>${formatNumber(marketStats?.totalMarketCap || 0)}</strong>, 
              a <span className="positive">2.8%</span> change in the last 24 hours.
            </p>
          </div>

          {/* Highlights Section */}
          <div className="highlights-section">
            <div className="highlights-grid">
              <div className="highlight-card">
                <div className="highlight-value">${formatNumber(marketStats?.totalMarketCap || 0)}</div>
                <div className="highlight-label">Market Cap</div>
                <div className="highlight-change positive">+2.8%</div>
              </div>
              <div className="highlight-card">
                <div className="highlight-value">${formatNumber(marketStats?.totalVolume || 0)}</div>
                <div className="highlight-label">24h Trading Volume</div>
              </div>
            </div>
            
            <div className="trending-section">
              <div className="trending-box">
                <h3>🔥 Trending</h3>
                <div className="trending-list">
                  {filteredCoins.slice(0, 3).map((coin, index) => (
                    <div key={coin.id} className="trending-item">
                      <span className="trending-name">{coin.name}</span>
                      <span className="trending-price">{formatPrice(coin.currentPrice)}</span>
                      <span className={`trending-change ${(coin.priceChangePercentage24h || 0) >= 0 ? 'positive' : 'negative'}`}>
                        {(coin.priceChangePercentage24h || 0) >= 0 ? '+' : ''}{(coin.priceChangePercentage24h || 0).toFixed(2)}%
                      </span>
                    </div>
                  ))}
                </div>
                <div className="trending-attribution">
                  <span className="attribution-text">Price data by <a href="https://www.coingecko.com" target="_blank" rel="noopener noreferrer" className="attribution-link">CoinGecko</a></span>
                </div>
              </div>
              
              <div className="trending-box">
                <h3>🚀 Top Gainers</h3>
                <div className="trending-list">
                  {filteredCoins
                    .filter(coin => (coin.priceChangePercentage24h || 0) > 0)
                    .sort((a, b) => (b.priceChangePercentage24h || 0) - (a.priceChangePercentage24h || 0))
                    .slice(0, 3)
                    .map((coin, index) => (
                    <div key={coin.id} className="trending-item">
                      <span className="trending-name">{coin.name}</span>
                      <span className="trending-price">{formatPrice(coin.currentPrice)}</span>
                      <span className="trending-change positive">
                        +{(coin.priceChangePercentage24h || 0).toFixed(2)}%
                      </span>
                    </div>
                  ))}
                </div>
                <div className="trending-attribution">
                  <span className="attribution-text">Price data by <a href="https://www.coingecko.com" target="_blank" rel="noopener noreferrer" className="attribution-link">CoinGecko</a></span>
                </div>
              </div>
            </div>
          </div>

          {/* Crypto Table */}
          <div className="crypto-table-section">
            <div className="table-controls">
              <div className="filter-tabs">
                <button className={`filter-tab ${selectedCategory === 'all' ? 'active' : ''}`}
                        onClick={() => setSelectedCategory('all')}>All</button>
                <button className={`filter-tab ${selectedCategory === 'highlights' ? 'active' : ''}`}
                        onClick={() => setSelectedCategory('highlights')}>Highlights</button>
                <button className={`filter-tab ${selectedCategory === 'ai' ? 'active' : ''}`}
                        onClick={() => setSelectedCategory('ai')}>AI Applications</button>
              </div>
              
              <div className="table-options">
                <select className="sort-select" value={sortBy} onChange={(e) => setSortBy(e.target.value)}>
                  <option value="market_cap">Market Cap</option>
                  <option value="price">Price</option>
                  <option value="change_24h">24h Change</option>
                </select>
                <button className="sort-btn" onClick={() => setSortOrder(sortOrder === 'desc' ? 'asc' : 'desc')}>
                  {sortOrder === 'desc' ? '↓' : '↑'}
                </button>
              </div>
            </div>

            {isLoading ? (
              <div className="loading-container">
                <div className="loading-spinner"></div>
                <p>Loading cryptocurrency data...</p>
              </div>
            ) : (
              <div className="crypto-table">
                <table>
                  <thead>
                    <tr>
                      <th>#</th>
                      <th>Coin</th>
                      <th>Price</th>
                      <th>1h</th>
                      <th>24h</th>
                      <th>7d</th>
                      <th>24h Volume</th>
                      <th>Market Cap</th>
                      <th>Last 7 Days</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredCoins.map((coin, index) => (
                      <tr key={coin.id} className="crypto-row">
                        <td className="rank">{index + 1}</td>
                        <td className="coin-info">
                          <div className="coin-name">
                            <strong>{coin.name}</strong>
                            <span className="coin-symbol">{coin.symbol?.toUpperCase()}</span>
                          </div>
                        </td>
                        <td className="price">{formatPrice(coin.currentPrice)}</td>
                        <td className={`change ${(coin.priceChangePercentage1hInCurrency || 0) >= 0 ? 'positive' : 'negative'}`}>
                          {(coin.priceChangePercentage1hInCurrency || 0) >= 0 ? '+' : ''}{(coin.priceChangePercentage1hInCurrency || 0).toFixed(2)}%
                        </td>
                        <td className={`change ${(coin.priceChangePercentage24h || 0) >= 0 ? 'positive' : 'negative'}`}>
                          {(coin.priceChangePercentage24h || 0) >= 0 ? '+' : ''}{(coin.priceChangePercentage24h || 0).toFixed(2)}%
                        </td>
                        <td className={`change ${(coin.priceChangePercentage7dInCurrency || 0) >= 0 ? 'positive' : 'negative'}`}>
                          {coin.priceChangePercentage7dInCurrency ? 
                            `${(coin.priceChangePercentage7dInCurrency >= 0 ? '+' : '')}${coin.priceChangePercentage7dInCurrency.toFixed(2)}%` : 
                            'N/A'
                          }
                        </td>
                        <td className="volume">${formatNumber(coin.totalVolume)}</td>
                        <td className="market-cap">${formatNumber(coin.marketCap)}</td>
                        <td className="chart-cell">
                          <div className="mini-chart">
                            <SparklineChart 
                              data={priceHistory[coin.id] || []}
                              color={(coin.priceChangePercentage24h || 0) >= 0 ? '#10b981' : '#ef4444'}
                              width={100}
                              height={30}
                            />
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
          
          {/* Attribution Footer */}
          <div className="attribution-footer">
            <div className="container">
              <div className="attribution-content">
                <span className="attribution-text">Price data by <a href="https://www.coingecko.com" target="_blank" rel="noopener noreferrer" className="attribution-link">CoinGecko</a></span>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};

export default CoinGeckoClone;
