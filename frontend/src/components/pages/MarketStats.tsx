import { TrendingUp, TrendingDown, DollarSign, Activity } from 'lucide-react';
import { Card } from '../ui/card';
import { Badge } from '../ui/badge';
import { LineChart, Line, AreaChart, Area, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';

const marketOverview = [
  { label: 'Total Market Cap', value: '$2.1T', change: '+3.45%', isPositive: true },
  { label: '24h Volume', value: '$89.2B', change: '+12.3%', isPositive: true },
  { label: 'BTC Dominance', value: '47.3%', change: '-0.8%', isPositive: false },
  { label: 'Active Coins', value: '12,450', change: '+23', isPositive: true },
];

const topCoins = [
  { rank: 1, symbol: 'BTC', name: 'Bitcoin', price: '$50,729', change24h: 2.34, volume: '$28.4B', marketCap: '$994.2B' },
  { rank: 2, symbol: 'ETH', name: 'Ethereum', price: '$2,041', change24h: -1.23, volume: '$15.2B', marketCap: '$245.3B' },
  { rank: 3, symbol: 'SOL', name: 'Solana', price: '$103.37', change24h: 5.67, volume: '$2.1B', marketCap: '$47.8B' },
  { rank: 4, symbol: 'USDT', name: 'Tether', price: '$1.00', change24h: 0.01, volume: '$45.3B', marketCap: '$112.5B' },
  { rank: 5, symbol: 'BNB', name: 'BNB', price: '$312.45', change24h: 1.89, volume: '$1.8B', marketCap: '$48.2B' },
];

const marketCapData = [
  { time: '00:00', cap: 1950 },
  { time: '04:00', cap: 1920 },
  { time: '08:00', cap: 1980 },
  { time: '12:00', cap: 2050 },
  { time: '16:00', cap: 2080 },
  { time: '20:00', cap: 2040 },
  { time: '24:00', cap: 2100 },
];

const volumeByExchange = [
  { name: 'Binance', volume: 28400 },
  { name: 'Coinbase', volume: 15200 },
  { name: 'Kraken', volume: 8900 },
  { name: 'Bybit', volume: 12300 },
  { name: 'OKX', volume: 10500 },
  { name: 'Others', volume: 13700 },
];

const marketShare = [
  { name: 'Bitcoin', value: 47.3, color: '#f7931a' },
  { name: 'Ethereum', value: 18.2, color: '#627eea' },
  { name: 'Stablecoins', value: 15.8, color: '#26a17b' },
  { name: 'Others', value: 18.7, color: '#6b7280' },
];

const priceChangeDistribution = [
  { range: '-10% to -5%', count: 145 },
  { range: '-5% to 0%', count: 892 },
  { range: '0% to 5%', count: 1523 },
  { range: '5% to 10%', count: 678 },
  { range: '10%+', count: 234 },
];

export default function MarketStats() {
  return (
    <>
      {/* Market Overview */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        {marketOverview.map((stat, index) => (
          <Card key={index} className="bg-gray-900 border-gray-800 p-6">
            <div className="text-gray-400 text-sm mb-2">{stat.label}</div>
            <div className="text-3xl mb-2">{stat.value}</div>
            <div className={`flex items-center gap-1 text-sm ${stat.isPositive ? 'text-emerald-500' : 'text-red-500'}`}>
              {stat.isPositive ? <TrendingUp className="w-4 h-4" /> : <TrendingDown className="w-4 h-4" />}
              {stat.change}
            </div>
          </Card>
        ))}
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
        {/* Market Cap Chart */}
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="mb-6">
            <h3 className="text-lg mb-1">Total Market Cap (24h)</h3>
            <p className="text-gray-400 text-sm">Historical trend</p>
          </div>
          <ResponsiveContainer width="100%" height={250}>
            <AreaChart data={marketCapData}>
              <defs>
                <linearGradient id="capGradient" x1="0" y1="0" x2="0" y2="1">
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
              <Area type="monotone" dataKey="cap" stroke="#22c55e" strokeWidth={2} fill="url(#capGradient)" />
            </AreaChart>
          </ResponsiveContainer>
        </Card>

        {/* Volume by Exchange */}
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="mb-6">
            <h3 className="text-lg mb-1">Volume by Exchange</h3>
            <p className="text-gray-400 text-sm">Last 24 hours</p>
          </div>
          <ResponsiveContainer width="100%" height={250}>
            <BarChart data={volumeByExchange}>
              <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
              <XAxis dataKey="name" stroke="#6b7280" />
              <YAxis stroke="#6b7280" />
              <Tooltip 
                contentStyle={{ backgroundColor: '#1f2937', border: '1px solid #374151', borderRadius: '8px' }}
                labelStyle={{ color: '#9ca3af' }}
              />
              <Bar dataKey="volume" fill="#22c55e" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </Card>
      </div>

      {/* Market Share & Distribution */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
        {/* Market Share Pie Chart */}
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="mb-6">
            <h3 className="text-lg mb-1">Market Share</h3>
            <p className="text-gray-400 text-sm">By market capitalization</p>
          </div>
          <div className="flex items-center gap-8">
            <ResponsiveContainer width="50%" height={200}>
              <PieChart>
                <Pie
                  data={marketShare}
                  cx="50%"
                  cy="50%"
                  innerRadius={60}
                  outerRadius={80}
                  paddingAngle={2}
                  dataKey="value"
                >
                  {marketShare.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip 
                  contentStyle={{ backgroundColor: '#1f2937', border: '1px solid #374151', borderRadius: '8px' }}
                />
              </PieChart>
            </ResponsiveContainer>
            <div className="space-y-3">
              {marketShare.map((item, index) => (
                <div key={index} className="flex items-center gap-3">
                  <div className="w-3 h-3 rounded-full" style={{ backgroundColor: item.color }}></div>
                  <div>
                    <div className="text-sm">{item.name}</div>
                    <div className="text-xs text-gray-400">{item.value}%</div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </Card>

        {/* Price Change Distribution */}
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="mb-6">
            <h3 className="text-lg mb-1">Price Change Distribution</h3>
            <p className="text-gray-400 text-sm">24h price movements</p>
          </div>
          <ResponsiveContainer width="100%" height={200}>
            <BarChart data={priceChangeDistribution} layout="vertical">
              <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
              <XAxis type="number" stroke="#6b7280" />
              <YAxis dataKey="range" type="category" stroke="#6b7280" width={100} />
              <Tooltip 
                contentStyle={{ backgroundColor: '#1f2937', border: '1px solid #374151', borderRadius: '8px' }}
                labelStyle={{ color: '#9ca3af' }}
              />
              <Bar dataKey="count" fill="#3b82f6" radius={[0, 4, 4, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </Card>
      </div>

      {/* Top Coins Table */}
      <Card className="bg-gray-900 border-gray-800">
        <div className="p-6 border-b border-gray-800">
          <h3 className="text-lg">Top Cryptocurrencies</h3>
          <p className="text-gray-400 text-sm mt-1">By market capitalization</p>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="border-b border-gray-800">
              <tr className="text-gray-400 text-sm">
                <th className="text-left p-4">Rank</th>
                <th className="text-left p-4">Coin</th>
                <th className="text-left p-4">Price</th>
                <th className="text-left p-4">24h Change</th>
                <th className="text-left p-4">24h Volume</th>
                <th className="text-left p-4">Market Cap</th>
              </tr>
            </thead>
            <tbody>
              {topCoins.map((coin) => (
                <tr key={coin.rank} className="border-b border-gray-800 hover:bg-gray-800/50 transition-colors">
                  <td className="p-4">
                    <div className={`w-8 h-8 rounded-full flex items-center justify-center ${
                      coin.rank === 1 ? 'bg-yellow-500/20 text-yellow-500' :
                      coin.rank === 2 ? 'bg-gray-400/20 text-gray-400' :
                      coin.rank === 3 ? 'bg-orange-500/20 text-orange-500' :
                      'bg-gray-800 text-gray-400'
                    }`}>
                      {coin.rank}
                    </div>
                  </td>
                  <td className="p-4">
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 rounded-full bg-emerald-500/10 flex items-center justify-center">
                        <span className="text-xs font-semibold text-emerald-500">{coin.symbol}</span>
                      </div>
                      <div>
                        <div className="text-sm">{coin.name}</div>
                        <div className="text-xs text-gray-400">{coin.symbol}</div>
                      </div>
                    </div>
                  </td>
                  <td className="p-4">{coin.price}</td>
                  <td className="p-4">
                    <Badge className={coin.change24h >= 0 ? 'bg-emerald-500/10 text-emerald-500 border-0' : 'bg-red-500/10 text-red-500 border-0'}>
                      {coin.change24h >= 0 ? '+' : ''}{coin.change24h}%
                    </Badge>
                  </td>
                  <td className="p-4 text-gray-400">{coin.volume}</td>
                  <td className="p-4">{coin.marketCap}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>
    </>
  );
}
