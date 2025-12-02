import { useState, useEffect } from 'react';
import { TrendingUp, ArrowUpRight, ArrowDownRight, DollarSign, Wallet, Activity, Clock, Loader2 } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Alert, AlertDescription } from '../../ui/alert';
import { DashboardApi, NavHistory, PnlHistory } from '../../../services/dashboard';
import { TradingApi } from '../../../api/trading';
import { useDashboardSummary } from '../../../contexts/DashboardContext';
import { NavChart } from '../../charts/NavChart';
import { TodayPnlChart } from '../../charts/TodayPnlChart';
import { ChartCard } from '../../charts/ChartCard';

interface TraderDashboardProps {
    onNavigate?: (page: string, orderId?: string) => void;
}

interface Holding {
    symbol: string;
    name: string;
    amount: number;
    valueUsd: number;
    change24h: number;
}

interface Order {
    id: string;
    symbol: string;
    type: 'LIMIT' | 'MARKET';
    side: 'BUY' | 'SELL';
    price: number;
    quantity: number;
    status: string;
    createdAt: string;
}

export default function TraderDashboard({ onNavigate }: TraderDashboardProps) {
    const { summary, error: summaryError } = useDashboardSummary();
    const [navHistory, setNavHistory] = useState<NavHistory | null>(null);
    const [pnlHistory, setPnlHistory] = useState<PnlHistory | null>(null);
    const [holdings, setHoldings] = useState<Holding[]>([]);
    const [recentOrders, setRecentOrders] = useState<Order[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const fetchDashboardData = async () => {
        setError(null);
        try {
            const [navRes, pnlRes, holdingsRes, ordersRes] = await Promise.all([
                DashboardApi.getNavHistory().catch(() => ({ ok: false, error: 'Network error', data: null })),
                DashboardApi.getPnlHistory('hourly').catch(() => ({ ok: false, error: 'Network error', data: null })),
                TradingApi.getHoldings().catch(() => ({ ok: false, error: 'Network error', data: null })),
                TradingApi.getOrders(1, 5).catch(() => ({ ok: false, error: 'Network error', data: null }))
            ]);

            if (navRes.ok) {
                setNavHistory(navRes.data);
            } else {
                console.warn('[Dashboard] NAV history failed:', !navRes.ok ? (navRes as any).error : 'Unknown error');
                setNavHistory({ from: '', to: '', data: [] });
            }

            if (pnlRes.ok) {
                setPnlHistory(pnlRes.data);
            } else {
                console.warn('[Dashboard] PnL history failed:', !pnlRes.ok ? (pnlRes as any).error : 'Unknown error');
                setPnlHistory({ granularity: 'hourly', date: '', data: [] });
            }

            // Set holdings data
            if (holdingsRes.ok) {
                setHoldings(holdingsRes.data);
            } else {
                console.warn('[Dashboard] Holdings failed:', !holdingsRes.ok ? (holdingsRes as any).error : 'Unknown error');
                setHoldings([]);
            }

            // Set recent orders data
            if (ordersRes.ok && ordersRes.data) {
                setRecentOrders(ordersRes.data.data || []);
            } else {
                console.warn('[Dashboard] Recent orders failed:', !ordersRes.ok ? (ordersRes as any).error : 'Unknown error');
                setRecentOrders([]);
            }

            console.log('[Dashboard] Data loaded:', {
                summary,
                nav: navRes.ok ? navRes.data : null,
                pnl: pnlRes.ok ? pnlRes.data : null,
                holdings: holdingsRes.ok ? holdingsRes.data : null,
                orders: ordersRes.ok ? ordersRes.data : null
            });
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
                {/* NAV Chart - Premium Component */}
                <div className="lg:col-span-2">
                    <ChartCard
                        title="Net Asset Value (NAV)"
                        subtitle="Last 30 days performance - Total net asset value (all crypto + USD) over time"
                        action={
                            <Button
                                size="sm"
                                className="bg-emerald-500 text-black hover:bg-emerald-600"
                                onClick={() => onNavigate?.('portfolio')}
                            >
                                View Portfolio
                            </Button>
                        }
                    >
                        {navData.length === 0 ? (
                            <div className="flex items-center justify-center h-[280px] text-gray-400">
                                <div className="text-center">
                                    <p className="mb-2">No NAV data yet</p>
                                    <p className="text-sm">Data will appear after you start trading</p>
                                </div>
                            </div>
                        ) : (
                            <NavChart data={navData} />
                        )}
                    </ChartCard>
                </div>

                {/* PnL Chart - Premium Component */}
                <div>
                    <ChartCard
                        title="Today's PnL"
                        subtitle="Hourly breakdown - Profit/loss per hour per day"
                    >
                        {pnlData.length === 0 ? (
                            <div className="flex items-center justify-center h-[280px] text-gray-400">
                                <div className="text-center">
                                    <p className="mb-2">No PnL data today yet</p>
                                    <p className="text-sm">Data will appear after trading</p>
                                </div>
                            </div>
                        ) : (
                            <TodayPnlChart data={pnlData} />
                        )}
                    </ChartCard>
                </div>
            </div>

            <div className="grid lg:grid-cols-2 gap-6">
                {/* Top Holdings - Hiển thị các tài sản crypto mà user đang nắm giữ nhiều nhất */}
                <Card className="bg-gray-900 border-gray-800 p-6">
                    <div className="flex items-center justify-between mb-6">
                        <div>
                            <h2 className="text-xl">Top Holdings</h2>
                            <p className="text-gray-400 text-sm">Crypto assets you hold, sorted by USD value</p>
                        </div>
                        <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => onNavigate?.('portfolio')}
                            className="text-emerald-500 hover:text-emerald-400"
                        >
                            View All
                        </Button>
                    </div>
                    <div className="space-y-3">
                        {holdings.length === 0 ? (
                            <div className="text-gray-400 text-sm text-center py-8">
                                No holdings yet. Start buying crypto to see them here!
                            </div>
                        ) : (
                            holdings.slice(0, 5).map((holding, index) => (
                                <div key={holding.symbol} className="flex items-center justify-between p-3 bg-gray-800/50 rounded-lg hover:bg-gray-800 transition-colors">
                                    <div className="flex items-center gap-3">
                                        <div className="w-8 h-8 bg-emerald-500/10 rounded-full flex items-center justify-center text-emerald-500 font-bold text-sm">
                                            {index + 1}
                                        </div>
                                        <div>
                                            <div className="text-white font-medium">{holding.symbol}</div>
                                            <div className="text-gray-400 text-xs">{holding.name}</div>
                                        </div>
                                    </div>
                                    <div className="text-right">
                                        <div className="text-white font-medium">{formatAmount(holding.amount, 4)} {holding.symbol}</div>
                                        <div className="text-gray-400 text-sm">{formatCurrency(holding.valueUsd)}</div>
                                        <div className={`text-xs ${holding.change24h >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
                                            {holding.change24h >= 0 ? '+' : ''}{holding.change24h.toFixed(2)}%
                                        </div>
                                    </div>
                                </div>
                            ))
                        )}
                    </div>
                </Card>

                {/* Recent Orders - Hiển thị 5 lệnh giao dịch gần đây nhất */}
                <Card className="bg-gray-900 border-gray-800 p-6">
                    <div className="flex items-center justify-between mb-6">
                        <div>
                            <h2 className="text-xl">Recent Orders</h2>
                            <p className="text-gray-400 text-sm">Your 5 most recent trades</p>
                        </div>
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
                        {recentOrders.length === 0 ? (
                            <div className="text-gray-400 text-sm text-center py-8">
                                No orders yet. Start trading to see them here!
                            </div>
                        ) : (
                            recentOrders.map((order) => (
                                <div
                                    key={order.id}
                                    className="flex items-center justify-between p-3 bg-gray-800/50 rounded-lg hover:bg-gray-800 transition-colors cursor-pointer"
                                    onClick={() => onNavigate?.('order-detail', order.id)}
                                >
                                    <div className="flex items-center gap-3">
                                        <div className={`w-2 h-2 rounded-full ${order.side === 'BUY' ? 'bg-emerald-500' : 'bg-red-500'}`} />
                                        <div>
                                            <div className="text-white font-medium">{order.symbol}</div>
                                            <div className="text-gray-400 text-xs">
                                                {order.side} • {order.type} • {order.status}
                                            </div>
                                        </div>
                                    </div>
                                    <div className="text-right">
                                        <div className="text-white text-sm">{formatAmount(order.quantity, 4)}</div>
                                        <div className="text-gray-400 text-xs">
                                            {order.price ? `@ ${formatCurrency(order.price)}` : 'Market'}
                                        </div>
                                        <div className="text-gray-500 text-xs mt-1">
                                            <Clock className="w-3 h-3 inline mr-1" />
                                            {formatTimeAgo(order.createdAt)}
                                        </div>
                                    </div>
                                </div>
                            ))
                        )}
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
