import { useState } from 'react';
import { Search, Download, TrendingUp, TrendingDown } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Badge } from '../../ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../ui/table';
import { Calendar } from '../../ui/calendar';
import { Popover, PopoverContent, PopoverTrigger } from '../../ui/popover';

export default function TradesHistory() {
  const [searchQuery, setSearchQuery] = useState('');
  const [filterPair, setFilterPair] = useState('all');
  const [dateRange, setDateRange] = useState<any>(null);

  const trades = [
    { id: 'TRD-001', time: '2025-01-15 14:23:45', pair: 'BTC/USDT', side: 'Buy', price: 50234.56, amount: 0.0234, total: 1175.49, fee: 1.18, pnl: 45.23, pnlPercent: 3.85 },
    { id: 'TRD-002', time: '2025-01-15 14:15:22', pair: 'ETH/USDT', side: 'Sell', price: 2845.32, amount: 1.2, total: 3414.38, fee: 3.41, pnl: 89.12, pnlPercent: 2.61 },
    { id: 'TRD-003', time: '2025-01-15 13:47:23', pair: 'SOL/USDT', side: 'Buy', price: 97.45, amount: 12, total: 1169.40, fee: 1.17, pnl: 23.45, pnlPercent: 2.01 },
    { id: 'TRD-004', time: '2025-01-15 12:30:18', pair: 'BNB/USDT', side: 'Sell', price: 312.89, amount: 3.5, total: 1095.12, fee: 1.10, pnl: -12.34, pnlPercent: -1.13 },
    { id: 'TRD-005', time: '2025-01-15 11:20:33', pair: 'BTC/USDT', side: 'Buy', price: 49890.12, amount: 0.05, total: 2494.51, fee: 2.49, pnl: 67.89, pnlPercent: 2.72 },
    { id: 'TRD-006', time: '2025-01-15 10:15:45', pair: 'ETH/USDT', side: 'Sell', price: 2830.00, amount: 0.5, total: 1415.00, fee: 1.42, pnl: -8.45, pnlPercent: -0.60 },
    { id: 'TRD-007', time: '2025-01-15 09:45:12', pair: 'SOL/USDT', side: 'Buy', price: 96.80, amount: 8, total: 774.40, fee: 0.77, pnl: 15.67, pnlPercent: 2.02 },
    { id: 'TRD-008', time: '2025-01-15 08:30:25', pair: 'BNB/USDT', side: 'Buy', price: 310.50, amount: 2, total: 621.00, fee: 0.62, pnl: 34.56, pnlPercent: 5.57 },
  ];

  const filteredTrades = trades.filter(trade => {
    const matchesSearch = trade.id.toLowerCase().includes(searchQuery.toLowerCase()) ||
                         trade.pair.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesPair = filterPair === 'all' || trade.pair === filterPair;
    return matchesSearch && matchesPair;
  });

  const stats = {
    totalTrades: trades.length,
    totalVolume: trades.reduce((sum, t) => sum + t.total, 0),
    totalPnL: trades.reduce((sum, t) => sum + t.pnl, 0),
    totalFees: trades.reduce((sum, t) => sum + t.fee, 0),
    winRate: (trades.filter(t => t.pnl > 0).length / trades.length * 100).toFixed(1),
  };

  const exportTrades = () => {
    // Export functionality
    console.log('Exporting trades...');
  };

  return (
    <div className="p-4 lg:p-8">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-3xl mb-2">Trade History</h1>
          <p className="text-gray-400">View all executed trades</p>
        </div>
        <Button onClick={exportTrades} variant="outline" className="border-gray-700">
          <Download className="w-4 h-4 mr-2" />
          Export CSV
        </Button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-4 mb-6">
        <Card className="bg-gray-900 border-gray-800 p-4">
          <div className="text-gray-400 text-sm mb-1">Total Trades</div>
          <div className="text-2xl text-white">{stats.totalTrades}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-4">
          <div className="text-gray-400 text-sm mb-1">Total Volume</div>
          <div className="text-2xl text-white">${stats.totalVolume.toFixed(0)}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-4">
          <div className="text-gray-400 text-sm mb-1">Total PnL</div>
          <div className={`text-2xl ${stats.totalPnL >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
            {stats.totalPnL >= 0 ? '+' : ''}${stats.totalPnL.toFixed(2)}
          </div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-4">
          <div className="text-gray-400 text-sm mb-1">Total Fees</div>
          <div className="text-2xl text-white">${stats.totalFees.toFixed(2)}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-4">
          <div className="text-gray-400 text-sm mb-1">Win Rate</div>
          <div className="text-2xl text-emerald-500">{stats.winRate}%</div>
        </Card>
      </div>

      {/* Filters */}
      <Card className="bg-gray-900 border-gray-800 p-6 mb-6">
        <div className="grid md:grid-cols-3 gap-4">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
            <Input
              placeholder="Search trades..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-10 bg-gray-800 border-gray-700"
            />
          </div>
          <Select value={filterPair} onValueChange={setFilterPair}>
            <SelectTrigger className="bg-gray-800 border-gray-700">
              <SelectValue placeholder="All Pairs" />
            </SelectTrigger>
            <SelectContent className="bg-gray-800 border-gray-700 text-white">
              <SelectItem value="all">All Pairs</SelectItem>
              <SelectItem value="BTC/USDT">BTC/USDT</SelectItem>
              <SelectItem value="ETH/USDT">ETH/USDT</SelectItem>
              <SelectItem value="SOL/USDT">SOL/USDT</SelectItem>
              <SelectItem value="BNB/USDT">BNB/USDT</SelectItem>
            </SelectContent>
          </Select>
          <Popover>
            <PopoverTrigger asChild>
              <Button variant="outline" className="border-gray-700 bg-gray-800 justify-start">
                {dateRange ? `${dateRange.from} - ${dateRange.to}` : 'Select Date Range'}
              </Button>
            </PopoverTrigger>
            <PopoverContent className="w-auto p-0 bg-gray-900 border-gray-800" align="start">
              <Calendar
                mode="range"
                selected={dateRange}
                onSelect={setDateRange}
                className="rounded-md border-0"
              />
            </PopoverContent>
          </Popover>
        </div>
      </Card>

      {/* Trades Table */}
      <Card className="bg-gray-900 border-gray-800 overflow-hidden">
        <div className="overflow-x-auto">
          <Table>
            <TableHeader>
              <TableRow className="border-gray-800 hover:bg-transparent">
                <TableHead className="text-gray-400">Trade ID</TableHead>
                <TableHead className="text-gray-400">Time</TableHead>
                <TableHead className="text-gray-400">Pair</TableHead>
                <TableHead className="text-gray-400">Side</TableHead>
                <TableHead className="text-gray-400 text-right">Price</TableHead>
                <TableHead className="text-gray-400 text-right">Amount</TableHead>
                <TableHead className="text-gray-400 text-right">Total</TableHead>
                <TableHead className="text-gray-400 text-right">Fee</TableHead>
                <TableHead className="text-gray-400 text-right">PnL</TableHead>
                <TableHead className="text-gray-400 text-right">PnL %</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredTrades.map((trade) => (
                <TableRow key={trade.id} className="border-gray-800 hover:bg-gray-800/50">
                  <TableCell className="text-emerald-500">{trade.id}</TableCell>
                  <TableCell className="text-gray-400">{trade.time}</TableCell>
                  <TableCell className="text-white">{trade.pair}</TableCell>
                  <TableCell>
                    <Badge className={trade.side === 'Buy' ? 'bg-emerald-500/10 text-emerald-500' : 'bg-red-500/10 text-red-500'}>
                      {trade.side}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-right text-white">${trade.price.toLocaleString()}</TableCell>
                  <TableCell className="text-right text-gray-300">{trade.amount}</TableCell>
                  <TableCell className="text-right text-white">${trade.total.toFixed(2)}</TableCell>
                  <TableCell className="text-right text-gray-400">${trade.fee.toFixed(2)}</TableCell>
                  <TableCell className="text-right">
                    <div className={`flex items-center justify-end gap-1 ${trade.pnl >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
                      {trade.pnl >= 0 ? <TrendingUp className="w-4 h-4" /> : <TrendingDown className="w-4 h-4" />}
                      {trade.pnl >= 0 ? '+' : ''}${trade.pnl.toFixed(2)}
                    </div>
                  </TableCell>
                  <TableCell className="text-right">
                    <span className={trade.pnlPercent >= 0 ? 'text-emerald-500' : 'text-red-500'}>
                      {trade.pnlPercent >= 0 ? '+' : ''}{trade.pnlPercent.toFixed(2)}%
                    </span>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </Card>
    </div>
  );
}
