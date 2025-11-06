import { useState } from 'react';
import { Search, Download, RefreshCw, TrendingUp, TrendingDown } from 'lucide-react';
import { Card } from '../ui/card';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Badge } from '../ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

interface Trade {
  id: string;
  tradeId: string;
  user: string;
  userId: string;
  pair: string;
  type: 'Buy' | 'Sell';
  price: string;
  volume: string;
  total: string;
  fee: string;
  timestamp: string;
}

const mockTrades: Trade[] = [
  { id: '1', tradeId: 'TRD-001', user: 'John Doe', userId: 'USR-123', pair: 'BTC/USDT', type: 'Buy', price: '$50,729', volume: '0.103 BTC', total: '$5,225.09', fee: '$5.23', timestamp: '2024-10-28 14:23:45' },
  { id: '2', tradeId: 'TRD-002', user: 'Sarah Chen', userId: 'USR-124', pair: 'ETH/USDT', type: 'Sell', price: '$2,041', volume: '1.2 ETH', total: '$2,449.20', fee: '$2.45', timestamp: '2024-10-28 14:15:32' },
  { id: '3', tradeId: 'TRD-003', user: 'Mike Johnson', userId: 'USR-125', pair: 'SOL/USDT', type: 'Buy', price: '$103.37', volume: '8.9 SOL', total: '$919.99', fee: '$0.92', timestamp: '2024-10-28 14:10:18' },
  { id: '4', tradeId: 'TRD-004', user: 'Emma Wilson', userId: 'USR-126', pair: 'BTC/USDT', type: 'Buy', price: '$50,819', volume: '0.061 BTC', total: '$3,099.96', fee: '$3.10', timestamp: '2024-10-28 13:45:27' },
  { id: '5', tradeId: 'TRD-005', user: 'Lisa Anderson', userId: 'USR-128', pair: 'ETH/USDT', type: 'Buy', price: '$2,041', volume: '4.0 ETH', total: '$8,164.00', fee: '$8.16', timestamp: '2024-10-28 13:15:55' },
  { id: '6', tradeId: 'TRD-006', user: 'David Kim', userId: 'USR-129', pair: 'BTC/USDT', type: 'Sell', price: '$50,729', volume: '0.2 BTC', total: '$10,145.80', fee: '$10.15', timestamp: '2024-10-28 13:00:12' },
  { id: '7', tradeId: 'TRD-007', user: 'Maria Garcia', userId: 'USR-130', pair: 'SOL/USDT', type: 'Buy', price: '$103.37', volume: '10.0 SOL', total: '$1,033.70', fee: '$1.03', timestamp: '2024-10-28 12:45:38' },
  { id: '8', tradeId: 'TRD-008', user: 'James Brown', userId: 'USR-131', pair: 'ETH/USDT', type: 'Sell', price: '$2,041', volume: '2.5 ETH', total: '$5,102.50', fee: '$5.10', timestamp: '2024-10-28 12:30:41' },
];

const volumeData = [
  { time: '10:00', volume: 2.4 },
  { time: '11:00', volume: 3.2 },
  { time: '12:00', volume: 2.8 },
  { time: '13:00', volume: 4.1 },
  { time: '14:00', volume: 3.5 },
];

export default function TradesMonitor() {
  const [trades] = useState<Trade[]>(mockTrades);
  const [searchQuery, setSearchQuery] = useState('');
  const [pairFilter, setPairFilter] = useState('all');
  const [typeFilter, setTypeFilter] = useState('all');

  const filteredTrades = trades.filter(trade => {
    const matchesSearch = 
      trade.tradeId.toLowerCase().includes(searchQuery.toLowerCase()) ||
      trade.user.toLowerCase().includes(searchQuery.toLowerCase()) ||
      trade.userId.toLowerCase().includes(searchQuery.toLowerCase());
    
    const matchesPair = pairFilter === 'all' || trade.pair === pairFilter;
    const matchesType = typeFilter === 'all' || trade.type === typeFilter;

    return matchesSearch && matchesPair && matchesType;
  });

  const totalVolume = trades.reduce((acc, trade) => {
    const total = parseFloat(trade.total.replace(/[$,]/g, ''));
    return acc + total;
  }, 0);

  const buyVolume = trades.filter(t => t.type === 'Buy').reduce((acc, trade) => {
    const total = parseFloat(trade.total.replace(/[$,]/g, ''));
    return acc + total;
  }, 0);

  const sellVolume = trades.filter(t => t.type === 'Sell').reduce((acc, trade) => {
    const total = parseFloat(trade.total.replace(/[$,]/g, ''));
    return acc + total;
  }, 0);

  return (
    <>
      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-6">
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Total Trades</div>
          <div className="text-3xl">{trades.length}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Total Volume</div>
          <div className="text-3xl">${(totalVolume / 1000).toFixed(1)}K</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center gap-2 mb-2">
            <TrendingUp className="w-4 h-4 text-emerald-500" />
            <span className="text-gray-400 text-sm">Buy Volume</span>
          </div>
          <div className="text-3xl text-emerald-500">${(buyVolume / 1000).toFixed(1)}K</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center gap-2 mb-2">
            <TrendingDown className="w-4 h-4 text-red-500" />
            <span className="text-gray-400 text-sm">Sell Volume</span>
          </div>
          <div className="text-3xl text-red-500">${(sellVolume / 1000).toFixed(1)}K</div>
        </Card>
      </div>

      {/* Volume Chart */}
      <Card className="bg-gray-900 border-gray-800 p-6 mb-6">
        <h3 className="text-lg mb-4">Trading Volume (Last 5 Hours)</h3>
        <ResponsiveContainer width="100%" height={200}>
          <LineChart data={volumeData}>
            <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
            <XAxis dataKey="time" stroke="#6b7280" />
            <YAxis stroke="#6b7280" />
            <Tooltip 
              contentStyle={{ backgroundColor: '#1f2937', border: '1px solid #374151', borderRadius: '8px' }}
              labelStyle={{ color: '#9ca3af' }}
            />
            <Line type="monotone" dataKey="volume" stroke="#22c55e" strokeWidth={2} />
          </LineChart>
        </ResponsiveContainer>
      </Card>

      {/* Filters */}
      <Card className="bg-gray-900 border-gray-800 p-6 mb-6">
        <div className="flex items-center gap-4 flex-wrap">
          <div className="relative flex-1 min-w-64">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
            <Input
              placeholder="Search by trade ID, user..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-10 bg-gray-800 border-gray-700"
            />
          </div>
          
          <Select value={pairFilter} onValueChange={setPairFilter}>
            <SelectTrigger className="w-48 bg-gray-800 border-gray-700">
              <SelectValue placeholder="All Pairs" />
            </SelectTrigger>
            <SelectContent className="bg-gray-900 border-gray-800">
              <SelectItem value="all">All Pairs</SelectItem>
              <SelectItem value="BTC/USDT">BTC/USDT</SelectItem>
              <SelectItem value="ETH/USDT">ETH/USDT</SelectItem>
              <SelectItem value="SOL/USDT">SOL/USDT</SelectItem>
            </SelectContent>
          </Select>

          <Select value={typeFilter} onValueChange={setTypeFilter}>
            <SelectTrigger className="w-40 bg-gray-800 border-gray-700">
              <SelectValue placeholder="All Types" />
            </SelectTrigger>
            <SelectContent className="bg-gray-900 border-gray-800">
              <SelectItem value="all">All Types</SelectItem>
              <SelectItem value="Buy">Buy</SelectItem>
              <SelectItem value="Sell">Sell</SelectItem>
            </SelectContent>
          </Select>

          <Button variant="outline" className="border-gray-700">
            <RefreshCw className="w-4 h-4 mr-2" />
            Refresh
          </Button>

          <Button variant="outline" className="border-gray-700">
            <Download className="w-4 h-4 mr-2" />
            Export
          </Button>
        </div>
      </Card>

      {/* Trades Table */}
      <Card className="bg-gray-900 border-gray-800">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="border-b border-gray-800">
              <tr className="text-gray-400 text-sm">
                <th className="text-left p-4">Trade ID</th>
                <th className="text-left p-4">User</th>
                <th className="text-left p-4">Pair</th>
                <th className="text-left p-4">Type</th>
                <th className="text-left p-4">Price</th>
                <th className="text-left p-4">Volume</th>
                <th className="text-left p-4">Total</th>
                <th className="text-left p-4">Fee</th>
                <th className="text-left p-4">Timestamp</th>
              </tr>
            </thead>
            <tbody>
              {filteredTrades.map((trade) => (
                <tr key={trade.id} className="border-b border-gray-800 hover:bg-gray-800/50 transition-colors">
                  <td className="p-4">
                    <span className="font-mono text-sm">{trade.tradeId}</span>
                  </td>
                  <td className="p-4">
                    <div>
                      <div className="text-sm">{trade.user}</div>
                      <div className="text-xs text-gray-400">{trade.userId}</div>
                    </div>
                  </td>
                  <td className="p-4">
                    <span className="font-medium">{trade.pair}</span>
                  </td>
                  <td className="p-4">
                    <Badge className={trade.type === 'Buy' ? 'bg-emerald-500/10 text-emerald-500 border-0' : 'bg-red-500/10 text-red-500 border-0'}>
                      {trade.type}
                    </Badge>
                  </td>
                  <td className="p-4 text-gray-400">{trade.price}</td>
                  <td className="p-4 text-gray-400">{trade.volume}</td>
                  <td className="p-4">{trade.total}</td>
                  <td className="p-4 text-gray-400">{trade.fee}</td>
                  <td className="p-4 text-gray-400 text-sm">{trade.timestamp}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>
    </>
  );
}
