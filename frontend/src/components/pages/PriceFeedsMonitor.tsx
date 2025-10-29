import { RefreshCw, CheckCircle, XCircle, Clock, AlertTriangle } from 'lucide-react';
import { Card } from '../ui/card';
import { Button } from '../ui/button';
import { Badge } from '../ui/badge';
import { Progress } from '../ui/progress';

interface PriceFeed {
  id: string;
  source: string;
  status: 'active' | 'error' | 'warning' | 'syncing';
  lastSync: string;
  nextSync: string;
  syncInterval: string;
  successRate: number;
  coinsTracked: number;
  avgLatency: string;
  errorMessage?: string;
}

const mockPriceFeeds: PriceFeed[] = [
  {
    id: '1',
    source: 'Binance API',
    status: 'active',
    lastSync: '2 minutes ago',
    nextSync: '1 minute',
    syncInterval: '5 minutes',
    successRate: 99.8,
    coinsTracked: 350,
    avgLatency: '120ms',
  },
  {
    id: '2',
    source: 'CoinGecko API',
    status: 'active',
    lastSync: '1 minute ago',
    nextSync: '4 minutes',
    syncInterval: '5 minutes',
    successRate: 98.5,
    coinsTracked: 420,
    avgLatency: '340ms',
  },
  {
    id: '3',
    source: 'CoinMarketCap API',
    status: 'warning',
    lastSync: '15 minutes ago',
    nextSync: 'Retrying...',
    syncInterval: '5 minutes',
    successRate: 95.2,
    coinsTracked: 280,
    avgLatency: '890ms',
    errorMessage: 'Rate limit approaching',
  },
  {
    id: '4',
    source: 'Kraken API',
    status: 'error',
    lastSync: '45 minutes ago',
    nextSync: 'Paused',
    syncInterval: '10 minutes',
    successRate: 87.3,
    coinsTracked: 120,
    avgLatency: 'N/A',
    errorMessage: 'Connection timeout - API unreachable',
  },
  {
    id: '5',
    source: 'Custom WebSocket',
    status: 'syncing',
    lastSync: 'Just now',
    nextSync: 'Real-time',
    syncInterval: 'Real-time',
    successRate: 99.9,
    coinsTracked: 50,
    avgLatency: '45ms',
  },
];

const recentErrors = [
  { id: 1, source: 'Kraken API', error: 'Connection timeout after 30s', timestamp: '2024-10-28 14:23:15', severity: 'high' },
  { id: 2, source: 'CoinMarketCap API', error: 'Rate limit warning: 85% quota used', timestamp: '2024-10-28 14:15:42', severity: 'medium' },
  { id: 3, source: 'Binance API', error: 'Temporary network latency spike', timestamp: '2024-10-28 13:45:10', severity: 'low' },
  { id: 4, source: 'CoinGecko API', error: 'Invalid response format for SOL price', timestamp: '2024-10-28 12:30:22', severity: 'medium' },
];

const syncHistory = [
  { time: '14:25', binance: 98, coingecko: 97, cmc: 45, kraken: 0, custom: 100 },
  { time: '14:20', binance: 100, coingecko: 99, cmc: 92, kraken: 0, custom: 100 },
  { time: '14:15', binance: 99, coingecko: 98, cmc: 88, kraken: 15, custom: 100 },
  { time: '14:10', binance: 100, coingecko: 100, cmc: 95, kraken: 78, custom: 100 },
  { time: '14:05', binance: 98, coingecko: 97, cmc: 96, kraken: 92, custom: 100 },
];

export default function PriceFeedsMonitor() {
  const getStatusIcon = (status: PriceFeed['status']) => {
    switch (status) {
      case 'active':
        return <CheckCircle className="w-5 h-5 text-emerald-500" />;
      case 'error':
        return <XCircle className="w-5 h-5 text-red-500" />;
      case 'warning':
        return <AlertTriangle className="w-5 h-5 text-yellow-500" />;
      case 'syncing':
        return <RefreshCw className="w-5 h-5 text-blue-500 animate-spin" />;
    }
  };

  const getStatusBadge = (status: PriceFeed['status']) => {
    const variants = {
      active: 'bg-emerald-500/10 text-emerald-500',
      error: 'bg-red-500/10 text-red-500',
      warning: 'bg-yellow-500/10 text-yellow-500',
      syncing: 'bg-blue-500/10 text-blue-500',
    };
    return variants[status];
  };

  return (
    <>
      {/* Summary Stats */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center gap-3 mb-2">
            <CheckCircle className="w-5 h-5 text-emerald-500" />
            <span className="text-gray-400">Active Feeds</span>
          </div>
          <div className="text-3xl">3 / 5</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center gap-3 mb-2">
            <Clock className="w-5 h-5 text-blue-500" />
            <span className="text-gray-400">Avg Latency</span>
          </div>
          <div className="text-3xl">324ms</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center gap-3 mb-2">
            <RefreshCw className="w-5 h-5 text-purple-500" />
            <span className="text-gray-400">Success Rate</span>
          </div>
          <div className="text-3xl">96.14%</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center gap-3 mb-2">
            <AlertTriangle className="w-5 h-5 text-yellow-500" />
            <span className="text-gray-400">Errors (24h)</span>
          </div>
          <div className="text-3xl">4</div>
        </Card>
      </div>

      {/* Price Feeds Status */}
      <Card className="bg-gray-900 border-gray-800 mb-8">
        <div className="p-6 border-b border-gray-800 flex items-center justify-between">
          <div>
            <h3 className="text-lg">Price Feed Sources</h3>
            <p className="text-gray-400 text-sm mt-1">Monitor and manage data sources</p>
          </div>
          <Button className="bg-emerald-500 hover:bg-emerald-600 text-black">
            <RefreshCw className="w-4 h-4 mr-2" />
            Sync All
          </Button>
        </div>
        <div className="divide-y divide-gray-800">
          {mockPriceFeeds.map((feed) => (
            <div key={feed.id} className="p-6 hover:bg-gray-800/50 transition-colors">
              <div className="flex items-start justify-between mb-4">
                <div className="flex items-center gap-3">
                  {getStatusIcon(feed.status)}
                  <div>
                    <h4 className="text-lg mb-1">{feed.source}</h4>
                    <Badge className={`${getStatusBadge(feed.status)} border-0`}>
                      {feed.status}
                    </Badge>
                  </div>
                </div>
                <div className="flex gap-2">
                  <Button size="sm" variant="outline" className="border-gray-700">
                    Configure
                  </Button>
                  <Button size="sm" variant="outline" className="border-gray-700">
                    <RefreshCw className="w-4 h-4" />
                  </Button>
                </div>
              </div>

              {feed.errorMessage && (
                <div className="mb-4 p-3 bg-red-500/10 border border-red-500/20 rounded-lg">
                  <div className="flex items-center gap-2 text-red-500 text-sm">
                    <XCircle className="w-4 h-4" />
                    {feed.errorMessage}
                  </div>
                </div>
              )}

              <div className="grid grid-cols-2 md:grid-cols-6 gap-4 mb-3">
                <div>
                  <div className="text-xs text-gray-400 mb-1">Last Sync</div>
                  <div className="text-sm">{feed.lastSync}</div>
                </div>
                <div>
                  <div className="text-xs text-gray-400 mb-1">Next Sync</div>
                  <div className="text-sm">{feed.nextSync}</div>
                </div>
                <div>
                  <div className="text-xs text-gray-400 mb-1">Interval</div>
                  <div className="text-sm">{feed.syncInterval}</div>
                </div>
                <div>
                  <div className="text-xs text-gray-400 mb-1">Coins Tracked</div>
                  <div className="text-sm">{feed.coinsTracked}</div>
                </div>
                <div>
                  <div className="text-xs text-gray-400 mb-1">Avg Latency</div>
                  <div className="text-sm">{feed.avgLatency}</div>
                </div>
                <div>
                  <div className="text-xs text-gray-400 mb-1">Success Rate</div>
                  <div className="text-sm text-emerald-500">{feed.successRate}%</div>
                </div>
              </div>

              <div>
                <Progress value={feed.successRate} className="h-2" />
              </div>
            </div>
          ))}
        </div>
      </Card>

      {/* Recent Errors */}
      <Card className="bg-gray-900 border-gray-800">
        <div className="p-6 border-b border-gray-800">
          <h3 className="text-lg">Recent Errors & Warnings</h3>
          <p className="text-gray-400 text-sm mt-1">Last 24 hours</p>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="border-b border-gray-800">
              <tr className="text-gray-400 text-sm">
                <th className="text-left p-4">Severity</th>
                <th className="text-left p-4">Source</th>
                <th className="text-left p-4">Error Message</th>
                <th className="text-left p-4">Timestamp</th>
              </tr>
            </thead>
            <tbody>
              {recentErrors.map((error) => (
                <tr key={error.id} className="border-b border-gray-800 hover:bg-gray-800/50">
                  <td className="p-4">
                    <Badge
                      className={
                        error.severity === 'high'
                          ? 'bg-red-500/10 text-red-500 border-0'
                          : error.severity === 'medium'
                          ? 'bg-yellow-500/10 text-yellow-500 border-0'
                          : 'bg-blue-500/10 text-blue-500 border-0'
                      }
                    >
                      {error.severity}
                    </Badge>
                  </td>
                  <td className="p-4">{error.source}</td>
                  <td className="p-4 text-gray-400">{error.error}</td>
                  <td className="p-4 text-gray-400 text-sm">{error.timestamp}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>
    </>
  );
}
