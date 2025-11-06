import { useState, useEffect } from 'react';
import { TrendingUp, TrendingDown, ArrowUpRight, ArrowDownRight, DollarSign, Wallet, Activity, Clock, Loader2 } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Badge } from '../../ui/badge';
import { Alert, AlertDescription } from '../../ui/alert';
import { AreaChart, Area, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { DashboardApi, DashboardSummary, NavHistory, PnlHistory } from '../../../services/dashboard';

interface TraderDashboardProps {
  onNavigate?: (page: string) => void;
}

export default function TraderDashboard({ onNavigate }: TraderDashboardProps) {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [navHistory, setNavHistory] = useState<NavHistory | null>(null);
  const [pnlHistory, setPnlHistory] = useState<PnlHistory | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchDashboardData = async () => {
    setError(null);
    
    try {
      console.log('[Dashboard] Fetching dashboard data...');
      
      // Add timeout to requests
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 8000); // 8 second timeout
      
      // Fetch summary, NAV history, and PnL history in parallel with timeout
      const [summaryRes, navRes, pnlRes] = await Promise.all([
        DashboardApi.getSummary().catch(() => ({ ok: false, error: 'Network error', data: null })),
        DashboardApi.getNavHistory().catch(() => ({ ok: false, error: 'Network error', data: null })),
        DashboardApi.getPnlHistory('hourly').catch(() => ({ ok: false, error: 'Network error', data: null }))
      ]);
      
      clearTimeout(timeoutId);
      
      // Handle errors gracefully - show error but don't block the UI completely
      if (!summaryRes.ok && !navRes.ok && !pnlRes.ok) {
        console.warn('[Dashboard] All API calls failed - backend may be unavailable');
        setError(summaryRes.error || navRes.error || pnlRes.error || 'Backend service unavailable. Please ensure the backend server is running.');
        setLoading(false);
        return;
      }
      
      // Set data for successful calls, use defaults for failed ones
      if (summaryRes.ok) {
        setSummary(summaryRes.data);
      } else {
        console.warn('[Dashboard] Summary failed:', summaryRes.error);
        // Set default summary if API fails
        setSummary({
          totalBalance: 0,
          totalBalanceChange: 0,
          totalBalanceChangePercent: 0,
          todayPnl: 0,
          todayPnlPercent: 0,
          availableBalance: 0,
          availableBalancePercent: 0,
          openOrdersCount: 0,
          openOrdersBuy: 0,
          openOrdersSell: 0
        });
      }
      
      if (navRes.ok) {
        setNavHistory(navRes.data);
      } else {
        console.warn('[Dashboard] NAV history failed:', navRes.error);
        setNavHistory({ data: [] });
      }
      
      if (pnlRes.ok) {
        setPnlHistory(pnlRes.data);
      } else {
        console.warn('[Dashboard] PnL history failed:', pnlRes.error);
        setPnlHistory({ data: [] });
      }
      
      console.log('[Dashboard] Data loaded:', { summary: summaryRes.data, nav: navRes.data, pnl: pnlRes.data });
    } catch (e: any) {
      console.error('[Dashboard] Fetch error:', e);
      if (e.name === 'AbortError') {
        setError('Request timeout - backend server may be unavailable');
      } else {
        setError(e?.message || 'Failed to load dashboard data');
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDashboardData();
    
    // Refresh every 30 seconds (as recommended: 30-60s)
    const interval = setInterval(fetchDashboardData, 30000);
    return () => clearInterval(interval);
  }, []);

  const formatTimeAgo = (date: string) => {
    const now = new Date();
    const orderDate = new Date(date);
    const diffMs = now.getTime() - orderDate.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    return `${Math.floor(diffHours / 24)}d ago`;
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
  };

  const formatAmount = (value: number, decimals: number = 8) => {
    return new Intl.NumberFormat('en-US', { minimumFractionDigits: 0, maximumFractionDigits: decimals }).format(value);
  };

  if (loading && !summary) {
    return (
      <div className="p-4 lg:p-8 flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <Loader2 className="w-8 h-8 text-emerald-500 animate-spin mx-auto mb-4" />
          <p className="text-gray-400">Loading dashboard...</p>
        </div>
      </div>
    );
  }

  // Show error banner but still render dashboard with default/empty data
  // This allows users to see the UI even when backend is unavailable
  const hasData = summary && navHistory && pnlHistory;
  
  // Use default data if API failed but we still want to show the UI
  const displaySummary = summary || {
    totalBalance: 0,
    totalBalanceChange: 0,
    totalBalanceChangePercent: 0,
    todayPnl: 0,
    todayPnlPercent: 0,
    availableBalance: 0,
    availableBalancePercent: 0,
    openOrdersCount: 0,
    openOrdersBuy: 0,
    openOrdersSell: 0
  };
  
  const displayNavHistory = navHistory || { data: [] };
  const displayPnlHistory = pnlHistory || { data: [] };

  const navData = displayNavHistory.data.map(h => ({ date: h.date, value: h.value }));
  const pnlData = displayPnlHistory.data.map(h => ({ time: h.time, pnl: h.pnl }));

  return (
    <div className="p-4 lg:p-8 space-y-6">
      {/* Error Banner - Show at top but don't block UI */}
      {error && (
        <Alert className="bg-yellow-500/10 border-yellow-500/50 text-yellow-400 mb-4">
          <AlertDescription>
            <div className="flex items-center justify-between">
              <span>{error}</span>
              <Button 
                variant="outline" 
                size="sm" 
                onClick={fetchDashboardData}
                className="ml-4 border-yellow-500/50 text-yellow-400 hover:bg-yellow-500/20"
              >
                Retry
              </Button>
            </div>
          </AlertDescription>
        </Alert>
      )}
      {/* Quick Stats */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="text-gray-400">Total Balance (NAV)</div>
            <div className="w-10 h-10 bg-emerald-500/10 rounded-full flex items-center justify-center">
              <Wallet className="w-5 h-5 text-emerald-500" />
            </div>
          </div>
          <div className="text-3xl text-white mb-1">{formatCurrency(displaySummary.totalBalance)}</div>
          <div className={`flex items-center gap-1 text-sm ${displaySummary.totalBalanceChange >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
            {displaySummary.totalBalanceChange >= 0 ? <ArrowUpRight className="w-4 h-4" /> : <ArrowDownRight className="w-4 h-4" />}
            {displaySummary.totalBalanceChange >= 0 ? '+' : ''}{formatCurrency(displaySummary.totalBalanceChange)} ({displaySummary.totalBalanceChangePercent >= 0 ? '+' : ''}{displaySummary.totalBalanceChangePercent.toFixed(2)}%)
          </div>
        </Card>

        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="text-gray-400">Today's PnL</div>
            <div className="w-10 h-10 bg-emerald-500/10 rounded-full flex items-center justify-center">
              <TrendingUp className="w-5 h-5 text-emerald-500" />
            </div>
          </div>
          <div className={`text-3xl mb-1 ${displaySummary.todayPnl >= 0 ? 'text-white' : 'text-red-500'}`}>
            {displaySummary.todayPnl >= 0 ? '+' : ''}{formatCurrency(displaySummary.todayPnl)}
          </div>
          <div className={`flex items-center gap-1 text-sm ${displaySummary.todayPnlPercent >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
            {displaySummary.todayPnlPercent >= 0 ? <ArrowUpRight className="w-4 h-4" /> : <ArrowDownRight className="w-4 h-4" />}
            {displaySummary.todayPnlPercent >= 0 ? '+' : ''}{displaySummary.todayPnlPercent.toFixed(2)}%
          </div>
        </Card>

        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="text-gray-400">Available Balance</div>
            <div className="w-10 h-10 bg-blue-500/10 rounded-full flex items-center justify-center">
              <DollarSign className="w-5 h-5 text-blue-500" />
            </div>
          </div>
          <div className="text-3xl text-white mb-1">{formatCurrency(displaySummary.availableBalance)}</div>
          <div className="text-gray-400 text-sm">{displaySummary.availableBalancePercent.toFixed(1)}% of total</div>
        </Card>

        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="text-gray-400">Open Orders</div>
            <div className="w-10 h-10 bg-yellow-500/10 rounded-full flex items-center justify-center">
              <Activity className="w-5 h-5 text-yellow-500" />
            </div>
          </div>
          <div className="text-3xl text-white mb-1">{displaySummary.openOrdersCount}</div>
          <div className="text-gray-400 text-sm">{displaySummary.openOrdersBuy} Buy, {displaySummary.openOrdersSell} Sell</div>
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
        {/* Holdings - Placeholder (can be added later with separate API) */}
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
            <div className="text-gray-400 text-sm text-center py-8">
              Holdings data will be available soon
            </div>
          </div>
        </Card>

        {/* Recent Orders - Placeholder (can be added later with separate API) */}
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
            <div className="text-gray-400 text-sm text-center py-8">
              Recent orders data will be available soon
            </div>
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
