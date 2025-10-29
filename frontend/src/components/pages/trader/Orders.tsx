import { useState } from 'react';
import { Search, Filter, X, Eye, Trash2 } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Badge } from '../../ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../ui/table';
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from '../../ui/alert-dialog';

interface OrdersProps {
  onNavigate?: (page: string, orderId?: string) => void;
}

export default function Orders({ onNavigate }: OrdersProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [filterPair, setFilterPair] = useState('all');
  const [filterStatus, setFilterStatus] = useState('all');
  const [filterType, setFilterType] = useState('all');
  const [cancelOrderId, setCancelOrderId] = useState<string | null>(null);

  const orders = [
    { 
      id: 'ORD-001', 
      time: '2025-01-15 14:23:45', 
      pair: 'BTC/USDT', 
      type: 'Limit', 
      side: 'Buy', 
      price: 50200.00, 
      amount: 0.0234, 
      filled: 0.0234, 
      total: 1174.68, 
      status: 'Filled' 
    },
    { 
      id: 'ORD-002', 
      time: '2025-01-15 14:15:22', 
      pair: 'ETH/USDT', 
      type: 'Market', 
      side: 'Sell', 
      price: 2845.32, 
      amount: 1.2, 
      filled: 1.2, 
      total: 3414.38, 
      status: 'Filled' 
    },
    { 
      id: 'ORD-003', 
      time: '2025-01-15 13:45:10', 
      pair: 'SOL/USDT', 
      type: 'Limit', 
      side: 'Buy', 
      price: 97.50, 
      amount: 45, 
      filled: 23, 
      total: 4387.50, 
      status: 'Partial' 
    },
    { 
      id: 'ORD-004', 
      time: '2025-01-15 12:30:18', 
      pair: 'BNB/USDT', 
      type: 'Limit', 
      side: 'Buy', 
      price: 310.00, 
      amount: 3.5, 
      filled: 0, 
      total: 1085.00, 
      status: 'Open' 
    },
    { 
      id: 'ORD-005', 
      time: '2025-01-15 11:20:33', 
      pair: 'BTC/USDT', 
      type: 'Limit', 
      side: 'Sell', 
      price: 51000.00, 
      amount: 0.05, 
      filled: 0, 
      total: 2550.00, 
      status: 'Open' 
    },
    { 
      id: 'ORD-006', 
      time: '2025-01-15 10:15:45', 
      pair: 'ETH/USDT', 
      type: 'Market', 
      side: 'Buy', 
      price: 2830.00, 
      amount: 0.5, 
      filled: 0, 
      total: 1415.00, 
      status: 'Canceled' 
    },
  ];

  const handleCancelOrder = (orderId: string) => {
    setCancelOrderId(orderId);
  };

  const confirmCancel = () => {
    // Cancel order logic here
    setCancelOrderId(null);
  };

  const filteredOrders = orders.filter(order => {
    const matchesSearch = order.id.toLowerCase().includes(searchQuery.toLowerCase()) ||
                         order.pair.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesPair = filterPair === 'all' || order.pair === filterPair;
    const matchesStatus = filterStatus === 'all' || order.status.toLowerCase() === filterStatus.toLowerCase();
    const matchesType = filterType === 'all' || order.type.toLowerCase() === filterType.toLowerCase();
    
    return matchesSearch && matchesPair && matchesStatus && matchesType;
  });

  const stats = {
    total: orders.length,
    open: orders.filter(o => o.status === 'Open').length,
    filled: orders.filter(o => o.status === 'Filled').length,
    partial: orders.filter(o => o.status === 'Partial').length,
  };

  return (
    <div className="p-4 lg:p-8">
      <div className="mb-6">
        <h1 className="text-3xl mb-2">Orders</h1>
        <p className="text-gray-400">View and manage your trading orders</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <Card className="bg-gray-900 border-gray-800 p-4">
          <div className="text-gray-400 text-sm mb-1">Total Orders</div>
          <div className="text-2xl text-white">{stats.total}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-4">
          <div className="text-gray-400 text-sm mb-1">Open</div>
          <div className="text-2xl text-yellow-500">{stats.open}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-4">
          <div className="text-gray-400 text-sm mb-1">Filled</div>
          <div className="text-2xl text-emerald-500">{stats.filled}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-4">
          <div className="text-gray-400 text-sm mb-1">Partial</div>
          <div className="text-2xl text-blue-500">{stats.partial}</div>
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
        <div className="overflow-x-auto">
          <Table>
            <TableHeader>
              <TableRow className="border-gray-800 hover:bg-transparent">
                <TableHead className="text-gray-400">Order ID</TableHead>
                <TableHead className="text-gray-400">Time</TableHead>
                <TableHead className="text-gray-400">Pair</TableHead>
                <TableHead className="text-gray-400">Type</TableHead>
                <TableHead className="text-gray-400">Side</TableHead>
                <TableHead className="text-gray-400 text-right">Price</TableHead>
                <TableHead className="text-gray-400 text-right">Amount</TableHead>
                <TableHead className="text-gray-400 text-right">Filled</TableHead>
                <TableHead className="text-gray-400 text-right">Total</TableHead>
                <TableHead className="text-gray-400">Status</TableHead>
                <TableHead className="text-gray-400 text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredOrders.map((order) => (
                <TableRow key={order.id} className="border-gray-800 hover:bg-gray-800/50">
                  <TableCell className="text-emerald-500 cursor-pointer" onClick={() => onNavigate?.('order-detail', order.id)}>
                    {order.id}
                  </TableCell>
                  <TableCell className="text-gray-400">{order.time}</TableCell>
                  <TableCell className="text-white">{order.pair}</TableCell>
                  <TableCell>
                    <Badge variant="outline" className="border-gray-700">
                      {order.type}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <Badge className={order.side === 'Buy' ? 'bg-emerald-500/10 text-emerald-500' : 'bg-red-500/10 text-red-500'}>
                      {order.side}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-right text-white">${order.price.toLocaleString()}</TableCell>
                  <TableCell className="text-right text-gray-300">{order.amount}</TableCell>
                  <TableCell className="text-right text-gray-300">{order.filled}</TableCell>
                  <TableCell className="text-right text-white">${order.total.toLocaleString()}</TableCell>
                  <TableCell>
                    <Badge className={
                      order.status === 'Filled' ? 'bg-emerald-500/10 text-emerald-500' :
                      order.status === 'Open' ? 'bg-yellow-500/10 text-yellow-500' :
                      order.status === 'Partial' ? 'bg-blue-500/10 text-blue-500' :
                      'bg-gray-500/10 text-gray-500'
                    }>
                      {order.status}
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
                      {(order.status === 'Open' || order.status === 'Partial') && (
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
      </Card>

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
