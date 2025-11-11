import { useState, useEffect } from 'react';
import { TrendingUp, TrendingDown, DollarSign, Loader2, AlertCircle } from 'lucide-react';
import { Card } from '../../ui/card';
import { Badge } from '../../ui/badge';
import { Alert, AlertDescription } from '../../ui/alert';
import { PieChart, Pie, Cell, ResponsiveContainer, AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip } from 'recharts';
import { PortfolioApi, PortfolioHolding } from '../../../api/portfolio';

export default function Portfolio() {
  const [holdings, setHoldings] = useState<PortfolioHolding[]>([]);
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

      console.log('[Portfolio] Fetching portfolio overview...');
      // Fetch portfolio overview from unified API
      const overviewRes = await PortfolioApi.getPortfolioOverview();

      // Check if API call was successful
      if (!overviewRes.ok) {
        console.error('[Portfolio] API call failed:', overviewRes.error);
        throw new Error(overviewRes.error || 'Failed to load portfolio data');
      }

      const data = overviewRes.data;
      console.log('[Portfolio] Data received:', {
        totalValue: data.totalValue,
        holdingsCount: data.holdings?.length || 0,
        navHistoryCount: data.navHistory?.length || 0
      });

      // Set portfolio summary
      setPortfolio({
        totalValue: data.totalValue,
        totalCost: data.totalCost,
        unrealizedPnL: data.unrealizedPnL,
        unrealizedPnLPercent: data.unrealizedPnLPercent,
        realizedPnL: data.realizedPnL
      });

      // Set holdings
      setHoldings(data.holdings || []);
      console.log('[Portfolio] Holdings set:', data.holdings?.length || 0);

      // Process NAV history for performance chart
      if (data.navHistory && data.navHistory.length > 0) {
        console.log('[Portfolio] Processing NAV history:', data.navHistory.length, 'items');
        const navData = data.navHistory.map(item => {
          try {
            // Handle both ISO string and Date object
            const dateValue = typeof item.date === 'string' ? item.date : item.date;
            const parsedDate = new Date(dateValue);
            
            if (isNaN(parsedDate.getTime())) {
              console.warn('[Portfolio] Invalid date:', item.date);
              return null;
            }
            
            return {
              date: parsedDate.toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
              value: item.value
            };
          } catch (e) {
            console.error('[Portfolio] Error parsing date:', item.date, e);
            return null;
          }
        }).filter(item => item !== null) as Array<{ date: string; value: number }>;
        
        console.log('[Portfolio] Processed NAV data:', navData.length, 'items');
        setPerformanceData(navData);
      } else {
        console.warn('[Portfolio] No NAV history data');
        setPerformanceData([]);
      }
      
      console.log('[Portfolio] Data processing complete:', {
        portfolio: portfolio,
        holdingsCount: holdings.length,
        performanceDataCount: performanceData.length
      });
    } catch (err: any) {
      console.error('[Portfolio] Error fetching portfolio data:', err);
      setError(err.message || 'Failed to load portfolio data');
      
      // Don't clear data completely - keep existing data if available
      // This allows partial data to still be displayed
      if (holdings.length === 0) {
        setHoldings([]);
      }
      if (performanceData.length === 0) {
        setPerformanceData([]);
      }
      // Only reset portfolio if we don't have any data
      if (portfolio.totalValue === 0) {
        setPortfolio({
          totalValue: 0,
          totalCost: 0,
          unrealizedPnL: 0,
          unrealizedPnLPercent: 0,
          realizedPnL: 0
        });
      }
    } finally {
      setLoading(false);
      console.log('[Portfolio] Loading complete');
    }
  };

  const allocationData = holdings.map(h => ({
    name: h.symbol,
    value: h.value,
    percentage: h.allocation
  }));

  const COLORS = ['#22c55e', '#3b82f6', '#f59e0b', '#6b7280', '#ef4444', '#8b5cf6', '#ec4899', '#14b8a6'];

  // Show loading only on initial load, not on refresh
  if (loading && holdings.length === 0 && portfolio.totalValue === 0) {
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
                {allocationData.map((_, index) => (
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
