import { TrendingUp, TrendingDown, DollarSign, PieChart as PieChartIcon } from 'lucide-react';
import { Card } from '../../ui/card';
import { Badge } from '../../ui/badge';
import { PieChart, Pie, Cell, ResponsiveContainer, AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip } from 'recharts';

export default function Portfolio() {
  const portfolio = {
    totalValue: 12458.32,
    totalCost: 12000.00,
    unrealizedPnL: 458.32,
    unrealizedPnLPercent: 3.82,
    realizedPnL: 234.12,
  };

  const holdings = [
    { symbol: 'BTC', name: 'Bitcoin', amount: 0.2341, avgPrice: 48500, currentPrice: 50234.56, value: 11759.82, pnl: 405.98, pnlPercent: 3.57, allocation: 37.2 },
    { symbol: 'ETH', name: 'Ethereum', amount: 4.5678, avgPrice: 2750, currentPrice: 2845.32, value: 12993.50, pnl: 435.37, pnlPercent: 3.47, allocation: 41.1 },
    { symbol: 'SOL', name: 'Solana', amount: 125.34, avgPrice: 99.20, currentPrice: 98.45, value: 12339.73, pnl: -94.00, pnlPercent: -0.76, allocation: 39.0 },
    { symbol: 'USDT', name: 'Tether', amount: 5234.56, avgPrice: 1.00, currentPrice: 1.00, value: 5234.56, pnl: 0, pnlPercent: 0, allocation: 16.6 },
  ];

  const performanceData = [
    { date: 'Jan 1', value: 10000 },
    { date: 'Jan 8', value: 10500 },
    { date: 'Jan 15', value: 10200 },
    { date: 'Jan 22', value: 11000 },
    { date: 'Jan 29', value: 11800 },
    { date: 'Feb 5', value: 12100 },
    { date: 'Feb 12', value: 12458 },
  ];

  const allocationData = holdings.map(h => ({
    name: h.symbol,
    value: h.value,
    percentage: h.allocation
  }));

  const COLORS = ['#22c55e', '#3b82f6', '#f59e0b', '#6b7280'];

  return (
    <div className="p-4 lg:p-8">
      <div className="mb-6">
        <h1 className="text-3xl mb-2">Portfolio</h1>
        <p className="text-gray-400">Track your assets and performance</p>
      </div>

      {/* Portfolio Summary */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="text-gray-400">Total Value</div>
            <DollarSign className="w-5 h-5 text-emerald-500" />
          </div>
          <div className="text-3xl text-white mb-1">${portfolio.totalValue.toLocaleString()}</div>
          <div className="flex items-center gap-1 text-emerald-500 text-sm">
            <TrendingUp className="w-4 h-4" />
            +{portfolio.unrealizedPnLPercent.toFixed(2)}%
          </div>
        </Card>

        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 mb-4">Total Cost</div>
          <div className="text-3xl text-white mb-1">${portfolio.totalCost.toLocaleString()}</div>
          <div className="text-gray-400 text-sm">Initial investment</div>
        </Card>

        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 mb-4">Unrealized PnL</div>
          <div className="text-3xl text-emerald-500 mb-1">+${portfolio.unrealizedPnL.toFixed(2)}</div>
          <div className="text-emerald-500 text-sm">+{portfolio.unrealizedPnLPercent.toFixed(2)}%</div>
        </Card>

        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 mb-4">Realized PnL</div>
          <div className="text-3xl text-emerald-500 mb-1">+${portfolio.realizedPnL.toFixed(2)}</div>
          <div className="text-gray-400 text-sm">From closed trades</div>
        </Card>
      </div>

      <div className="grid lg:grid-cols-3 gap-6 mb-6">
        {/* Performance Chart */}
        <Card className="lg:col-span-2 bg-gray-900 border-gray-800 p-6">
          <h2 className="text-xl mb-6">Portfolio Performance</h2>
          <ResponsiveContainer width="100%" height={300}>
            <AreaChart data={performanceData}>
              <defs>
                <linearGradient id="portfolioGradient" x1="0" y1="0" x2="0" y2="1">
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
              <Area type="monotone" dataKey="value" stroke="#22c55e" fillOpacity={1} fill="url(#portfolioGradient)" />
            </AreaChart>
          </ResponsiveContainer>
        </Card>

        {/* Asset Allocation */}
        <Card className="bg-gray-900 border-gray-800 p-6">
          <h2 className="text-xl mb-6">Asset Allocation</h2>
          <ResponsiveContainer width="100%" height={300}>
            <PieChart>
              <Pie
                data={allocationData}
                cx="50%"
                cy="50%"
                innerRadius={60}
                outerRadius={100}
                fill="#8884d8"
                paddingAngle={2}
                dataKey="value"
              >
                {allocationData.map((entry, index) => (
                  <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip 
                contentStyle={{ backgroundColor: '#1f2937', border: '1px solid #374151', borderRadius: '8px' }}
                formatter={(value: number) => `$${value.toFixed(2)}`}
              />
            </PieChart>
          </ResponsiveContainer>
          <div className="mt-4 space-y-2">
            {allocationData.map((item, index) => (
              <div key={item.name} className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-full" style={{ backgroundColor: COLORS[index % COLORS.length] }}></div>
                  <span className="text-gray-300">{item.name}</span>
                </div>
                <span className="text-white">{item.percentage.toFixed(1)}%</span>
              </div>
            ))}
          </div>
        </Card>
      </div>

      {/* Holdings Table */}
      <Card className="bg-gray-900 border-gray-800 p-6">
        <h2 className="text-xl mb-6">Holdings</h2>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="text-left text-gray-400 text-sm border-b border-gray-800">
                <th className="pb-3">Asset</th>
                <th className="pb-3 text-right">Amount</th>
                <th className="pb-3 text-right">Avg Price</th>
                <th className="pb-3 text-right">Current Price</th>
                <th className="pb-3 text-right">Value</th>
                <th className="pb-3 text-right">PnL</th>
                <th className="pb-3 text-right">PnL %</th>
                <th className="pb-3 text-right">Allocation</th>
              </tr>
            </thead>
            <tbody>
              {holdings.map((holding) => (
                <tr key={holding.symbol} className="border-b border-gray-800 hover:bg-gray-800/50">
                  <td className="py-4">
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 bg-emerald-500/10 rounded-full flex items-center justify-center">
                        <span className="text-emerald-500 text-sm">{holding.symbol}</span>
                      </div>
                      <div>
                        <div className="text-white">{holding.name}</div>
                        <div className="text-sm text-gray-400">{holding.symbol}</div>
                      </div>
                    </div>
                  </td>
                  <td className="py-4 text-right text-white">{holding.amount.toFixed(4)}</td>
                  <td className="py-4 text-right text-gray-300">${holding.avgPrice.toLocaleString()}</td>
                  <td className="py-4 text-right text-white">${holding.currentPrice.toLocaleString()}</td>
                  <td className="py-4 text-right text-white">${holding.value.toLocaleString()}</td>
                  <td className="py-4 text-right">
                    <div className={`flex items-center justify-end gap-1 ${holding.pnl >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
                      {holding.pnl >= 0 ? <TrendingUp className="w-4 h-4" /> : <TrendingDown className="w-4 h-4" />}
                      {holding.pnl >= 0 ? '+' : ''}${Math.abs(holding.pnl).toFixed(2)}
                    </div>
                  </td>
                  <td className="py-4 text-right">
                    <Badge className={holding.pnlPercent >= 0 ? 'bg-emerald-500/10 text-emerald-500' : 'bg-red-500/10 text-red-500'}>
                      {holding.pnlPercent >= 0 ? '+' : ''}{holding.pnlPercent.toFixed(2)}%
                    </Badge>
                  </td>
                  <td className="py-4 text-right text-gray-300">{holding.allocation.toFixed(1)}%</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
}
