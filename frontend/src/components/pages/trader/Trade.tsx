import { useState, useEffect } from 'react';
import { TrendingUp, TrendingDown, Info, AlertCircle, Loader2 } from 'lucide-react';
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
import { TradingApi, OrderBook, TradingBalances } from '../../../api/trading';
import { MarketApi, Crypto } from '../../../api/market';

interface TradeProps {
  onNavigate?: (page: string) => void;
}

interface TradingPair {
  symbol: string;
  price: number;
  change: number;
}

export default function Trade({ onNavigate }: TradeProps) {
  const [selectedPair, setSelectedPair] = useState('BTC/USDT');
  const [orderType, setOrderType] = useState<'market' | 'limit'>('market');
  const [side, setSide] = useState<'buy' | 'sell'>('buy');
  const [amount, setAmount] = useState('');
  const [price, setPrice] = useState('');
  const [percentage, setPercentage] = useState([0]);
  const [showPreview, setShowPreview] = useState(false);
  const [loading, setLoading] = useState(true);
  const [loadingOrderBook, setLoadingOrderBook] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  const [pairs, setPairs] = useState<TradingPair[]>([]);
  const [orderBook, setOrderBook] = useState<OrderBook | null>(null);
  const [balances, setBalances] = useState<TradingBalances | null>(null);
  const [cryptos, setCryptos] = useState<Crypto[]>([]);

  // Fetch cryptocurrencies and create trading pairs
  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      setError(null);
      
      try {
        // Fetch top cryptocurrencies for trading pairs
        const cryptosRes = await MarketApi.getCryptocurrencies();
        if (!cryptosRes.ok) {
          setError(cryptosRes.error);
          setLoading(false);
          return;
        }
        
        const topCryptos = cryptosRes.data.slice(0, 10);
        setCryptos(topCryptos);
        
        // Create trading pairs (symbol/USDT)
        const tradingPairs: TradingPair[] = topCryptos.map(crypto => ({
          symbol: `${crypto.symbol.toUpperCase()}/USDT`,
          price: crypto.currentPrice,
          change: crypto.priceChangePercentage24h,
        }));
        
        setPairs(tradingPairs);
        
        // Fetch balances
        const balancesRes = await TradingApi.getBalances();
        if (balancesRes.ok) {
          setBalances(balancesRes.data);
        }
        
        setLoading(false);
      } catch (e: any) {
        setError(e?.message || 'Failed to load trading data');
        setLoading(false);
      }
    };
    
    fetchData();
  }, []);

  // Fetch order book when pair changes
  useEffect(() => {
    const fetchOrderBook = async () => {
      if (!selectedPair) return;
      setLoadingOrderBook(true);
      try {
        const res = await TradingApi.getOrderBook(selectedPair);
        if (res.ok) {
          setOrderBook(res.data);
        } else {
          console.error('Failed to fetch order book:', res.error);
        }
      } catch (e: any) {
        console.error('Error fetching order book:', e);
      } finally {
        setLoadingOrderBook(false);
      }
    };
    
    fetchOrderBook();
    
    // Refresh order book every 3 seconds
    const interval = setInterval(fetchOrderBook, 3000);
    return () => clearInterval(interval);
  }, [selectedPair]);

  const currentPair = pairs.find(p => p.symbol === selectedPair) || pairs[0];
  
  // Get available balance from API
  const getAvailableBalance = () => {
    if (!balances) return side === 'buy' ? 0 : 0;
    const baseSymbol = selectedPair.split('/')[0].toUpperCase();
    if (side === 'buy') {
      // Use USDT balance for buying
      const usdtWallet = balances.wallets.find(w => w.symbol.toUpperCase() === 'USDT');
      return usdtWallet?.available || 0;
    } else {
      // Use crypto balance for selling
      const cryptoWallet = balances.wallets.find(w => w.symbol.toUpperCase() === baseSymbol);
      return cryptoWallet?.available || 0;
    }
  };
  
  const availableBalance = getAvailableBalance();
  const currency = side === 'buy' ? 'USDT' : selectedPair.split('/')[0];

  const handlePercentageChange = (value: number[]) => {
    setPercentage(value);
    const calculatedAmount = (availableBalance * value[0]) / 100;
    setAmount(calculatedAmount.toFixed(side === 'buy' ? 2 : 8));
  };

  const calculateTotal = () => {
    if (!amount) return '0.00';
    const priceToUse = orderType === 'market' 
      ? (orderBook?.currentPrice || currentPair?.price || 0)
      : parseFloat(price) || 0;
    const amountNum = parseFloat(amount) || 0;
    
    if (side === 'buy') {
      // Buying crypto with USDT
      return (amountNum * priceToUse).toFixed(2);
    } else {
      // Selling crypto for USDT
      return (amountNum * priceToUse).toFixed(2);
    }
  };
  
  const formatPrice = (value: number) => {
    if (value >= 1000) return `$${value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    if (value >= 1) return `$${value.toFixed(2)}`;
    return `$${value.toFixed(4)}`;
  };
  
  if (loading) {
    return (
      <div className="p-4 lg:p-8 flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <Loader2 className="w-8 h-8 text-emerald-500 animate-spin mx-auto mb-4" />
          <p className="text-gray-400">Loading trading data...</p>
        </div>
      </div>
    );
  }

  const handleSubmit = async () => {
    setShowPreview(true);
  };

  const confirmOrder = async () => {
    if (!amount || (orderType === 'limit' && !price)) return;
    
    setError(null);
    try {
      const res = await TradingApi.placeOrder({
        symbol: selectedPair,
        side: side === 'buy' ? 'Buy' : 'Sell',
        type: orderType === 'market' ? 'Market' : 'Limit',
        quantity: parseFloat(amount),
        price: orderType === 'limit' ? parseFloat(price) : undefined,
      });
      
      if (!res.ok) {
        setError(res.error);
        setShowPreview(false);
        return;
      }
      
      setShowPreview(false);
      // Clear form
      setAmount('');
      setPrice('');
      setPercentage([0]);
      // Navigate to orders page
      onNavigate?.('orders');
    } catch (e: any) {
      setError(e?.message || 'Failed to place order');
      setShowPreview(false);
    }
  };

  return (
    <div className="p-4 lg:p-8">
      <div className="mb-6">
        <h1 className="text-3xl mb-2">Trade</h1>
        <p className="text-gray-400">Execute market or limit orders</p>
        {error && (
          <Alert className="bg-red-500/10 border-red-500/50 text-red-500 mt-4">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
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
                {currentPair && (
                  <>
                    <span className="text-2xl text-white">
                      {formatPrice(orderBook?.currentPrice || currentPair.price)}
                    </span>
                    <Badge className={(orderBook?.priceChangePercentage24h ?? currentPair.change) >= 0 ? 'bg-emerald-500/10 text-emerald-500' : 'bg-red-500/10 text-red-500'}>
                      {(orderBook?.priceChangePercentage24h ?? currentPair.change) >= 0 ? <TrendingUp className="w-3 h-3 mr-1" /> : <TrendingDown className="w-3 h-3 mr-1" />}
                      {Math.abs(orderBook?.priceChangePercentage24h ?? currentPair.change).toFixed(2)}%
                    </Badge>
                  </>
                )}
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
                  <p className="text-sm text-gray-400 mt-1">
                    Current price: {formatPrice(orderBook?.currentPrice || currentPair?.price || 0)}
                  </p>
                </div>
              </TabsContent>
            </Tabs>

            {/* Amount */}
            <div>
              <div className="flex items-center justify-between mb-2">
                <Label htmlFor="amount">Amount ({side === 'buy' ? selectedPair.split('/')[0] : selectedPair.split('/')[0]})</Label>
                <span className="text-sm text-gray-400">
                  Available: {availableBalance.toLocaleString('en-US', { 
                    minimumFractionDigits: side === 'buy' ? 2 : 8, 
                    maximumFractionDigits: side === 'buy' ? 2 : 8 
                  })} {currency}
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
                {loadingOrderBook ? (
                  <div className="text-center py-4">
                    <Loader2 className="w-4 h-4 text-emerald-500 animate-spin mx-auto" />
                  </div>
                ) : orderBook?.asks.length ? (
                  orderBook.asks.slice(0, 5).map((ask, i) => (
                    <div key={i} className="flex justify-between text-sm py-1">
                      <span className="text-red-500">{formatPrice(ask.price)}</span>
                      <span className="text-gray-400">{ask.amount.toFixed(4)}</span>
                    </div>
                  ))
                ) : (
                  <div className="text-gray-400 text-sm py-2">No orders</div>
                )}
              </div>

              {/* Current Price */}
              <div className="py-2 text-center border-y border-gray-800">
                {orderBook ? (
                  <>
                    <div className="text-xl text-emerald-500">{formatPrice(orderBook.currentPrice)}</div>
                    <div className="text-xs text-gray-400 mt-1">
                      {orderBook.priceChangePercentage24h >= 0 ? '+' : ''}
                      {orderBook.priceChangePercentage24h.toFixed(2)}%
                    </div>
                  </>
                ) : (
                  <Loader2 className="w-5 h-5 text-emerald-500 animate-spin mx-auto" />
                )}
              </div>

              {/* Bids */}
              <div>
                <div className="text-sm text-gray-400 mb-2">Buy Orders</div>
                {loadingOrderBook ? (
                  <div className="text-center py-4">
                    <Loader2 className="w-4 h-4 text-emerald-500 animate-spin mx-auto" />
                  </div>
                ) : orderBook?.bids.length ? (
                  orderBook.bids.slice(0, 5).map((bid, i) => (
                    <div key={i} className="flex justify-between text-sm py-1">
                      <span className="text-emerald-500">{formatPrice(bid.price)}</span>
                      <span className="text-gray-400">{bid.amount.toFixed(4)}</span>
                    </div>
                  ))
                ) : (
                  <div className="text-gray-400 text-sm py-2">No orders</div>
                )}
              </div>
            </div>
          </Card>

          <Card className="bg-gray-900 border-gray-800 p-6">
            <h3 className="mb-4">Account Balance</h3>
            {balances ? (
              <div className="space-y-3">
                <div className="flex justify-between">
                  <span className="text-gray-400">Total Balance</span>
                  <span className="text-white">${balances.totalBalance.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Available</span>
                  <span className="text-emerald-500">${balances.availableBalance.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Locked</span>
                  <span className="text-yellow-500">${balances.lockedBalance.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
                <div className="pt-3 border-t border-gray-800 mt-3 space-y-2">
                  {balances.wallets.slice(0, 3).map((wallet) => (
                    <div key={wallet.symbol} className="flex justify-between text-sm">
                      <span className="text-gray-400">{wallet.symbol}</span>
                      <span className="text-white">{wallet.available.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 8 })}</span>
                    </div>
                  ))}
                </div>
              </div>
            ) : (
              <div className="text-center py-4">
                <Loader2 className="w-5 h-5 text-emerald-500 animate-spin mx-auto" />
              </div>
            )}
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
