import { useState, useEffect } from 'react';
import { Copy, Upload, CheckCircle2, Info, CreditCard } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Label } from '../../ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../ui/select';
import { Alert, AlertDescription } from '../../ui/alert';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../ui/tabs';
import { Badge } from '../../ui/badge';
import { PaymentApi } from '../../../api/payment';
import { TradingApi } from '../../../api/trading';

export default function Deposit() {
  const [selectedCurrency, setSelectedCurrency] = useState('BTC');
  const [amount, setAmount] = useState('');
  const [vndAmount, setVndAmount] = useState('');
  const [uploadedFile, setUploadedFile] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const depositAddress = '1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa';

  const cryptoCurrencies = [
    { symbol: 'BTC', name: 'Bitcoin', network: 'Bitcoin', minDeposit: 0.0001, fee: 0 },
    { symbol: 'ETH', name: 'Ethereum', network: 'Ethereum (ERC20)', minDeposit: 0.01, fee: 0 },
    { symbol: 'USDT', name: 'Tether', network: 'Ethereum (ERC20)', minDeposit: 10, fee: 0 },
  ];

  const fiatMethods = [
    { method: 'VNPay', currency: 'VND', fee: '0%', processing: 'Instant', icon: CreditCard },
  ];

  // Kiểm tra URL params khi component mount (callback từ VNPay)
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const status = params.get('status');
    const amount = params.get('amount');
    const message = params.get('message');

    if (status === 'success') {
      setSuccess(`Deposit thành công! Số tiền: ${amount ? amount + ' VND' : ''}`);
      
      // Refresh balance sau khi deposit thành công
      TradingApi.getBalances().then(res => {
        if (res.ok) {
          console.log('Balance refreshed after deposit:', res.data);
          // Trigger custom event để các component khác có thể refresh balance
          window.dispatchEvent(new CustomEvent('balanceUpdated'));
        } else {
          console.error('Failed to refresh balance:', res.error);
        }
      });
      
      // Xóa params khỏi URL
      window.history.replaceState({}, '', window.location.pathname);
    } else if (status === 'failed') {
      setError(message || 'Deposit thất bại. Vui lòng thử lại.');
      window.history.replaceState({}, '', window.location.pathname);
    } else if (status === 'error') {
      setError('Có lỗi xảy ra khi xử lý giao dịch.');
      window.history.replaceState({}, '', window.location.pathname);
    }
  }, []);

  const recentDeposits = [
    { id: 'DEP-001', currency: 'BTC', amount: 0.05, status: 'Completed', time: '2025-01-14 10:15' },
    { id: 'DEP-002', currency: 'USDT', amount: 1000, status: 'Processing', time: '2025-01-13 16:45' },
  ];

  const handleCopy = () => {
    navigator.clipboard.writeText(depositAddress);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setUploadedFile(file.name);
    }
  };

  // Xử lý VNPay deposit
  const handleVnpayDeposit = async () => {
    setError('');
    setSuccess('');

    if (!vndAmount || parseFloat(vndAmount) < 10000) {
      setError('Số tiền tối thiểu là 10,000 VND');
      return;
    }

    setLoading(true);
    try {
      const result = await PaymentApi.createVnpayDeposit({
        amount: parseFloat(vndAmount),
      });

      if (!result.ok) {
        setError(result.error || 'Có lỗi xảy ra khi tạo giao dịch');
        setLoading(false);
        return;
      }

      if (result.data.paymentUrl) {
        // Redirect đến VNPay
        window.location.href = result.data.paymentUrl;
      }
    } catch (err: any) {
      console.error('Error creating deposit:', err);
      setError(err?.message || 'Có lỗi xảy ra khi tạo giao dịch');
      setLoading(false);
    }
  };

  const currentCurrency = cryptoCurrencies.find(c => c.symbol === selectedCurrency) || cryptoCurrencies[0];

  return (
    <div className="p-4 lg:p-8">
      <div className="mb-6">
        <h1 className="text-3xl mb-2">Deposit Funds</h1>
        <p className="text-gray-400">Add funds to your account</p>
      </div>

      {/* Hiển thị thông báo */}
      {error && (
        <Alert className="mb-4 bg-red-500/10 border-red-500/50">
          <AlertDescription className="text-red-500">{error}</AlertDescription>
        </Alert>
      )}

      {success && (
        <Alert className="mb-4 bg-emerald-500/10 border-emerald-500/50">
          <AlertDescription className="text-emerald-500">{success}</AlertDescription>
        </Alert>
      )}

      <div className="grid lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2 bg-gray-900 border-gray-800 p-6">
          <Tabs defaultValue="crypto">
            <TabsList className="bg-gray-800 w-full mb-6">
              <TabsTrigger value="crypto" className="flex-1 data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
                Cryptocurrency
              </TabsTrigger>
              <TabsTrigger value="fiat" className="flex-1 data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
                Fiat (VNPay)
              </TabsTrigger>
            </TabsList>

            <TabsContent value="crypto" className="space-y-6">
              <div>
                <Label>Select Currency</Label>
                <Select value={selectedCurrency} onValueChange={setSelectedCurrency}>
                  <SelectTrigger className="bg-gray-800 border-gray-700">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent className="bg-gray-800 border-gray-700 text-white">
                    {cryptoCurrencies.map((currency) => (
                      <SelectItem key={currency.symbol} value={currency.symbol}>
                        {currency.name} ({currency.symbol})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <Alert className="bg-blue-500/10 border-blue-500/50">
                <Info className="h-4 w-4 text-blue-500" />
                <AlertDescription className="text-blue-500">
                  <strong>Network:</strong> {currentCurrency.network}<br />
                  <strong>Minimum Deposit:</strong> {currentCurrency.minDeposit} {currentCurrency.symbol}
                </AlertDescription>
              </Alert>

              <div>
                <Label>Deposit Address</Label>
                <div className="flex gap-2 mt-2">
                  <Input
                    value={depositAddress}
                    readOnly
                    className="bg-gray-800 border-gray-700 text-white"
                  />
                  <Button
                    onClick={handleCopy}
                    className="bg-emerald-500 text-black hover:bg-emerald-600"
                  >
                    {copied ? <CheckCircle2 className="w-4 h-4" /> : <Copy className="w-4 h-4" />}
                  </Button>
                </div>
                <p className="text-sm text-gray-400 mt-2">
                  Send {selectedCurrency} to this address. Funds will be credited after network confirmations.
                </p>
              </div>

              <div className="border border-gray-800 rounded-lg p-8 text-center">
                <div className="w-48 h-48 bg-white rounded-lg mx-auto mb-4 flex items-center justify-center">
                  <span className="text-gray-900">QR Code</span>
                </div>
                <p className="text-gray-400 text-sm">Scan this QR code to get the deposit address</p>
              </div>

              <Alert className="bg-yellow-500/10 border-yellow-500/50">
                <Info className="h-4 w-4 text-yellow-500" />
                <AlertDescription className="text-yellow-500">
                  <strong>Important:</strong> Only send {selectedCurrency} to this address. Sending other assets may result in permanent loss.
                </AlertDescription>
              </Alert>
            </TabsContent>

            <TabsContent value="fiat" className="space-y-6">
              <div>
                <Label>Payment Method</Label>
                <div className="space-y-3 mt-3">
                  {fiatMethods.map((method) => (
                    <div
                      key={method.method}
                      className="flex items-center justify-between p-4 bg-gray-800 rounded-lg hover:bg-gray-700 transition-colors"
                    >
                      <div className="flex items-center gap-3">
                        <method.icon className="w-6 h-6 text-emerald-500" />
                        <div>
                          <div className="text-white mb-1 font-semibold">{method.method}</div>
                          <div className="text-sm text-gray-400">Processing: {method.processing}</div>
                        </div>
                      </div>
                      <Badge variant="outline" className="border-emerald-500 text-emerald-500">
                        Fee: {method.fee}
                      </Badge>
                    </div>
                  ))}
                </div>
              </div>

              <div>
                <Label htmlFor="vnd-amount">Amount (VND)</Label>
                <Input
                  id="vnd-amount"
                  type="number"
                  placeholder="100000"
                  value={vndAmount}
                  onChange={(e) => setVndAmount(e.target.value)}
                  className="bg-gray-800 border-gray-700 text-white"
                  min="10000"
                  step="1000"
                />
                <p className="text-sm text-gray-400 mt-2">Minimum: 10,000 VND</p>
                {vndAmount && parseFloat(vndAmount) >= 10000 && (
                  <p className="text-sm text-emerald-500 mt-1">
                    ≈ ${(parseFloat(vndAmount) / 24000).toFixed(2)} USD
                  </p>
                )}
              </div>

              <Button
                onClick={handleVnpayDeposit}
                disabled={loading || !vndAmount || parseFloat(vndAmount) < 10000}
                className="w-full bg-emerald-500 text-black hover:bg-emerald-600 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {loading ? 'Đang xử lý...' : 'Thanh toán qua VNPay'}
              </Button>

              <Alert className="bg-blue-500/10 border-blue-500/50">
                <Info className="h-4 w-4 text-blue-500" />
                <AlertDescription className="text-blue-500 text-sm">
                  <strong>Lưu ý:</strong> Bạn sẽ được chuyển hướng đến trang thanh toán VNPay. 
                  Sau khi thanh toán thành công, bạn sẽ được chuyển về trang này.
                </AlertDescription>
              </Alert>
            </TabsContent>
          </Tabs>
        </Card>

        <div className="space-y-6">
          <Card className="bg-gray-900 border-gray-800 p-6">
            <h3 className="mb-4">Deposit Instructions</h3>
            <div className="space-y-3 text-sm">
              <div className="flex gap-2">
                <span className="text-emerald-500">1.</span>
                <span className="text-gray-300">Select your preferred currency</span>
              </div>
              <div className="flex gap-2">
                <span className="text-emerald-500">2.</span>
                <span className="text-gray-300">Copy the deposit address or scan QR code</span>
              </div>
              <div className="flex gap-2">
                <span className="text-emerald-500">3.</span>
                <span className="text-gray-300">Send funds from your wallet</span>
              </div>
              <div className="flex gap-2">
                <span className="text-emerald-500">4.</span>
                <span className="text-gray-300">Wait for network confirmations</span>
              </div>
              <div className="flex gap-2">
                <span className="text-emerald-500">5.</span>
                <span className="text-gray-300">Funds will be credited automatically</span>
              </div>
            </div>
          </Card>

          <Card className="bg-gray-900 border-gray-800 p-6">
            <h3 className="mb-4">Recent Deposits</h3>
            <div className="space-y-3">
              {recentDeposits.map((deposit) => (
                <div key={deposit.id} className="p-3 bg-gray-800 rounded-lg">
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-white">{deposit.amount} {deposit.currency}</span>
                    <Badge className={deposit.status === 'Completed' ? 'bg-emerald-500/10 text-emerald-500' : 'bg-yellow-500/10 text-yellow-500'}>
                      {deposit.status}
                    </Badge>
                  </div>
                  <div className="text-sm text-gray-400">{deposit.time}</div>
                  <div className="text-sm text-gray-500">{deposit.id}</div>
                </div>
              ))}
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}
