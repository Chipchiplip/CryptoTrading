import { useState } from 'react';
import { TrendingUp, TrendingDown, Star, Search, ArrowUpRight, ArrowDownRight } from 'lucide-react';
import { Input } from '../../ui/input';
import { Button } from '../../ui/button';
import { Badge } from '../../ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../ui/tabs';

interface MarketsProps {
  onNavigate?: (page: string, coinId?: string) => void;
}

export default function Markets({ onNavigate }: MarketsProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [favorites, setFavorites] = useState<string[]>(['BTC', 'ETH']);

  const marketData = [
    { 
      id: 'btc',
      rank: 1, 
      symbol: 'BTC', 
      name: 'Bitcoin', 
      price: 50234.56, 
      change24h: 2.34, 
      volume24h: 28500000000, 
      marketCap: 982000000000,
      chart: [45, 52, 48, 55, 51, 58, 50]
    },
    { 
      id: 'eth',
      rank: 2, 
      symbol: 'ETH', 
      name: 'Ethereum', 
      price: 2845.32, 
      change24h: 1.82, 
      volume24h: 14200000000, 
      marketCap: 342000000000,
      chart: [42, 45, 43, 48, 46, 50, 48]
    },
    { 
      id: 'bnb',
      rank: 3, 
      symbol: 'BNB', 
      name: 'BNB', 
      price: 312.89, 
      change24h: 3.12, 
      volume24h: 1200000000, 
      marketCap: 48200000000,
      chart: [35, 38, 36, 42, 40, 45, 44]
    },
    { 
      id: 'sol',
      rank: 4, 
      symbol: 'SOL', 
      name: 'Solana', 
      price: 98.45, 
      change24h: -0.45, 
      volume24h: 2100000000, 
      marketCap: 42800000000,
      chart: [52, 50, 48, 45, 46, 44, 42]
    },
    { 
      id: 'xrp',
      rank: 5, 
      symbol: 'XRP', 
      name: 'Ripple', 
      price: 0.5234, 
      change24h: 4.23, 
      volume24h: 1800000000, 
      marketCap: 28400000000,
      chart: [30, 35, 32, 38, 36, 42, 45]
    },
    { 
      id: 'ada',
      rank: 6, 
      symbol: 'ADA', 
      name: 'Cardano', 
      price: 0.4523, 
      change24h: -1.34, 
      volume24h: 980000000, 
      marketCap: 15800000000,
      chart: [48, 45, 42, 40, 38, 36, 35]
    },
    { 
      id: 'doge',
      rank: 7, 
      symbol: 'DOGE', 
      name: 'Dogecoin', 
      price: 0.0823, 
      change24h: 5.67, 
      volume24h: 1200000000, 
      marketCap: 11600000000,
      chart: [25, 30, 28, 35, 33, 40, 42]
    },
    { 
      id: 'avax',
      rank: 8, 
      symbol: 'AVAX', 
      name: 'Avalanche', 
      price: 34.23, 
      change24h: -2.12, 
      volume24h: 620000000, 
      marketCap: 12400000000,
      chart: [55, 52, 48, 45, 42, 40, 38]
    },
    { 
      id: 'dot',
      rank: 9, 
      symbol: 'DOT', 
      name: 'Polkadot', 
      price: 6.45, 
      change24h: 1.23, 
      volume24h: 340000000, 
      marketCap: 8200000000,
      chart: [40, 42, 41, 45, 44, 48, 46]
    },
    { 
      id: 'matic',
      rank: 10, 
      symbol: 'MATIC', 
      name: 'Polygon', 
      price: 0.7823, 
      change24h: -0.89, 
      volume24h: 420000000, 
      marketCap: 7300000000,
      chart: [50, 48, 45, 42, 40, 38, 36]
    },
  ];

  const toggleFavorite = (symbol: string) => {
    setFavorites(prev => 
      prev.includes(symbol) 
        ? prev.filter(s => s !== symbol)
        : [...prev, symbol]
    );
  };

  const formatPrice = (price: number) => {
    if (price >= 1000) return `$${price.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    if (price >= 1) return `$${price.toFixed(2)}`;
    return `$${price.toFixed(4)}`;
  };

  const formatVolume = (volume: number) => {
    if (volume >= 1e9) return `$${(volume / 1e9).toFixed(2)}B`;
    if (volume >= 1e6) return `$${(volume / 1e6).toFixed(2)}M`;
    return `$${volume.toLocaleString()}`;
  };

  const filteredData = marketData.filter(coin =>
    coin.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    coin.symbol.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const favoriteCoins = filteredData.filter(coin => favorites.includes(coin.symbol));

  return (
    <div className="min-h-screen bg-black text-white">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-4xl mb-2">Market Overview</h1>
          <p className="text-gray-400">Real-time cryptocurrency prices and market data</p>
        </div>

        {/* Market Stats */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
          <div className="bg-gray-900 border border-gray-800 rounded-lg p-4">
            <div className="text-gray-400 text-sm mb-1">Market Cap</div>
            <div className="text-2xl text-white">$1.82T</div>
            <div className="text-emerald-500 text-sm mt-1">+2.4%</div>
          </div>
          <div className="bg-gray-900 border border-gray-800 rounded-lg p-4">
            <div className="text-gray-400 text-sm mb-1">24h Volume</div>
            <div className="text-2xl text-white">$89.4B</div>
            <div className="text-emerald-500 text-sm mt-1">+5.2%</div>
          </div>
          <div className="bg-gray-900 border border-gray-800 rounded-lg p-4">
            <div className="text-gray-400 text-sm mb-1">BTC Dominance</div>
            <div className="text-2xl text-white">53.8%</div>
            <div className="text-gray-400 text-sm mt-1">-0.3%</div>
          </div>
          <div className="bg-gray-900 border border-gray-800 rounded-lg p-4">
            <div className="text-gray-400 text-sm mb-1">Active Markets</div>
            <div className="text-2xl text-white">350+</div>
            <div className="text-emerald-500 text-sm mt-1">Live</div>
          </div>
        </div>

        {/* Search */}
        <div className="mb-6">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
            <Input
              placeholder="Search cryptocurrencies..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-10 bg-gray-900 border-gray-800 text-white"
            />
          </div>
        </div>

        {/* Tabs */}
        <Tabs defaultValue="all" className="w-full">
          <TabsList className="bg-gray-900 border border-gray-800 mb-6">
            <TabsTrigger value="all" className="data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
              All Markets
            </TabsTrigger>
            <TabsTrigger value="favorites" className="data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
              Favorites ({favorites.length})
            </TabsTrigger>
            <TabsTrigger value="gainers" className="data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
              Top Gainers
            </TabsTrigger>
            <TabsTrigger value="losers" className="data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
              Top Losers
            </TabsTrigger>
          </TabsList>

          <TabsContent value="all">
            <MarketTable 
              data={filteredData} 
              favorites={favorites} 
              onToggleFavorite={toggleFavorite}
              onSelectCoin={(coinId) => onNavigate?.('coin-detail', coinId)}
              formatPrice={formatPrice}
              formatVolume={formatVolume}
            />
          </TabsContent>

          <TabsContent value="favorites">
            {favoriteCoins.length > 0 ? (
              <MarketTable 
                data={favoriteCoins} 
                favorites={favorites} 
                onToggleFavorite={toggleFavorite}
                onSelectCoin={(coinId) => onNavigate?.('coin-detail', coinId)}
                formatPrice={formatPrice}
                formatVolume={formatVolume}
              />
            ) : (
              <div className="text-center py-16 bg-gray-900 border border-gray-800 rounded-lg">
                <Star className="w-12 h-12 text-gray-600 mx-auto mb-4" />
                <p className="text-gray-400">No favorites yet. Click the star icon to add coins to your watchlist.</p>
              </div>
            )}
          </TabsContent>

          <TabsContent value="gainers">
            <MarketTable 
              data={[...filteredData].sort((a, b) => b.change24h - a.change24h)} 
              favorites={favorites} 
              onToggleFavorite={toggleFavorite}
              onSelectCoin={(coinId) => onNavigate?.('coin-detail', coinId)}
              formatPrice={formatPrice}
              formatVolume={formatVolume}
            />
          </TabsContent>

          <TabsContent value="losers">
            <MarketTable 
              data={[...filteredData].sort((a, b) => a.change24h - b.change24h)} 
              favorites={favorites} 
              onToggleFavorite={toggleFavorite}
              onSelectCoin={(coinId) => onNavigate?.('coin-detail', coinId)}
              formatPrice={formatPrice}
              formatVolume={formatVolume}
            />
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}

interface MarketTableProps {
  data: any[];
  favorites: string[];
  onToggleFavorite: (symbol: string) => void;
  onSelectCoin: (coinId: string) => void;
  formatPrice: (price: number) => string;
  formatVolume: (volume: number) => string;
}

function MarketTable({ data, favorites, onToggleFavorite, onSelectCoin, formatPrice, formatVolume }: MarketTableProps) {
  return (
    <div className="bg-gray-900 border border-gray-800 rounded-lg overflow-hidden">
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-gray-800/50">
            <tr className="text-left text-gray-400 text-sm">
              <th className="p-4 w-12"></th>
              <th className="p-4">#</th>
              <th className="p-4">Name</th>
              <th className="p-4 text-right">Price</th>
              <th className="p-4 text-right">24h %</th>
              <th className="p-4 text-right hidden md:table-cell">24h Volume</th>
              <th className="p-4 text-right hidden lg:table-cell">Market Cap</th>
              <th className="p-4 text-right hidden xl:table-cell">Last 7 Days</th>
              <th className="p-4 text-right">Action</th>
            </tr>
          </thead>
          <tbody>
            {data.map((coin) => (
              <tr 
                key={coin.id}
                className="border-t border-gray-800 hover:bg-gray-800/50 transition-colors cursor-pointer"
                onClick={() => onSelectCoin(coin.id)}
              >
                <td className="p-4">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      onToggleFavorite(coin.symbol);
                    }}
                    className="text-gray-400 hover:text-yellow-500 transition-colors"
                  >
                    <Star 
                      className={`w-5 h-5 ${favorites.includes(coin.symbol) ? 'fill-yellow-500 text-yellow-500' : ''}`}
                    />
                  </button>
                </td>
                <td className="p-4 text-gray-400">{coin.rank}</td>
                <td className="p-4">
                  <div className="flex items-center gap-3">
                    <div className="w-8 h-8 bg-emerald-500/10 rounded-full flex items-center justify-center flex-shrink-0">
                      <span className="text-emerald-500 text-xs">{coin.symbol}</span>
                    </div>
                    <div>
                      <div className="text-white">{coin.name}</div>
                      <div className="text-sm text-gray-400">{coin.symbol}</div>
                    </div>
                  </div>
                </td>
                <td className="p-4 text-right text-white">{formatPrice(coin.price)}</td>
                <td className="p-4 text-right">
                  <div className={`flex items-center justify-end gap-1 ${coin.change24h >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
                    {coin.change24h >= 0 ? <ArrowUpRight className="w-4 h-4" /> : <ArrowDownRight className="w-4 h-4" />}
                    {Math.abs(coin.change24h).toFixed(2)}%
                  </div>
                </td>
                <td className="p-4 text-right text-gray-300 hidden md:table-cell">{formatVolume(coin.volume24h)}</td>
                <td className="p-4 text-right text-gray-300 hidden lg:table-cell">{formatVolume(coin.marketCap)}</td>
                <td className="p-4 hidden xl:table-cell">
                  <div className="flex items-end justify-end gap-0.5 h-8">
                    {coin.chart.map((value: number, index: number) => (
                      <div
                        key={index}
                        className={`w-1 rounded-sm ${coin.change24h >= 0 ? 'bg-emerald-500/50' : 'bg-red-500/50'}`}
                        style={{ height: `${value}%` }}
                      ></div>
                    ))}
                  </div>
                </td>
                <td className="p-4 text-right">
                  <Button
                    size="sm"
                    className="bg-emerald-500 text-black hover:bg-emerald-600"
                    onClick={(e) => {
                      e.stopPropagation();
                      // Would navigate to trade page
                    }}
                  >
                    Trade
                  </Button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
