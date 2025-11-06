import { ArrowLeft, Mail, Phone, Calendar, Activity, Lock, Unlock } from 'lucide-react';
import { Card } from '../ui/card';
import { Button } from '../ui/button';
import { Badge } from '../ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { Avatar, AvatarFallback, AvatarImage } from '../ui/avatar';

interface UserDetailProps {
  userId: string | null;
  onBack: () => void;
}

const mockUserData = {
  id: '1',
  name: 'John Doe',
  email: 'john@example.com',
  phone: '+1 234 567 8900',
  status: 'active',
  role: 'Premium',
  joinedDate: '2023-01-15',
  lastActive: '2 minutes ago',
  totalOrders: 145,
  totalTrades: 892,
  walletBalance: '$45,230',
};

const mockOrders = [
  { id: 'ORD-001', date: '2024-10-28', type: 'Buy', coin: 'BTC', amount: '$5,230', quantity: '0.103 BTC', status: 'completed' },
  { id: 'ORD-002', date: '2024-10-27', type: 'Sell', coin: 'ETH', amount: '$2,450', quantity: '1.2 ETH', status: 'completed' },
  { id: 'ORD-003', date: '2024-10-26', type: 'Buy', coin: 'SOL', amount: '$920', quantity: '8.9 SOL', status: 'pending' },
  { id: 'ORD-004', date: '2024-10-25', type: 'Buy', coin: 'BTC', amount: '$3,100', quantity: '0.061 BTC', status: 'completed' },
];

const mockTrades = [
  { id: 'TRD-001', date: '2024-10-28 14:23', pair: 'BTC/USDT', type: 'Buy', price: '$50,729', volume: '0.103 BTC', total: '$5,225.09', fee: '$4.91' },
  { id: 'TRD-002', date: '2024-10-27 09:15', pair: 'ETH/USDT', type: 'Sell', price: '$2,041', volume: '1.2 ETH', total: '$2,449.20', fee: '$2.45' },
  { id: 'TRD-003', date: '2024-10-26 16:45', pair: 'SOL/USDT', type: 'Buy', price: '$103.37', volume: '8.9 SOL', total: '$919.99', fee: '$0.92' },
];

const mockWallets = [
  { coin: 'BTC', name: 'Bitcoin', balance: '0.892', usdValue: '$45,230', address: '1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa' },
  { coin: 'ETH', name: 'Ethereum', balance: '12.45', usdValue: '$25,418', address: '0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb' },
  { coin: 'USDT', name: 'Tether', balance: '8920.00', usdValue: '$8,920', address: '0x4838B106FCe9647Bdf1E7877BF73cE8B0BAD5f97' },
  { coin: 'SOL', name: 'Solana', balance: '89.2', usdValue: '$9,220', address: 'DYw8jCTfwHNRJhhmFcbXvVDTqWMEVFBX6ZKUmG5CNSKK' },
];

const mockSubscriptions = [
  { plan: 'Premium Trading', status: 'active', startDate: '2023-06-15', endDate: '2024-06-15', price: '$29.99/month' },
  { plan: 'Advanced Analytics', status: 'active', startDate: '2023-08-01', endDate: '2024-08-01', price: '$19.99/month' },
  { plan: 'API Access', status: 'expired', startDate: '2023-01-15', endDate: '2024-01-15', price: '$49.99/month' },
];

const mockActivity = [
  { id: 1, action: 'Login', description: 'Logged in from 192.168.1.1', timestamp: '2024-10-28 14:30' },
  { id: 2, action: 'Trade', description: 'Executed buy order for 0.103 BTC', timestamp: '2024-10-28 14:23' },
  { id: 3, action: 'Withdrawal', description: 'Withdrew $5,000 to bank account', timestamp: '2024-10-27 16:45' },
  { id: 4, action: 'Login', description: 'Logged in from 192.168.1.1', timestamp: '2024-10-27 09:12' },
  { id: 5, action: 'Trade', description: 'Executed sell order for 1.2 ETH', timestamp: '2024-10-27 09:15' },
  { id: 6, action: 'Deposit', description: 'Deposited $10,000 via bank transfer', timestamp: '2024-10-26 11:20' },
];

export default function UserDetail({ userId, onBack }: UserDetailProps) {
  if (!userId) return null;

  return (
    <>
      {/* Back Button */}
      <Button variant="ghost" onClick={onBack} className="mb-6 hover:bg-gray-900">
        <ArrowLeft className="w-4 h-4 mr-2" />
        Back to Users
      </Button>

      {/* User Info Card */}
      <Card className="bg-gray-900 border-gray-800 p-6 mb-6">
        <div className="flex items-start justify-between">
          <div className="flex items-start gap-4">
            <Avatar className="w-20 h-20">
              <AvatarImage src={`https://api.dicebear.com/7.x/avataaars/svg?seed=${mockUserData.name}`} />
              <AvatarFallback>{mockUserData.name.split(' ').map(n => n[0]).join('')}</AvatarFallback>
            </Avatar>
            <div>
              <div className="flex items-center gap-3 mb-2">
                <h2 className="text-2xl">{mockUserData.name}</h2>
                <Badge
                  variant="default"
                  className="bg-emerald-500/10 text-emerald-500 border-0"
                >
                  {mockUserData.status}
                </Badge>
                <Badge variant="outline" className="border-gray-700">
                  {mockUserData.role}
                </Badge>
              </div>
              <div className="grid grid-cols-2 gap-x-6 gap-y-2 text-sm text-gray-400">
                <div className="flex items-center gap-2">
                  <Mail className="w-4 h-4" />
                  {mockUserData.email}
                </div>
                <div className="flex items-center gap-2">
                  <Phone className="w-4 h-4" />
                  {mockUserData.phone}
                </div>
                <div className="flex items-center gap-2">
                  <Calendar className="w-4 h-4" />
                  Joined {mockUserData.joinedDate}
                </div>
                <div className="flex items-center gap-2">
                  <Activity className="w-4 h-4" />
                  Last active {mockUserData.lastActive}
                </div>
              </div>
            </div>
          </div>
          <div className="flex gap-2">
            <Button variant="outline" className="border-gray-700">
              <Lock className="w-4 h-4 mr-2" />
              Lock Account
            </Button>
            <Button className="bg-emerald-500 hover:bg-emerald-600 text-black">
              Edit User
            </Button>
          </div>
        </div>
      </Card>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Total Orders</div>
          <div className="text-3xl mb-1">{mockUserData.totalOrders}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Total Trades</div>
          <div className="text-3xl mb-1">{mockUserData.totalTrades}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Wallet Balance</div>
          <div className="text-3xl text-emerald-500 mb-1">{mockUserData.walletBalance}</div>
        </Card>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="orders" className="space-y-6">
        <TabsList className="bg-gray-900 border border-gray-800">
          <TabsTrigger value="orders">Orders</TabsTrigger>
          <TabsTrigger value="trades">Trades</TabsTrigger>
          <TabsTrigger value="wallets">Wallets</TabsTrigger>
          <TabsTrigger value="subscriptions">Subscriptions</TabsTrigger>
          <TabsTrigger value="activity">Activity</TabsTrigger>
        </TabsList>

        {/* Orders Tab */}
        <TabsContent value="orders">
          <Card className="bg-gray-900 border-gray-800">
            <div className="p-6 border-b border-gray-800">
              <h3 className="text-lg">Order History</h3>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="border-b border-gray-800">
                  <tr className="text-gray-400 text-sm">
                    <th className="text-left p-4">Order ID</th>
                    <th className="text-left p-4">Date</th>
                    <th className="text-left p-4">Type</th>
                    <th className="text-left p-4">Coin</th>
                    <th className="text-left p-4">Amount</th>
                    <th className="text-left p-4">Quantity</th>
                    <th className="text-left p-4">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {mockOrders.map((order) => (
                    <tr key={order.id} className="border-b border-gray-800 hover:bg-gray-800/50">
                      <td className="p-4">{order.id}</td>
                      <td className="p-4 text-gray-400">{order.date}</td>
                      <td className="p-4">
                        <Badge className={order.type === 'Buy' ? 'bg-emerald-500/10 text-emerald-500 border-0' : 'bg-red-500/10 text-red-500 border-0'}>
                          {order.type}
                        </Badge>
                      </td>
                      <td className="p-4">{order.coin}</td>
                      <td className="p-4">{order.amount}</td>
                      <td className="p-4 text-gray-400">{order.quantity}</td>
                      <td className="p-4">
                        <Badge className={order.status === 'completed' ? 'bg-emerald-500/10 text-emerald-500 border-0' : 'bg-yellow-500/10 text-yellow-500 border-0'}>
                          {order.status}
                        </Badge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Card>
        </TabsContent>

        {/* Trades Tab */}
        <TabsContent value="trades">
          <Card className="bg-gray-900 border-gray-800">
            <div className="p-6 border-b border-gray-800">
              <h3 className="text-lg">Trade History</h3>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="border-b border-gray-800">
                  <tr className="text-gray-400 text-sm">
                    <th className="text-left p-4">Trade ID</th>
                    <th className="text-left p-4">Date & Time</th>
                    <th className="text-left p-4">Pair</th>
                    <th className="text-left p-4">Type</th>
                    <th className="text-left p-4">Price</th>
                    <th className="text-left p-4">Volume</th>
                    <th className="text-left p-4">Total</th>
                    <th className="text-left p-4">Fee</th>
                  </tr>
                </thead>
                <tbody>
                  {mockTrades.map((trade) => (
                    <tr key={trade.id} className="border-b border-gray-800 hover:bg-gray-800/50">
                      <td className="p-4">{trade.id}</td>
                      <td className="p-4 text-gray-400">{trade.date}</td>
                      <td className="p-4">{trade.pair}</td>
                      <td className="p-4">
                        <Badge className={trade.type === 'Buy' ? 'bg-emerald-500/10 text-emerald-500 border-0' : 'bg-red-500/10 text-red-500 border-0'}>
                          {trade.type}
                        </Badge>
                      </td>
                      <td className="p-4">{trade.price}</td>
                      <td className="p-4 text-gray-400">{trade.volume}</td>
                      <td className="p-4">{trade.total}</td>
                      <td className="p-4 text-gray-400">{trade.fee}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Card>
        </TabsContent>

        {/* Wallets Tab */}
        <TabsContent value="wallets">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {mockWallets.map((wallet) => (
              <Card key={wallet.coin} className="bg-gray-900 border-gray-800 p-6">
                <div className="flex items-start justify-between mb-4">
                  <div>
                    <div className="flex items-center gap-2 mb-1">
                      <div className="w-10 h-10 rounded-full bg-emerald-500/10 flex items-center justify-center">
                        <span className="font-semibold text-emerald-500">{wallet.coin}</span>
                      </div>
                      <div>
                        <div className="text-lg">{wallet.name}</div>
                        <div className="text-gray-400 text-sm">{wallet.coin}</div>
                      </div>
                    </div>
                  </div>
                </div>
                <div className="space-y-2">
                  <div>
                    <div className="text-gray-400 text-sm">Balance</div>
                    <div className="text-2xl">{wallet.balance} {wallet.coin}</div>
                    <div className="text-emerald-500">{wallet.usdValue}</div>
                  </div>
                  <div>
                    <div className="text-gray-400 text-sm">Address</div>
                    <div className="text-xs font-mono bg-gray-800 p-2 rounded mt-1 break-all">
                      {wallet.address}
                    </div>
                  </div>
                </div>
              </Card>
            ))}
          </div>
        </TabsContent>

        {/* Subscriptions Tab */}
        <TabsContent value="subscriptions">
          <Card className="bg-gray-900 border-gray-800">
            <div className="p-6 border-b border-gray-800">
              <h3 className="text-lg">Active Subscriptions</h3>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="border-b border-gray-800">
                  <tr className="text-gray-400 text-sm">
                    <th className="text-left p-4">Plan</th>
                    <th className="text-left p-4">Status</th>
                    <th className="text-left p-4">Start Date</th>
                    <th className="text-left p-4">End Date</th>
                    <th className="text-left p-4">Price</th>
                  </tr>
                </thead>
                <tbody>
                  {mockSubscriptions.map((sub, index) => (
                    <tr key={index} className="border-b border-gray-800 hover:bg-gray-800/50">
                      <td className="p-4">{sub.plan}</td>
                      <td className="p-4">
                        <Badge className={sub.status === 'active' ? 'bg-emerald-500/10 text-emerald-500 border-0' : 'bg-gray-500/10 text-gray-500 border-0'}>
                          {sub.status}
                        </Badge>
                      </td>
                      <td className="p-4 text-gray-400">{sub.startDate}</td>
                      <td className="p-4 text-gray-400">{sub.endDate}</td>
                      <td className="p-4">{sub.price}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Card>
        </TabsContent>

        {/* Activity Tab */}
        <TabsContent value="activity">
          <Card className="bg-gray-900 border-gray-800">
            <div className="p-6 border-b border-gray-800">
              <h3 className="text-lg">Recent Activity</h3>
            </div>
            <div className="divide-y divide-gray-800">
              {mockActivity.map((activity) => (
                <div key={activity.id} className="p-4 hover:bg-gray-800/50 transition-colors">
                  <div className="flex items-start gap-4">
                    <div className="w-10 h-10 rounded-full bg-emerald-500/10 flex items-center justify-center flex-shrink-0">
                      <Activity className="w-5 h-5 text-emerald-500" />
                    </div>
                    <div className="flex-1">
                      <div className="flex items-center justify-between mb-1">
                        <span className="font-medium">{activity.action}</span>
                        <span className="text-xs text-gray-400">{activity.timestamp}</span>
                      </div>
                      <div className="text-sm text-gray-400">{activity.description}</div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </Card>
        </TabsContent>
      </Tabs>
    </>
  );
}
