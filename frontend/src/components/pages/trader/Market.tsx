import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { TrendingUp, TrendingDown, ArrowUpRight, ArrowDownRight, Search, ArrowUpDown } from 'lucide-react';
import { Input } from '../../ui/input';
import { Button } from '../../ui/button';
import { Card } from '../../ui/card';
import { CoinIcon } from '../../ui/CoinIcon';
import { MarketApi, Crypto } from '../../../api/market';

interface MarketProps {
  onNavigate?: (page: string, coinId?: string) => void;
}

type SortField = 'price' | 'change24h' | 'marketCap' | 'none';
type SortDirection = 'asc' | 'desc';

// No longer filtering by popular coins - show all coins from API

// Format functions
const formatPrice = (price: number | undefined | null) => {
  const numPrice = Number(price) || 0;
  if (numPrice >= 1000) {
    return `$${numPrice.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }
  if (numPrice >= 1) {
    return `$${numPrice.toFixed(2)}`;
  }
  return `$${numPrice.toFixed(4)}`;
};

const formatMarketCap = (cap: number | undefined | null) => {
  const numCap = Number(cap) || 0;
  if (numCap >= 1e12) return `$${(numCap / 1e12).toFixed(2)}T`;
  if (numCap >= 1e9) return `$${(numCap / 1e9).toFixed(2)}B`;
  if (numCap >= 1e6) return `$${(numCap / 1e6).toFixed(2)}M`;
  return `$${numCap.toLocaleString()}`;
};

const formatVolume = (volume: number | undefined | null) => {
  const numVolume = Number(volume) || 0;
  if (numVolume >= 1e9) return `$${(numVolume / 1e9).toFixed(2)}B`;
  if (numVolume >= 1e6) return `$${(numVolume / 1e6).toFixed(2)}M`;
  return `$${numVolume.toLocaleString()}`;
};

// Component for coin row with image error handling
function CoinRow({ coin, index, onNavigate }: { coin: Crypto; index: number; onNavigate?: (page: string, coinId?: string) => void }) {
  const navigate = useNavigate();
  const change24h = Number(coin.priceChangePercentage24h) || 0;
  const isPositive = change24h >= 0;

  return (
    <tr
      className="border-t border-gray-800 hover:bg-gray-800/50 transition-colors cursor-pointer"
      onClick={() => {
        navigate(`/trade?pair=${coin.symbol}/USDT`);
      }}
    >
      <td className="p-4 text-gray-400">{index + 1}</td>
      <td className="p-4">
        <div className="flex items-center w-full">
          <div className="mr-2">
            <CoinIcon symbol={coin.symbol} image={coin.image} size="sm" />
          </div>
          <div className="flex flex-col items-start">
            <div className="text-white dark:text-gray-100 font-semibold text-sm leading-5">
              {coin.name || 'N/A'}
              <div className="block text-xs leading-4 text-gray-500 dark:text-gray-400 font-medium">
                {coin.symbol || 'N/A'}
              </div>
            </div>
          </div>
        </div>
      </td>
      <td className="p-4 text-right text-white font-medium">
        {formatPrice(coin.currentPrice)}
      </td>
      <td className="p-4 text-right">
        <div
          className={`flex items-center justify-end gap-1 font-medium ${
            isPositive ? 'text-emerald-500' : 'text-red-500'
          }`}
        >
          {isPositive ? (
            <ArrowUpRight className="w-4 h-4" />
          ) : (
            <ArrowDownRight className="w-4 h-4" />
          )}
          {Math.abs(change24h).toFixed(2)}%
        </div>
      </td>
      <td className="p-4 text-right text-gray-300 hidden md:table-cell">
        {formatMarketCap(coin.marketCap)}
      </td>
      <td className="p-4 text-right text-gray-300 hidden lg:table-cell">
        {formatVolume(coin.totalVolume)}
      </td>
    </tr>
  );
}

export default function Market({ onNavigate }: MarketProps) {
  const navigate = useNavigate();
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [coins, setCoins] = useState<Crypto[]>([]);
  const [sortField, setSortField] = useState<SortField>('none');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

  // Fetch market data
  const fetchMarketData = async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await MarketApi.getCryptocurrencies();
      if (!result.ok) {
        throw new Error(result.error || 'Failed to load market data');
      }
      
      // Normalize all coins data (handle both snake_case and camelCase from API)
      const allCoins = result.data
        .filter((coin: any) => {
          const id = coin.id || coin.Id || '';
          const symbol = coin.symbol || coin.Symbol || '';
          return id && symbol; // Filter out invalid entries
        })
        .map((coin: any, idx: number) => {
          // Debug: Log để kiểm tra field image (chỉ log coin đầu tiên)
          if (idx === 0) {
            console.log('=== DEBUG COIN IMAGE ===');
            console.log('Raw coin object:', coin);
            console.log('All coin keys:', Object.keys(coin));
            console.log('Image field values:', {
              'coin.image': coin.image,
              'coin.Image': coin.Image,
              'coin.image_url': coin.image_url,
              'coin.imageUrl': coin.imageUrl,
            });
          }
          
          // Backend serialize với camelCase, nên "Image" property từ C# sẽ thành "image" (lowercase)
          // CoinGecko API trả về "image" field, backend map vào property "Image" với [JsonPropertyName("image")]
          // Khi serialize lại từ controller, camelCase policy sẽ làm "Image" -> "image"
          // Vậy frontend nên nhận được "image" (lowercase)
          let imageUrl = coin.image || coin.Image || coin.image_url || coin.imageUrl || null;
          
          // Fallback: Nếu không có image từ API, có thể construct từ coin ID
          // CoinGecko image pattern: https://assets.coingecko.com/coins/images/{id_number}/standard/{coin_id}.png
          // Nhưng chúng ta không có id_number, nên chỉ dùng fallback icon
          
          if (idx === 0) {
            console.log('Final imageUrl:', imageUrl);
            console.log('Coin ID for fallback:', coin.id || coin.Id);
            console.log('========================');
          }
          
          return {
            id: coin.id || coin.Id || '',
            symbol: String(coin.symbol || coin.Symbol || '').toUpperCase(),
            name: coin.name || coin.Name || '',
            currentPrice: Number(coin.current_price ?? coin.currentPrice ?? coin.CurrentPrice ?? 0),
            priceChange24h: Number(coin.price_change_24h ?? coin.priceChange24h ?? coin.PriceChange24h ?? 0),
            priceChangePercentage24h: Number(coin.price_change_percentage_24h ?? coin.priceChangePercentage24h ?? coin.PriceChangePercentage24h ?? 0),
            marketCap: Number(coin.market_cap ?? coin.marketCap ?? coin.MarketCap ?? 0),
            totalVolume: Number(coin.total_volume ?? coin.totalVolume ?? coin.TotalVolume ?? 0),
            image: imageUrl,
          };
        });
      
      // Sort by market cap by default
      const sorted = [...allCoins].sort((a, b) => 
        (b.marketCap || 0) - (a.marketCap || 0)
      );
      
      setCoins(sorted);
    } catch (e: any) {
      setError(e?.message || 'Failed to load market data');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let mounted = true;
    let interval: ReturnType<typeof setInterval>;

    // Initial fetch
    fetchMarketData();

    // Set up polling every 30 seconds
    interval = setInterval(() => {
      if (mounted) {
        fetchMarketData();
      }
    }, 30000);

    // SignalR connection for real-time updates
    (async () => {
      try {
        const hubConnection = new signalR.HubConnectionBuilder()
          .withUrl('/marketHub')
          .withAutomaticReconnect()
          .configureLogging(signalR.LogLevel.Error)
          .build();

        hubConnection.on('ReceivePriceList', (list: any[]) => {
          if (!mounted) return;
          
          const mapped: Crypto[] = (list || [])
            .map((c: any) => ({
              id: c.id || c.Id || '',
              symbol: String(c.symbol || c.Symbol || '').toUpperCase(),
              name: c.name || c.Name || '',
              currentPrice: Number(c.current_price ?? c.currentPrice ?? 0),
              priceChange24h: Number(c.price_change_24h ?? c.priceChange24h ?? 0),
              priceChangePercentage24h: Number(c.price_change_percentage_24h ?? c.priceChangePercentage24h ?? 0),
              marketCap: Number(c.market_cap ?? c.marketCap ?? 0),
              totalVolume: Number(c.total_volume ?? c.totalVolume ?? 0),
              image: c.image || c.Image || c.image_url || c.imageUrl || null, // CoinGecko trả về "image" field
            }))
            .filter(coin => coin.id && coin.symbol); // Filter out invalid entries

          // Sort all coins by market cap
          const sorted = [...mapped].sort((a, b) => 
            (b.marketCap || 0) - (a.marketCap || 0)
          );
          
          setCoins(sorted);
        });

        await hubConnection.start();
        try {
          await hubConnection.invoke('JoinMarketGroup');
        } catch {}

        if (mounted) {
          setConnection(hubConnection);
        }
      } catch (err) {
        console.error('SignalR connection error:', err);
        // Continue with polling fallback
      }
    })();

    return () => {
      mounted = false;
      clearInterval(interval);
      if (connection) {
        try {
          connection.stop();
        } catch {}
      }
    };
  }, []);

  // Sorting logic
  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortDirection('desc');
    }
  };

  const sortedCoins = useMemo(() => {
    if (sortField === 'none') return coins;

    const sorted = [...coins].sort((a, b) => {
      let aValue: number, bValue: number;

      switch (sortField) {
        case 'price':
          aValue = a.currentPrice || 0;
          bValue = b.currentPrice || 0;
          break;
        case 'change24h':
          aValue = a.priceChangePercentage24h || 0;
          bValue = b.priceChangePercentage24h || 0;
          break;
        case 'marketCap':
          aValue = a.marketCap || 0;
          bValue = b.marketCap || 0;
          break;
        default:
          return 0;
      }

      return sortDirection === 'asc' 
        ? aValue - bValue 
        : bValue - aValue;
    });

    return sorted;
  }, [coins, sortField, sortDirection]);

  // Filter by search
  const filteredCoins = useMemo(() => {
    if (!searchQuery.trim()) return sortedCoins;
    
    const query = searchQuery.toLowerCase();
    return sortedCoins.filter(coin =>
      coin.name.toLowerCase().includes(query) ||
      coin.symbol.toLowerCase().includes(query)
    );
  }, [sortedCoins, searchQuery]);


  const SortButton = ({ field, label }: { field: SortField; label: string }) => (
    <button
      onClick={() => handleSort(field)}
      className="flex items-center gap-1 hover:text-emerald-500 transition-colors"
    >
      {label}
      {sortField === field && (
        <ArrowUpDown className={`w-4 h-4 ${sortDirection === 'asc' ? 'rotate-180' : ''}`} />
      )}
    </button>
  );

  return (
    <div className="p-4 lg:p-8">
      <Card className="bg-gray-900 border-gray-800 p-6">
        {/* Header */}
        <div className="mb-6">
          <h1 className="text-3xl font-bold mb-2">Thị trường</h1>
          <p className="text-gray-400">Dữ liệu tiền mã hóa theo thời gian thực - Cập nhật mỗi 30 giây</p>
        </div>

        {/* Search */}
        <div className="mb-6">
          <div className="relative max-w-md">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
            <Input
              placeholder="Search coins..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-10 bg-black border-gray-800 text-white"
            />
          </div>
        </div>

        {/* Error Message */}
        {error && (
          <div className="mb-4 p-4 bg-red-500/10 border border-red-500/50 rounded-lg text-red-400">
            {error}
          </div>
        )}

        {/* Loading State */}
        {loading && coins.length === 0 ? (
          <div className="text-center py-12 text-gray-400">
            Loading market data...
          </div>
        ) : (
          /* Market Table */
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-gray-800/50">
                <tr className="text-left text-gray-400 text-sm">
                  <th className="p-4">#</th>
                  <th className="p-4">Coin</th>
                  <th className="p-4 text-right">
                    <SortButton field="price" label="Giá (USD)" />
                  </th>
                  <th className="p-4 text-right">
                    <SortButton field="change24h" label="24h %" />
                  </th>
                  <th className="p-4 text-right hidden md:table-cell">
                    <SortButton field="marketCap" label="Vốn hóa" />
                  </th>
                  <th className="p-4 text-right hidden lg:table-cell">Khối lượng 24h</th>
                </tr>
              </thead>
              <tbody>
                {filteredCoins.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="p-8 text-center text-gray-400">
                      No coins found
                    </td>
                  </tr>
                ) : (
                  filteredCoins.map((coin, index) => (
                    <CoinRow key={coin.id} coin={coin} index={index} onNavigate={onNavigate} />
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}

        {/* Update Indicator */}
        <div className="mt-4 text-xs text-gray-500 text-center">
          Last updated: {new Date().toLocaleTimeString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })}
        </div>
      </Card>
    </div>
  );
}