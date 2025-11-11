import React, { useState, useEffect } from 'react';
import { Search, Filter, X, Eye, Trash2, Loader2, TrendingUp, TrendingDown } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Badge } from '../../ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../ui/table';
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from '../../ui/alert-dialog';
import { TradingApi, Order, OrderStatus } from '../../../api/trading';
import { MarketApi } from '../../../api/market';
import { useUserRole } from '../../../hooks/useUserRole';
import { CoinIcon } from '../../ui/CoinIcon';

interface OrdersProps {
  onNavigate?: (page: string, orderId?: string) => void;
}

export default function Orders({ onNavigate }: OrdersProps) {
  const { isAdmin } = useUserRole();
  const [searchQuery, setSearchQuery] = useState('');
  const [filterPair, setFilterPair] = useState('all');
  const [filterStatus, setFilterStatus] = useState('all');
  const [filterType, setFilterType] = useState('all');
  const [cancelOrderId, setCancelOrderId] = useState<string | null>(null);
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [cryptoImageMap, setCryptoImageMap] = useState<Map<string, string | null>>(new Map());
  const pageSize = 20;

  // Fetch cryptocurrencies for logo mapping
  useEffect(() => {
    const fetchCryptos = async () => {
      try {
        const res = await MarketApi.getCryptocurrencies();
        if (res.ok) {
          const imageMap = new Map<string, string | null>();
          res.data.forEach((coin: any) => {
            const symbol = String(coin.symbol || coin.Symbol || '').toUpperCase();
            const imageUrl = coin.image || coin.Image || coin.image_url || coin.imageUrl || null;
            imageMap.set(symbol, imageUrl);
          });
          setCryptoImageMap(imageMap);
        }
      } catch (e) {
        console.warn('Failed to fetch crypto images:', e);
      }
    };
    fetchCryptos();
  }, []);

  // Fetch orders from API
  useEffect(() => {
    fetchOrders();
  }, [currentPage, filterPair, filterStatus, filterType]);

  const fetchOrders = async () => {
    setLoading(true);
    setError(null);
    
    try {
      const params: any = {
        page: currentPage,
        pageSize,
      };
      
      if (filterPair !== 'all') params.symbol = filterPair;
      if (filterType !== 'all') params.type = filterType.toUpperCase();
      if (filterStatus !== 'all') {
        // Map UI status to backend status
        const statusMap: Record<string, OrderStatus> = {
          'open': 'NEW',
          'partial': 'PARTIAL',
          'filled': 'FILLED',
          'canceled': 'CANCELED',
        };
        params.status = [statusMap[filterStatus] || filterStatus.toUpperCase()];
      }
      
      const res = await TradingApi.getOrders(params);
      
      if (!res.ok) {
        setError(res.error);
        setLoading(false);
        return;
      }
      
      setOrders(res.data.data);
      setTotalPages(res.data.totalPages);
      setLoading(false);
    } catch (e: any) {
      setError(e?.message || 'Failed to load orders');
      setLoading(false);
    }
  };

  const handleCancelOrder = (orderId: string) => {
    setCancelOrderId(orderId);
  };

  const confirmCancel = async () => {
    if (!cancelOrderId) return;
    
    try {
      const res = await TradingApi.cancelOrder(cancelOrderId);
      
      if (!res.ok) {
        setError(res.error);
        setCancelOrderId(null);
        return;
      }
      
      // Refresh orders list
      await fetchOrders();
      setCancelOrderId(null);
    } catch (e: any) {
      setError(e?.message || 'Failed to cancel order');
      setCancelOrderId(null);
    }
  };
  
  // Map backend status to UI display
  const getStatusDisplay = (status: OrderStatus): string => {
    const statusMap: Record<OrderStatus, string> = {
      'NEW': 'Open',
      'PARTIAL': 'Partial',
      'FILLED': 'Filled',
      'CANCELED': 'Canceled',
      'REJECTED': 'Rejected',
    };
    return statusMap[status] || status;
  };

  // Client-side filter for search query (API already filtered by pair/status/type)
  const filteredOrders = orders.filter(order => {
    if (!searchQuery) return true;
    const searchLower = searchQuery.toLowerCase();
    
    // Admin can search by ID or symbol, User can only search by symbol
    if (isAdmin) {
      return order.id.toLowerCase().includes(searchLower) ||
             order.symbol.toLowerCase().includes(searchLower);
    } else {
      return order.symbol.toLowerCase().includes(searchLower);
    }
  });

  const stats = {
    total: orders.length,
    open: orders.filter(o => o.status === 'NEW').length,
    filled: orders.filter(o => o.status === 'FILLED').length,
    partial: orders.filter(o => o.status === 'PARTIAL').length,
  };

  return (
    <div className="p-4 lg:p-8">
      <div className="mb-6">
        <h1 className="text-3xl mb-2">Orders</h1>
        <p className="text-gray-400">View and manage your trading orders</p>
      </div>

      {/* Error Message */}
      {error && (
        <div className="mb-4 p-4 bg-red-500/10 border border-red-500/50 rounded-lg text-red-400">
          {error}
        </div>
      )}

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <Card className="group relative overflow-hidden bg-gradient-to-br from-blue-600/20 via-blue-500/10 to-gray-900 border-2 border-blue-500/30 p-6 hover:border-blue-500/60 hover:shadow-xl hover:shadow-blue-500/20 transition-all duration-300 hover:scale-[1.02]">
          <div className="absolute top-0 right-0 w-32 h-32 bg-blue-500/10 rounded-full blur-3xl -mr-16 -mt-16"></div>
          <div className="relative z-10">
            <div className="flex items-start justify-between mb-4">
              <div className="flex-1">
                <div className="text-blue-400 text-xs font-semibold uppercase tracking-wider mb-3">Total Orders</div>
                <div className="text-4xl font-bold text-blue-500 mb-1">{stats.total}</div>
                <div className="text-xs text-blue-400/70">All time</div>
              </div>
              <div className="w-14 h-14 rounded-xl bg-blue-500/30 backdrop-blur-sm flex items-center justify-center group-hover:bg-blue-500/40 group-hover:scale-110 transition-all shadow-lg shadow-blue-500/20">
                <Filter className="w-7 h-7 text-blue-500" />
              </div>
            </div>
          </div>
          <div className="absolute bottom-0 left-0 right-0 h-1.5 bg-gradient-to-r from-blue-500 via-blue-400 to-transparent"></div>
        </Card>

        <Card className="group relative overflow-hidden bg-gradient-to-br from-yellow-600/20 via-yellow-500/10 to-gray-900 border-2 border-yellow-500/30 p-6 hover:border-yellow-500/60 hover:shadow-xl hover:shadow-yellow-500/20 transition-all duration-300 hover:scale-[1.02]">
          <div className="absolute top-0 right-0 w-32 h-32 bg-yellow-500/10 rounded-full blur-3xl -mr-16 -mt-16"></div>
          <div className="relative z-10">
            <div className="flex items-start justify-between mb-4">
              <div className="flex-1">
                <div className="text-yellow-400 text-xs font-semibold uppercase tracking-wider mb-3">Open</div>
                <div className="text-4xl font-bold text-yellow-500 mb-1">{stats.open}</div>
                <div className="text-xs text-yellow-400/70">Active orders</div>
              </div>
              <div className="w-14 h-14 rounded-xl bg-yellow-500/30 backdrop-blur-sm flex items-center justify-center group-hover:bg-yellow-500/40 group-hover:scale-110 transition-all shadow-lg shadow-yellow-500/20">
                <Loader2 className="w-7 h-7 text-yellow-500 animate-spin" />
              </div>
            </div>
          </div>
          <div className="absolute bottom-0 left-0 right-0 h-1.5 bg-gradient-to-r from-yellow-500 via-yellow-400 to-transparent"></div>
        </Card>

        <Card className="group relative overflow-hidden bg-gradient-to-br from-emerald-600/20 via-emerald-500/10 to-gray-900 border-2 border-emerald-500/30 p-6 hover:border-emerald-500/60 hover:shadow-xl hover:shadow-emerald-500/20 transition-all duration-300 hover:scale-[1.02]">
          <div className="absolute top-0 right-0 w-32 h-32 bg-emerald-500/10 rounded-full blur-3xl -mr-16 -mt-16"></div>
          <div className="relative z-10">
            <div className="flex items-start justify-between mb-4">
              <div className="flex-1">
                <div className="text-emerald-400 text-xs font-semibold uppercase tracking-wider mb-3">Filled</div>
                <div className="text-4xl font-bold text-emerald-500 mb-1">{stats.filled}</div>
                <div className="text-xs text-emerald-400/70">Completed</div>
              </div>
              <div className="w-14 h-14 rounded-xl bg-emerald-500/30 backdrop-blur-sm flex items-center justify-center group-hover:bg-emerald-500/40 group-hover:scale-110 transition-all shadow-lg shadow-emerald-500/20">
                <TrendingUp className="w-7 h-7 text-emerald-500" />
              </div>
            </div>
          </div>
          <div className="absolute bottom-0 left-0 right-0 h-1.5 bg-gradient-to-r from-emerald-500 via-emerald-400 to-transparent"></div>
        </Card>

        <Card className="group relative overflow-hidden bg-gradient-to-br from-cyan-600/20 via-cyan-500/10 to-gray-900 border-2 border-cyan-500/30 p-6 hover:border-cyan-500/60 hover:shadow-xl hover:shadow-cyan-500/20 transition-all duration-300 hover:scale-[1.02]">
          <div className="absolute top-0 right-0 w-32 h-32 bg-cyan-500/10 rounded-full blur-3xl -mr-16 -mt-16"></div>
          <div className="relative z-10">
            <div className="flex items-start justify-between mb-4">
              <div className="flex-1">
                <div className="text-cyan-400 text-xs font-semibold uppercase tracking-wider mb-3">Partial</div>
                <div className="text-4xl font-bold text-cyan-500 mb-1">{stats.partial}</div>
                <div className="text-xs text-cyan-400/70">In progress</div>
              </div>
              <div className="w-14 h-14 rounded-xl bg-cyan-500/30 backdrop-blur-sm flex items-center justify-center group-hover:bg-cyan-500/40 group-hover:scale-110 transition-all shadow-lg shadow-cyan-500/20">
                <TrendingDown className="w-7 h-7 text-cyan-500" />
              </div>
            </div>
          </div>
          <div className="absolute bottom-0 left-0 right-0 h-1.5 bg-gradient-to-r from-cyan-500 via-cyan-400 to-transparent"></div>
        </Card>
      </div>

      {/* Filters */}
      <Card className="bg-gray-900 border-gray-800 p-6 mb-6">
        <div className="grid md:grid-cols-4 gap-4">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
            <Input
              placeholder="Search orders..."
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
          <Select value={filterStatus} onValueChange={setFilterStatus}>
            <SelectTrigger className="bg-gray-800 border-gray-700">
              <SelectValue placeholder="All Status" />
            </SelectTrigger>
            <SelectContent className="bg-gray-800 border-gray-700 text-white">
              <SelectItem value="all">All Status</SelectItem>
              <SelectItem value="open">Open</SelectItem>
              <SelectItem value="filled">Filled</SelectItem>
              <SelectItem value="partial">Partial</SelectItem>
              <SelectItem value="canceled">Canceled</SelectItem>
            </SelectContent>
          </Select>
          <Select value={filterType} onValueChange={setFilterType}>
            <SelectTrigger className="bg-gray-800 border-gray-700">
              <SelectValue placeholder="All Types" />
            </SelectTrigger>
            <SelectContent className="bg-gray-800 border-gray-700 text-white">
              <SelectItem value="all">All Types</SelectItem>
              <SelectItem value="market">Market</SelectItem>
              <SelectItem value="limit">Limit</SelectItem>
            </SelectContent>
          </Select>
        </div>
        {(searchQuery || filterPair !== 'all' || filterStatus !== 'all' || filterType !== 'all') && (
          <div className="flex items-center gap-2 mt-4">
            <span className="text-sm text-gray-400">Active filters:</span>
            {searchQuery && (
              <Badge variant="outline" className="border-gray-700">
                Search: {searchQuery}
                <X className="w-3 h-3 ml-1 cursor-pointer" onClick={() => setSearchQuery('')} />
              </Badge>
            )}
            {filterPair !== 'all' && (
              <Badge variant="outline" className="border-gray-700">
                {filterPair}
                <X className="w-3 h-3 ml-1 cursor-pointer" onClick={() => setFilterPair('all')} />
              </Badge>
            )}
            {filterStatus !== 'all' && (
              <Badge variant="outline" className="border-gray-700">
                {filterStatus}
                <X className="w-3 h-3 ml-1 cursor-pointer" onClick={() => setFilterStatus('all')} />
              </Badge>
            )}
            {filterType !== 'all' && (
              <Badge variant="outline" className="border-gray-700">
                {filterType}
                <X className="w-3 h-3 ml-1 cursor-pointer" onClick={() => setFilterType('all')} />
              </Badge>
            )}
          </div>
        )}
      </Card>

      {/* Orders Table */}
      <Card className="bg-gray-900 border-gray-800 overflow-hidden">
        {loading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="w-8 h-8 text-emerald-500 animate-spin" />
          </div>
        ) : filteredOrders.length === 0 ? (
          <div className="text-center py-12 text-gray-400">
            <p>No orders found</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow className="border-gray-800 hover:bg-transparent bg-gray-800/50">
                  {isAdmin && <TableHead className="text-gray-300 font-semibold">Order ID</TableHead>}
                  <TableHead className="text-gray-300 font-semibold">Time</TableHead>
                  <TableHead className="text-gray-300 font-semibold">Pair</TableHead>
                  <TableHead className="text-gray-300 font-semibold">Type</TableHead>
                  <TableHead className="text-gray-300 font-semibold">Side</TableHead>
                  <TableHead className="text-gray-300 font-semibold text-right">Price</TableHead>
                  <TableHead className="text-gray-300 font-semibold text-right">Quantity</TableHead>
                  <TableHead className="text-gray-300 font-semibold text-right">Filled</TableHead>
                  <TableHead className="text-gray-300 font-semibold">Status</TableHead>
                  <TableHead className="text-gray-300 font-semibold text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredOrders.map((order) => (
                  <TableRow key={order.id} className="border-gray-800 hover:bg-gray-800/50 transition-colors">
                    {isAdmin && (
                      <TableCell className="text-emerald-500 cursor-pointer" onClick={() => onNavigate?.('order-detail', order.id)}>
                        {order.id}
                      </TableCell>
                    )}
                    <TableCell className="text-gray-400">
                      <div className="text-sm">
                        {new Date(order.createdAt).toLocaleString('vi-VN', { 
                          timeZone: 'Asia/Ho_Chi_Minh',
                          month: 'short',
                          day: 'numeric',
                          hour: '2-digit',
                          minute: '2-digit'
                        })}
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        {(() => {
                          const baseSymbol = order.symbol.split('/')[0];
                          const imageUrl = cryptoImageMap.get(baseSymbol);
                          return (
                            <CoinIcon 
                              symbol={baseSymbol} 
                              image={imageUrl || null} 
                              size="sm" 
                            />
                          );
                        })()}
                        <div>
                          <div className="text-white font-medium">{order.symbol}</div>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline" className="border-gray-700">
                        {order.type}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Badge className={
                        order.side === 'BUY' 
                          ? 'bg-emerald-500/10 text-emerald-500 border-emerald-500/20' 
                          : 'bg-red-500/10 text-red-500 border-red-500/20'
                      }>
                        <div className="flex items-center gap-1">
                          {order.side === 'BUY' ? (
                            <TrendingUp className="w-3 h-3" />
                          ) : (
                            <TrendingDown className="w-3 h-3" />
                          )}
                          {order.side}
                        </div>
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="text-white font-medium">
                        {order.price ? `$${order.price.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : (
                          <Badge variant="outline" className="border-gray-600 text-gray-300">Market</Badge>
                        )}
                      </div>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="text-gray-200 font-medium">{order.quantity.toLocaleString('en-US', { maximumFractionDigits: 8 })}</div>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="text-emerald-400 font-medium">{order.filled.toLocaleString('en-US', { maximumFractionDigits: 8 })}</div>
                    </TableCell>
                    <TableCell>
                      <Badge className={
                        order.status === 'FILLED' ? 'bg-emerald-500/10 text-emerald-500' :
                        order.status === 'NEW' ? 'bg-yellow-500/10 text-yellow-500' :
                        order.status === 'PARTIAL' ? 'bg-blue-500/10 text-blue-500' :
                        order.status === 'REJECTED' ? 'bg-red-500/10 text-red-500' :
                        'bg-gray-500/10 text-gray-500'
                      }>
                        {getStatusDisplay(order.status)}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-2">
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => onNavigate?.('order-detail', order.id)}
                        >
                          <Eye className="w-4 h-4" />
                        </Button>
                        {(order.status === 'NEW' || order.status === 'PARTIAL') && (
                          <Button
                            size="sm"
                            variant="ghost"
                            className="text-red-500 hover:text-red-400 hover:bg-red-500/10"
                            onClick={() => handleCancelOrder(order.id)}
                          >
                            <Trash2 className="w-4 h-4" />
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
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

      {/* Cancel Order Dialog */}
      <AlertDialog open={!!cancelOrderId} onOpenChange={() => setCancelOrderId(null)}>
        <AlertDialogContent className="bg-gray-900 border-gray-800 text-white">
          <AlertDialogHeader>
            <AlertDialogTitle>Cancel Order</AlertDialogTitle>
            <AlertDialogDescription className="text-gray-400">
              Are you sure you want to cancel order {cancelOrderId}? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="bg-gray-800 border-gray-700 text-white hover:bg-gray-700">
              Keep Order
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={confirmCancel}
              className="bg-red-500 text-white hover:bg-red-600"
            >
              Cancel Order
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
