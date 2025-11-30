import React, { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Loader2, TrendingUp, TrendingDown, Clock, CheckCircle2, XCircle, AlertCircle, DollarSign, RefreshCw } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Badge } from '../../ui/badge';
import { Separator } from '../../ui/separator';
import { BotApi, BotDetail as BotDetailType, BotOrder } from '../../../api/bots';
import { CoinIcon } from '../../ui/CoinIcon';

interface BotDetailProps {
  botId?: string;
  onNavigate?: (page: string) => void;
}

export default function BotDetail({ botId: propBotId, onNavigate }: BotDetailProps) {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const urlBotId = searchParams.get('id') || searchParams.get('botId');
  const botId = propBotId || urlBotId || '';
  
  const [bot, setBot] = useState<BotDetailType | null>(null);
  const [orders, setOrders] = useState<BotOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [ordersLoading, setOrdersLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [closingPositions, setClosingPositions] = useState(false);
  const pageSize = 20;

  useEffect(() => {
    if (!botId) {
      setError('No bot ID provided');
      setLoading(false);
      return;
    }
    
    fetchBotDetail();
    fetchBotOrders(1);
  }, [botId]);

  const fetchBotDetail = async () => {
    if (!botId) return;
    
    setLoading(true);
    setError(null);
    
    try {
      const res = await BotApi.getBot(botId);
      
      if (!res.ok) {
        setError(res.error);
        setLoading(false);
        return;
      }
      
      setBot(res.data);
      setLoading(false);
    } catch (e: any) {
      setError(e?.message || 'Failed to load bot details');
      setLoading(false);
    }
  };

  const fetchBotOrders = async (page: number) => {
    if (!botId) return;
    
    setOrdersLoading(true);
    try {
      const res = await BotApi.getBotOrders(botId, page, pageSize);
      
      if (res.ok) {
        setOrders(res.data.data);
        setTotalPages(res.data.totalPages);
        setCurrentPage(res.data.page);
      }
    } catch (e: any) {
      console.error('Failed to load bot orders:', e);
    } finally {
      setOrdersLoading(false);
    }
  };

  const handleClosePositions = async () => {
    if (!botId) return;
    
    if (!confirm('Bạn có chắc muốn chốt lời (bán tất cả inventory đang hold)?')) {
      return;
    }
    
    setClosingPositions(true);
    try {
      const res = await BotApi.closePositions(botId);
      
      if (res.ok) {
        alert(`Đã chốt lời: ${res.data.closedQuantity} ${bot?.baseAsset || ''}`);
        // Refresh bot detail để cập nhật P&L
        await fetchBotDetail();
        await fetchBotOrders(1);
      } else {
        alert(`Lỗi: ${res.error || 'Failed to close positions'}`);
      }
    } catch (e: any) {
      console.error('Failed to close positions:', e);
      alert('Lỗi khi chốt lời: ' + (e?.message || 'Unknown error'));
    } finally {
      setClosingPositions(false);
    }
  };

  const getStatusDisplay = (status: string): string => {
    const statusMap: Record<string, string> = {
      'NEW': 'Open',
      'PARTIAL': 'Partial',
      'FILLED': 'Filled',
      'CANCELED': 'Canceled',
      'REJECTED': 'Rejected',
    };
    return statusMap[status] || status;
  };

  const getStatusColor = (status: string): string => {
    switch (status) {
      case 'FILLED':
        return 'bg-emerald-500/10 text-emerald-500';
      case 'PARTIAL':
        return 'bg-yellow-500/10 text-yellow-500';
      case 'NEW':
        return 'bg-blue-500/10 text-blue-500';
      case 'CANCELED':
        return 'bg-gray-500/10 text-gray-500';
      case 'REJECTED':
        return 'bg-red-500/10 text-red-500';
      default:
        return 'bg-gray-500/10 text-gray-500';
    }
  };

  const getSideColor = (side: string): string => {
    return side === 'BUY' ? 'bg-emerald-500/10 text-emerald-500' : 'bg-red-500/10 text-red-500';
  };

  if (loading) {
    return (
      <div className="p-4 lg:p-8">
        <Button
          variant="ghost"
          onClick={() => onNavigate?.('bots') || navigate('/bots')}
          className="mb-6 text-gray-400 hover:text-white"
        >
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Bots
        </Button>
        <div className="flex items-center justify-center min-h-[400px]">
          <Loader2 className="w-8 h-8 text-emerald-500 animate-spin" />
        </div>
      </div>
    );
  }

  if (error || !bot) {
    return (
      <div className="p-4 lg:p-8">
        <Button
          variant="ghost"
          onClick={() => onNavigate?.('bots') || navigate('/bots')}
          className="mb-6 text-gray-400 hover:text-white"
        >
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Bots
        </Button>
        <div className="p-4 bg-red-500/10 border border-red-500/50 rounded-lg text-red-400">
          {error || 'Bot not found'}
        </div>
      </div>
    );
  }

  const baseAsset = bot.baseAsset || 'BTC';
  const quoteAsset = bot.quoteAsset || 'USD';
  const runtime = bot.runtime;

  return (
    <div className="p-4 lg:p-8">
      <Button
        variant="ghost"
        onClick={() => onNavigate?.('bots') || navigate('/bots')}
        className="mb-6 text-gray-400 hover:text-white"
      >
        <ArrowLeft className="w-4 h-4 mr-2" />
        Back to Bots
      </Button>

      <div className="grid lg:grid-cols-3 gap-6 mb-6">
        {/* Bot Information */}
        <Card className="lg:col-span-2 bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h1 className="text-2xl mb-1 font-bold">{bot.name}</h1>
              <p className="text-gray-400 flex items-center gap-2">
                <CoinIcon symbol={baseAsset} className="w-5 h-5" />
                {baseAsset}/{quoteAsset}
              </p>
            </div>
            <Badge className={
              bot.status === 'RUNNING' ? 'bg-emerald-500/10 text-emerald-500 text-lg px-4 py-2' :
              bot.status === 'STOPPED' ? 'bg-gray-500/10 text-gray-500 text-lg px-4 py-2' :
              bot.status === 'ERROR' ? 'bg-red-500/10 text-red-500 text-lg px-4 py-2' :
              'bg-gray-500/10 text-gray-500 text-lg px-4 py-2'
            }>
              {bot.status}
            </Badge>
          </div>

          {/* Strategy Info */}
          <div className="grid md:grid-cols-2 gap-4 mb-6">
            <Card className="bg-gray-800/50 border-gray-700 p-4">
              <div className="text-gray-400 text-sm mb-2">Strategy</div>
              <div className="text-white font-semibold">{bot.strategy?.displayName || bot.strategy?.key}</div>
              <div className="text-gray-500 text-xs mt-1">v{bot.strategy?.version}</div>
            </Card>
            
            <Card className="bg-gray-800/50 border-gray-700 p-4">
              <div className="text-gray-400 text-sm mb-2">Execution Interval</div>
              <div className="text-white font-semibold">{bot.executionIntervalSeconds || 60}s</div>
            </Card>
          </div>

          {/* Runtime Metrics */}
          {runtime && (
            <>
              <Separator className="my-6 bg-gray-800" />
              <h3 className="text-lg font-semibold mb-4">Performance Metrics</h3>
              <div className="grid md:grid-cols-4 gap-4">
                {/* Total P&L - Most prominent */}
                <Card className="bg-gray-800/50 border-gray-700 p-4 md:col-span-2">
                  <div className="flex items-center gap-2 mb-2">
                    <DollarSign className="w-5 h-5 text-emerald-500" />
                    <div className="text-gray-400 text-sm">Total P&L</div>
                  </div>
                  <div className={`text-2xl font-bold ${
                    ((runtime.realizedPnl ?? 0) + (runtime.unrealizedPnl ?? 0)) >= 0 ? 'text-emerald-500' : 'text-red-500'
                  }`}>
                    {((runtime.realizedPnl ?? 0) + (runtime.unrealizedPnl ?? 0)) >= 0 ? '+' : ''}
                    {((runtime.realizedPnl ?? 0) + (runtime.unrealizedPnl ?? 0)).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} {quoteAsset}
                  </div>
                  <div className="text-xs text-gray-500 mt-1">
                    Realized + Unrealized
                  </div>
                </Card>
                
                <Card className="bg-gray-800/50 border-gray-700 p-4">
                  <div className="flex items-center gap-2 mb-2">
                    <TrendingUp className="w-5 h-5 text-emerald-500" />
                    <div className="text-gray-400 text-sm">Realized P&L</div>
                  </div>
                  <div className={`text-xl font-semibold ${(runtime.realizedPnl ?? 0) >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
                    {(runtime.realizedPnl ?? 0) >= 0 ? '+' : ''}{((runtime.realizedPnl ?? 0)).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} {quoteAsset}
                  </div>
                </Card>
                
                <Card className="bg-gray-800/50 border-gray-700 p-4">
                  <div className="flex items-center gap-2 mb-2">
                    <TrendingUp className="w-5 h-5 text-blue-500" />
                    <div className="text-gray-400 text-sm">Unrealized P&L</div>
                  </div>
                  <div className={`text-xl font-semibold ${(runtime.unrealizedPnl ?? 0) >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
                    {(runtime.unrealizedPnl ?? 0) >= 0 ? '+' : ''}{((runtime.unrealizedPnl ?? 0)).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} {quoteAsset}
                  </div>
                </Card>
                
                <Card className="bg-gray-800/50 border-gray-700 p-4 md:col-span-4">
                  <div className="flex items-center gap-2 mb-2">
                    <DollarSign className="w-5 h-5 text-yellow-500" />
                    <div className="text-gray-400 text-sm">Total Fees</div>
                  </div>
                  <div className="text-xl font-semibold text-white">
                    {((runtime.totalFees ?? 0)).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} {quoteAsset}
                  </div>
                </Card>
                
                {/* Close Positions Button */}
                {runtime && runtime.openPositions && runtime.openPositions > 0 && (
                  <Card className="bg-gray-800/50 border-gray-700 p-4 md:col-span-4">
                    <div className="flex items-center justify-between">
                      <div>
                        <div className="text-gray-400 text-sm mb-1">Open Positions</div>
                        <div className="text-white text-lg font-semibold">
                          {runtime.openPositions.toLocaleString('en-US', { minimumFractionDigits: 4, maximumFractionDigits: 4 })} {baseAsset}
                        </div>
                        <div className="text-xs text-gray-500 mt-1">
                          Unrealized P&L: {((runtime.unrealizedPnl ?? 0) >= 0 ? '+' : '')}
                          {((runtime.unrealizedPnl ?? 0)).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} {quoteAsset}
                        </div>
                      </div>
                      <Button
                        onClick={handleClosePositions}
                        disabled={closingPositions}
                        className="bg-emerald-600 hover:bg-emerald-700 text-white"
                      >
                        {closingPositions ? (
                          <>
                            <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                            Closing...
                          </>
                        ) : (
                          <>
                            <CheckCircle2 className="w-4 h-4 mr-2" />
                            Chốt Lời
                          </>
                        )}
                      </Button>
                    </div>
                  </Card>
                )}
              </div>
            </>
          )}

          {/* Parameters */}
          {bot.parameters && Object.keys(bot.parameters).length > 0 && (
            <>
              <Separator className="my-6 bg-gray-800" />
              <h3 className="text-lg font-semibold mb-4">Parameters</h3>
              <div className="grid md:grid-cols-2 gap-4">
                {Object.entries(bot.parameters).map(([key, value]) => (
                  <div key={key} className="bg-gray-800/50 border border-gray-700 rounded-lg p-3">
                    <div className="text-gray-400 text-sm mb-1">{key}</div>
                    <div className="text-white font-semibold">
                      {typeof value === 'object' ? JSON.stringify(value) : String(value)}
                    </div>
                  </div>
                ))}
              </div>
            </>
          )}
        </Card>

        {/* Sidebar */}
        <Card className="bg-gray-900 border-gray-800 p-6">
          <h3 className="text-lg font-semibold mb-4">Bot Statistics</h3>
          {runtime ? (
            <div className="space-y-4">
              <div>
                <div className="text-gray-400 text-sm mb-1">Total Orders</div>
                <div className="text-white text-xl font-semibold">{runtime.totalOrders ?? 0}</div>
              </div>
              <div>
                <div className="text-gray-400 text-sm mb-1">Filled Orders</div>
                <div className="text-emerald-500 text-xl font-semibold">{runtime.filledOrders ?? 0}</div>
              </div>
              <div>
                <div className="text-gray-400 text-sm mb-1">Open Positions</div>
                <div className="text-white text-xl font-semibold">{runtime.openPositions ?? 0}</div>
              </div>
              {runtime.lastExecutionAt && (
                <div>
                  <div className="text-gray-400 text-sm mb-1">Last Execution</div>
                  <div className="text-white text-sm">
                    {new Date(runtime.lastExecutionAt).toLocaleString('vi-VN', { 
                      timeZone: 'Asia/Ho_Chi_Minh',
                      year: 'numeric',
                      month: 'short',
                      day: 'numeric',
                      hour: '2-digit',
                      minute: '2-digit'
                    })}
                  </div>
                </div>
              )}
              {runtime.nextRunAt && (
                <div>
                  <div className="text-gray-400 text-sm mb-1">Next Run</div>
                  <div className="text-white text-sm">
                    {new Date(runtime.nextRunAt).toLocaleString('vi-VN', { 
                      timeZone: 'Asia/Ho_Chi_Minh',
                      year: 'numeric',
                      month: 'short',
                      day: 'numeric',
                      hour: '2-digit',
                      minute: '2-digit'
                    })}
                  </div>
                </div>
              )}
            </div>
          ) : (
            <div className="text-gray-500 text-sm">No runtime data available</div>
          )}
        </Card>
      </div>

      {/* Bot Orders */}
      <Card className="bg-gray-900 border-gray-800 p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-xl font-semibold">Bot Orders</h2>
          <Button
            variant="outline"
            size="sm"
            onClick={() => fetchBotOrders(currentPage)}
            disabled={ordersLoading}
            className="border-gray-700 text-gray-200 hover:bg-gray-900"
          >
            {ordersLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : <RefreshCw className="w-4 h-4" />}
          </Button>
        </div>

        {ordersLoading ? (
          <div className="flex justify-center py-10">
            <Loader2 className="w-6 h-6 text-emerald-500 animate-spin" />
          </div>
        ) : orders.length === 0 ? (
          <div className="text-center py-10 text-gray-500">
            No orders found for this bot
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="bg-gray-800 text-gray-400 uppercase text-xs tracking-wider">
                  <tr>
                    <th className="px-4 py-3 font-semibold">Order ID</th>
                    <th className="px-4 py-3 font-semibold">Side</th>
                    <th className="px-4 py-3 font-semibold">Type</th>
                    <th className="px-4 py-3 font-semibold">Price</th>
                    <th className="px-4 py-3 font-semibold">Quantity</th>
                    <th className="px-4 py-3 font-semibold">Filled</th>
                    <th className="px-4 py-3 font-semibold">Status</th>
                    <th className="px-4 py-3 font-semibold">Created At</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-800">
                  {orders.map((order) => {
                    const orderDetails = order.orderDetails;
                    if (!orderDetails) {
                      return (
                        <tr key={order.id} className="hover:bg-gray-800/50 transition-colors">
                          <td colSpan={8} className="px-4 py-3 text-gray-500 text-center">
                            Order {order.orderId} - No details available
                          </td>
                        </tr>
                      );
                    }
                    
                    return (
                      <tr key={order.id} className="hover:bg-gray-800/50 transition-colors">
                        <td className="px-4 py-3">
                          <div className="text-white font-mono text-xs">{order.orderId}</div>
                        </td>
                        <td className="px-4 py-3">
                          <Badge className={getSideColor(orderDetails.side)}>
                            {orderDetails.side}
                          </Badge>
                        </td>
                        <td className="px-4 py-3 text-gray-200">{orderDetails.type}</td>
                        <td className="px-4 py-3 text-gray-200">
                          {orderDetails.price && typeof orderDetails.price === 'number' 
                            ? `$${orderDetails.price.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` 
                            : 'Market'}
                        </td>
                        <td className="px-4 py-3 text-white">
                          {((orderDetails.quantity ?? 0)).toLocaleString('en-US', { maximumFractionDigits: 8 })} {baseAsset}
                        </td>
                        <td className="px-4 py-3 text-emerald-500">
                          {((orderDetails.filled ?? 0)).toLocaleString('en-US', { maximumFractionDigits: 8 })} {baseAsset}
                        </td>
                        <td className="px-4 py-3">
                          <Badge className={getStatusColor(orderDetails.status)}>
                            {getStatusDisplay(orderDetails.status)}
                          </Badge>
                        </td>
                        <td className="px-4 py-3 text-gray-400 text-xs">
                          {orderDetails.createdAt 
                            ? new Date(orderDetails.createdAt).toLocaleString('vi-VN')
                            : '—'}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-between mt-6 pt-6 border-t border-gray-800">
                <div className="text-gray-400 text-sm">
                  Page {currentPage} of {totalPages}
                </div>
                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => fetchBotOrders(Math.max(currentPage - 1, 1))}
                    disabled={currentPage === 1 || ordersLoading}
                    className="border-gray-700 text-gray-200 hover:bg-gray-900"
                  >
                    Previous
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => fetchBotOrders(Math.min(currentPage + 1, totalPages))}
                    disabled={currentPage === totalPages || ordersLoading}
                    className="border-gray-700 text-gray-200 hover:bg-gray-900"
                  >
                    Next
                  </Button>
                </div>
              </div>
            )}
          </>
        )}
      </Card>
    </div>
  );
}

