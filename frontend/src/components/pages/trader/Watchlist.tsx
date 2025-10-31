import { useState, useEffect } from 'react';
import { Star, Plus, TrendingUp, TrendingDown, Trash2, Search, Loader2, AlertCircle } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '../../ui/dialog';
import { Badge } from '../../ui/badge';
import { Alert, AlertDescription } from '../../ui/alert';
import { PortfolioApi, WatchlistCoin } from '../../../api/portfolio';
import { MarketApi, Crypto } from '../../../api/market';

interface WatchlistProps {
  onNavigate?: (page: string) => void;
}

interface WatchlistCoinWithPrice extends WatchlistCoin {
  volume24h?: number;
  chart?: number[];
}

export default function Watchlist({ onNavigate }: WatchlistProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [watchlistId, setWatchlistId] = useState<string | null>(null);
  const [watchlistCoins, setWatchlistCoins] = useState<WatchlistCoinWithPrice[]>([]);
  const [allCryptos, setAllCryptos] = useState<Crypto[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingPrices, setLoadingPrices] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [addCoinDialogOpen, setAddCoinDialogOpen] = useState(false);

  // Fetch default watchlist
  useEffect(() => {
    const fetchWatchlist = async () => {
      setLoading(true);
      setError(null);
      try {
        const res = await PortfolioApi.getDefaultWatchlist();
        if (!res.ok) {
          setError(res.error);
          setLoading(false);
          return;
        }
        setWatchlistId(res.data.id);
        setWatchlistCoins(res.data.coins.map(c => ({
          ...c,
          volume24h: 0, // Will be filled from market data
          chart: Array.from({ length: 7 }, () => Math.random() * 50 + 40) // Mock chart data
        })));
        setLoading(false);
      } catch (e: any) {
        setError(e?.message || 'Failed to load watchlist');
        setLoading(false);
      }
    };
    
    fetchWatchlist();
  }, []);

  // Fetch realtime prices
  const fetchPrices = async () => {
    if (!watchlistId) return;
    setLoadingPrices(true);
    try {
      const res = await MarketApi.getWatchlistRealtime(watchlistId);
      if (!res.ok) {
        console.error('Failed to fetch prices:', res.error);
        setLoadingPrices(false);
        return;
      }
      
      // Update prices for coins in watchlist
      setWatchlistCoins(prev => prev.map(coin => {
        const update = res.data.updates.find(u => u.symbol.toUpperCase() === coin.symbol.toUpperCase());
        if (update) {
          return {
            ...coin,
            currentPrice: update.currentPrice,
            priceChange24h: update.priceChange24h,
            priceChangePercent24h: update.priceChangePercentage24h,
          };
        }
        return coin;
      }));
    } catch (e: any) {
      console.error('Error fetching prices:', e);
    } finally {
      setLoadingPrices(false);
    }
  };

  // Fetch realtime prices when watchlistId changes
  useEffect(() => {
    if (!watchlistId) return;
    
    fetchPrices();
    const interval = setInterval(fetchPrices, 5000);
    
    return () => clearInterval(interval);
  }, [watchlistId]);

  // Fetch all cryptocurrencies for add coin dialog
  useEffect(() => {
    if (addCoinDialogOpen && allCryptos.length === 0) {
      const fetchCryptos = async () => {
        const res = await MarketApi.getCryptocurrencies();
        if (res.ok) {
          setAllCryptos(res.data);
        }
      };
      fetchCryptos();
    }
  }, [addCoinDialogOpen]);

  const removeFromWatchlist = async (symbol: string) => {
    if (!watchlistId) return;
    const res = await PortfolioApi.removeCoinFromWatchlist(watchlistId, symbol);
    if (!res.ok) {
      setError(res.error);
      return;
    }
        setWatchlistCoins(prev => prev.filter(coin => coin.symbol !== symbol));
  };

  const addToWatchlist = async (crypto: Crypto) => {
    const res = await PortfolioApi.addCoinToDefault({ coinSymbol: crypto.symbol });
    if (!res.ok) {
      setError(res.error);
      return;
    }
    // Reload watchlist
    const watchlistRes = await PortfolioApi.getDefaultWatchlist();
    if (watchlistRes.ok) {
      setWatchlistId(watchlistRes.data.id);
      setWatchlistCoins(watchlistRes.data.coins.map(c => ({
        ...c,
        volume24h: 0,
        chart: Array.from({ length: 7 }, () => Math.random() * 50 + 40)
      })));
      setAddCoinDialogOpen(false);
    }
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
          <Dialog open={addCoinDialogOpen} onOpenChange={setAddCoinDialogOpen}>
            <DialogTrigger asChild>
              <Button className="bg-emerald-500 text-black hover:bg-emerald-600">
                <Plus className="w-4 h-4 mr-2" />
                Add Coin
              </Button>
            </DialogTrigger>
            <DialogContent className="bg-gray-900 border-gray-800 text-white max-w-2xl max-h-[80vh] overflow-y-auto">
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
                  {allCryptos
                    .filter(coin => 
                      !watchlistCoins.find(w => w.symbol.toUpperCase() === coin.symbol.toUpperCase()) &&
                      (coin.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
                       coin.symbol.toLowerCase().includes(searchQuery.toLowerCase()))
                    )
                    .slice(0, 50) // Limit to first 50 results
                    .map((coin) => (
                      <div
                        key={coin.id}
                        className="flex items-center justify-between p-3 rounded-lg hover:bg-gray-800 transition-colors"
                      >
                        <div className="flex items-center gap-3">
                          <div className="w-10 h-10 bg-emerald-500/10 rounded-full flex items-center justify-center">
                            <span className="text-emerald-500 text-sm">{coin.symbol.toUpperCase()}</span>
                          </div>
                          <div>
                            <div className="text-white">{coin.name}</div>
                            <div className="text-sm text-gray-400">{coin.symbol.toUpperCase()}</div>
                          </div>
                        </div>
                        <div className="flex items-center gap-4">
                          <div className="text-right">
                            <div className="text-white">{formatPrice(coin.currentPrice)}</div>
                            <div className={coin.priceChangePercentage24h >= 0 ? 'text-emerald-500 text-sm' : 'text-red-500 text-sm'}>
                              {coin.priceChangePercentage24h >= 0 ? '+' : ''}{coin.priceChangePercentage24h.toFixed(2)}%
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

        {error && (
          <Alert className="bg-red-500/10 border-red-500/50 text-red-500 mb-4">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        <Card className="bg-gray-900 border-gray-800 p-4">
          <div className="flex items-center gap-2 text-sm text-gray-400">
            <Star className="w-4 h-4 text-yellow-500 fill-yellow-500" />
            <span>{watchlistCoins.length} coins in your watchlist</span>
          </div>
        </Card>
      </div>

      {loading ? (
        <div className="flex items-center justify-center min-h-[400px]">
          <div className="text-center">
            <Loader2 className="w-8 h-8 text-emerald-500 animate-spin mx-auto mb-4" />
            <p className="text-gray-400">Loading watchlist...</p>
          </div>
        </div>
      ) : watchlistCoins.length === 0 ? (
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
              <DialogContent className="bg-gray-900 border-gray-800 text-white max-w-2xl max-h-[80vh] overflow-y-auto">
                <DialogHeader>
                  <DialogTitle>Add to Watchlist</DialogTitle>
                </DialogHeader>
                <div className="space-y-4">
                  {allCryptos.length === 0 ? (
                    <div className="text-center py-8">
                      <Loader2 className="w-6 h-6 text-emerald-500 animate-spin mx-auto mb-2" />
                      <p className="text-gray-400">Loading cryptocurrencies...</p>
                    </div>
                  ) : (
                    <div className="space-y-2">
                      {allCryptos.slice(0, 50).map((coin) => (
                        <div
                          key={coin.id}
                          className="flex items-center justify-between p-3 rounded-lg hover:bg-gray-800 transition-colors"
                        >
                          <div className="flex items-center gap-3">
                            <div className="w-10 h-10 bg-emerald-500/10 rounded-full flex items-center justify-center">
                              <span className="text-emerald-500 text-sm">{coin.symbol.toUpperCase()}</span>
                            </div>
                            <div>
                              <div className="text-white">{coin.name}</div>
                              <div className="text-sm text-gray-400">{coin.symbol.toUpperCase()}</div>
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
                  )}
                </div>
              </DialogContent>
            </Dialog>
          </div>
        </Card>
      ) : (
        <div className="grid gap-4">
          {watchlistCoins.map((coin) => (
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
                    <div className="text-sm text-gray-400">24h Vol: {coin.volume24h ? formatVolume(coin.volume24h) : 'N/A'}</div>
                  </div>
                </div>

                <div className="flex items-center gap-8">
                  {/* Mini Chart */}
                  {coin.chart && (
                    <div className="hidden lg:flex items-end gap-0.5 h-12">
                      {coin.chart.map((value, index) => (
                        <div
                          key={index}
                          className={`w-2 rounded-sm ${coin.priceChangePercent24h >= 0 ? 'bg-emerald-500/50' : 'bg-red-500/50'}`}
                          style={{ height: `${value}%` }}
                        ></div>
                      ))}
                    </div>
                  )}

                  {/* Price */}
                  <div className="text-right min-w-[120px]">
                    <div className="text-2xl text-white mb-1">{formatPrice(coin.currentPrice)}</div>
                    <div className={`flex items-center justify-end gap-1 ${coin.priceChangePercent24h >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
                      {coin.priceChangePercent24h >= 0 ? <TrendingUp className="w-4 h-4" /> : <TrendingDown className="w-4 h-4" />}
                      {Math.abs(coin.priceChangePercent24h).toFixed(2)}%
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
                      onClick={() => removeFromWatchlist(coin.symbol.toUpperCase())}
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
