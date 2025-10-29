import { TrendingUp, TrendingDown, ArrowUpRight, ArrowDownRight, DollarSign, Wallet, Activity, Clock } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Badge } from '../../ui/badge';
import { AreaChart, Area, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

interface TraderDashboardProps {
  onNavigate?: (page: string) => void;
}

export default function TraderDashboard({ onNavigate }: TraderDashboardProps) {
  const navData = [
    { date: 'Jan 1', value: 10000 },
    { date: 'Jan 8', value: 10500 },
    { date: 'Jan 15', value: 10200 },
    { date: 'Jan 22', value: 11000 },
    { date: 'Jan 29', value: 11800 },
    { date: 'Feb 5', value: 12100 },
    { date: 'Feb 12', value: 12458 },
  ];

  const pnlData = [
    { time: '00:00', pnl: 0 },
    { time: '04:00', pnl: 45 },
    { time: '08:00', pnl: 120 },
    { time: '12:00', pnl: 89 },
    { time: '16:00', pnl: 234 },
    { time: '20:00', pnl: 198 },
    { time: '24:00', pnl: 234 },
  ];

  const recentOrders = [
    { id: '1', time: '2m ago', pair: 'BTC/USDT', type: 'Buy', side: 'Market', amount: '0.0234 BTC', price: '$50,234', status: 'Filled', pnl: '+$45' },
    { id: '2', time: '15m ago', pair: 'ETH/USDT', type: 'Sell', side: 'Limit', amount: '1.2 ETH', price: '$2,845', status: 'Filled', pnl: '+$89' },
    { id: '3', time: '1h ago', pair: 'SOL/USDT', type: 'Buy', side: 'Limit', amount: '45 SOL', price: '$98.45', status: 'Partial', pnl: '+$23' },
    { id: '4', time: '2h ago', pair: 'BNB/USDT', type: 'Sell', side: 'Market', amount: '3.5 BNB', price: '$312.89', status: 'Filled', pnl: '+$67' },
  ];

  const holdings = [
    { symbol: 'BTC', name: 'Bitcoin', amount: '0.2341', value: '$11,759.82', change: '+2.34%', positive: true },
    { symbol: 'ETH', name: 'Ethereum', amount: '4.5678', value: '$12,993.50', change: '+1.82%', positive: true },
    { symbol: 'SOL', name: 'Solana', amount: '125.34', value: '$12,339.73', change: '-0.45%', positive: false },
    { symbol: 'USDT', name: 'Tether', amount: '5234.56', value: '$5,234.56', change: '0.00%', positive: true },
  ];

  return (
    <div className="p-4 lg:p-8 space-y-6">
      {/* Quick Stats */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="text-gray-400">Total Balance (NAV)</div>
            <div className="w-10 h-10 bg-emerald-500/10 rounded-full flex items-center justify-center">
              <Wallet className="w-5 h-5 text-emerald-500" />
            </div>
          </div>
          <div className="text-3xl text-white mb-1">$12,458.32</div>
          <div className="flex items-center gap-1 text-emerald-500 text-sm">
            <ArrowUpRight className="w-4 h-4" />
            +$234.12 (1.9%)
          </div>
        </Card>

        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="text-gray-400">Today's PnL</div>
            <div className="w-10 h-10 bg-emerald-500/10 rounded-full flex items-center justify-center">
              <TrendingUp className="w-5 h-5 text-emerald-500" />
            </div>
          </div>
          <div className="text-3xl text-white mb-1">+$234.12</div>
          <div className="flex items-center gap-1 text-emerald-500 text-sm">
            <ArrowUpRight className="w-4 h-4" />
            +3.45%
          </div>
        </Card>

        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="text-gray-400">Available Balance</div>
            <div className="w-10 h-10 bg-blue-500/10 rounded-full flex items-center justify-center">
              <DollarSign className="w-5 h-5 text-blue-500" />
            </div>
          </div>
          <div className="text-3xl text-white mb-1">$8,234.56</div>
          <div className="text-gray-400 text-sm">66.1% of total</div>
        </Card>

        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="text-gray-400">Open Orders</div>
            <div className="w-10 h-10 bg-yellow-500/10 rounded-full flex items-center justify-center">
              <Activity className="w-5 h-5 text-yellow-500" />
            </div>
          </div>
          <div className="text-3xl text-white mb-1">3</div>
          <div className="text-gray-400 text-sm">2 Buy, 1 Sell</div>
        </Card>
      </div>

      <div className="grid lg:grid-cols-3 gap-6">
        {/* NAV Chart */}
        <Card className="lg:col-span-2 bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h2 className="text-xl mb-1">Net Asset Value (NAV)</h2>
              <p className="text-gray-400 text-sm">Last 30 days performance</p>
            </div>
            <Button
              size="sm"
              className="bg-emerald-500 text-black hover:bg-emerald-600"
              onClick={() => onNavigate?.('portfolio')}
            >
              View Portfolio
            </Button>
          </div>
          <ResponsiveContainer width="100%" height={250}>
            <AreaChart data={navData}>
              <defs>
                <linearGradient id="navGradient" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#22c55e" stopOpacity={0.3}/>
                  <stop offset="95%" stopColor="#22c55e" stopOpacity={0}/>
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
              <XAxis dataKey="date" stroke="#9ca3af" />
              <YAxis stroke="#9ca3af" />
              <Tooltip 
                contentStyle={{ backgroundColor: '#1f2937', border: '1px solid #374151', borderRadius: '8px' }}
                labelStyle={{ color: '#9ca3af' }}
              />
              <Area type="monotone" dataKey="value" stroke="#22c55e" fillOpacity={1} fill="url(#navGradient)" />
            </AreaChart>
          </ResponsiveContainer>
        </Card>

        {/* PnL Chart */}
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="mb-6">
            <h2 className="text-xl mb-1">Today's PnL</h2>
            <p className="text-gray-400 text-sm">Hourly breakdown</p>
          </div>
          <ResponsiveContainer width="100%" height={250}>
            <LineChart data={pnlData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
              <XAxis dataKey="time" stroke="#9ca3af" />
              <YAxis stroke="#9ca3af" />
              <Tooltip 
                contentStyle={{ backgroundColor: '#1f2937', border: '1px solid #374151', borderRadius: '8px' }}
                labelStyle={{ color: '#9ca3af' }}
              />
              <Line type="monotone" dataKey="pnl" stroke="#22c55e" strokeWidth={2} />
            </LineChart>
          </ResponsiveContainer>
        </Card>
      </div>

      <div className="grid lg:grid-cols-2 gap-6">
        {/* Holdings */}
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-xl">Top Holdings</h2>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => onNavigate?.('wallets')}
              className="text-emerald-500 hover:text-emerald-400"
            >
              View All
            </Button>
          </div>
          <div className="space-y-4">
            {holdings.map((holding) => (
              <div key={holding.symbol} className="flex items-center justify-between p-3 rounded-lg hover:bg-gray-800 transition-colors">
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 bg-emerald-500/10 rounded-full flex items-center justify-center">
                    <span className="text-emerald-500 text-sm">{holding.symbol}</span>
                  </div>
                  <div>
                    <div className="text-white">{holding.name}</div>
                    <div className="text-sm text-gray-400">{holding.amount} {holding.symbol}</div>
                  </div>
                </div>
                <div className="text-right">
                  <div className="text-white">{holding.value}</div>
                  <div className={holding.positive ? 'text-emerald-500 text-sm' : 'text-red-500 text-sm'}>
                    {holding.change}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </Card>

        {/* Recent Orders */}
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-xl">Recent Orders</h2>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => onNavigate?.('orders')}
              className="text-emerald-500 hover:text-emerald-400"
            >
              View All
            </Button>
          </div>
          <div className="space-y-3">
            {recentOrders.map((order) => (
              <div
                key={order.id}
                className="p-4 rounded-lg border border-gray-800 hover:border-gray-700 transition-colors cursor-pointer"
                onClick={() => onNavigate?.('order-detail')}
              >
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center gap-2">
                    <span className="text-white">{order.pair}</span>
                    <Badge className={order.type === 'Buy' ? 'bg-emerald-500/10 text-emerald-500' : 'bg-red-500/10 text-red-500'}>
                      {order.type}
                    </Badge>
                    <Badge variant="outline" className="border-gray-700">
                      {order.side}
                    </Badge>
                  </div>
                  <div className="flex items-center gap-2 text-sm text-gray-400">
                    <Clock className="w-4 h-4" />
                    {order.time}
                  </div>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <div className="text-gray-400">
                    {order.amount} @ {order.price}
                  </div>
                  <div className="flex items-center gap-3">
                    <Badge className={
                      order.status === 'Filled' ? 'bg-emerald-500/10 text-emerald-500' :
                      order.status === 'Partial' ? 'bg-yellow-500/10 text-yellow-500' :
                      'bg-gray-500/10 text-gray-500'
                    }>
                      {order.status}
                    </Badge>
                    <span className="text-emerald-500">{order.pnl}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      {/* Quick Actions */}
      <Card className="bg-gradient-to-r from-emerald-500/10 to-transparent border-emerald-500/20 p-6">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="mb-2">Ready to trade?</h3>
            <p className="text-gray-400">Execute market or limit orders instantly</p>
          </div>
          <div className="flex gap-3">
            <Button
              className="bg-emerald-500 text-black hover:bg-emerald-600"
              onClick={() => onNavigate?.('trade')}
            >
              Start Trading
            </Button>
            <Button
              variant="outline"
              className="border-gray-700 hover:bg-gray-800"
              onClick={() => onNavigate?.('watchlist')}
            >
              My Watchlist
            </Button>
          </div>
        </div>
      </Card>
    </div>
  );
}
