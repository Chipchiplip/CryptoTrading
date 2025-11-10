import { useEffect, useMemo, useState } from 'react';
import { ArrowLeft, TrendingUp, TrendingDown, Star, ExternalLink, Info } from 'lucide-react';
import { Button } from '../../ui/button';
import { Card } from '../../ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../ui/tabs';
import { Badge } from '../../ui/badge';
import { LineChart, Line, AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

interface CoinDetailProps {
  coinId?: string;
  onBack?: () => void;
  onNavigate?: (page: string) => void;
}

export default function CoinDetail({ coinId = 'btc', onBack, onNavigate }: CoinDetailProps) {
  const [timeframe, setTimeframe] = useState('1D');
  const [isFavorite, setIsFavorite] = useState(false);
  const [coinData, setCoinData] = useState<any | null>(null);
  const [history, setHistory] = useState<Array<{ time: string; price: number }>>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;
    const controller = new AbortController();
    const load = async () => {
      try {
        setError(null);
        // details
        const [detailRes, histRes] = await Promise.all([
          fetch(`/api/market/cryptocurrencies/${coinId}`, { signal: controller.signal }),
          fetch(`/api/market/cryptocurrencies/${coinId}/history?days=${timeframe === '1D' ? 1 : timeframe === '7D' ? 7 : 30}`,
            { signal: controller.signal }),
        ]);
        const detail = await detailRes.json().catch(() => ({}));
        const hist = await histRes.json().catch(() => []);
        if (!detailRes.ok || !histRes.ok) throw new Error('Failed to load');

        if (!mounted) return;
        setCoinData({
          id: detail.id,
          symbol: String(detail.symbol || '').toUpperCase(),
          name: detail.name,
          price: Number(detail.current_price ?? detail.currentPrice ?? 0),
          change24h: Number(detail.price_change_percentage_24h ?? detail.priceChangePercentage24h ?? 0),
          high24h: Number(detail.high_24h ?? detail.high24h ?? detail.current_price ?? 0),
          low24h: Number(detail.low_24h ?? detail.low24h ?? detail.current_price ?? 0),
          volume24h: Number(detail.total_volume ?? detail.totalVolume ?? 0),
          marketCap: Number(detail.market_cap ?? detail.marketCap ?? 0),
          circulatingSupply: Number(detail.circulating_supply ?? detail.circulatingSupply ?? 0),
          maxSupply: Number(detail.max_supply ?? detail.maxSupply ?? 0),
          rank: Number(detail.market_cap_rank ?? detail.rank ?? 0),
          description: detail.description || '—',
        });

        const mapped: Array<{ time: string; price: number }> = (hist as any[]).map((h) => ({
          time: new Date(h.timestamp || h.Time || h.time || Date.now()).toLocaleTimeString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }),
          price: Number(h.price ?? h.Price ?? 0),
        }));
        setHistory(mapped);
      } catch (e: any) {
        if (mounted) setError(e?.message || 'Failed to load');
      }
    };
    load();
    const interval = setInterval(load, 5_000);
    return () => { mounted = false; controller.abort(); clearInterval(interval); };
  }, [coinId, timeframe]);

  const priceData = useMemo(() => ({
    '1D': history,
    '7D': history,
    '1M': history,
  }), [history]);

  const orderBook = {
    bids: [
      { price: 50230.00, amount: 0.5234, total: 26278.34 },
      { price: 50229.50, amount: 1.2341, total: 61998.35 },
      { price: 50229.00, amount: 0.8923, total: 44809.45 },
      { price: 50228.50, amount: 2.1234, total: 106640.87 },
      { price: 50228.00, amount: 0.6789, total: 34099.77 },
    ],
    asks: [
      { price: 50235.00, amount: 0.4123, total: 20711.91 },
      { price: 50235.50, amount: 1.5234, total: 76523.77 },
      { price: 50236.00, amount: 0.7891, total: 39651.28 },
      { price: 50236.50, amount: 1.8923, total: 95082.35 },
      { price: 50237.00, amount: 0.5678, total: 28524.57 },
    ]
  };

  const recentTrades = [
    { price: 50234.56, amount: 0.0234, time: '14:23:45', type: 'buy' },
    { price: 50233.12, amount: 0.1523, time: '14:23:42', type: 'sell' },
    { price: 50235.89, amount: 0.0891, time: '14:23:38', type: 'buy' },
    { price: 50232.45, amount: 0.2341, time: '14:23:35', type: 'sell' },
    { price: 50236.23, amount: 0.0567, time: '14:23:31', type: 'buy' },
  ];

  const formatPrice = (price: number) => {
    return `$${price.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  };

  const formatVolume = (volume: number) => {
    if (volume >= 1e9) return `$${(volume / 1e9).toFixed(2)}B`;
    if (volume >= 1e6) return `$${(volume / 1e6).toFixed(2)}M`;
    return `$${volume.toLocaleString()}`;
  };

  return (
    <div className="min-h-screen bg-black text-white">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Back Button */}
        <Button
          variant="ghost"
          onClick={onBack}
          className="mb-6 text-gray-400 hover:text-white"
        >
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Markets
        </Button>

        {/* Coin Header */}
        <div className="mb-8">
          <div className="flex items-start justify-between mb-6">
            <div className="flex items-center gap-4">
              <div className="w-16 h-16 bg-emerald-500/10 rounded-full flex items-center justify-center">
                <span className="text-emerald-500 text-2xl">{coinData.symbol}</span>
              </div>
              <div>
                <div className="flex items-center gap-3">
                  <h1 className="text-4xl">{coinData.name}</h1>
                  <Badge className="bg-gray-800 text-gray-300">#{coinData.rank}</Badge>
                </div>
                <div className="text-gray-400 mt-1">{coinData.symbol}/USD</div>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setIsFavorite(!isFavorite)}
                className={isFavorite ? 'border-yellow-500' : ''}
              >
                <Star className={`w-4 h-4 ${isFavorite ? 'fill-yellow-500 text-yellow-500' : ''}`} />
              </Button>
              <Button
                size="sm"
                className="bg-emerald-500 text-black hover:bg-emerald-600"
                onClick={() => onNavigate?.('login')}
              >
                Start Trading
              </Button>
            </div>
          </div>

          {/* Price Info */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div>
              <div className="text-gray-400 text-sm mb-1">Price</div>
              <div className="text-3xl text-white mb-1">{formatPrice(coinData.price)}</div>
              <div className={`flex items-center gap-1 ${coinData.change24h >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
                {coinData.change24h >= 0 ? <TrendingUp className="w-4 h-4" /> : <TrendingDown className="w-4 h-4" />}
                {Math.abs(coinData.change24h).toFixed(2)}%
              </div>
            </div>
            <div>
              <div className="text-gray-400 text-sm mb-1">24h High</div>
              <div className="text-2xl text-white">{formatPrice(coinData.high24h)}</div>
            </div>
            <div>
              <div className="text-gray-400 text-sm mb-1">24h Low</div>
              <div className="text-2xl text-white">{formatPrice(coinData.low24h)}</div>
            </div>
            <div>
              <div className="text-gray-400 text-sm mb-1">24h Volume</div>
              <div className="text-2xl text-white">{formatVolume(coinData.volume24h)}</div>
            </div>
          </div>
        </div>

        <div className="grid lg:grid-cols-3 gap-6">
          {/* Left Column - Chart */}
          <div className="lg:col-span-2 space-y-6">
            {/* Price Chart */}
            <Card className="bg-gray-900 border-gray-800 p-6">
              <div className="flex items-center justify-between mb-6">
                <h2 className="text-xl">Price Chart</h2>
                <div className="flex gap-2">
                  {['1D', '7D', '1M', '3M', '1Y', 'ALL'].map((tf) => (
                    <button
                      key={tf}
                      onClick={() => setTimeframe(tf)}
                      className={`px-3 py-1 rounded text-sm transition-colors ${
                        timeframe === tf
                          ? 'bg-emerald-500 text-black'
                          : 'bg-gray-800 text-gray-400 hover:text-white'
                      }`}
                    >
                      {tf}
                    </button>
                  ))}
                </div>
              </div>
              <ResponsiveContainer width="100%" height={300}>
                <AreaChart data={priceData[timeframe as keyof typeof priceData] || priceData['1D']}>
                  <defs>
                    <linearGradient id="priceGradient" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="#22c55e" stopOpacity={0.3}/>
                      <stop offset="95%" stopColor="#22c55e" stopOpacity={0}/>
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
                  <XAxis dataKey="time" stroke="#9ca3af" />
                  <YAxis stroke="#9ca3af" />
                  <Tooltip 
                    contentStyle={{ backgroundColor: '#1f2937', border: '1px solid #374151', borderRadius: '8px' }}
                    labelStyle={{ color: '#9ca3af' }}
                  />
                  <Area type="monotone" dataKey="price" stroke="#22c55e" fillOpacity={1} fill="url(#priceGradient)" />
                </AreaChart>
              </ResponsiveContainer>
            </Card>

            {/* Market Stats */}
            <Card className="bg-gray-900 border-gray-800 p-6">
              <h2 className="text-xl mb-4">Market Statistics</h2>
              <div className="grid md:grid-cols-2 gap-4">
                <div className="flex justify-between py-3 border-b border-gray-800">
                  <span className="text-gray-400">Market Cap</span>
                  <span className="text-white">{formatVolume(coinData.marketCap)}</span>
                </div>
                <div className="flex justify-between py-3 border-b border-gray-800">
                  <span className="text-gray-400">24h Volume</span>
                  <span className="text-white">{formatVolume(coinData.volume24h)}</span>
                </div>
                <div className="flex justify-between py-3 border-b border-gray-800">
                  <span className="text-gray-400">Circulating Supply</span>
                  <span className="text-white">{coinData.circulatingSupply.toLocaleString()} {coinData.symbol}</span>
                </div>
                <div className="flex justify-between py-3 border-b border-gray-800">
                  <span className="text-gray-400">Max Supply</span>
                  <span className="text-white">{coinData.maxSupply.toLocaleString()} {coinData.symbol}</span>
                </div>
              </div>
            </Card>

            {/* About */}
            <Card className="bg-gray-900 border-gray-800 p-6">
              <div className="flex items-center gap-2 mb-4">
                <Info className="w-5 h-5 text-emerald-500" />
                <h2 className="text-xl">About {coinData.name}</h2>
              </div>
              <p className="text-gray-300 leading-relaxed">{coinData.description}</p>
              <Button variant="link" className="mt-4 text-emerald-500 p-0">
                Learn more <ExternalLink className="w-4 h-4 ml-1" />
              </Button>
            </Card>
          </div>

          {/* Right Column - Order Book & Trades */}
          <div className="space-y-6">
            {/* Order Book */}
            <Card className="bg-gray-900 border-gray-800 p-6">
              <h2 className="text-xl mb-4">Order Book</h2>
              <div className="space-y-4">
                {/* Asks */}
                <div>
                  <div className="text-sm text-gray-400 mb-2">Sell Orders</div>
                  <div className="space-y-1">
                    {orderBook.asks.slice().reverse().map((ask, index) => (
                      <div key={index} className="flex justify-between text-sm py-1 relative">
                        <div className="absolute inset-0 bg-red-500/5" style={{ width: `${(ask.amount / 2) * 100}%` }}></div>
                        <span className="text-red-500 relative z-10">{formatPrice(ask.price)}</span>
                        <span className="text-gray-400 relative z-10">{ask.amount.toFixed(4)}</span>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Current Price */}
                <div className="py-3 text-center border-y border-gray-800">
                  <div className="text-2xl text-emerald-500">{formatPrice(coinData.price)}</div>
                  <div className="text-xs text-gray-400 mt-1">Current Price</div>
                </div>

                {/* Bids */}
                <div>
                  <div className="text-sm text-gray-400 mb-2">Buy Orders</div>
                  <div className="space-y-1">
                    {orderBook.bids.map((bid, index) => (
                      <div key={index} className="flex justify-between text-sm py-1 relative">
                        <div className="absolute inset-0 bg-emerald-500/5" style={{ width: `${(bid.amount / 2) * 100}%` }}></div>
                        <span className="text-emerald-500 relative z-10">{formatPrice(bid.price)}</span>
                        <span className="text-gray-400 relative z-10">{bid.amount.toFixed(4)}</span>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </Card>

            {/* Recent Trades */}
            <Card className="bg-gray-900 border-gray-800 p-6">
              <h2 className="text-xl mb-4">Recent Trades</h2>
              <div className="space-y-2">
                <div className="flex justify-between text-sm text-gray-400 pb-2 border-b border-gray-800">
                  <span>Price</span>
                  <span>Amount</span>
                  <span>Time</span>
                </div>
                {recentTrades.map((trade, index) => (
                  <div key={index} className="flex justify-between text-sm py-1">
                    <span className={trade.type === 'buy' ? 'text-emerald-500' : 'text-red-500'}>
                      {formatPrice(trade.price)}
                    </span>
                    <span className="text-gray-400">{trade.amount.toFixed(4)}</span>
                    <span className="text-gray-400">{trade.time}</span>
                  </div>
                ))}
              </div>
            </Card>

            {/* Trading CTA */}
            <Card className="bg-gradient-to-br from-emerald-500/10 to-transparent border-emerald-500/20 p-6">
              <h3 className="mb-2">Ready to Trade?</h3>
              <p className="text-gray-400 text-sm mb-4">
                Sign up now to start trading {coinData.name} and 350+ other cryptocurrencies
              </p>
              <Button
                className="w-full bg-emerald-500 text-black hover:bg-emerald-600"
                onClick={() => onNavigate?.('register')}
              >
                Create Free Account
              </Button>
              <Button
                variant="link"
                className="w-full mt-2 text-emerald-500"
                onClick={() => onNavigate?.('login')}
              >
                Already have an account? Login
              </Button>
            </Card>
          </div>
        </div>
      </div>
    </div>
  );
}
