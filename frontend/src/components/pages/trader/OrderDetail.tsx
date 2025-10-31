import { ArrowLeft, Clock, CheckCircle2, XCircle, AlertCircle } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Badge } from '../../ui/badge';
import { Separator } from '../../ui/separator';

interface OrderDetailProps {
  orderId?: string;
  onNavigate?: (page: string) => void;
}

export default function OrderDetail({ orderId = 'ORD-003', onNavigate }: OrderDetailProps) {
  const order = {
    id: 'ORD-003',
    createdAt: '2025-01-15 13:45:10',
    updatedAt: '2025-01-15 14:23:45',
    pair: 'SOL/USDT',
    type: 'Limit',
    side: 'Buy',
    price: 97.50,
    amount: 45,
    filled: 23,
    remaining: 22,
    total: 4387.50,
    avgPrice: 97.45,
    fee: 2.24,
    status: 'Partial',
    fills: [
      { time: '2025-01-15 13:47:23', price: 97.45, amount: 12, fee: 1.17, total: 1169.40 },
      { time: '2025-01-15 13:52:18', price: 97.48, amount: 6, fee: 0.58, total: 584.88 },
      { time: '2025-01-15 14:05:32', price: 97.42, amount: 5, fee: 0.49, total: 487.10 },
    ],
    timeline: [
      { time: '2025-01-15 13:45:10', status: 'created', message: 'Order created' },
      { time: '2025-01-15 13:47:23', status: 'partial', message: 'Partially filled: 12 SOL @ $97.45' },
      { time: '2025-01-15 13:52:18', status: 'partial', message: 'Partially filled: 6 SOL @ $97.48' },
      { time: '2025-01-15 14:05:32', status: 'partial', message: 'Partially filled: 5 SOL @ $97.42' },
    ]
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
              <h1 className="text-2xl mb-1">Order #{order.id}</h1>
              <p className="text-gray-400">{order.pair}</p>
            </div>
            <Badge className={
              order.status === 'Filled' ? 'bg-emerald-500/10 text-emerald-500 text-lg px-4 py-2' :
              order.status === 'Partial' ? 'bg-yellow-500/10 text-yellow-500 text-lg px-4 py-2' :
              order.status === 'Open' ? 'bg-blue-500/10 text-blue-500 text-lg px-4 py-2' :
              'bg-gray-500/10 text-gray-500 text-lg px-4 py-2'
            }>
              {order.status}
            </Badge>
          </div>

          <div className="grid md:grid-cols-2 gap-6 mb-6">
            <div className="space-y-4">
              <div>
                <div className="text-gray-400 text-sm mb-1">Order Type</div>
                <div className="text-white">{order.type}</div>
              </div>
              <div>
                <div className="text-gray-400 text-sm mb-1">Side</div>
                <Badge className={order.side === 'Buy' ? 'bg-emerald-500/10 text-emerald-500' : 'bg-red-500/10 text-red-500'}>
                  {order.side}
                </Badge>
              </div>
              <div>
                <div className="text-gray-400 text-sm mb-1">Limit Price</div>
                <div className="text-white text-xl">${order.price.toFixed(2)}</div>
              </div>
              <div>
                <div className="text-gray-400 text-sm mb-1">Average Fill Price</div>
                <div className="text-white text-xl">${order.avgPrice.toFixed(2)}</div>
              </div>
            </div>

            <div className="space-y-4">
              <div>
                <div className="text-gray-400 text-sm mb-1">Amount</div>
                <div className="text-white">{order.amount} SOL</div>
              </div>
              <div>
                <div className="text-gray-400 text-sm mb-1">Filled</div>
                <div className="text-emerald-500">{order.filled} SOL ({((order.filled / order.amount) * 100).toFixed(1)}%)</div>
              </div>
              <div>
                <div className="text-gray-400 text-sm mb-1">Remaining</div>
                <div className="text-yellow-500">{order.remaining} SOL</div>
              </div>
              <div>
                <div className="text-gray-400 text-sm mb-1">Total Value</div>
                <div className="text-white text-xl">${order.total.toFixed(2)}</div>
              </div>
            </div>
          </div>

          <Separator className="my-6 bg-gray-800" />

          {/* Fill History */}
          <div>
            <h2 className="text-xl mb-4">Fill History</h2>
            <div className="space-y-3">
              {order.fills.map((fill, index) => (
                <div key={index} className="flex items-center justify-between p-4 bg-gray-800 rounded-lg">
                  <div>
                    <div className="text-white mb-1">{fill.amount} SOL @ ${fill.price.toFixed(2)}</div>
                    <div className="text-sm text-gray-400">{fill.time}</div>
                  </div>
                  <div className="text-right">
                    <div className="text-white mb-1">${fill.total.toFixed(2)}</div>
                    <div className="text-sm text-gray-400">Fee: ${fill.fee.toFixed(2)}</div>
                  </div>
                </div>
              ))}
            </div>
            <div className="mt-4 p-4 bg-gray-800/50 rounded-lg flex items-center justify-between">
              <span className="text-gray-400">Total Fees</span>
              <span className="text-white">${order.fee.toFixed(2)}</span>
            </div>
          </div>

          <Separator className="my-6 bg-gray-800" />

          {/* Timestamps */}
          <div className="grid md:grid-cols-2 gap-4 text-sm">
            <div>
              <div className="text-gray-400 mb-1">Created At</div>
              <div className="text-white">{order.createdAt}</div>
            </div>
            <div>
              <div className="text-gray-400 mb-1">Last Updated</div>
              <div className="text-white">{order.updatedAt}</div>
            </div>
          </div>
        </Card>

        {/* Timeline */}
        <div className="space-y-6">
          <Card className="bg-gray-900 border-gray-800 p-6">
            <h2 className="text-xl mb-6">Order Timeline</h2>
            <div className="space-y-6">
              {order.timeline.map((event, index) => (
                <div key={index} className="flex gap-3">
                  <div className="flex flex-col items-center">
                    <div className="w-8 h-8 bg-gray-800 rounded-full flex items-center justify-center">
                      {getStatusIcon(event.status)}
                    </div>
                    {index < order.timeline.length - 1 && (
                      <div className="w-0.5 h-12 bg-gray-800 my-1"></div>
                    )}
                  </div>
                  <div className="flex-1">
                    <div className="text-white mb-1">{event.message}</div>
                    <div className="text-sm text-gray-400">{event.time}</div>
                  </div>
                </div>
              ))}
            </div>
          </Card>

          {order.status === 'Partial' && (
            <Card className="bg-red-500/10 border-red-500/20 p-6">
              <h3 className="mb-2 text-red-500">Cancel Order</h3>
              <p className="text-gray-400 text-sm mb-4">
                Cancel the remaining {order.remaining} SOL from this order.
              </p>
              <Button
                className="w-full bg-red-500 text-white hover:bg-red-600"
              >
                Cancel Order
              </Button>
            </Card>
          )}

          <Card className="bg-gray-900 border-gray-800 p-6">
            <h3 className="mb-4">Progress</h3>
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span className="text-gray-400">Filled</span>
                <span className="text-white">{((order.filled / order.amount) * 100).toFixed(1)}%</span>
              </div>
              <div className="w-full bg-gray-800 rounded-full h-2">
                <div
                  className="bg-emerald-500 h-2 rounded-full transition-all"
                  style={{ width: `${(order.filled / order.amount) * 100}%` }}
                ></div>
              </div>
              <div className="flex justify-between text-sm text-gray-400">
                <span>{order.filled} / {order.amount} SOL</span>
              </div>
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}
