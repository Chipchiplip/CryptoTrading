import { useEffect, useMemo, useState } from 'react';
import * as signalR from '@microsoft/signalr';
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
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [stats, setStats] = useState<{
    marketCap?: number;
    volume?: number;
    btcDominance?: number;
    active?: number;
  }>({});
  const [rows, setRows] = useState<any[]>([]);
  const [binance, setBinance] = useState<Record<string, { price: number; change24h: number; volume24h?: number }>>({});

  useEffect(() => {
    let isMounted = true;
    const controller = new AbortController();
    let connection: signalR.HubConnection | null = null;
    const fetchAll = async () => {
      setLoading(true);
      setError(null);
      try {
        const [statsRes, listRes] = await Promise.all([
          fetch('/api/market/stats', { signal: controller.signal }),
          fetch('/api/market/cryptocurrencies', { signal: controller.signal }),
        ]);
        const statsJson = await statsRes.json().catch(() => ({}));
        const listJson = await listRes.json().catch(() => []);
        if (!statsRes.ok) throw new Error((statsJson as any)?.message || `HTTP ${statsRes.status}`);
        if (!listRes.ok) throw new Error((listJson as any)?.message || `HTTP ${listRes.status}`);

        if (isMounted) {
          setStats({
            marketCap: statsJson?.total_market_cap ?? statsJson?.totalMarketCap,
            volume: statsJson?.total_volume ?? statsJson?.totalVolume,
            btcDominance: statsJson?.btc_dominance ?? statsJson?.btcDominance,
            active: statsJson?.active_cryptocurrencies ?? statsJson?.activeCryptocurrencies,
          });

          const mapped = (listJson as any[]).map((c, idx) => ({
            id: c.id,
            rank: idx + 1,
            symbol: String(c.symbol || '').toUpperCase(),
            name: c.name,
            price: Number(c.current_price ?? c.currentPrice ?? 0),
            change24h: Number(c.price_change_percentage_24h ?? c.priceChangePercentage24h ?? 0),
            volume24h: Number(c.total_volume ?? c.totalVolume ?? 0),
            marketCap: Number(c.market_cap ?? c.marketCap ?? 0),
            chart: [45, 50, 48, 55, 51, 58, 50], // placeholder small sparkline
          }));
          setRows(mapped);
        }
      } catch (e: any) {
        if (isMounted) setError(e?.message || 'Failed to load market data');
      } finally {
        if (isMounted) setLoading(false);
      }
    };
    fetchAll();
    const interval = setInterval(fetchAll, 2_000);

    // SignalR realtime updates
    (async () => {
      try {
        connection = new signalR.HubConnectionBuilder()
          .withUrl('/marketHub')
          .withAutomaticReconnect()
          .configureLogging(signalR.LogLevel.Error)
          .build();

        connection.on('ReceiveMarketStats', (s: any) => {
          if (!isMounted) return;
          setStats({
            marketCap: s?.total_market_cap ?? s?.totalMarketCap,
            volume: s?.total_volume ?? s?.totalVolume,
            btcDominance: s?.btc_dominance ?? s?.btcDominance,
            active: s?.active_cryptocurrencies ?? s?.activeCryptocurrencies,
          });
        });

        connection.on('ReceivePriceList', (list: any[]) => {
          if (!isMounted) return;
          const mapped = (list || []).map((c: any, idx: number) => ({
            id: c.id,
            rank: idx + 1,
            symbol: String(c.symbol || '').toUpperCase(),
            name: c.name,
            price: Number(c.current_price ?? c.currentPrice ?? 0),
            change24h: Number(c.price_change_percentage_24h ?? c.priceChangePercentage24h ?? 0),
            volume24h: Number(c.total_volume ?? c.totalVolume ?? 0),
            marketCap: Number(c.market_cap ?? c.marketCap ?? 0),
            chart: [45, 50, 48, 55, 51, 58, 50],
          }));
          setRows(mapped);
        });

        await connection.start();
        // optional group join
        try { await connection.invoke('JoinMarketGroup'); } catch {}
      } catch {
        // ignore, fallback to polling
      }
    })();

    // Binance realtime (client-side) for top symbols
    let binanceSocket: WebSocket | null = null;
    const startBinance = (symbols: string[]) => {
      if (!symbols.length) return;
      const streams = symbols.map((s) => `${s.toLowerCase()}usdt@ticker`).join('/');
      const url = `wss://stream.binance.com:9443/stream?streams=${streams}`;
      try {
        if (binanceSocket) {
          try { binanceSocket.close(); } catch {}
        }
        binanceSocket = new WebSocket(url);
        binanceSocket.onmessage = (ev) => {
          try {
            const msg = JSON.parse(ev.data);
            const d = msg?.data;
            if (!d || !d.s || !d.c) return;
            const sym = String(d.s).replace('USDT', '').toUpperCase();
            const price = Number(d.c);
            const changePct = Number(d.P);
            setBinance((prev) => ({ ...prev, [sym]: { price, change24h: changePct } }));
          } catch {}
        };
      } catch {}
    };

    // start/refresh binance subscription when rows update (top 12)
    const refreshBinance = () => {
      const topSymbols = rows.slice(0, 12).map((r) => String(r.symbol || '').toUpperCase());
      if (topSymbols.length) startBinance(Array.from(new Set(topSymbols)));
    };
    const binanceStartTimer = setInterval(refreshBinance, 2000);
    return () => {
      isMounted = false;
      controller.abort();
      clearInterval(interval);
      if (connection) {
        try { connection.stop(); } catch {}
      }
      if (binanceSocket) {
        try { binanceSocket.close(); } catch {}
      }
    };
  }, []);

  const marketData = useMemo(() => {
    if (!rows.length || Object.keys(binance).length === 0) return rows;
    return rows.map((r) => {
      const b = binance[r.symbol];
      if (!b) return r;
      return {
        ...r,
        price: b.price || r.price,
        change24h: Number.isFinite(b.change24h) ? b.change24h : r.change24h,
      };
    });
  }, [rows, binance]);

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
            <div className="text-2xl text-white">{stats.marketCap ? `$${(stats.marketCap/1e12).toFixed(2)}T` : '—'}</div>
            <div className="text-emerald-500 text-sm mt-1">{stats.marketCap ? '+24h' : ''}</div>
          </div>
          <div className="bg-gray-900 border border-gray-800 rounded-lg p-4">
            <div className="text-gray-400 text-sm mb-1">24h Volume</div>
            <div className="text-2xl text-white">{stats.volume ? `$${(stats.volume/1e9).toFixed(1)}B` : '—'}</div>
            <div className="text-emerald-500 text-sm mt-1">+5.2%</div>
          </div>
          <div className="bg-gray-900 border border-gray-800 rounded-lg p-4">
            <div className="text-gray-400 text-sm mb-1">BTC Dominance</div>
            <div className="text-2xl text-white">{stats.btcDominance ? `${stats.btcDominance.toFixed(1)}%` : '—'}</div>
            <div className="text-gray-400 text-sm mt-1">24h</div>
          </div>
          <div className="bg-gray-900 border border-gray-800 rounded-lg p-4">
            <div className="text-gray-400 text-sm mb-1">Active Markets</div>
            <div className="text-2xl text-white">{stats.active ? `${stats.active}+` : '—'}</div>
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
            {error && (
              <div className="mb-4 text-red-400">{error}</div>
            )}
            {loading && rows.length === 0 ? (
              <div className="text-gray-400">Loading market data...</div>
            ) : null}
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
