import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { ArrowLeft, Clock, CheckCircle2, XCircle, AlertCircle, Loader2, DollarSign, TrendingUp, TrendingDown } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Badge } from '../../ui/badge';
import { Separator } from '../../ui/separator';
import { TradingApi, OrderDetail as OrderDetailType, OrderStatus } from '../../../api/trading';
import { useUserRole } from '../../../hooks/useUserRole';

interface OrderDetailProps {
  orderId?: string;
  onNavigate?: (page: string) => void;
}

export default function OrderDetail({ orderId: propOrderId, onNavigate }: OrderDetailProps) {
  const { isAdmin } = useUserRole();
  const [searchParams] = useSearchParams();
  const urlOrderId = searchParams.get('id') || searchParams.get('orderId');
  const orderId = propOrderId || urlOrderId || '';
  const [order, setOrder] = useState<OrderDetailType | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [canceling, setCanceling] = useState(false);

  useEffect(() => {
    if (!orderId) {
      setError('No order ID provided');
      setLoading(false);
      return;
    }
    
    fetchOrderDetail();
  }, [orderId]);

  const fetchOrderDetail = async () => {
    if (!orderId) return;
    
    setLoading(true);
    setError(null);
    
    try {
      const res = await TradingApi.getOrder(orderId);
      
      if (!res.ok) {
        setError(res.error);
        setLoading(false);
        return;
      }
      
      setOrder(res.data);
      setLoading(false);
    } catch (e: any) {
      setError(e?.message || 'Failed to load order details');
      setLoading(false);
    }
  };

  const handleCancelOrder = async () => {
    if (!order) return;
    
    setCanceling(true);
    try {
      const res = await TradingApi.cancelOrder(order.id);
      
      if (!res.ok) {
        setError(res.error);
        setCanceling(false);
        return;
      }
      
      // Refresh order details
      await fetchOrderDetail();
      setCanceling(false);
    } catch (e: any) {
      setError(e?.message || 'Failed to cancel order');
      setCanceling(false);
    }
  };

  // Map backend status to UI display
  const getStatusDisplay = (status: OrderStatus): string => {
    const statusMap: Record<OrderStatus, string> = {
      'NEW': 'Open',
      'PARTIAL': 'Partial',
      'FILLED': 'Filled',
      'CANCELED': 'Canceled',
      'REJECTED': 'Rejected',
    };
    return statusMap[status] || status;
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'created':
        return <Clock className="w-4 h-4 text-blue-500" />;
      case 'partial':
        return <AlertCircle className="w-4 h-4 text-yellow-500" />;
      case 'filled':
        return <CheckCircle2 className="w-4 h-4 text-emerald-500" />;
      case 'canceled':
        return <XCircle className="w-4 h-4 text-red-500" />;
      default:
        return <Clock className="w-4 h-4 text-gray-500" />;
    }
  };

  if (loading) {
    return (
      <div className="p-4 lg:p-8">
        <Button
          variant="ghost"
          onClick={() => onNavigate?.('orders')}
          className="mb-6 text-gray-400 hover:text-white"
        >
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Orders
        </Button>
        <div className="flex items-center justify-center min-h-[400px]">
          <Loader2 className="w-8 h-8 text-emerald-500 animate-spin" />
        </div>
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="p-4 lg:p-8">
        <Button
          variant="ghost"
          onClick={() => onNavigate?.('orders')}
          className="mb-6 text-gray-400 hover:text-white"
        >
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Orders
        </Button>
        <div className="p-4 bg-red-500/10 border border-red-500/50 rounded-lg text-red-400">
          {error || 'Order not found'}
        </div>
      </div>
    );
  }

  const baseAsset = order.symbol.split('/')[0];
  const fillPercentage = order.quantity > 0 ? (order.filled / order.quantity) * 100 : 0;

  return (
    <div className="p-4 lg:p-8">
      <Button
        variant="ghost"
        onClick={() => onNavigate?.('orders')}
        className="mb-6 text-gray-400 hover:text-white"
      >
        <ArrowLeft className="w-4 h-4 mr-2" />
        Back to Orders
      </Button>

      <div className="grid lg:grid-cols-3 gap-6">
        {/* Order Details */}
        <Card className="lg:col-span-2 bg-gray-900 border-gray-800 p-6">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h1 className="text-2xl mb-1">
                {isAdmin ? `Order #${order.id}` : 'Order Details'}
              </h1>
              <p className="text-gray-400">{order.symbol}</p>
            </div>
            <Badge className={
              order.status === 'FILLED' ? 'bg-emerald-500/10 text-emerald-500 text-lg px-4 py-2' :
              order.status === 'PARTIAL' ? 'bg-yellow-500/10 text-yellow-500 text-lg px-4 py-2' :
              order.status === 'NEW' ? 'bg-blue-500/10 text-blue-500 text-lg px-4 py-2' :
              order.status === 'REJECTED' ? 'bg-red-500/10 text-red-500 text-lg px-4 py-2' :
              'bg-gray-500/10 text-gray-500 text-lg px-4 py-2'
            }>
              {getStatusDisplay(order.status)}
            </Badge>
          </div>

          {/* Summary Cards */}
          <div className="grid md:grid-cols-3 gap-4 mb-6">
            <Card className="bg-gray-800/50 border-gray-700 p-4">
              <div className="flex items-center gap-2 mb-2">
                {order.side === 'BUY' ? (
                  <TrendingUp className="w-5 h-5 text-emerald-500" />
                ) : (
                  <TrendingDown className="w-5 h-5 text-red-500" />
                )}
                <div className="text-gray-400 text-sm">Side</div>
              </div>
              <Badge className={order.side === 'BUY' ? 'bg-emerald-500/10 text-emerald-500' : 'bg-red-500/10 text-red-500'}>
                {order.side}
              </Badge>
            </Card>
            
            <Card className="bg-gray-800/50 border-gray-700 p-4">
              <div className="flex items-center gap-2 mb-2">
                <DollarSign className="w-5 h-5 text-blue-500" />
                <div className="text-gray-400 text-sm">Order Type</div>
              </div>
              <div className="text-white font-semibold">{order.type}</div>
            </Card>
            
            <Card className="bg-gray-800/50 border-gray-700 p-4">
              <div className="text-gray-400 text-sm mb-2">Price</div>
              <div className="text-white font-semibold text-lg">
                {order.price ? `$${order.price.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : 'Market'}
              </div>
              {order.avgPrice && order.avgPrice !== order.price && (
                <div className="text-gray-400 text-xs mt-1">
                  Avg: ${order.avgPrice.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </div>
              )}
            </Card>
          </div>

          {/* Detailed Information */}
          <div className="grid md:grid-cols-2 gap-6 mb-6">
            <div className="space-y-4">
              <h3 className="text-lg font-semibold mb-3">Order Information</h3>
              {isAdmin && (
                <div>
                  <div className="text-gray-400 text-sm mb-1">Order ID</div>
                  <div className="text-white font-mono text-sm">{order.id}</div>
                </div>
              )}
              <div>
                <div className="text-gray-400 text-sm mb-1">Symbol</div>
                <div className="text-white font-semibold">{order.symbol}</div>
              </div>
              <div>
                <div className="text-gray-400 text-sm mb-1">Order Type</div>
                <Badge variant="outline" className="border-gray-700">
                  {order.type}
                </Badge>
              </div>
              {order.type === 'LIMIT' && order.price && (
                <div>
                  <div className="text-gray-400 text-sm mb-1">Limit Price</div>
                  <div className="text-white text-xl font-semibold">
                    ${order.price.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </div>
                </div>
              )}
              {order.avgPrice && (
                <div>
                  <div className="text-gray-400 text-sm mb-1">Average Fill Price</div>
                  <div className="text-white text-xl font-semibold text-emerald-500">
                    ${order.avgPrice.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </div>
                </div>
              )}
            </div>

            <div className="space-y-4">
              <h3 className="text-lg font-semibold mb-3">Quantity & Fills</h3>
              <div>
                <div className="text-gray-400 text-sm mb-1">Total Quantity</div>
                <div className="text-white text-xl font-semibold">
                  {order.quantity.toLocaleString('en-US', { maximumFractionDigits: 8 })} {baseAsset}
                </div>
              </div>
              <div>
                <div className="text-gray-400 text-sm mb-1">Filled</div>
                <div className="text-emerald-500 text-xl font-semibold">
                  {order.filled.toLocaleString('en-US', { maximumFractionDigits: 8 })} {baseAsset}
                </div>
                <div className="text-gray-400 text-xs mt-1">{fillPercentage.toFixed(2)}% filled</div>
              </div>
              <div>
                <div className="text-gray-400 text-sm mb-1">Remaining</div>
                <div className="text-yellow-500 text-xl font-semibold">
                  {order.remaining.toLocaleString('en-US', { maximumFractionDigits: 8 })} {baseAsset}
                </div>
              </div>
              {order.totalFees !== undefined && (
                <div>
                  <div className="text-gray-400 text-sm mb-1">Total Fees</div>
                  <div className="text-white text-lg font-semibold">
                    ${order.totalFees.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </div>
                </div>
              )}
              {order.avgPrice && order.filled > 0 && (
                <div>
                  <div className="text-gray-400 text-sm mb-1">Total Value</div>
                  <div className="text-white text-lg font-semibold">
                    ${(order.avgPrice * order.filled).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </div>
                </div>
              )}
            </div>
          </div>

          <Separator className="my-6 bg-gray-800" />

          {/* Fill History (Trades) */}
          <div>
            <h2 className="text-xl mb-4">Fill History</h2>
            {order.trades && order.trades.length > 0 ? (
              <>
                <div className="space-y-3 mb-4">
                  {order.trades.map((trade, index) => {
                    const total = trade.price * trade.quantity;
                    return (
                      <Card key={trade.id} className="bg-gray-800/50 border-gray-700 p-4">
                        <div className="flex items-center justify-between">
                          <div className="flex-1">
                            <div className="flex items-center gap-3 mb-2">
                              <div className="w-8 h-8 rounded-full bg-emerald-500/10 flex items-center justify-center">
                                <span className="text-emerald-500 text-xs font-bold">#{index + 1}</span>
                              </div>
                              <div>
                                <div className="text-white font-semibold mb-1">
                                  {trade.quantity.toLocaleString('en-US', { maximumFractionDigits: 8 })} {baseAsset} @ ${trade.price.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                                </div>
                                <div className="text-sm text-gray-400">
                                  {new Date(trade.createdAt).toLocaleString('vi-VN', { 
                                    timeZone: 'Asia/Ho_Chi_Minh',
                                    year: 'numeric',
                                    month: 'short',
                                    day: 'numeric',
                                    hour: '2-digit',
                                    minute: '2-digit',
                                    second: '2-digit'
                                  })}
                                </div>
                              </div>
                            </div>
                          </div>
                          <div className="text-right ml-4">
                            <div className="text-white font-semibold text-lg mb-1">
                              ${total.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                            </div>
                            <div className="text-sm text-gray-400">
                              Fee: ${trade.fee.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                            </div>
                          </div>
                        </div>
                      </Card>
                    );
                  })}
                </div>
                <Card className="bg-emerald-500/10 border-emerald-500/20 p-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <div className="text-gray-300 text-sm mb-1">Total Filled</div>
                      <div className="text-white font-semibold">
                        {order.filled.toLocaleString('en-US', { maximumFractionDigits: 8 })} {baseAsset}
                      </div>
                    </div>
                    <div className="text-right">
                      <div className="text-gray-300 text-sm mb-1">Total Fees</div>
                      <div className="text-white font-semibold">
                        ${order.totalFees?.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) || '0.00'}
                      </div>
                    </div>
                  </div>
                </Card>
              </>
            ) : (
              <Card className="bg-gray-800/50 border-gray-700 p-8">
                <div className="text-center text-gray-400">
                  <Clock className="w-12 h-12 mx-auto mb-3 opacity-50" />
                  <p>No fills yet</p>
                  <p className="text-sm mt-2">This order has not been filled yet.</p>
                </div>
              </Card>
            )}
          </div>

          <Separator className="my-6 bg-gray-800" />

          {/* Timestamps */}
          <Card className="bg-gray-800/50 border-gray-700 p-4">
            <h3 className="text-lg font-semibold mb-4">Timeline</h3>
            <div className="grid md:grid-cols-2 gap-4">
              <div>
                <div className="text-gray-400 text-sm mb-1">Created At</div>
                <div className="text-white font-semibold">
                  {new Date(order.createdAt).toLocaleString('vi-VN', { 
                    timeZone: 'Asia/Ho_Chi_Minh',
                    year: 'numeric',
                    month: 'short',
                    day: 'numeric',
                    hour: '2-digit',
                    minute: '2-digit',
                    second: '2-digit'
                  })}
                </div>
              </div>
              <div>
                <div className="text-gray-400 text-sm mb-1">Last Updated</div>
                <div className="text-white font-semibold">
                  {new Date(order.updatedAt).toLocaleString('vi-VN', { 
                    timeZone: 'Asia/Ho_Chi_Minh',
                    year: 'numeric',
                    month: 'short',
                    day: 'numeric',
                    hour: '2-digit',
                    minute: '2-digit',
                    second: '2-digit'
                  })}
                </div>
              </div>
            </div>
          </Card>
        </Card>

        {/* Timeline & Actions */}
        <div className="space-y-6">
          <Card className="bg-gray-900 border-gray-800 p-6">
            <h2 className="text-xl mb-6">Order Timeline</h2>
            <div className="space-y-6">
              {/* Order Created */}
              <div className="flex gap-3">
                <div className="flex flex-col items-center">
                  <div className="w-8 h-8 bg-gray-800 rounded-full flex items-center justify-center">
                    <Clock className="w-4 h-4 text-blue-500" />
                  </div>
                  {order.trades && order.trades.length > 0 && (
                    <div className="w-0.5 h-12 bg-gray-800 my-1"></div>
                  )}
                </div>
                <div className="flex-1">
                  <div className="text-white mb-1">Order created</div>
                  <div className="text-sm text-gray-400">{new Date(order.createdAt).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })}</div>
                </div>
              </div>
              
              {/* Trade fills */}
              {order.trades && order.trades.map((trade, index) => (
                <div key={trade.id} className="flex gap-3">
                  <div className="flex flex-col items-center">
                    <div className="w-8 h-8 bg-gray-800 rounded-full flex items-center justify-center">
                      {order.status === 'FILLED' && index === order.trades!.length - 1 
                        ? <CheckCircle2 className="w-4 h-4 text-emerald-500" />
                        : <AlertCircle className="w-4 h-4 text-yellow-500" />
                      }
                    </div>
                    {index < order.trades!.length - 1 && (
                      <div className="w-0.5 h-12 bg-gray-800 my-1"></div>
                    )}
                  </div>
                  <div className="flex-1">
                    <div className="text-white mb-1">
                      Filled: {trade.quantity} {baseAsset} @ ${trade.price.toFixed(2)}
                    </div>
                    <div className="text-sm text-gray-400">{new Date(trade.createdAt).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })}</div>
                  </div>
                </div>
              ))}
            </div>
          </Card>

          {(order.status === 'NEW' || order.status === 'PARTIAL') && (
            <Card className="bg-red-500/10 border-red-500/20 p-6">
              <h3 className="mb-2 text-red-500">Cancel Order</h3>
              <p className="text-gray-400 text-sm mb-4">
                Cancel the remaining {order.remaining} {baseAsset} from this order.
              </p>
              <Button
                className="w-full bg-red-500 text-white hover:bg-red-600"
                onClick={handleCancelOrder}
                disabled={canceling}
              >
                {canceling ? <Loader2 className="w-4 h-4 animate-spin mr-2" /> : null}
                {canceling ? 'Canceling...' : 'Cancel Order'}
              </Button>
            </Card>
          )}

          <Card className="bg-gray-900 border-gray-800 p-6">
            <h3 className="mb-4">Progress</h3>
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span className="text-gray-400">Filled</span>
                <span className="text-white">{fillPercentage.toFixed(1)}%</span>
              </div>
              <div className="w-full bg-gray-800 rounded-full h-2">
                <div
                  className="bg-emerald-500 h-2 rounded-full transition-all"
                  style={{ width: `${fillPercentage}%` }}
                ></div>
              </div>
              <div className="flex justify-between text-sm text-gray-400">
                <span>{order.filled} / {order.quantity} {baseAsset}</span>
              </div>
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}
