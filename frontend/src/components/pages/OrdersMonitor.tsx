import { useState } from 'react';
import { Search, Filter, Download, RefreshCw } from 'lucide-react';
import { Card } from '../ui/card';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Badge } from '../ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';

interface Order {
  id: string;
  orderId: string;
  user: string;
  userId: string;
  type: 'Buy' | 'Sell';
  coin: string;
  amount: string;
  quantity: string;
  price: string;
  status: 'pending' | 'completed' | 'failed' | 'cancelled';
  timestamp: string;
  fee: string;
}

const mockOrders: Order[] = [
  { id: '1', orderId: 'ORD-001', user: 'John Doe', userId: 'USR-123', type: 'Buy', coin: 'BTC', amount: '$5,230', quantity: '0.103 BTC', price: '$50,729', status: 'completed', timestamp: '2024-10-28 14:23', fee: '$5.23' },
  { id: '2', orderId: 'ORD-002', user: 'Sarah Chen', userId: 'USR-124', type: 'Sell', coin: 'ETH', amount: '$2,450', quantity: '1.2 ETH', price: '$2,041', status: 'completed', timestamp: '2024-10-28 14:15', fee: '$2.45' },
  { id: '3', orderId: 'ORD-003', user: 'Mike Johnson', userId: 'USR-125', type: 'Buy', coin: 'SOL', amount: '$920', quantity: '8.9 SOL', price: '$103.37', status: 'pending', timestamp: '2024-10-28 14:10', fee: '$0.92' },
  { id: '4', orderId: 'ORD-004', user: 'Emma Wilson', userId: 'USR-126', type: 'Buy', coin: 'BTC', amount: '$3,100', quantity: '0.061 BTC', price: '$50,819', status: 'completed', timestamp: '2024-10-28 13:45', fee: '$3.10' },
  { id: '5', orderId: 'ORD-005', user: 'Alex Rivera', userId: 'USR-127', type: 'Sell', coin: 'USDT', amount: '$15,600', quantity: '15,600 USDT', price: '$1.00', status: 'failed', timestamp: '2024-10-28 13:30', fee: '$0' },
  { id: '6', orderId: 'ORD-006', user: 'Lisa Anderson', userId: 'USR-128', type: 'Buy', coin: 'ETH', amount: '$8,164', quantity: '4.0 ETH', price: '$2,041', status: 'pending', timestamp: '2024-10-28 13:15', fee: '$8.16' },
  { id: '7', orderId: 'ORD-007', user: 'David Kim', userId: 'USR-129', type: 'Sell', coin: 'BTC', amount: '$10,145', quantity: '0.2 BTC', price: '$50,729', status: 'cancelled', timestamp: '2024-10-28 13:00', fee: '$0' },
  { id: '8', orderId: 'ORD-008', user: 'Maria Garcia', userId: 'USR-130', type: 'Buy', coin: 'SOL', amount: '$1,033', quantity: '10.0 SOL', price: '$103.37', status: 'completed', timestamp: '2024-10-28 12:45', fee: '$1.03' },
];

export default function OrdersMonitor() {
  const [orders, setOrders] = useState<Order[]>(mockOrders);
  const [searchQuery, setSearchQuery] = useState('');
  const [coinFilter, setCoinFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');
  const [typeFilter, setTypeFilter] = useState('all');

  const filteredOrders = orders.filter(order => {
    const matchesSearch = 
      order.orderId.toLowerCase().includes(searchQuery.toLowerCase()) ||
      order.user.toLowerCase().includes(searchQuery.toLowerCase()) ||
      order.userId.toLowerCase().includes(searchQuery.toLowerCase());
    
    const matchesCoin = coinFilter === 'all' || order.coin === coinFilter;
    const matchesStatus = statusFilter === 'all' || order.status === statusFilter;
    const matchesType = typeFilter === 'all' || order.type === typeFilter;

    return matchesSearch && matchesCoin && matchesStatus && matchesType;
  });

  const stats = {
    total: orders.length,
    pending: orders.filter(o => o.status === 'pending').length,
    completed: orders.filter(o => o.status === 'completed').length,
    failed: orders.filter(o => o.status === 'failed').length,
  };

  return (
    <>
      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-6">
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Total Orders</div>
          <div className="text-3xl">{stats.total}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Pending</div>
          <div className="text-3xl text-yellow-500">{stats.pending}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Completed</div>
          <div className="text-3xl text-emerald-500">{stats.completed}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Failed</div>
          <div className="text-3xl text-red-500">{stats.failed}</div>
        </Card>
      </div>

      {/* Filters */}
      <Card className="bg-gray-900 border-gray-800 p-6 mb-6">
        <div className="flex items-center gap-4 flex-wrap">
          <div className="relative flex-1 min-w-64">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
            <Input
              placeholder="Search by order ID, user..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-10 bg-gray-800 border-gray-700"
            />
          </div>
          
          <Select value={coinFilter} onValueChange={setCoinFilter}>
            <SelectTrigger className="w-40 bg-gray-800 border-gray-700">
              <SelectValue placeholder="All Coins" />
            </SelectTrigger>
            <SelectContent className="bg-gray-900 border-gray-800">
              <SelectItem value="all">All Coins</SelectItem>
              <SelectItem value="BTC">Bitcoin</SelectItem>
              <SelectItem value="ETH">Ethereum</SelectItem>
              <SelectItem value="SOL">Solana</SelectItem>
              <SelectItem value="USDT">Tether</SelectItem>
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

          <Select value={statusFilter} onValueChange={setStatusFilter}>
            <SelectTrigger className="w-40 bg-gray-800 border-gray-700">
              <SelectValue placeholder="All Status" />
            </SelectTrigger>
            <SelectContent className="bg-gray-900 border-gray-800">
              <SelectItem value="all">All Status</SelectItem>
              <SelectItem value="pending">Pending</SelectItem>
              <SelectItem value="completed">Completed</SelectItem>
              <SelectItem value="failed">Failed</SelectItem>
              <SelectItem value="cancelled">Cancelled</SelectItem>
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

      {/* Orders Table */}
      <Card className="bg-gray-900 border-gray-800">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="border-b border-gray-800">
              <tr className="text-gray-400 text-sm">
                <th className="text-left p-4">Order ID</th>
                <th className="text-left p-4">User</th>
                <th className="text-left p-4">Type</th>
                <th className="text-left p-4">Coin</th>
                <th className="text-left p-4">Amount</th>
                <th className="text-left p-4">Quantity</th>
                <th className="text-left p-4">Price</th>
                <th className="text-left p-4">Fee</th>
                <th className="text-left p-4">Status</th>
                <th className="text-left p-4">Timestamp</th>
              </tr>
            </thead>
            <tbody>
              {filteredOrders.map((order) => (
                <tr key={order.id} className="border-b border-gray-800 hover:bg-gray-800/50 transition-colors">
                  <td className="p-4">
                    <span className="font-mono text-sm">{order.orderId}</span>
                  </td>
                  <td className="p-4">
                    <div>
                      <div className="text-sm">{order.user}</div>
                      <div className="text-xs text-gray-400">{order.userId}</div>
                    </div>
                  </td>
                  <td className="p-4">
                    <Badge className={order.type === 'Buy' ? 'bg-emerald-500/10 text-emerald-500 border-0' : 'bg-red-500/10 text-red-500 border-0'}>
                      {order.type}
                    </Badge>
                  </td>
                  <td className="p-4">
                    <span className="font-medium">{order.coin}</span>
                  </td>
                  <td className="p-4">{order.amount}</td>
                  <td className="p-4 text-gray-400">{order.quantity}</td>
                  <td className="p-4 text-gray-400">{order.price}</td>
                  <td className="p-4 text-gray-400">{order.fee}</td>
                  <td className="p-4">
                    <Badge
                      className={
                        order.status === 'completed' ? 'bg-emerald-500/10 text-emerald-500 border-0' :
                        order.status === 'pending' ? 'bg-yellow-500/10 text-yellow-500 border-0' :
                        order.status === 'failed' ? 'bg-red-500/10 text-red-500 border-0' :
                        'bg-gray-500/10 text-gray-500 border-0'
                      }
                    >
                      {order.status}
                    </Badge>
                  </td>
                  <td className="p-4 text-gray-400 text-sm">{order.timestamp}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>
    </>
  );
}
