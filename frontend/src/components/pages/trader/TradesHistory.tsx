import React, { useState, useEffect } from 'react';
import { Search, Download, TrendingUp, TrendingDown, Loader2 } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Badge } from '../../ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../ui/table';
import { Calendar } from '../../ui/calendar';
import { Popover, PopoverContent, PopoverTrigger } from '../../ui/popover';
import { TradingApi, Trade } from '../../../api/trading';

export default function TradesHistory() {
  const [searchQuery, setSearchQuery] = useState('');
  const [filterPair, setFilterPair] = useState('all');
  const [dateRange, setDateRange] = useState<any>(null);
  const [trades, setTrades] = useState<Trade[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const pageSize = 20;

  // Fetch trades from API
  useEffect(() => {
    fetchTrades();
  }, [currentPage, filterPair, dateRange]);

  const fetchTrades = async () => {
    setLoading(true);
    setError(null);
    
    try {
      const params: any = {
        page: currentPage,
        pageSize,
      };
      
      if (filterPair !== 'all') params.symbol = filterPair;
      
      if (dateRange?.from) {
        params.fromDate = dateRange.from.toISOString();
      }
      if (dateRange?.to) {
        params.toDate = dateRange.to.toISOString();
      }
      
      const res = await TradingApi.getTrades(params);
      
      if (!res.ok) {
        setError(res.error);
        setLoading(false);
        return;
      }
      
      setTrades(res.data.data);
      setTotalPages(res.data.totalPages);
      setLoading(false);
    } catch (e: any) {
      setError(e?.message || 'Failed to load trades');
      setLoading(false);
    }
  };

  // Client-side search filter
  const filteredTrades = trades.filter(trade => {
    if (!searchQuery) return true;
    const searchLower = searchQuery.toLowerCase();
    return trade.id.toLowerCase().includes(searchLower) ||
           trade.symbol.toLowerCase().includes(searchLower) ||
           trade.orderId.toLowerCase().includes(searchLower);
  });

  const stats = {
    totalTrades: trades.length,
    totalVolume: trades.reduce((sum, t) => sum + (t.price * t.quantity), 0),
    totalFees: trades.reduce((sum, t) => sum + t.fee, 0),
  };

  const exportTrades = () => {
    // Export functionality - convert trades to CSV
    const csv = [
      ['Trade ID', 'Order ID', 'Symbol', 'Price', 'Quantity', 'Fee', 'Time'].join(','),
      ...filteredTrades.map(t => [
        t.id,
        t.orderId,
        t.symbol,
        t.price,
        t.quantity,
        t.fee,
        new Date(t.createdAt).toISOString()
      ].join(','))
    ].join('\n');
    
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `trades-${new Date().toISOString()}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="p-4 lg:p-8">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-3xl mb-2">Trade History</h1>
          <p className="text-gray-400">View all executed trades</p>
        </div>
        <Button onClick={exportTrades} variant="outline" className="border-gray-700" disabled={loading || filteredTrades.length === 0}>
          <Download className="w-4 h-4 mr-2" />
          Export CSV
        </Button>
      </div>

      {/* Error Message */}
      {error && (
        <div className="mb-4 p-4 bg-red-500/10 border border-red-500/50 rounded-lg text-red-400">
          {error}
        </div>
      )}

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-3 gap-4 mb-6">
        <Card className="bg-gray-900 border-gray-800 p-4">
          <div className="text-gray-400 text-sm mb-1">Total Trades</div>
          <div className="text-2xl text-white">{loading ? '-' : stats.totalTrades}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-4">
          <div className="text-gray-400 text-sm mb-1">Total Volume</div>
          <div className="text-2xl text-white">{loading ? '-' : `$${stats.totalVolume.toFixed(2)}`}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-4">
          <div className="text-gray-400 text-sm mb-1">Total Fees</div>
          <div className="text-2xl text-white">{loading ? '-' : `$${stats.totalFees.toFixed(2)}`}</div>
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
        {loading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="w-8 h-8 text-emerald-500 animate-spin" />
          </div>
        ) : filteredTrades.length === 0 ? (
          <div className="text-center py-12 text-gray-400">
            <p>No trades found</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow className="border-gray-800 hover:bg-transparent">
                  <TableHead className="text-gray-400">Trade ID</TableHead>
                  <TableHead className="text-gray-400">Order ID</TableHead>
                  <TableHead className="text-gray-400">Time</TableHead>
                  <TableHead className="text-gray-400">Pair</TableHead>
                  <TableHead className="text-gray-400 text-right">Price</TableHead>
                  <TableHead className="text-gray-400 text-right">Quantity</TableHead>
                  <TableHead className="text-gray-400 text-right">Total</TableHead>
                  <TableHead className="text-gray-400 text-right">Fee</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredTrades.map((trade) => {
                  const total = trade.price * trade.quantity;
                  return (
                    <TableRow key={trade.id} className="border-gray-800 hover:bg-gray-800/50">
                      <TableCell className="text-emerald-500">{trade.id}</TableCell>
                      <TableCell className="text-blue-400 cursor-pointer hover:underline">
                        {trade.orderId}
                      </TableCell>
                      <TableCell className="text-gray-400">
                        {new Date(trade.createdAt).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })}
                      </TableCell>
                      <TableCell className="text-white">{trade.symbol}</TableCell>
                      <TableCell className="text-right text-white">${trade.price.toLocaleString()}</TableCell>
                      <TableCell className="text-right text-gray-300">{trade.quantity}</TableCell>
                      <TableCell className="text-right text-white">${total.toFixed(2)}</TableCell>
                      <TableCell className="text-right text-gray-400">${trade.fee.toFixed(2)}</TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </div>
        )}
      </Card>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2 mt-6">
          <Button
            variant="outline"
            size="sm"
            disabled={currentPage === 1}
            onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
            className="border-gray-700"
          >
            Previous
          </Button>
          <span className="text-gray-400 text-sm">
            Page {currentPage} of {totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            disabled={currentPage === totalPages}
            onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
            className="border-gray-700"
          >
            Next
          </Button>
        </div>
      )}
    </div>
  );
}
