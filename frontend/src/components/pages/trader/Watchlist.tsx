import { useState } from 'react';
import { Star, Plus, TrendingUp, TrendingDown, Trash2, Search } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '../../ui/dialog';
import { Badge } from '../../ui/badge';

interface WatchlistProps {
  onNavigate?: (page: string) => void;
}

export default function Watchlist({ onNavigate }: WatchlistProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [watchlist, setWatchlist] = useState([
    { symbol: 'BTC', name: 'Bitcoin', price: 50234.56, change24h: 2.34, volume24h: 28500000000, chart: [45, 52, 48, 55, 51, 58, 50] },
    { symbol: 'ETH', name: 'Ethereum', price: 2845.32, change24h: 1.82, volume24h: 14200000000, chart: [42, 45, 43, 48, 46, 50, 48] },
    { symbol: 'SOL', name: 'Solana', price: 98.45, change24h: -0.45, volume24h: 2100000000, chart: [52, 50, 48, 45, 46, 44, 42] },
    { symbol: 'BNB', name: 'BNB', price: 312.89, change24h: 3.12, volume24h: 1200000000, chart: [35, 38, 36, 42, 40, 45, 44] },
  ]);

  const allCoins = [
    { symbol: 'ADA', name: 'Cardano', price: 0.4523, change24h: -1.34 },
    { symbol: 'DOT', name: 'Polkadot', price: 6.45, change24h: 1.23 },
    { symbol: 'AVAX', name: 'Avalanche', price: 34.23, change24h: -2.12 },
    { symbol: 'MATIC', name: 'Polygon', price: 0.7823, change24h: -0.89 },
    { symbol: 'LINK', name: 'Chainlink', price: 14.56, change24h: 2.45 },
  ];

  const removeFromWatchlist = (symbol: string) => {
    setWatchlist(watchlist.filter(coin => coin.symbol !== symbol));
  };

  const addToWatchlist = (coin: typeof allCoins[0]) => {
    const newCoin = {
      ...coin,
      volume24h: Math.random() * 1000000000,
      chart: Array.from({ length: 7 }, () => Math.random() * 100)
    };
    setWatchlist([...watchlist, newCoin]);
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

  return (
    <div className="p-4 lg:p-8">
      <div className="mb-6">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h1 className="text-3xl mb-2">My Watchlist</h1>
            <p className="text-gray-400">Track your favorite cryptocurrencies</p>
          </div>
          <Dialog>
            <DialogTrigger asChild>
              <Button className="bg-emerald-500 text-black hover:bg-emerald-600">
                <Plus className="w-4 h-4 mr-2" />
                Add Coin
              </Button>
            </DialogTrigger>
            <DialogContent className="bg-gray-900 border-gray-800 text-white">
              <DialogHeader>
                <DialogTitle>Add to Watchlist</DialogTitle>
              </DialogHeader>
              <div className="space-y-4">
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
                  <Input
                    placeholder="Search coins..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    className="pl-10 bg-gray-800 border-gray-700"
                  />
                </div>
                <div className="max-h-96 overflow-y-auto space-y-2">
                  {allCoins
                    .filter(coin => 
                      !watchlist.find(w => w.symbol === coin.symbol) &&
                      (coin.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
                       coin.symbol.toLowerCase().includes(searchQuery.toLowerCase()))
                    )
                    .map((coin) => (
                      <div
                        key={coin.symbol}
                        className="flex items-center justify-between p-3 rounded-lg hover:bg-gray-800 transition-colors"
                      >
                        <div className="flex items-center gap-3">
                          <div className="w-10 h-10 bg-emerald-500/10 rounded-full flex items-center justify-center">
                            <span className="text-emerald-500 text-sm">{coin.symbol}</span>
                          </div>
                          <div>
                            <div className="text-white">{coin.name}</div>
                            <div className="text-sm text-gray-400">{coin.symbol}</div>
                          </div>
                        </div>
                        <div className="flex items-center gap-4">
                          <div className="text-right">
                            <div className="text-white">{formatPrice(coin.price)}</div>
                            <div className={coin.change24h >= 0 ? 'text-emerald-500 text-sm' : 'text-red-500 text-sm'}>
                              {coin.change24h >= 0 ? '+' : ''}{coin.change24h.toFixed(2)}%
                            </div>
                          </div>
                          <Button
                            size="sm"
                            onClick={() => addToWatchlist(coin)}
                            className="bg-emerald-500 text-black hover:bg-emerald-600"
                          >
                            Add
                          </Button>
                        </div>
                      </div>
                    ))}
                </div>
              </div>
            </DialogContent>
          </Dialog>
        </div>

        <Card className="bg-gray-900 border-gray-800 p-4">
          <div className="flex items-center gap-2 text-sm text-gray-400">
            <Star className="w-4 h-4 text-yellow-500 fill-yellow-500" />
            <span>{watchlist.length} coins in your watchlist</span>
          </div>
        </Card>
      </div>

      {watchlist.length === 0 ? (
        <Card className="bg-gray-900 border-gray-800 p-12">
          <div className="text-center">
            <div className="w-16 h-16 bg-gray-800 rounded-full flex items-center justify-center mx-auto mb-4">
              <Star className="w-8 h-8 text-gray-600" />
            </div>
            <h3 className="mb-2">Your watchlist is empty</h3>
            <p className="text-gray-400 mb-6">Add coins to track their prices and performance</p>
            <Dialog>
              <DialogTrigger asChild>
                <Button className="bg-emerald-500 text-black hover:bg-emerald-600">
                  <Plus className="w-4 h-4 mr-2" />
                  Add Your First Coin
                </Button>
              </DialogTrigger>
              <DialogContent className="bg-gray-900 border-gray-800 text-white">
                <DialogHeader>
                  <DialogTitle>Add to Watchlist</DialogTitle>
                </DialogHeader>
                <div className="space-y-4">
                  {allCoins.map((coin) => (
                    <div
                      key={coin.symbol}
                      className="flex items-center justify-between p-3 rounded-lg hover:bg-gray-800 transition-colors"
                    >
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 bg-emerald-500/10 rounded-full flex items-center justify-center">
                          <span className="text-emerald-500 text-sm">{coin.symbol}</span>
                        </div>
                        <div>
                          <div className="text-white">{coin.name}</div>
                          <div className="text-sm text-gray-400">{coin.symbol}</div>
                        </div>
                      </div>
                      <Button
                        size="sm"
                        onClick={() => addToWatchlist(coin)}
                        className="bg-emerald-500 text-black hover:bg-emerald-600"
                      >
                        Add
                      </Button>
                    </div>
                  ))}
                </div>
              </DialogContent>
            </Dialog>
          </div>
        </Card>
      ) : (
        <div className="grid gap-4">
          {watchlist.map((coin) => (
            <Card key={coin.symbol} className="bg-gray-900 border-gray-800 p-6 hover:border-emerald-500/50 transition-colors">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-4 flex-1">
                  <div className="w-12 h-12 bg-emerald-500/10 rounded-full flex items-center justify-center">
                    <span className="text-emerald-500">{coin.symbol}</span>
                  </div>
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <h3>{coin.name}</h3>
                      <span className="text-gray-400 text-sm">{coin.symbol}</span>
                    </div>
                    <div className="text-sm text-gray-400">24h Vol: {formatVolume(coin.volume24h)}</div>
                  </div>
                </div>

                <div className="flex items-center gap-8">
                  {/* Mini Chart */}
                  <div className="hidden lg:flex items-end gap-0.5 h-12">
                    {coin.chart.map((value, index) => (
                      <div
                        key={index}
                        className={`w-2 rounded-sm ${coin.change24h >= 0 ? 'bg-emerald-500/50' : 'bg-red-500/50'}`}
                        style={{ height: `${value}%` }}
                      ></div>
                    ))}
                  </div>

                  {/* Price */}
                  <div className="text-right min-w-[120px]">
                    <div className="text-2xl text-white mb-1">{formatPrice(coin.price)}</div>
                    <div className={`flex items-center justify-end gap-1 ${coin.change24h >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
                      {coin.change24h >= 0 ? <TrendingUp className="w-4 h-4" /> : <TrendingDown className="w-4 h-4" />}
                      {Math.abs(coin.change24h).toFixed(2)}%
                    </div>
                  </div>

                  {/* Actions */}
                  <div className="flex items-center gap-2">
                    <Button
                      size="sm"
                      className="bg-emerald-500 text-black hover:bg-emerald-600"
                      onClick={() => onNavigate?.('trade')}
                    >
                      Trade
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      className="text-red-500 hover:text-red-400 hover:bg-red-500/10"
                      onClick={() => removeFromWatchlist(coin.symbol)}
                    >
                      <Trash2 className="w-4 h-4" />
                    </Button>
                  </div>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
