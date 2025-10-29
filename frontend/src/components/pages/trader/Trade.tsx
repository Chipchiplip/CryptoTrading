import { useState } from 'react';
import { TrendingUp, TrendingDown, Info, AlertCircle } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Label } from '../../ui/label';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../ui/tabs';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../ui/select';
import { Slider } from '../../ui/slider';
import { Alert, AlertDescription } from '../../ui/alert';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '../../ui/dialog';
import { Badge } from '../../ui/badge';

interface TradeProps {
  onNavigate?: (page: string) => void;
}

export default function Trade({ onNavigate }: TradeProps) {
  const [selectedPair, setSelectedPair] = useState('BTC/USDT');
  const [orderType, setOrderType] = useState<'market' | 'limit'>('market');
  const [side, setSide] = useState<'buy' | 'sell'>('buy');
  const [amount, setAmount] = useState('');
  const [price, setPrice] = useState('');
  const [percentage, setPercentage] = useState([0]);
  const [showPreview, setShowPreview] = useState(false);

  const pairs = [
    { symbol: 'BTC/USDT', price: 50234.56, change: 2.34 },
    { symbol: 'ETH/USDT', price: 2845.32, change: 1.82 },
    { symbol: 'SOL/USDT', price: 98.45, change: -0.45 },
    { symbol: 'BNB/USDT', price: 312.89, change: 3.12 },
  ];

  const currentPair = pairs.find(p => p.symbol === selectedPair) || pairs[0];
  const availableBalance = side === 'buy' ? 5234.56 : 0.2341;
  const currency = side === 'buy' ? 'USDT' : selectedPair.split('/')[0];

  const handlePercentageChange = (value: number[]) => {
    setPercentage(value);
    const calculatedAmount = (availableBalance * value[0]) / 100;
    setAmount(calculatedAmount.toFixed(side === 'buy' ? 2 : 8));
  };

  const calculateTotal = () => {
    if (!amount) return '0.00';
    const priceToUse = orderType === 'market' ? currentPair.price : parseFloat(price) || 0;
    const amountNum = parseFloat(amount) || 0;
    
    if (side === 'buy') {
      // Buying crypto with USDT
      return (amountNum * priceToUse).toFixed(2);
    } else {
      // Selling crypto for USDT
      return (amountNum * priceToUse).toFixed(2);
    }
  };

  const handleSubmit = () => {
    setShowPreview(true);
  };

  const confirmOrder = () => {
    setShowPreview(false);
    // Navigate to orders page
    onNavigate?.('orders');
  };

  return (
    <div className="p-4 lg:p-8">
      <div className="mb-6">
        <h1 className="text-3xl mb-2">Trade</h1>
        <p className="text-gray-400">Execute market or limit orders</p>
      </div>

      <div className="grid lg:grid-cols-3 gap-6">
        {/* Order Form */}
        <Card className="lg:col-span-2 bg-gray-900 border-gray-800 p-6">
          <div className="space-y-6">
            {/* Pair Selection */}
            <div>
              <Label>Trading Pair</Label>
              <Select value={selectedPair} onValueChange={setSelectedPair}>
                <SelectTrigger className="bg-gray-800 border-gray-700">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent className="bg-gray-800 border-gray-700 text-white">
                  {pairs.map((pair) => (
                    <SelectItem key={pair.symbol} value={pair.symbol}>
                      <div className="flex items-center justify-between w-full gap-4">
                        <span>{pair.symbol}</span>
                        <span className={pair.change >= 0 ? 'text-emerald-500' : 'text-red-500'}>
                          {pair.change >= 0 ? '+' : ''}{pair.change}%
                        </span>
                      </div>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <div className="mt-2 flex items-center gap-2">
                <span className="text-2xl text-white">${currentPair.price.toLocaleString()}</span>
                <Badge className={currentPair.change >= 0 ? 'bg-emerald-500/10 text-emerald-500' : 'bg-red-500/10 text-red-500'}>
                  {currentPair.change >= 0 ? <TrendingUp className="w-3 h-3 mr-1" /> : <TrendingDown className="w-3 h-3 mr-1" />}
                  {Math.abs(currentPair.change)}%
                </Badge>
              </div>
            </div>

            {/* Side Selection */}
            <div>
              <Label>Order Side</Label>
              <div className="grid grid-cols-2 gap-3 mt-2">
                <Button
                  type="button"
                  onClick={() => setSide('buy')}
                  className={side === 'buy' ? 'bg-emerald-500 text-black hover:bg-emerald-600' : 'bg-gray-800 text-gray-300 hover:bg-gray-700'}
                >
                  Buy
                </Button>
                <Button
                  type="button"
                  onClick={() => setSide('sell')}
                  className={side === 'sell' ? 'bg-red-500 text-white hover:bg-red-600' : 'bg-gray-800 text-gray-300 hover:bg-gray-700'}
                >
                  Sell
                </Button>
              </div>
            </div>

            {/* Order Type */}
            <Tabs value={orderType} onValueChange={(v) => setOrderType(v as 'market' | 'limit')}>
              <TabsList className="bg-gray-800 w-full">
                <TabsTrigger value="market" className="flex-1 data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
                  Market Order
                </TabsTrigger>
                <TabsTrigger value="limit" className="flex-1 data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
                  Limit Order
                </TabsTrigger>
              </TabsList>

              <TabsContent value="market" className="space-y-4 mt-4">
                <Alert className="bg-blue-500/10 border-blue-500/50">
                  <Info className="h-4 w-4 text-blue-500" />
                  <AlertDescription className="text-blue-500">
                    Market orders execute immediately at the current market price
                  </AlertDescription>
                </Alert>
              </TabsContent>

              <TabsContent value="limit" className="space-y-4 mt-4">
                <div>
                  <Label htmlFor="limitPrice">Limit Price (USDT)</Label>
                  <Input
                    id="limitPrice"
                    type="number"
                    placeholder="Enter price"
                    value={price}
                    onChange={(e) => setPrice(e.target.value)}
                    className="bg-gray-800 border-gray-700"
                  />
                  <p className="text-sm text-gray-400 mt-1">Current price: ${currentPair.price.toLocaleString()}</p>
                </div>
              </TabsContent>
            </Tabs>

            {/* Amount */}
            <div>
              <div className="flex items-center justify-between mb-2">
                <Label htmlFor="amount">Amount ({side === 'buy' ? selectedPair.split('/')[0] : selectedPair.split('/')[0]})</Label>
                <span className="text-sm text-gray-400">
                  Available: {availableBalance.toFixed(side === 'buy' ? 2 : 8)} {currency}
                </span>
              </div>
              <Input
                id="amount"
                type="number"
                placeholder="0.00"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                className="bg-gray-800 border-gray-700"
              />
              
              {/* Percentage Slider */}
              <div className="mt-4">
                <div className="flex justify-between text-sm text-gray-400 mb-2">
                  <span>0%</span>
                  <span>25%</span>
                  <span>50%</span>
                  <span>75%</span>
                  <span>100%</span>
                </div>
                <Slider
                  value={percentage}
                  onValueChange={handlePercentageChange}
                  max={100}
                  step={1}
                  className="w-full"
                />
              </div>

              <div className="grid grid-cols-4 gap-2 mt-3">
                {[25, 50, 75, 100].map((pct) => (
                  <Button
                    key={pct}
                    type="button"
                    size="sm"
                    variant="outline"
                    className="border-gray-700 hover:bg-gray-800"
                    onClick={() => handlePercentageChange([pct])}
                  >
                    {pct}%
                  </Button>
                ))}
              </div>
            </div>

            {/* Total */}
            <div className="pt-4 border-t border-gray-800">
              <div className="flex items-center justify-between mb-4">
                <span className="text-gray-400">Total</span>
                <span className="text-2xl text-white">{calculateTotal()} USDT</span>
              </div>
              
              {/* Fees */}
              <div className="space-y-2 text-sm">
                <div className="flex justify-between text-gray-400">
                  <span>Trading Fee (0.1%)</span>
                  <span>{(parseFloat(calculateTotal()) * 0.001).toFixed(2)} USDT</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-white">Estimated Total</span>
                  <span className="text-white">{(parseFloat(calculateTotal()) * (side === 'buy' ? 1.001 : 0.999)).toFixed(2)} USDT</span>
                </div>
              </div>
            </div>

            {/* Submit */}
            <Button
              onClick={handleSubmit}
              disabled={!amount || (orderType === 'limit' && !price)}
              className={`w-full ${
                side === 'buy' 
                  ? 'bg-emerald-500 text-black hover:bg-emerald-600' 
                  : 'bg-red-500 text-white hover:bg-red-600'
              }`}
            >
              {side === 'buy' ? 'Buy' : 'Sell'} {selectedPair.split('/')[0]}
            </Button>
          </div>
        </Card>

        {/* Order Book & Info */}
        <div className="space-y-4">
          <Card className="bg-gray-900 border-gray-800 p-6">
            <h3 className="mb-4">Order Book Preview</h3>
            <div className="space-y-3">
              {/* Asks */}
              <div>
                <div className="text-sm text-gray-400 mb-2">Sell Orders</div>
                {[50236.50, 50236.00, 50235.50].map((askPrice, i) => (
                  <div key={i} className="flex justify-between text-sm py-1">
                    <span className="text-red-500">${askPrice.toLocaleString()}</span>
                    <span className="text-gray-400">{(0.2 + i * 0.1).toFixed(4)}</span>
                  </div>
                ))}
              </div>

              {/* Current Price */}
              <div className="py-2 text-center border-y border-gray-800">
                <div className="text-xl text-emerald-500">${currentPair.price.toLocaleString()}</div>
              </div>

              {/* Bids */}
              <div>
                <div className="text-sm text-gray-400 mb-2">Buy Orders</div>
                {[50233.50, 50233.00, 50232.50].map((bidPrice, i) => (
                  <div key={i} className="flex justify-between text-sm py-1">
                    <span className="text-emerald-500">${bidPrice.toLocaleString()}</span>
                    <span className="text-gray-400">{(0.3 + i * 0.1).toFixed(4)}</span>
                  </div>
                ))}
              </div>
            </div>
          </Card>

          <Card className="bg-gray-900 border-gray-800 p-6">
            <h3 className="mb-4">Account Balance</h3>
            <div className="space-y-3">
              <div>
                <div className="text-sm text-gray-400">USDT</div>
                <div className="text-xl text-white">5,234.56</div>
              </div>
              <div>
                <div className="text-sm text-gray-400">{selectedPair.split('/')[0]}</div>
                <div className="text-xl text-white">0.2341</div>
              </div>
            </div>
          </Card>
        </div>
      </div>

      {/* Order Preview Dialog */}
      <Dialog open={showPreview} onOpenChange={setShowPreview}>
        <DialogContent className="bg-gray-900 border-gray-800 text-white">
          <DialogHeader>
            <DialogTitle>Confirm Order</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <Alert className={side === 'buy' ? 'bg-emerald-500/10 border-emerald-500/50' : 'bg-red-500/10 border-red-500/50'}>
              <AlertCircle className={`h-4 w-4 ${side === 'buy' ? 'text-emerald-500' : 'text-red-500'}`} />
              <AlertDescription className={side === 'buy' ? 'text-emerald-500' : 'text-red-500'}>
                You are about to {side} {selectedPair.split('/')[0]}
              </AlertDescription>
            </Alert>

            <div className="space-y-3 p-4 bg-gray-800 rounded-lg">
              <div className="flex justify-between">
                <span className="text-gray-400">Pair</span>
                <span className="text-white">{selectedPair}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-400">Order Type</span>
                <Badge>{orderType === 'market' ? 'Market' : 'Limit'}</Badge>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-400">Side</span>
                <Badge className={side === 'buy' ? 'bg-emerald-500/10 text-emerald-500' : 'bg-red-500/10 text-red-500'}>
                  {side === 'buy' ? 'Buy' : 'Sell'}
                </Badge>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-400">Amount</span>
                <span className="text-white">{amount} {selectedPair.split('/')[0]}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-400">Price</span>
                <span className="text-white">
                  ${orderType === 'market' ? currentPair.price.toLocaleString() : parseFloat(price).toLocaleString()}
                </span>
              </div>
              <div className="flex justify-between border-t border-gray-700 pt-3">
                <span className="text-white">Total (incl. fees)</span>
                <span className="text-white text-lg">
                  {(parseFloat(calculateTotal()) * (side === 'buy' ? 1.001 : 0.999)).toFixed(2)} USDT
                </span>
              </div>
            </div>

            <p className="text-sm text-gray-400">
              By confirming, you agree to execute this order at the specified terms.
            </p>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowPreview(false)} className="border-gray-700">
              Cancel
            </Button>
            <Button
              onClick={confirmOrder}
              className={side === 'buy' ? 'bg-emerald-500 text-black hover:bg-emerald-600' : 'bg-red-500 text-white hover:bg-red-600'}
            >
              Confirm {side === 'buy' ? 'Buy' : 'Sell'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
