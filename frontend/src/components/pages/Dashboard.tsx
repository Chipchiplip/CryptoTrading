import { TrendingUp, TrendingDown, Users, DollarSign, Activity, ArrowUpRight, ArrowDownRight } from 'lucide-react';
import { Card } from '../ui/card';
import { Button } from '../ui/button';
import { LineChart, Line, AreaChart, Area, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

const tradingVolumeData = [
  { time: '00:00', volume: 2400000 },
  { time: '04:00', volume: 1800000 },
  { time: '08:00', volume: 3200000 },
  { time: '12:00', volume: 4100000 },
  { time: '16:00', volume: 3800000 },
  { time: '20:00', volume: 2900000 },
  { time: '24:00', volume: 3500000 },
];

const userActivityData = [
  { day: 'Mon', active: 12400, new: 2400 },
  { day: 'Tue', active: 13900, new: 2100 },
  { day: 'Wed', active: 15200, new: 2800 },
  { day: 'Thu', active: 14100, new: 2200 },
  { day: 'Fri', active: 16800, new: 3100 },
  { day: 'Sat', active: 14200, new: 2600 },
  { day: 'Sun', active: 13500, new: 2300 },
];

const revenueData = [
  { month: 'Jan', revenue: 420000 },
  { month: 'Feb', revenue: 380000 },
  { month: 'Mar', revenue: 510000 },
  { month: 'Apr', revenue: 580000 },
  { month: 'May', revenue: 620000 },
  { month: 'Jun', revenue: 690000 },
];

const recentTransactions = [
  { id: 1, user: 'John Doe', type: 'Buy', coin: 'BTC', amount: '$45,230', volume: '0.892 BTC', status: 'completed', time: '2m ago' },
  { id: 2, user: 'Sarah Chen', type: 'Sell', coin: 'ETH', amount: '$12,450', volume: '5.2 ETH', status: 'completed', time: '5m ago' },
  { id: 3, user: 'Mike Johnson', type: 'Buy', coin: 'SOL', amount: '$8,920', volume: '89.2 SOL', status: 'pending', time: '8m ago' },
  { id: 4, user: 'Emma Wilson', type: 'Buy', coin: 'BTC', amount: '$23,100', volume: '0.456 BTC', status: 'completed', time: '12m ago' },
  { id: 5, user: 'Alex Rivera', type: 'Sell', coin: 'USDT', amount: '$15,600', volume: '15,600 USDT', status: 'completed', time: '15m ago' },
];

const topTraders = [
  { rank: 1, name: 'CryptoWhale', profit: '+$128,450', trades: 342, winRate: '78%' },
  { rank: 2, name: 'MoonShot', profit: '+$98,230', trades: 287, winRate: '72%' },
  { rank: 3, name: 'DiamondHands', profit: '+$87,920', trades: 198, winRate: '81%' },
  { rank: 4, name: 'BullRunner', profit: '+$76,540', trades: 256, winRate: '69%' },
  { rank: 5, name: 'HodlMaster', profit: '+$65,890', trades: 167, winRate: '75%' },
];

export default function Dashboard() {
  return (
    <>
      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="p-3 bg-emerald-500/10 rounded-lg">
              <Users className="w-6 h-6 text-emerald-500" />
            </div>
            <div className="flex items-center gap-1 text-emerald-500 text-sm">
              <ArrowUpRight className="w-4 h-4" />
              <span>+12.5%</span>
            </div>
          </div>
          <div className="text-3xl mb-1">24,382</div>
          <div className="text-gray-400 text-sm">Total Users</div>
        </Card>

        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="p-3 bg-blue-500/10 rounded-lg">
              <TrendingUp className="w-6 h-6 text-blue-500" />
            </div>
            <div className="flex items-center gap-1 text-emerald-500 text-sm">
              <ArrowUpRight className="w-4 h-4" />
              <span>+8.2%</span>
            </div>
          </div>
          <div className="text-3xl mb-1">$3.2M</div>
          <div className="text-gray-400 text-sm">24h Trading Volume</div>
        </Card>

        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="p-3 bg-purple-500/10 rounded-lg">
              <Activity className="w-6 h-6 text-purple-500" />
            </div>
            <div className="flex items-center gap-1 text-emerald-500 text-sm">
              <ArrowUpRight className="w-4 h-4" />
              <span>+15.3%</span>
            </div>
          </div>
          <div className="text-3xl mb-1">8,942</div>
          <div className="text-gray-400 text-sm">Active Traders</div>
        </Card>

        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="p-3 bg-yellow-500/10 rounded-lg">
              <DollarSign className="w-6 h-6 text-yellow-500" />
            </div>
            <div className="flex items-center gap-1 text-red-500 text-sm">
              <ArrowDownRight className="w-4 h-4" />
              <span>-2.4%</span>
            </div>
          </div>
          <div className="text-3xl mb-1">$124K</div>
          <div className="text-gray-400 text-sm">Revenue (24h)</div>
        </Card>
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
        {/* Trading Volume Chart */}
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h3 className="text-lg mb-1">Trading Volume</h3>
              <p className="text-gray-400 text-sm">Last 24 hours</p>
            </div>
            <div className="flex gap-2">
              <Button size="sm" variant="ghost" className="text-xs">24H</Button>
              <Button size="sm" variant="ghost" className="text-xs bg-emerald-500/10 text-emerald-500">7D</Button>
              <Button size="sm" variant="ghost" className="text-xs">1M</Button>
            </div>
          </div>
          <ResponsiveContainer width="100%" height={250}>
            <AreaChart data={tradingVolumeData}>
              <defs>
                <linearGradient id="volumeGradient" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#22c55e" stopOpacity={0.3}/>
                  <stop offset="95%" stopColor="#22c55e" stopOpacity={0}/>
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
              <XAxis dataKey="time" stroke="#6b7280" />
              <YAxis stroke="#6b7280" />
              <Tooltip 
                contentStyle={{ backgroundColor: '#1f2937', border: '1px solid #374151', borderRadius: '8px' }}
                labelStyle={{ color: '#9ca3af' }}
              />
              <Area type="monotone" dataKey="volume" stroke="#22c55e" strokeWidth={2} fill="url(#volumeGradient)" />
            </AreaChart>
          </ResponsiveContainer>
        </Card>

        {/* User Activity Chart */}
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h3 className="text-lg mb-1">User Activity</h3>
              <p className="text-gray-400 text-sm">Weekly overview</p>
            </div>
          </div>
          <ResponsiveContainer width="100%" height={250}>
            <BarChart data={userActivityData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
              <XAxis dataKey="day" stroke="#6b7280" />
              <YAxis stroke="#6b7280" />
              <Tooltip 
                contentStyle={{ backgroundColor: '#1f2937', border: '1px solid #374151', borderRadius: '8px' }}
                labelStyle={{ color: '#9ca3af' }}
              />
              <Bar dataKey="active" fill="#22c55e" radius={[4, 4, 0, 0]} />
              <Bar dataKey="new" fill="#3b82f6" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </Card>
      </div>

      {/* Revenue Chart */}
      <div className="mb-8">
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h3 className="text-lg mb-1">Revenue Trend</h3>
              <p className="text-gray-400 text-sm">Monthly performance</p>
            </div>
            <div className="text-right">
              <div className="text-2xl text-emerald-500">$690,000</div>
              <div className="text-gray-400 text-sm">This month</div>
            </div>
          </div>
          <ResponsiveContainer width="100%" height={200}>
            <LineChart data={revenueData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
              <XAxis dataKey="month" stroke="#6b7280" />
              <YAxis stroke="#6b7280" />
              <Tooltip 
                contentStyle={{ backgroundColor: '#1f2937', border: '1px solid #374151', borderRadius: '8px' }}
                labelStyle={{ color: '#9ca3af' }}
              />
              <Line type="monotone" dataKey="revenue" stroke="#22c55e" strokeWidth={3} dot={{ fill: '#22c55e', r: 6 }} />
            </LineChart>
          </ResponsiveContainer>
        </Card>
      </div>

      {/* Tables */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Recent Transactions */}
        <Card className="bg-gray-900 border-gray-800">
          <div className="p-6 border-b border-gray-800">
            <h3 className="text-lg">Recent Transactions</h3>
            <p className="text-gray-400 text-sm mt-1">Latest trading activity</p>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="border-b border-gray-800">
                <tr className="text-gray-400 text-sm">
                  <th className="text-left p-4">User</th>
                  <th className="text-left p-4">Type</th>
                  <th className="text-left p-4">Amount</th>
                  <th className="text-left p-4">Status</th>
                </tr>
              </thead>
              <tbody>
                {recentTransactions.map((tx) => (
                  <tr key={tx.id} className="border-b border-gray-800 hover:bg-gray-800/50 transition-colors">
                    <td className="p-4">
                      <div>
                        <div className="text-sm">{tx.user}</div>
                        <div className="text-xs text-gray-400">{tx.time}</div>
                      </div>
                    </td>
                    <td className="p-4">
                      <span className={`inline-flex items-center gap-1 px-2 py-1 rounded text-xs ${
                        tx.type === 'Buy' ? 'bg-emerald-500/10 text-emerald-500' : 'bg-red-500/10 text-red-500'
                      }`}>
                        {tx.type === 'Buy' ? <TrendingUp className="w-3 h-3" /> : <TrendingDown className="w-3 h-3" />}
                        {tx.type} {tx.coin}
                      </span>
                    </td>
                    <td className="p-4">
                      <div>
                        <div className="text-sm">{tx.amount}</div>
                        <div className="text-xs text-gray-400">{tx.volume}</div>
                      </div>
                    </td>
                    <td className="p-4">
                      <span className={`px-2 py-1 rounded text-xs ${
                        tx.status === 'completed' ? 'bg-emerald-500/10 text-emerald-500' : 'bg-yellow-500/10 text-yellow-500'
                      }`}>
                        {tx.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>

        {/* Top Traders */}
        <Card className="bg-gray-900 border-gray-800">
          <div className="p-6 border-b border-gray-800">
            <h3 className="text-lg">Top Traders</h3>
            <p className="text-gray-400 text-sm mt-1">This week's leaderboard</p>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="border-b border-gray-800">
                <tr className="text-gray-400 text-sm">
                  <th className="text-left p-4">Rank</th>
                  <th className="text-left p-4">Trader</th>
                  <th className="text-left p-4">Profit</th>
                  <th className="text-left p-4">Win Rate</th>
                </tr>
              </thead>
              <tbody>
                {topTraders.map((trader) => (
                  <tr key={trader.rank} className="border-b border-gray-800 hover:bg-gray-800/50 transition-colors">
                    <td className="p-4">
                      <div className={`w-8 h-8 rounded-full flex items-center justify-center ${
                        trader.rank === 1 ? 'bg-yellow-500/20 text-yellow-500' :
                        trader.rank === 2 ? 'bg-gray-400/20 text-gray-400' :
                        trader.rank === 3 ? 'bg-orange-500/20 text-orange-500' :
                        'bg-gray-800 text-gray-400'
                      }`}>
                        {trader.rank}
                      </div>
                    </td>
                    <td className="p-4">
                      <div>
                        <div className="text-sm">{trader.name}</div>
                        <div className="text-xs text-gray-400">{trader.trades} trades</div>
                      </div>
                    </td>
                    <td className="p-4">
                      <span className="text-emerald-500 text-sm">{trader.profit}</span>
                    </td>
                    <td className="p-4">
                      <span className="text-sm">{trader.winRate}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      </div>
    </>
  );
}
