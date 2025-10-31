import React, { useState, useEffect, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { TrendingUp, TrendingDown, Info, Loader2 } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Label } from '../../ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../ui/select';
import { Alert, AlertDescription } from '../../ui/alert';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '../../ui/dialog';
import { Badge } from '../../ui/badge';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { TradingApi, OrderBook, TradingBalances } from '../../../api/trading';
import { MarketApi, Crypto, PriceHistoryItem } from '../../../api/market';

interface TradeProps {
  onNavigate?: (page: string) => void;
}

type Timeframe = '1D' | '7D' | '1M' | '3M' | '1Y';

const timeframeDays: Record<Timeframe, number> = {
  '1D': 1,
  '7D': 7,
  '1M': 30,
  '3M': 90,
  '1Y': 365,
};

export default function Trade({ onNavigate }: TradeProps) {
  const [searchParams, setSearchParams] = useSearchParams();
  const urlPair = searchParams.get('pair');
  const [selectedPair, setSelectedPair] = useState(urlPair || 'BTC/USDT');
  const [side, setSide] = useState<'buy' | 'sell'>('buy');
  const [buyAmount, setBuyAmount] = useState('');
  const [useAmount, setUseAmount] = useState('');
  const [showPreview, setShowPreview] = useState(false);
  const [loading, setLoading] = useState(true);
  const [loadingChart, setLoadingChart] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [timeframe, setTimeframe] = useState<Timeframe>('1D');
  
  const [pairs, setPairs] = useState<Crypto[]>([]);
  const [orderBook, setOrderBook] = useState<OrderBook | null>(null);
  const [balances, setBalances] = useState<TradingBalances | null>(null);
  const [priceHistory, setPriceHistory] = useState<PriceHistoryItem[]>([]);
  const [selectedCoin, setSelectedCoin] = useState<Crypto | null>(null);

  // Update selectedPair when URL param changes
  useEffect(() => {
    const urlPair = searchParams.get('pair');
    if (urlPair) {
      setSelectedPair(urlPair);
    }
  }, [searchParams]);

  // Fetch cryptocurrencies and find selected coin
  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      setError(null);
      
      try {
        const cryptosRes = await MarketApi.getCryptocurrencies();
        if (!cryptosRes.ok) {
          setError(cryptosRes.error);
          setLoading(false);
          return;
        }
        
        // Normalize data
        const normalized = cryptosRes.data.map((coin: any) => ({
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
        
        setPairs(normalized);
        
        // Find selected coin by symbol
        const baseSymbol = selectedPair.split('/')[0].toUpperCase();
        const coin = normalized.find(c => c.symbol.toUpperCase() === baseSymbol);
        if (coin) {
          setSelectedCoin(coin);
        }
        
        // Fetch balances
        const balancesRes = await TradingApi.getBalances();
        if (balancesRes.ok) {
          setBalances(balancesRes.data);
        }
        
        setLoading(false);
      } catch (e: any) {
        setError(e?.message || 'Failed to load trading data');
        setLoading(false);
      }
    };
    
    fetchData();
  }, []);

  // Update selected coin when pair changes
  useEffect(() => {
    if (pairs.length > 0) {
      const baseSymbol = selectedPair.split('/')[0].toUpperCase();
      const coin = pairs.find(c => c.symbol.toUpperCase() === baseSymbol);
      if (coin) {
        setSelectedCoin(coin);
      }
    }
  }, [selectedPair, pairs]);

  // Fetch price history when coin or timeframe changes
  useEffect(() => {
    const fetchPriceHistory = async () => {
      if (!selectedCoin?.id) return;
      
      setLoadingChart(true);
      try {
        const days = timeframeDays[timeframe];
        // Use coin.id which is the CoinGecko coin ID (e.g., "bitcoin", "ethereum")
        const coinId = selectedCoin.id.toLowerCase();
        const res = await MarketApi.getPriceHistory(coinId, days);
        if (res.ok) {
          setPriceHistory(res.data);
        } else {
          console.error('Failed to fetch price history:', res.error);
        }
      } catch (e: any) {
        console.error('Error fetching price history:', e);
      } finally {
        setLoadingChart(false);
      }
    };
    
    fetchPriceHistory();
  }, [selectedCoin?.id, timeframe]);

  // Fetch order book when pair changes
  useEffect(() => {
    const fetchOrderBook = async () => {
      if (!selectedPair) return;
      try {
        const res = await TradingApi.getOrderBook(selectedPair);
        if (res.ok) {
          setOrderBook(res.data);
        }
      } catch (e: any) {
        console.error('Error fetching order book:', e);
      }
    };
    
    fetchOrderBook();
    const interval = setInterval(fetchOrderBook, 3000);
    return () => clearInterval(interval);
  }, [selectedPair]);

  // Calculate chart data
  const chartData = useMemo(() => {
    if (!priceHistory.length) return [];
    
    return priceHistory.map((item) => ({
      time: new Date(item.timestamp).toLocaleTimeString('en-US', { 
        hour: '2-digit', 
        minute: '2-digit',
        hour12: false 
      }),
      date: new Date(item.timestamp).toLocaleDateString('en-US', { 
        month: 'short', 
        day: 'numeric' 
      }),
      price: Number(item.price),
      timestamp: item.timestamp,
    }));
  }, [priceHistory]);

  // Format price
  const formatPrice = (value: number | undefined | null) => {
    const numValue = Number(value) || 0;
    if (numValue >= 1000) return `$${numValue.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    if (numValue >= 1) return `$${numValue.toFixed(2)}`;
    return `$${numValue.toFixed(4)}`;
  };

  // Calculate conversion
  const currentPrice = selectedCoin?.currentPrice || orderBook?.currentPrice || 0;
  
  // When buyAmount changes, calculate useAmount
  useEffect(() => {
    if (buyAmount && currentPrice > 0) {
      const calculated = (parseFloat(buyAmount) * currentPrice).toFixed(2);
      setUseAmount(calculated);
    } else if (!buyAmount) {
      setUseAmount('');
    }
  }, [buyAmount, currentPrice]);

  // When useAmount changes, calculate buyAmount
  useEffect(() => {
    if (useAmount && currentPrice > 0 && !buyAmount) {
      // Only update if buyAmount is empty to avoid circular updates
    } else if (useAmount && currentPrice > 0) {
      const calculated = (parseFloat(useAmount) / currentPrice).toFixed(8);
      // Only update if user is typing in useAmount field
    }
  }, [useAmount, currentPrice]);

  const handleUseAmountChange = (value: string) => {
    setUseAmount(value);
    if (value && currentPrice > 0) {
      const calculated = (parseFloat(value) / currentPrice).toFixed(8);
      setBuyAmount(calculated);
    } else {
      setBuyAmount('');
    }
  };

  const handleBuyAmountChange = (value: string) => {
    setBuyAmount(value);
    if (value && currentPrice > 0) {
      const calculated = (parseFloat(value) * currentPrice).toFixed(2);
      setUseAmount(calculated);
    } else {
      setUseAmount('');
    }
  };

  const handleSubmit = async () => {
    if (!buyAmount || !useAmount) return;
    setShowPreview(true);
  };

  const confirmOrder = async () => {
    if (!buyAmount || !useAmount) return;
    
    setError(null);
    try {
      const res = await TradingApi.placeOrder({
        symbol: selectedPair,
        side: side === 'buy' ? 'Buy' : 'Sell',
        type: 'Market',
        quantity: parseFloat(buyAmount),
        price: undefined,
      });
      
      if (!res.ok) {
        setError(res.error);
        setShowPreview(false);
        return;
      }
      
      setShowPreview(false);
      setBuyAmount('');
      setUseAmount('');
      onNavigate?.('orders');
    } catch (e: any) {
      setError(e?.message || 'Failed to place order');
      setShowPreview(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <Loader2 className="w-8 h-8 text-emerald-500 animate-spin mx-auto mb-4" />
          <p className="text-gray-400">Loading trading data...</p>
        </div>
      </div>
    );
  }

  const priceChange = selectedCoin?.priceChangePercentage24h || orderBook?.priceChangePercentage24h || 0;
  const isPositive = priceChange >= 0;

  return (
    <div className="p-4 lg:p-8 bg-[#0d1117] min-h-screen">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-3xl font-bold mb-2 text-white">Trade</h1>
        <p className="text-gray-400">Execute market or limit orders</p>
      </div>

      {/* Error Message */}
      {error && (
        <div className="mb-4 p-4 bg-red-500/10 border border-red-500/50 rounded-lg text-red-400">
          {error}
        </div>
      )}

      <div className="grid lg:grid-cols-3 gap-6">
        {/* Left: Chart and Trading Info */}
        <div className="lg:col-span-2 space-y-6">
          {/* Coin Info and Pair Selection */}
          <Card className="bg-[#1a1d24] border-gray-800 p-6">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-4">
                {selectedCoin?.image && (
                  <img src={selectedCoin.image} alt={selectedCoin.symbol} className="w-12 h-12 rounded-full" />
                )}
                <div>
                  <div className="flex items-center gap-2">
                    <h2 className="text-2xl font-bold text-white">
                      {selectedCoin?.name || 'Bitcoin'} ({selectedCoin?.symbol || 'BTC'})
                    </h2>
                    <Badge className="bg-orange-500/10 text-orange-500">HOT</Badge>
                  </div>
                  <div className="flex items-center gap-2 mt-1">
                    <span className="text-white">
                      {selectedPair.split('/')[0]} sang {selectedPair.split('/')[1]}: 1 {selectedPair.split('/')[0]} = {formatPrice(currentPrice)} {selectedPair.split('/')[1]}
                    </span>
                    <Badge className={isPositive ? 'bg-emerald-500/10 text-emerald-500' : 'bg-red-500/10 text-red-500'}>
                      {isPositive ? <TrendingUp className="w-3 h-3 mr-1" /> : <TrendingDown className="w-3 h-3 mr-1" />}
                      {isPositive ? '+' : ''}{priceChange.toFixed(2)}%
                    </Badge>
                    <span className="text-gray-400 text-sm">1 ngày</span>
                  </div>
                </div>
              </div>
              <Select 
                value={selectedPair} 
                onValueChange={(value) => {
                  setSelectedPair(value);
                  setSearchParams({ pair: value });
                }}
              >
                <SelectTrigger className="bg-gray-800 border-gray-700 w-48">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent className="bg-gray-800 border-gray-700 text-white">
                  {pairs.slice(0, 20).map((coin) => (
                    <SelectItem key={coin.id} value={`${coin.symbol}/USDT`}>
                      {coin.symbol}/USDT
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Timeframe Selector */}
            <div className="flex gap-2 mb-4">
              {(['1D', '7D', '1M', '3M', '1Y'] as Timeframe[]).map((tf) => (
                <Button
                  key={tf}
                  type="button"
                  variant={timeframe === tf ? 'default' : 'outline'}
                  onClick={() => setTimeframe(tf)}
                  className={timeframe === tf 
                    ? 'bg-emerald-500 text-black hover:bg-emerald-600' 
                    : 'border-gray-700 text-gray-400 hover:bg-gray-800'
                  }
                  size="sm"
                >
                  {tf === '1D' ? '1 ngày' : tf === '7D' ? '7 ngày' : tf === '1M' ? '1 tháng' : tf === '3M' ? '3 Tháng' : '1Y'}
                </Button>
              ))}
            </div>

            {/* Price Chart */}
            <div className="h-[400px] w-full">
              {loadingChart ? (
                <div className="flex items-center justify-center h-full">
                  <Loader2 className="w-8 h-8 text-[#f2c94c] animate-spin" />
                </div>
              ) : chartData.length > 0 ? (
                <ResponsiveContainer width="100%" height="100%">
                  <LineChart data={chartData}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#2d3748" />
                    <XAxis 
                      dataKey="time" 
                      stroke="#9ca3af"
                      tick={{ fill: '#9ca3af', fontSize: 12 }}
                    />
                    <YAxis 
                      stroke="#9ca3af"
                      tick={{ fill: '#9ca3af', fontSize: 12 }}
                      domain={['auto', 'auto']}
                      tickFormatter={(value) => {
                        if (value >= 1000) return `$${(value / 1000).toFixed(0)}K`;
                        return `$${value.toFixed(0)}`;
                      }}
                    />
                    <Tooltip
                      contentStyle={{ 
                        backgroundColor: '#1a1d24', 
                        border: '1px solid #374151',
                        borderRadius: '8px',
                        color: '#fff'
                      }}
                      formatter={(value: any) => formatPrice(value)}
                    />
                    <Line 
                      type="monotone" 
                      dataKey="price" 
                      stroke="#f2c94c" 
                      strokeWidth={2}
                      dot={false}
                      activeDot={{ r: 4, fill: '#f2c94c' }}
                    />
                  </LineChart>
                </ResponsiveContainer>
              ) : (
                <div className="flex items-center justify-center h-full text-gray-400">
                  No chart data available
                </div>
              )}
            </div>

            <div className="mt-4 text-xs text-gray-500">
              Lần gần nhất cập nhật trang: {new Date().toLocaleString('vi-VN', { 
                year: 'numeric', 
                month: '2-digit', 
                day: '2-digit', 
                hour: '2-digit', 
                minute: '2-digit',
                timeZone: 'UTC'
              })} (UTC+0)
            </div>
          </Card>
        </div>

        {/* Right: Trading Form */}
        <div className="space-y-6">
          {/* Trading Form */}
          <Card className="bg-[#1a1d24] border-gray-800 p-6">
            <div className="flex gap-2 mb-4 border-b border-gray-800 pb-2">
              <Button
                type="button"
                variant="ghost"
                onClick={() => setSide('buy')}
                className={`flex-1 ${side === 'buy' 
                  ? 'border-b-2 border-[#f2c94c] text-[#f2c94c]' 
                  : 'text-gray-400 hover:text-gray-300'
                }`}
              >
                Mua {selectedPair.split('/')[0]}
              </Button>
              <Button
                type="button"
                variant="ghost"
                onClick={() => setSide('sell')}
                className={`flex-1 ${side === 'sell' 
                  ? 'border-b-2 border-[#f2c94c] text-[#f2c94c]' 
                  : 'text-gray-400 hover:text-gray-300'
                }`}
              >
                Giao dịch {selectedPair.split('/')[0]}
              </Button>
            </div>

            {side === 'buy' ? (
              <div className="space-y-4">
                {/* You Buy */}
                <div>
                  <Label className="text-gray-400 mb-2 block">Bạn mua</Label>
                  <div className="flex gap-2">
                    <Input
                      type="number"
                      placeholder="0"
                      value={buyAmount}
                      onChange={(e) => handleBuyAmountChange(e.target.value)}
                      className="bg-gray-800 border-gray-700 text-white flex-1"
                    />
                    <Button variant="outline" className="border-gray-700 text-gray-400 w-20">
                      {selectedPair.split('/')[0]}
                    </Button>
                  </div>
                  <p className="text-sm text-gray-500 mt-2">
                    1 {selectedPair.split('/')[0]} ≈ {selectedPair.split('/')[1]} {formatPrice(currentPrice)}
                  </p>
                </div>

                {/* You Use */}
                <div>
                  <Label className="text-gray-400 mb-2 block">Bạn sử dụng</Label>
                  <div className="flex gap-2">
                    <Input
                      type="number"
                      placeholder="10 - 50,000"
                      value={useAmount}
                      onChange={(e) => handleUseAmountChange(e.target.value)}
                      className="bg-gray-800 border-gray-700 text-white flex-1"
                    />
                    <Button variant="outline" className="border-gray-700 text-gray-400 w-20">
                      {selectedPair.split('/')[1]}
                    </Button>
                  </div>
                </div>

                {/* Buy Button */}
                <Button
                  onClick={handleSubmit}
                  disabled={!buyAmount || !useAmount}
                  className="w-full bg-[#f2c94c] text-black hover:bg-[#e5b73d] font-bold py-6 text-lg"
                >
                  Mua {selectedPair.split('/')[0]}
                </Button>
              </div>
            ) : (
              <div className="space-y-4">
                <Alert className="bg-blue-500/10 border-blue-500/50">
                  <Info className="h-4 w-4 text-blue-500" />
                  <AlertDescription className="text-blue-500">
                    Sell functionality coming soon
                  </AlertDescription>
                </Alert>
              </div>
            )}
          </Card>

          {/* Order Book Preview */}
          <Card className="bg-[#1a1d24] border-gray-800 p-6">
            <h3 className="mb-4 text-white">Order Book Preview</h3>
            <div className="space-y-3">
              <div>
                <div className="text-sm text-gray-400 mb-2">Sell Orders</div>
                {orderBook?.asks?.length ? (
                  orderBook.asks.slice(0, 5).map((ask, i) => (
                    <div key={i} className="flex justify-between text-sm py-1">
                      <span className="text-red-500">{formatPrice(ask.price)}</span>
                      <span className="text-gray-400">{ask.amount.toFixed(4)}</span>
                    </div>
                  ))
                ) : (
                  <div className="text-gray-400 text-sm py-2">No orders</div>
                )}
              </div>

              <div className="py-2 text-center border-y border-gray-800">
                {orderBook ? (
                  <>
                    <div className="text-xl text-emerald-500">{formatPrice(orderBook.currentPrice)}</div>
                    <div className="text-xs text-gray-400 mt-1">
                      {orderBook.priceChangePercentage24h >= 0 ? '+' : ''}
                      {orderBook.priceChangePercentage24h.toFixed(2)}%
                    </div>
                  </>
                ) : (
                  <Loader2 className="w-5 h-5 text-emerald-500 animate-spin mx-auto" />
                )}
              </div>

              <div>
                <div className="text-sm text-gray-400 mb-2">Buy Orders</div>
                {orderBook?.bids?.length ? (
                  orderBook.bids.slice(0, 5).map((bid, i) => (
                    <div key={i} className="flex justify-between text-sm py-1">
                      <span className="text-emerald-500">{formatPrice(bid.price)}</span>
                      <span className="text-gray-400">{bid.amount.toFixed(4)}</span>
                    </div>
                  ))
                ) : (
                  <div className="text-gray-400 text-sm py-2">No orders</div>
                )}
              </div>
            </div>
          </Card>

          {/* Account Balance */}
          <Card className="bg-[#1a1d24] border-gray-800 p-6">
            <h3 className="mb-4 text-white">Account Balance</h3>
            {balances ? (
              <div className="space-y-3">
                <div className="flex justify-between">
                  <span className="text-gray-400">Total Balance</span>
                  <span className="text-emerald-500">${(balances?.totalBalance || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Available</span>
                  <span className="text-emerald-500">${(balances?.availableBalance || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Locked</span>
                  <span className="text-orange-500">${(balances?.lockedBalance || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
              </div>
            ) : (
              <div className="text-center py-4">
                <Loader2 className="w-5 h-5 text-emerald-500 animate-spin mx-auto" />
              </div>
            )}
          </Card>
        </div>
      </div>

      {/* Order Preview Dialog */}
      <Dialog open={showPreview} onOpenChange={setShowPreview}>
        <DialogContent className="bg-[#1a1d24] border-gray-800 text-white">
          <DialogHeader>
            <DialogTitle>Confirm Order</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-3 p-4 bg-gray-800 rounded-lg">
              <div className="flex justify-between">
                <span className="text-gray-400">Pair</span>
                <span className="text-white">{selectedPair}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-400">Side</span>
                <Badge className="bg-emerald-500/10 text-emerald-500">Buy</Badge>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-400">Amount</span>
                <span className="text-white">{buyAmount} {selectedPair.split('/')[0]}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-400">Price</span>
                <span className="text-white">{formatPrice(currentPrice)}</span>
              </div>
              <div className="flex justify-between border-t border-gray-700 pt-3">
                <span className="text-white">Total</span>
                <span className="text-white text-lg">{useAmount} {selectedPair.split('/')[1]}</span>
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowPreview(false)} className="border-gray-700">
              Cancel
            </Button>
            <Button
              onClick={confirmOrder}
              className="bg-[#f2c94c] text-black hover:bg-[#e5b73d]"
            >
              Confirm Buy
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
