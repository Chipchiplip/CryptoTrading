import React, { useCallback, useEffect, useState } from 'react';
import { Star, Plus, TrendingUp, TrendingDown, Trash2, Search, Loader2, AlertCircle } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '../../ui/dialog';
import { Badge } from '../../ui/badge';
import { Alert, AlertDescription } from '../../ui/alert';
import { PortfolioApi, WatchlistCoin, WatchlistQuota } from '../../../api/portfolio';
import { MarketApi, Crypto } from '../../../api/market';
import { useSubscriptionPlan } from '../../../hooks/useSubscriptionPlan';

interface WatchlistProps {
  onNavigate?: (page: string) => void;
}

interface WatchlistCoinWithPrice extends WatchlistCoin {
  volume24h?: number;
  chart?: number[];
}

// Component to display coin icon with fallback
function CoinIcon({ coin, size = 'sm' }: { coin: { image?: string; iconUrl?: string; symbol: string }; size?: 'sm' | 'md' }) {
  const [imageError, setImageError] = React.useState(false);
  const sizeClasses = size === 'md' ? 'w-10 h-10' : 'w-6 h-6';
  const textSizeClasses = size === 'md' ? 'text-sm' : 'text-xs';
  const imageUrl = coin.image || coin.iconUrl || null;
  
  return (
    <>
      {imageUrl && !imageError ? (
        <img
          src={imageUrl}
          alt={coin.symbol}
          className={`${sizeClasses} object-fill flex-shrink-0 rounded-full`}
          loading="lazy"
          onError={() => setImageError(true)}
        />
      ) : (
        <div className={`${sizeClasses} bg-emerald-500/10 rounded-full flex items-center justify-center flex-shrink-0`}>
          <span className={`text-emerald-500 ${textSizeClasses} font-semibold`}>
            {(coin.symbol || '').charAt(0).toUpperCase()}
          </span>
        </div>
      )}
    </>
  );
}

export default function Watchlist({ onNavigate }: WatchlistProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [watchlistId, setWatchlistId] = useState<string | null>(null);
  const [watchlistCoins, setWatchlistCoins] = useState<WatchlistCoinWithPrice[]>([]);
  const [allCryptos, setAllCryptos] = useState<Crypto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [addCoinDialogOpen, setAddCoinDialogOpen] = useState(false);
  const [quota, setQuota] = useState<WatchlistQuota | null>(null);
  const { planType, loading: planLoading } = useSubscriptionPlan();
  const isPremium = planType === 2;
  const freeLimit = 2;
  const reachedLimit = !isPremium && watchlistCoins.length >= freeLimit;

  const fetchWatchlist = useCallback(async () => {
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
      setWatchlistCoins(res.data.coins.map((c) => ({
        ...c,
        volume24h: 0,
        chart: Array.from({ length: 7 }, () => Math.random() * 50 + 40),
      })));
    } catch (e: any) {
      setError(e?.message || 'Failed to load watchlist');
    } finally {
      setLoading(false);
    }
  }, []);

  const loadQuota = useCallback(async () => {
    const res = await PortfolioApi.getWatchlistQuota();
    if (res.ok) {
      setQuota(res.data);
    }
  }, []);

  useEffect(() => {
    void fetchWatchlist();
    void loadQuota();
  }, [fetchWatchlist, loadQuota]);

  // Fetch realtime prices
  const fetchPrices = async () => {
    if (!watchlistId) return;
    try {
      const res = await MarketApi.getWatchlistRealtime(watchlistId);
      if (!res.ok) {
        console.error('Failed to fetch prices:', res.error);
        return;
      }
      
      // Update prices for coins in watchlist
      setWatchlistCoins(prev => prev.map(coin => {
        const update = res.data.updates.find(u => u.symbol.toUpperCase() === coin.symbol.toUpperCase());
        // Also try to get image from allCryptos if available
        const cryptoData = allCryptos.length > 0 ? allCryptos.find(c => c.symbol.toUpperCase() === coin.symbol.toUpperCase()) : null;
        if (update || cryptoData) {
          return {
            ...coin,
            currentPrice: update?.currentPrice ?? coin.currentPrice,
            priceChange24h: update?.priceChange24h ?? coin.priceChange24h,
            priceChangePercent24h: update?.priceChangePercentage24h ?? coin.priceChangePercent24h,
            // Add image from crypto data if available (only if not already set)
            iconUrl: cryptoData?.image || coin.iconUrl || '',
          };
        }
        return coin;
      }));
    } catch (e: any) {
      console.error('Error fetching prices:', e);
    } finally {
      // intentionally left blank
    }
  };

  // Fetch realtime prices when watchlistId changes
  useEffect(() => {
    if (!watchlistId) return;
    
    fetchPrices();
    const interval = setInterval(fetchPrices, 5000);
    
    return () => clearInterval(interval);
  }, [watchlistId]);

  // Fetch all cryptocurrencies for add coin dialog and image mapping
  useEffect(() => {
    // Fetch once on mount to have image data available for watchlist coins
    if (allCryptos.length === 0) {
      const fetchCryptos = async () => {
        const res = await MarketApi.getCryptocurrencies();
        if (res.ok) {
          // Normalize data - handle both snake_case from backend JsonPropertyName and camelCase
          const normalized = res.data.map((coin: any) => ({
            id: coin.id || coin.Id || '',
            symbol: String(coin.symbol || coin.Symbol || '').toUpperCase(),
            name: coin.name || coin.Name || '',
            currentPrice: Number(coin.current_price ?? coin.currentPrice ?? coin.CurrentPrice ?? 0),
            priceChange24h: Number(coin.price_change_24h ?? coin.priceChange24h ?? coin.PriceChange24h ?? 0),
            priceChangePercentage24h: Number(coin.price_change_percentage_24h ?? coin.priceChangePercentage24h ?? coin.PriceChangePercentage24h ?? 0),
            marketCap: Number(coin.market_cap ?? coin.marketCap ?? coin.MarketCap ?? 0),
            totalVolume: Number(coin.total_volume ?? coin.totalVolume ?? coin.TotalVolume ?? 0),
            image: coin.image || coin.Image || coin.image_url || coin.imageUrl || null,
          }));
          setAllCryptos(normalized);
          
          // Update watchlist coins with images if available
          setWatchlistCoins(prev => prev.map(coin => {
            const cryptoData = normalized.find(c => c.symbol.toUpperCase() === coin.symbol.toUpperCase());
            if (cryptoData?.image && !coin.iconUrl) {
              return { ...coin, iconUrl: cryptoData.image };
            }
            return coin;
          }));
        }
      };
      fetchCryptos();
    }
  }, []);

  const removeFromWatchlist = async (symbol: string) => {
    if (!watchlistId) return;
    const res = await PortfolioApi.removeCoinFromWatchlist(watchlistId, symbol);
    if (!res.ok) {
      setError(res.error);
      return;
    }
    setWatchlistCoins((prev) => prev.filter((coin) => coin.symbol !== symbol));
    await loadQuota();
  };

  const addToWatchlist = async (crypto: Crypto) => {
    if (reachedLimit) {
      setError('Free plan can only track up to 2 coins. Please upgrade to Premium to add more coins.');
      return;
    }
    const res = await PortfolioApi.addCoinToDefault({ coinSymbol: crypto.symbol });
    if (!res.ok) {
      setError(res.error);
      return;
    }
    await fetchWatchlist();
    await loadQuota();
    setAddCoinDialogOpen(false);
  };

  const formatPrice = (price: number | undefined | null) => {
    const numPrice = Number(price) || 0;
    if (numPrice >= 1000) return `$${numPrice.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    if (numPrice >= 1) return `$${numPrice.toFixed(2)}`;
    return `$${numPrice.toFixed(4)}`;
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
            <p className="text-sm text-gray-500 mt-1">
              {planLoading ? 'Checking plan...' : isPremium ? 'You are using Premium - unlimited watchlist.' : 'You are on Free plan - maximum 2 coins in watchlist.'}
            </p>
          </div>
          <Dialog open={addCoinDialogOpen} onOpenChange={setAddCoinDialogOpen}>
            <DialogTrigger asChild>
              <Button
                className={`bg-emerald-500 text-black hover:bg-emerald-600 ${reachedLimit ? 'opacity-60 cursor-not-allowed' : ''}`}
                disabled={reachedLimit}
              >
                <Plus className="w-4 h-4 mr-2" />
                {reachedLimit ? 'Limit Reached' : 'Add Coin'}
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
                          <CoinIcon coin={coin} size="md" />
                          <div>
                            <div className="text-white">{coin.name}</div>
                            <div className="text-sm text-gray-400">{coin.symbol.toUpperCase()}</div>
                          </div>
                        </div>
                        <div className="flex items-center gap-4">
                          <div className="text-right">
                            <div className="text-white">{formatPrice(coin.currentPrice)}</div>
                            <div className={(coin.priceChangePercentage24h || 0) >= 0 ? 'text-emerald-500 text-sm' : 'text-red-500 text-sm'}>
                              {(coin.priceChangePercentage24h || 0) >= 0 ? '+' : ''}{Math.abs(coin.priceChangePercentage24h || 0).toFixed(2)}%
                            </div>
                          </div>
                          <Button
                            size="sm"
                            onClick={() => addToWatchlist(coin)}
                            className="bg-emerald-500 text-black hover:bg-emerald-600"
                            disabled={reachedLimit}
                          >
                            {reachedLimit ? 'Locked' : 'Add'}
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

        {!isPremium && (
          <Alert className="bg-yellow-500/10 border-yellow-500/50 text-yellow-300 mb-4">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>
              Free plan only tracks a maximum of {freeLimit} coins. You are currently tracking {watchlistCoins.length}/{freeLimit}. Upgrade to Premium to unlock unlimited watchlist.
            </AlertDescription>
          </Alert>
        )}

        <Card className="bg-gray-900 border-gray-800 p-4">
          <div className="flex items-center gap-2 text-sm text-gray-400">
            <Star className="w-4 h-4 text-yellow-500 fill-yellow-500" />
            <span>{watchlistCoins.length} coins in your watchlist</span>
            {isPremium ? (
              <Badge className="bg-emerald-500/20 text-emerald-300 border-emerald-500/30">Unlimited</Badge>
            ) : (
              <Badge className="bg-yellow-500/10 text-yellow-300 border border-yellow-500/30">
                Free limit {freeLimit}
              </Badge>
            )}
          </div>
          {quota && (
            <p className="text-xs text-gray-500 mt-2">
              Quota: {quota.currentCount}/{quota.maxAllowed === 1000 ? '∞' : quota.maxAllowed} • {quota.subscriptionTier} plan
            </p>
          )}
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
                <Button
                  className={`bg-emerald-500 text-black hover:bg-emerald-600 ${reachedLimit ? 'opacity-60 cursor-not-allowed' : ''}`}
                  disabled={reachedLimit}
                >
                  <Plus className="w-4 h-4 mr-2" />
                  {reachedLimit ? 'Limit Reached' : 'Add Your First Coin'}
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
                            <CoinIcon coin={coin} size="md" />
                            <div>
                              <div className="text-white">{coin.name}</div>
                              <div className="text-sm text-gray-400">{coin.symbol.toUpperCase()}</div>
                            </div>
                          </div>
                          <Button
                            size="sm"
                            onClick={() => addToWatchlist(coin)}
                            className="bg-emerald-500 text-black hover:bg-emerald-600"
                            disabled={reachedLimit}
                          >
                            {reachedLimit ? 'Locked' : 'Add'}
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
                  <CoinIcon coin={coin} size="md" />
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
                          className={`w-2 rounded-sm ${(coin.priceChangePercent24h || 0) >= 0 ? 'bg-emerald-500/50' : 'bg-red-500/50'}`}
                          style={{ height: `${value}%` }}
                        ></div>
                      ))}
                    </div>
                  )}

                  {/* Price */}
                  <div className="text-right min-w-[120px]">
                    <div className="text-2xl text-white mb-1">{formatPrice(coin.currentPrice)}</div>
                    <div className={`flex items-center justify-end gap-1 ${(coin.priceChangePercent24h || 0) >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
                      {(coin.priceChangePercent24h || 0) >= 0 ? <TrendingUp className="w-4 h-4" /> : <TrendingDown className="w-4 h-4" />}
                      {Math.abs(coin.priceChangePercent24h || 0).toFixed(2)}%
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
