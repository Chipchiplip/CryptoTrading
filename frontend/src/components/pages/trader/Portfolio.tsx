import { useState, useEffect } from 'react';
import { TrendingUp, TrendingDown, DollarSign, PieChart as PieChartIcon, Loader2, AlertCircle } from 'lucide-react';
import { Card } from '../../ui/card';
import { Badge } from '../../ui/badge';
import { Alert, AlertDescription } from '../../ui/alert';
import { PieChart, Pie, Cell, ResponsiveContainer, AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip } from 'recharts';
import { TradingApi } from '../../../api/trading';
import { MarketApi } from '../../../api/market';
import { DashboardApi } from '../../../services/dashboard';

interface Holding {
  symbol: string;
  name: string;
  amount: number;
  avgPrice: number;
  currentPrice: number;
  value: number;
  pnl: number;
  pnlPercent: number;
  allocation: number;
  cost: number;
}

export default function Portfolio() {
  const [holdings, setHoldings] = useState<Holding[]>([]);
  const [performanceData, setPerformanceData] = useState<Array<{ date: string; value: number }>>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  const [portfolio, setPortfolio] = useState({
    totalValue: 0,
    totalCost: 0,
    unrealizedPnL: 0,
    unrealizedPnLPercent: 0,
    realizedPnL: 0,
  });

  useEffect(() => {
    fetchPortfolioData();
  }, []);

  const fetchPortfolioData = async () => {
    try {
      setLoading(true);
      setError(null);

      // Fetch all data in parallel
      const [holdingsRes, navHistoryRes, balancesRes, tradesRes, cryptosRes] = await Promise.all([
        TradingApi.getHoldings().catch(err => ({ ok: false, error: err.message })),
        DashboardApi.getNavHistory().catch(err => ({ ok: false, error: err.message })),
        TradingApi.getBalances().catch(err => ({ ok: false, error: err.message })),
        TradingApi.getTrades({ page: 1, pageSize: 100 }).catch(err => ({ ok: false, error: err.message })),
        MarketApi.getCryptocurrencies().catch(err => ({ ok: false, error: err.message }))
      ]);

      // Process NAV history for performance chart
      if (navHistoryRes.ok && navHistoryRes.data) {
        const navDataArray = navHistoryRes.data.data || navHistoryRes.data;
        const navData = (Array.isArray(navDataArray) ? navDataArray : []).map((item: any) => ({
          date: new Date(item.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
          value: item.value
        }));
        setPerformanceData(navData);
      }

      // Get crypto prices map
      const cryptoPriceMap = new Map<string, number>();
      if (cryptosRes.ok && cryptosRes.data) {
        cryptosRes.data.forEach(crypto => {
          cryptoPriceMap.set(crypto.symbol.toUpperCase(), crypto.currentPrice);
        });
      }

      // Calculate realized PnL from trades
      let realizedPnL = 0;
      if (tradesRes.ok && tradesRes.data) {
        const tradesArray = Array.isArray(tradesRes.data) ? tradesRes.data : (tradesRes.data.data || []);
        tradesArray.forEach((trade: any) => {
          if (trade.side === 'SELL') {
            realizedPnL += (trade.priceUsd * trade.quantityCoin) - trade.feeUsd;
          } else {
            realizedPnL -= (trade.priceUsd * trade.quantityCoin) + trade.feeUsd;
          }
        });
      }

      // Process holdings
      if (holdingsRes.ok && holdingsRes.data && balancesRes.ok && balancesRes.data) {
        const walletMap = new Map(balancesRes.data.wallets.map(w => [w.symbol, w]));
        
        // Calculate cost basis from trades
        const costBasisMap = new Map<string, { totalCost: number; totalAmount: number }>();
        if (tradesRes.ok && tradesRes.data) {
          const tradesArray = Array.isArray(tradesRes.data) ? tradesRes.data : (tradesRes.data.data || []);
          tradesArray.forEach((trade: any) => {
            if (trade.side === 'BUY') {
              const existing = costBasisMap.get(trade.symbol) || { totalCost: 0, totalAmount: 0 };
              costBasisMap.set(trade.symbol, {
                totalCost: existing.totalCost + (trade.priceUsd * trade.quantityCoin + trade.feeUsd),
                totalAmount: existing.totalAmount + trade.quantityCoin
              });
            }
          });
        }

        let totalValue = 0;
        let totalCost = 0;

        const processedHoldings: Holding[] = holdingsRes.data
          .filter(h => h.amount > 0)
          .map(holding => {
            const currentPrice = cryptoPriceMap.get(holding.symbol.toUpperCase()) || 0;
            const value = holding.amount * currentPrice;
            
            // Get cost basis
            const costBasis = costBasisMap.get(holding.symbol.toUpperCase()) || { totalCost: 0, totalAmount: holding.amount };
            const avgPrice = costBasis.totalAmount > 0 ? costBasis.totalCost / costBasis.totalAmount : currentPrice;
            const cost = holding.amount * avgPrice;
            
            const pnl = value - cost;
            const pnlPercent = cost > 0 ? (pnl / cost) * 100 : 0;

            totalValue += value;
            totalCost += cost;

            return {
              symbol: holding.symbol.toUpperCase(),
              name: holding.name,
              amount: holding.amount,
              avgPrice,
              currentPrice,
              value,
              pnl,
              pnlPercent,
              allocation: 0, // Will calculate after
              cost
            };
          });

        // Calculate allocations
        processedHoldings.forEach(h => {
          h.allocation = totalValue > 0 ? (h.value / totalValue) * 100 : 0;
        });

        setHoldings(processedHoldings);

        const unrealizedPnL = totalValue - totalCost;
        const unrealizedPnLPercent = totalCost > 0 ? (unrealizedPnL / totalCost) * 100 : 0;

        setPortfolio({
          totalValue,
          totalCost,
          unrealizedPnL,
          unrealizedPnLPercent,
          realizedPnL
        });
      }
    } catch (err: any) {
      console.error('Error fetching portfolio data:', err);
      setError(err.message || 'Failed to load portfolio data');
      
      // Set empty data on error to prevent blank screen
      setHoldings([]);
      setPerformanceData([]);
      setPortfolio({
        totalValue: 0,
        totalCost: 0,
        unrealizedPnL: 0,
        unrealizedPnLPercent: 0,
        realizedPnL: 0
      });
    } finally {
      setLoading(false);
    }
  };

  const allocationData = holdings.map(h => ({
    name: h.symbol,
    value: h.value,
    percentage: h.allocation
  }));

  const COLORS = ['#22c55e', '#3b82f6', '#f59e0b', '#6b7280', '#ef4444', '#8b5cf6', '#ec4899', '#14b8a6'];

  if (loading) {
    return (
      <div className="p-4 lg:p-8">
        <div className="flex items-center justify-center h-96">
          <div className="text-center">
            <Loader2 className="w-12 h-12 text-emerald-500 animate-spin mx-auto mb-4" />
            <p className="text-gray-400">Loading portfolio data...</p>
          </div>
        </div>
      </div>
    );
  }

  // Don't show error as full-screen block, show it as banner instead
  // This allows partial data to still be displayed

  return (
    <div className="p-4 lg:p-8">
      <div className="mb-6">
        <h1 className="text-3xl mb-2">Portfolio</h1>
        <p className="text-gray-400">Track your assets and performance</p>
      </div>

      {/* Show error banner if there was an error */}
      {error && (
        <Alert className="bg-red-500/10 border-red-500/50 mb-6">
          <AlertCircle className="w-4 h-4 text-red-500" />
          <AlertDescription className="text-red-500">
            {error}
          </AlertDescription>
        </Alert>
      )}

      {/* Portfolio Summary */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="text-gray-400">Total Value</div>
            <DollarSign className="w-5 h-5 text-emerald-500" />
          </div>
          <div className="text-3xl text-white mb-1">${portfolio.totalValue.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</div>
          <div className="flex items-center gap-1 text-emerald-500 text-sm">
            <TrendingUp className="w-4 h-4" />
            +{portfolio.unrealizedPnLPercent.toFixed(2)}%
          </div>
        </Card>

        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 mb-4">Total Cost</div>
          <div className="text-3xl text-white mb-1">${portfolio.totalCost.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</div>
          <div className="text-gray-400 text-sm">Initial investment</div>
        </Card>

        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 mb-4">Unrealized PnL</div>
          <div className={`text-3xl mb-1 ${portfolio.unrealizedPnL >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
            {portfolio.unrealizedPnL >= 0 ? '+' : ''}${portfolio.unrealizedPnL.toFixed(2)}
          </div>
          <div className={`text-sm ${portfolio.unrealizedPnLPercent >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
            {portfolio.unrealizedPnLPercent >= 0 ? '+' : ''}{portfolio.unrealizedPnLPercent.toFixed(2)}%
          </div>
        </Card>

        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 mb-4">Realized PnL</div>
          <div className={`text-3xl mb-1 ${portfolio.realizedPnL >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
            {portfolio.realizedPnL >= 0 ? '+' : ''}${portfolio.realizedPnL.toFixed(2)}
          </div>
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
                  <td className="py-4 text-right text-white">{holding.amount.toFixed(6)}</td>
                  <td className="py-4 text-right text-gray-300">${holding.avgPrice.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                  <td className="py-4 text-right text-white">${holding.currentPrice.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                  <td className="py-4 text-right text-white">${holding.value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
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
