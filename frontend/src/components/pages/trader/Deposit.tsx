import { useState, useEffect } from 'react';
import {
  Copy,
  Upload,
  CheckCircle2,
  Info,
  CreditCard,
  Sparkles,
  Shield,
  Headphones,
  ArrowRight,
} from 'lucide-react';
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
  const [stripeLoading, setStripeLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [selectedFiatMethod, setSelectedFiatMethod] = useState<'vnpay' | 'stripe'>('vnpay');
  const [stripeAmount, setStripeAmount] = useState('');

  const depositAddress = '1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa';
  const STRIPE_MAX_AMOUNT = 999999.99;
  const formattedStripeMax = STRIPE_MAX_AMOUNT.toLocaleString('en-US', { maximumFractionDigits: 2 });

  const cryptoCurrencies = [
    { symbol: 'BTC', name: 'Bitcoin', network: 'Bitcoin', minDeposit: 0.0001, fee: 0 },
    { symbol: 'ETH', name: 'Ethereum', network: 'Ethereum (ERC20)', minDeposit: 0.01, fee: 0 },
    { symbol: 'USDT', name: 'Tether', network: 'Ethereum (ERC20)', minDeposit: 10, fee: 0 },
  ];

  const fiatMethods = [
    { id: 'vnpay', method: 'VNPay', currency: 'VND', fee: '0%', processing: 'Instant', icon: Upload },
    { id: 'stripe', method: 'Stripe (Card)', currency: 'USD', fee: '2.9% + $0.30', processing: 'Instant', icon: CreditCard },
  ] as const;

  const instructionSteps = [
    'Select the cryptocurrency or fiat method you prefer.',
    'Copy the deposit address or launch the payment gateway.',
    'Send funds from your secure wallet/payment account.',
    'Wait for network confirmations or processor approval.',
    'Track status in Recent Deposits — funds auto-credit.',
  ];

  const quickHighlights = [
    {
      title: 'Security First',
      description:
        'Cold-storage custody, 2FA enforcement và giám sát giao dịch liên tục giúp nạp tiền an toàn.',
      icon: Shield,
    },
    {
      title: '1:1 Support',
      description:
        'Nhóm concierge 24/7 sẵn sàng hỗ trợ các khoản nạp lớn hoặc yêu cầu xác nhận thủ công.',
      icon: Headphones,
    },
  ];

      // Handle redirect parameters from payment providers
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const status = params.get('status');
    const amountParam = params.get('amount');
    const message = params.get('message');
    const provider = params.get('provider');
    const sessionId = params.get('sessionId');

    const resetUrl = () => window.history.replaceState({}, '', window.location.pathname);
    const refreshBalances = () =>
      TradingApi.getBalances()
        .then((res) => {
          if (res.ok) {
            window.dispatchEvent(new CustomEvent('balanceUpdated'));
          } else {
            console.error('Failed to refresh balance:', res.error);
          }
        })
        .catch((err) => console.error('Failed to refresh balance:', err));

    if (status === 'success' && provider === 'stripe' && sessionId) {
      (async () => {
        const sessionResult = await PaymentApi.getStripeSession(sessionId);
        if (!sessionResult.ok) {
          setError(sessionResult.error || 'Unable to load Stripe deposit details.');
          resetUrl();
          return;
        }

        const confirmResult = await PaymentApi.confirmStripeDeposit(sessionId);
        if (!confirmResult.ok || !confirmResult.data) {
          setError(confirmResult.error || 'Unable to finalize Stripe deposit.');
          resetUrl();
          return;
        }

        const currency = (sessionResult.data.sessionCurrency || 'usd').toUpperCase();
        const credited = confirmResult.data.creditedAmount;
        const formattedAmount = credited.toFixed(2);
        setSuccess(`Stripe deposit credited: ${currency === 'USD' ? '$' : ''}${formattedAmount} ${currency}`);
        refreshBalances();
        resetUrl();
      })();
      return;
    }

    if (status === 'success') {
      setSuccess(`Deposit successful${amountParam ? ` - Amount: ${amountParam} VND` : ''}`);
      refreshBalances();
      resetUrl();
    } else if (status === 'failed') {
      setError(message || 'Deposit failed. Please try again.');
      resetUrl();
    } else if (status === 'error') {
      setError('An error occurred while processing the transaction.');
      resetUrl();
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
      setError('Minimum amount is 10,000 VND');
      return;
    }

    setLoading(true);
    try {
      const result = await PaymentApi.createVnpayDeposit({
        amount: parseFloat(vndAmount),
      });

      if (!result.ok) {
        setError(result.error || 'An error occurred while creating the VNPay transaction');
        setLoading(false);
        return;
      }

      if (result.data.paymentUrl) {
        window.location.href = result.data.paymentUrl;
      }
    } catch (err: any) {
      console.error('Error creating deposit:', err);
      setError(err?.message || 'An error occurred while creating the VNPay transaction');
      setLoading(false);
    }
  };
  const handleStripeDeposit = async () => {
    setError('');
    setSuccess('');

    const parsedAmount = parseFloat(stripeAmount);

    if (!stripeAmount || parsedAmount < 1) {
      setError('Stripe deposit requires at least $1');
      return;
    }

    if (parsedAmount > STRIPE_MAX_AMOUNT) {
      setError(`Stripe deposit limit is $${STRIPE_MAX_AMOUNT.toLocaleString('en-US', { maximumFractionDigits: 2 })} per transaction.`);
      return;
    }

    setStripeLoading(true);
    try {
      const result = await PaymentApi.createStripeDeposit({
        amount: parsedAmount,
        currency: 'USD',
      });

      if (!result.ok) {
        setError(result.error || 'Failed to create Stripe checkout session');
        return;
      }

      if (result.data.checkoutUrl) {
        window.location.href = result.data.checkoutUrl;
      } else {
        setError('Stripe checkout session is missing a redirect URL');
      }
    } catch (err: any) {
      console.error('Error creating Stripe deposit:', err);
      setError(err?.message || 'Failed to create Stripe checkout session');
    } finally {
      setStripeLoading(false);
    }
  };

  const currentCurrency = cryptoCurrencies.find(c => c.symbol === selectedCurrency) || cryptoCurrencies[0];

  return (
    <div className="p-4 lg:p-8 space-y-6 bg-slate-950/40">
      <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-6">
        <div>
          <p className="text-xs tracking-[0.35em] text-emerald-400 uppercase">Funding Desk</p>
          <h1 className="text-3xl font-semibold text-white mt-2">Deposit Funds</h1>
          <p className="text-gray-400 mt-1">Top up your trading balance via secure cryptocurrency rails or instant fiat gateways.</p>
        </div>
        <div className="grid sm:grid-cols-3 gap-4 w-full lg:w-auto">
          {[
            { label: 'Fiat Methods', value: 'VNPay & Stripe', caption: 'Instant settlement' },
            { label: 'Network Fees', value: '0.0% - 0.3%', caption: 'Exchange subsidized' },
            { label: 'Support', value: '24/7 Desk', caption: 'Priority concierge' },
          ].map((tile) => (
            <div key={tile.label} className="rounded-xl bg-gray-900/60 border border-gray-800 px-4 py-3">
              <p className="text-[11px] uppercase tracking-wide text-gray-500">{tile.label}</p>
              <p className="text-lg font-semibold text-white">{tile.value}</p>
              <p className="text-xs text-gray-500">{tile.caption}</p>
            </div>
          ))}
        </div>
      </div>

      {(error || success) && (
        <Alert className={`${error ? 'border-red-500/60 bg-red-500/5 text-red-300' : 'border-emerald-500/60 bg-emerald-500/5 text-emerald-300'} transition-colors duration-200`}>
          <AlertDescription>{error || success}</AlertDescription>
        </Alert>
      )}

      <div className="grid xl:grid-cols-3 gap-6">
        <div className="space-y-6 xl:col-span-2">
          <Card className="bg-gray-900/80 border-gray-800 shadow-2xl shadow-emerald-500/5">
            <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 border-b border-gray-800 pb-5 mb-6">
              <div>
                <p className="text-sm text-gray-400">Choose a funding rail</p>
                <h2 className="text-2xl text-white font-semibold">Crypto or instant fiat</h2>
              </div>
              <div className="flex items-center gap-2 text-xs text-gray-400">
                <Sparkles className="w-4 h-4 text-emerald-400" />
                <span>Ledger-synced in real time</span>
              </div>
            </div>

            <Tabs defaultValue="crypto">
              <TabsList className="bg-gray-800/80 w-full mb-6 rounded-xl">
                <TabsTrigger value="crypto" className="flex-1 data-[state=active]:bg-emerald-500 data-[state=active]:text-black rounded-lg">Crypto Deposit</TabsTrigger>
                <TabsTrigger value="fiat" className="flex-1 data-[state=active]:bg-emerald-500 data-[state=active]:text-black rounded-lg">Fiat (VNPay & Stripe)</TabsTrigger>
              </TabsList>

              <TabsContent value="crypto" className="space-y-6">
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <Label>Select currency</Label>
                    <Select value={selectedCurrency} onValueChange={setSelectedCurrency}>
                      <SelectTrigger className="mt-2 bg-gray-800 border-gray-700">
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
                  <Alert className="bg-blue-500/5 border-blue-500/20">
                    <Info className="h-4 w-4 text-blue-400" />
                    <AlertDescription className="text-blue-200 text-sm">
                      Network: {currentCurrency.network} · Minimum: {currentCurrency.minDeposit} {currentCurrency.symbol}
                    </AlertDescription>
                  </Alert>
                </div>

                <div className="bg-gradient-to-br from-gray-900 to-gray-800 border border-gray-800 rounded-2xl p-4 md:p-6">
                  <div className="flex items-center justify-between mb-3">
                    <Label className="text-white">Deposit Address</Label>
                    <span className="text-xs text-gray-500">Hot wallet • monitored</span>
                  </div>
                  <div className="flex gap-2">
                    <Input value={depositAddress} readOnly className="bg-gray-900 border-gray-700 text-white" />
                    <Button onClick={handleCopy} className="bg-emerald-500 text-black hover:bg-emerald-400">
                      {copied ? <CheckCircle2 className="w-4 h-4" /> : <Copy className="w-4 h-4" />}
                    </Button>
                  </div>
                  <p className="text-xs text-gray-400 mt-2">Send {selectedCurrency} to the address above. Funds credit after the configured confirmation threshold.</p>
                </div>

                <div className="grid md:grid-cols-2 gap-4">
                  <div className="border border-dashed border-gray-700 rounded-2xl p-6 text-center">
                    <div className="w-40 h-40 bg-white/80 rounded-xl mx-auto mb-4 flex items-center justify-center text-gray-900 font-semibold">QR</div>
                    <p className="text-gray-400 text-sm">Scan to push the address to your mobile wallet.</p>
                  </div>
                  <Alert className="bg-yellow-500/5 border-yellow-500/30 rounded-2xl">
                    <Info className="h-4 w-4 text-yellow-400" />
                    <AlertDescription className="text-yellow-100 text-sm">Send only {selectedCurrency}. Deposits of other assets to this address result in unrecoverable loss.</AlertDescription>
                  </Alert>
                </div>
              </TabsContent>

              <TabsContent value="fiat" className="space-y-6">
                <div className="grid gap-3 md:grid-cols-2">
                  {fiatMethods.map((method) => (
                    <button
                      key={method.id}
                      type="button"
                      onClick={() => setSelectedFiatMethod(method.id)}
                      className={`p-4 rounded-2xl border transition-all ${selectedFiatMethod === method.id ? 'border-emerald-500/80 bg-emerald-500/10 shadow-emerald-500/10 shadow-lg' : 'border-gray-800 bg-gray-900/40 hover:border-gray-700'}`}
                    >
                      <div className="flex items-center gap-4">
                        <div className="w-10 h-10 rounded-full bg-emerald-500/10 flex items-center justify-center text-emerald-400">
                          <method.icon className="w-5 h-5" />
                        </div>
                        <div>
                          <p className="text-white font-semibold">{method.method}</p>
                          <p className="text-xs text-gray-400">Processing: {method.processing}</p>
                        </div>
                      </div>
                      <div className="mt-4 flex items-center justify-between text-xs text-gray-400">
                        <span>{method.currency}</span>
                        <Badge variant="outline" className="border-emerald-500 text-emerald-400">Fee: {method.fee}</Badge>
                      </div>
                    </button>
                  ))}
                </div>

                {selectedFiatMethod === 'vnpay' && (
                  <div className="space-y-4">
                    <div>
                      <Label htmlFor="vnd-amount">Amount (VND)</Label>
                      <Input
                        id="vnd-amount"
                        type="number"
                        placeholder="100000"
                        value={vndAmount}
                        onChange={(e) => setVndAmount(e.target.value)}
                        className="bg-gray-800 border-gray-700 text-white mt-2"
                      />
                      <p className="text-xs text-gray-400 mt-2">Minimum 10,000 VND • Tỷ giá cập nhật mỗi 60 giây.</p>
                    </div>
                    {vndAmount && parseFloat(vndAmount) >= 10000 && (
                      <div className="pl-3 text-sm text-emerald-400">˜ {(parseFloat(vndAmount) / 24000).toFixed(2)} USD</div>
                    )}
                    <Button
                      type="button"
                      onClick={handleVnpayDeposit}
                      disabled={loading || !vndAmount || parseFloat(vndAmount) < 10000}
                      className="w-full bg-emerald-500 text-black hover:bg-emerald-400 disabled:opacity-50"
                    >
                      {loading ? 'Redirecting to VNPay…' : 'Pay with VNPay'}
                    </Button>
                    <Alert className="bg-blue-500/5 border-blue-500/30">
                      <Info className="h-4 w-4 text-blue-400" />
                      <AlertDescription className="text-blue-200 text-sm">You will be redirected to VNPay to authorize the payment and automatically returned upon completion.</AlertDescription>
                    </Alert>
                  </div>
                )}

                {selectedFiatMethod === 'stripe' && (
                  <div className="space-y-4">
                    <div>
                      <Label htmlFor="stripe-amount">Amount (USD)</Label>
                      <Input
                        id="stripe-amount"
                        type="number"
                        placeholder="1000"
                        value={stripeAmount}
                        onChange={(e) => setStripeAmount(e.target.value)}
                        className="bg-gray-800 border-gray-700 text-white mt-2"
                      />
                      <p className="text-xs text-gray-400 mt-2">Minimum $1 • Maximum ${formattedStripeMax}</p>
                    </div>
                    <Button
                      type="button"
                      onClick={handleStripeDeposit}
                      disabled={
                        stripeLoading ||
                        !stripeAmount ||
                        parseFloat(stripeAmount) < 1 ||
                        parseFloat(stripeAmount) > STRIPE_MAX_AMOUNT
                      }
                      className="w-full bg-emerald-500 text-black hover:bg-emerald-400 disabled:opacity-50"
                    >
                      {stripeLoading ? 'Launching Stripe Checkout...' : 'Pay with Stripe'}
                    </Button>
                    <Alert className="bg-indigo-500/5 border-indigo-500/30">
                      <Info className="h-4 w-4 text-indigo-300" />
                      <AlertDescription className="text-indigo-100 text-sm">Use Stripe test cards (e.g. 4242 4242 4242 4242, 12/34, CVC 123) to simulate payments.</AlertDescription>
                    </Alert>
                  </div>
                )}
              </TabsContent>
            </Tabs>
          </Card>

          <div className="grid md:grid-cols-2 gap-4">
            {quickHighlights.map((item) => (
              <Card key={item.title} className="bg-gradient-to-br from-gray-900 to-gray-800 border-gray-800">
                <div className="flex gap-4">
                  <div className="w-12 h-12 rounded-full bg-emerald-500/10 flex items-center justify-center text-emerald-400">
                    <item.icon className="w-5 h-5" />
                  </div>
                  <div>
                    <p className="text-white font-semibold">{item.title}</p>
                    <p className="text-sm text-gray-400 leading-relaxed">{item.description}</p>
                  </div>
                </div>
              </Card>
            ))}
          </div>
        </div>

        <div className="space-y-6">
          <Card className="bg-gray-900/70 border-gray-800 p-6">
            <h3 className="text-white font-semibold mb-4">Deposit Checklist</h3>
            <div className="space-y-4">
              {instructionSteps.map((step, idx) => (
                <div key={step} className="flex gap-3">
                  <div className="w-6 h-6 rounded-full bg-emerald-500/20 text-emerald-400 text-xs flex items-center justify-center">{idx + 1}</div>
                  <p className="text-sm text-gray-300">{step}</p>
                </div>
              ))}
            </div>
          </Card>

          <Card className="bg-gray-900/70 border-gray-800 p-6">
            <h3 className="text-white font-semibold mb-4">Quick Tips</h3>
            <ul className="space-y-3 text-sm text-gray-300">
              <li className="flex gap-2"><Sparkles className="w-4 h-4 text-emerald-400 mt-0.5" /><span>Large deposits (&gt; $250k) qualify for dedicated routing — message us before sending for pre-authorization.</span></li>
              <li className="flex gap-2"><Shield className="w-4 h-4 text-emerald-400 mt-0.5" /><span>Always double-check memo tags if required. Missing memos are the #1 reason for delays.</span></li>
              <li className="flex gap-2"><Headphones className="w-4 h-4 text-emerald-400 mt-0.5" /><span>Need proof-of-funds? Support can generate exportable receipts once your deposit clears.</span></li>
            </ul>
          </Card>

          <Card className="bg-gray-900/70 border-gray-800 p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-white font-semibold">Recent Deposits</h3>
              <Button variant="ghost" size="sm" className="text-emerald-400 hover:text-emerald-300">
                View all <ArrowRight className="w-4 h-4 ml-1" />
              </Button>
            </div>
            <div className="space-y-3">
              {recentDeposits.map((deposit) => (
                <div key={deposit.id} className="p-4 rounded-xl border border-gray-800 bg-gray-900/40">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="text-white font-semibold">{deposit.amount} {deposit.currency}</p>
                      <p className="text-xs text-gray-500">{deposit.time}</p>
                    </div>
                    <Badge className={deposit.status === 'Completed' ? 'bg-emerald-500/10 text-emerald-400' : 'bg-yellow-500/10 text-yellow-400'}>
                      {deposit.status}
                    </Badge>
                  </div>
                  <p className="text-xs text-gray-500 mt-1">Ref: {deposit.id}</p>
                </div>
              ))}
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}


