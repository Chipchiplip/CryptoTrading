import { useState, useEffect } from 'react';
import {
  Upload,
  CheckCircle2,
  CreditCard,
  Shield,
  TrendingUp,
  Clock,
  Wallet,
  ArrowLeft,
} from 'lucide-react';

import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Label } from '../../ui/label';
import { Alert, AlertDescription } from '../../ui/alert';
import { Badge } from '../../ui/badge';
import { PaymentApi } from '../../../api/payment';
import { TradingApi } from '../../../api/trading';

export default function Deposit() {
  const [amount, setAmount] = useState('5000');
  const [vndAmount, setVndAmount] = useState('');
  const [loading, setLoading] = useState(false);
  const [stripeLoading, setStripeLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [selectedMethod, setSelectedMethod] = useState<'vnpay' | 'stripe'>('stripe');
  const [currentBalance, setCurrentBalance] = useState(0);
  const STRIPE_MAX_AMOUNT = 999999.99;

  // Fetch current balance
  useEffect(() => {
    const fetchBalance = async () => {
      try {
        const result = await TradingApi.getBalances();
        if (result.ok && result.data) {
          setCurrentBalance(result.data.availableBalance || 0);
        }
      } catch (err) {
        console.error('Failed to fetch balance:', err);
      }
    };
    fetchBalance();

    // Listen for balance update events
    const handleBalanceUpdate = () => {
      fetchBalance();
    };
    window.addEventListener('balanceUpdated', handleBalanceUpdate);
    return () => window.removeEventListener('balanceUpdated', handleBalanceUpdate);
  }, []);

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
        if (!confirmResult.ok) {
          setError('Unable to finalize Stripe deposit.');
          resetUrl();
          return;
        }

        if (!confirmResult.data) {
          setError('Deposit confirmed but no data returned.');
          resetUrl();
          return;
        }

        const currency = (sessionResult.data.sessionCurrency || 'usd').toUpperCase();
        const credited = confirmResult.data.creditedAmount;
        const formattedAmount = credited.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        setSuccess(`✅ Nạp tiền thành công! ${currency === 'USD' ? '$' : ''}${formattedAmount} ${currency} đã được cộng vào tài khoản của bạn.`);
        refreshBalances();
        resetUrl();
      })();
      return;
    }

    if (status === 'success') {
      const formattedVndAmount = amountParam ? parseFloat(amountParam).toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 }) : '';
      setSuccess(`✅ Nạp tiền thành công! ${formattedVndAmount ? `₫${formattedVndAmount} VND` : 'Số tiền'} đã được cộng vào tài khoản của bạn.`);
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

  // Xử lý VNPay deposit
  const handleVnpayDeposit = async () => {
    setError('');
    setSuccess('');

    const depositAmount = selectedMethod === 'vnpay' ? parseFloat(vndAmount) : parseFloat(amount);

    if (selectedMethod === 'vnpay' && (!vndAmount || depositAmount < 10000)) {
      setError('Minimum amount is 10,000 VND');
      return;
    }

    setLoading(true);
    try {
      const result = await PaymentApi.createVnpayDeposit({
        amount: depositAmount,
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

    const parsedAmount = parseFloat(amount);

    if (!amount || parsedAmount < 1) {
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

  // Calculate deposit amount (no processing fee)
  const depositAmount = selectedMethod === 'vnpay'
    ? parseFloat(vndAmount) || 0
    : parseFloat(amount) || 0;
  const totalAmount = depositAmount;

  const quickAmounts = [100, 500, 1000];

  const handleConfirmDeposit = () => {
    if (selectedMethod === 'stripe') {
      handleStripeDeposit();
    } else if (selectedMethod === 'vnpay') {
      handleVnpayDeposit();
    }
  };

  return (
    <div className="bg-black text-white p-4 md:p-8">
      {/* Header */}
      <div className="max-w-7xl mx-auto mb-8">
        <div className="flex items-center justify-between">
          <div>
            <button className="flex items-center gap-2 text-gray-400 hover:text-white mb-4">
              <ArrowLeft className="w-4 h-4" />
              <span className="text-sm">Back</span>
            </button>
            <h1 className="text-3xl md:text-4xl font-bold mb-2">Deposit Funds</h1>
            <p className="text-gray-400">Add funds to your trading account</p>
          </div>
          <div className="flex items-center gap-3 bg-gray-900/50 border border-emerald-500/30 rounded-2xl px-6 py-4">
            <Wallet className="w-5 h-5 text-emerald-400" />
            <div>
              <p className="text-xs text-gray-400">Available Balance</p>
              <p className="text-2xl font-bold text-emerald-400">${currentBalance.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Error/Success Messages */}
      {(error || success) && (
        <div className="max-w-7xl mx-auto mb-6">
          <Alert className={`${error ? 'border-red-500/60 bg-red-500/10 text-red-300' : 'border-emerald-500/60 bg-emerald-500/10 text-emerald-300'}`}>
            <AlertDescription>{error || success}</AlertDescription>
          </Alert>
        </div>
      )}

      {/* Main Content */}
      <div className="max-w-7xl mx-auto grid lg:grid-cols-3 gap-6">
        {/* Left Column - Payment Selection */}
        <div className="lg:col-span-2 space-y-6">
          <>
              {/* Payment Method Selection */}
              <div>
                <h2 className="text-xl font-semibold mb-4">Select Payment Method</h2>
                <p className="text-sm text-gray-400 mb-4">Choose how you'd like to deposit funds</p>

                <div className="grid md:grid-cols-2 gap-4">
                  {/* VNPay Card */}
                  <button
                    onClick={() => setSelectedMethod('vnpay')}
                    className={`relative p-6 rounded-2xl border-2 transition-all text-left ${selectedMethod === 'vnpay'
                      ? 'border-emerald-500 bg-emerald-500/5'
                      : 'border-gray-800 bg-gray-900/30 hover:border-gray-700'
                      }`}
                  >
                    {selectedMethod === 'vnpay' && (
                      <div className="absolute top-4 right-4">
                        <CheckCircle2 className="w-6 h-6 text-emerald-400" />
                      </div>
                    )}
                    <div className="absolute top-4 left-4">
                      <Badge className="bg-emerald-500 text-black text-xs px-2 py-1">Recommended</Badge>
                    </div>
                    <div className="mt-8 mb-4">
                      <div className="w-12 h-12 rounded-xl bg-gray-800 flex items-center justify-center mb-3">
                        <Upload className="w-6 h-6 text-emerald-400" />
                      </div>
                      <h3 className="text-lg font-semibold mb-1">VNPay</h3>
                      <p className="text-sm text-gray-400">Vietnamese payment gateway</p>
                    </div>
                    <div className="flex items-center gap-2 text-xs text-gray-400 mb-2">
                      <Clock className="w-3 h-3" />
                      <span>Fee</span>
                      <span className="ml-auto text-emerald-400 font-medium">1.5%</span>
                    </div>
                    <div className="flex items-center gap-2 text-xs text-gray-400">
                      <Clock className="w-3 h-3" />
                      <span>Processing</span>
                      <span className="ml-auto text-white font-medium">Instant</span>
                    </div>
                  </button>

                  {/* Stripe Card */}
                  <button
                    onClick={() => setSelectedMethod('stripe')}
                    className={`relative p-6 rounded-2xl border-2 transition-all text-left ${selectedMethod === 'stripe'
                      ? 'border-emerald-500 bg-emerald-500/5'
                      : 'border-gray-800 bg-gray-900/30 hover:border-gray-700'
                      }`}
                  >
                    {selectedMethod === 'stripe' && (
                      <div className="absolute top-4 right-4">
                        <CheckCircle2 className="w-6 h-6 text-emerald-400" />
                      </div>
                    )}
                    <div className="mt-4 mb-4">
                      <div className="w-12 h-12 rounded-xl bg-gray-800 flex items-center justify-center mb-3">
                        <CreditCard className="w-6 h-6 text-emerald-400" />
                      </div>
                      <h3 className="text-lg font-semibold mb-1">Stripe</h3>
                      <p className="text-sm text-gray-400">International cards accepted</p>
                    </div>
                    <div className="flex items-center gap-2 text-xs text-gray-400 mb-2">
                      <Clock className="w-3 h-3" />
                      <span>Fee</span>
                      <span className="ml-auto text-emerald-400 font-medium">2.9%</span>
                    </div>
                    <div className="flex items-center gap-2 text-xs text-gray-400">
                      <Clock className="w-3 h-3" />
                      <span>Processing</span>
                      <span className="ml-auto text-white font-medium">1-2 minutes</span>
                    </div>
                  </button>
                </div>
              </div>

              {/* Amount Input */}
              <div>
                <h2 className="text-xl font-semibold mb-4">Enter Amount</h2>
                {selectedMethod === 'vnpay' ? (
                  <div>
                    <Label htmlFor="vnd-amount" className="text-gray-400 mb-2 block">Amount (VND)</Label>
                    <div className="relative">
                      <span className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-400 text-lg">₫</span>
                      <Input
                        id="vnd-amount"
                        type="number"
                        placeholder="100000"
                        value={vndAmount}
                        onChange={(e) => setVndAmount(e.target.value)}
                        className="bg-gray-900/50 border-gray-800 text-white text-2xl h-16 pl-10 rounded-xl"
                      />
                    </div>
                    <p className="text-xs text-gray-400 mt-2">Minimum 10,000 VND</p>
                  </div>
                ) : (
                  <div>
                    <Label htmlFor="amount" className="text-gray-400 mb-2 block">Amount (USD)</Label>
                    <div className="relative">
                      <span className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-400 text-lg">$</span>
                      <Input
                        id="amount"
                        type="number"
                        placeholder="5000"
                        value={amount}
                        onChange={(e) => setAmount(e.target.value)}
                        className="bg-gray-900/50 border-gray-800 text-white text-2xl h-16 pl-10 rounded-xl"
                      />
                    </div>
                  </div>
                )}

                {/* Quick Select */}
                {selectedMethod !== 'vnpay' && (
                  <div className="mt-4">
                    <p className="text-sm text-gray-400 mb-3">Quick Select</p>
                    <div className="grid grid-cols-4 gap-3">
                      {quickAmounts.map((quickAmount) => (
                        <button
                          key={quickAmount}
                          onClick={() => setAmount(quickAmount.toString())}
                          className="py-3 px-4 rounded-xl bg-gray-900/50 border border-gray-800 hover:border-emerald-500 hover:bg-emerald-500/5 transition-all font-medium"
                        >
                          ${quickAmount}
                        </button>
                      ))}
                      <button className="py-3 px-4 rounded-xl bg-gray-900/50 border border-gray-800 hover:border-emerald-500 hover:bg-emerald-500/5 transition-all font-medium">
                        Custom
                      </button>
                    </div>
                  </div>
                )}
              </div>

              {/* Accepted Payment Methods */}
              {selectedMethod === 'stripe' && (
                <div className="bg-gray-900/30 border border-gray-800 rounded-2xl p-6">
                  <h3 className="font-semibold mb-3">Accepted with Stripe:</h3>
                  <ul className="space-y-2 text-sm text-gray-300">
                    <li className="flex items-center gap-2">
                      <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                      Visa, Mastercard, American Express
                    </li>
                    <li className="flex items-center gap-2">
                      <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                      Apple Pay, Google Pay
                    </li>
                    <li className="flex items-center gap-2">
                      <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                      International bank cards
                    </li>
                  </ul>
                </div>
              )}
          </>
        </div>

        {/* Right Column - Transaction Summary */}
        <div className="space-y-6">
          {/* Transaction Summary */}
          <div className="bg-gray-900/50 border border-gray-800 rounded-2xl p-6">
            <h3 className="text-xl font-semibold mb-6">Transaction Summary</h3>

            <div className="space-y-4 mb-6">
              <div className="flex justify-between items-center">
                <span className="text-gray-400">Deposit Amount</span>
                <span className="text-lg font-medium">{selectedMethod === 'vnpay' ? '₫' : '$'}{depositAmount.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
              </div>
              <div className="border-t border-gray-800 pt-4">
                <div className="flex justify-between items-center">
                  <span className="text-lg font-semibold">Total Amount</span>
                  <span className="text-2xl font-bold text-emerald-400">{selectedMethod === 'vnpay' ? '₫' : '$'}{totalAmount.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
              </div>
            </div>

            <Button
              onClick={handleConfirmDeposit}
              disabled={loading || stripeLoading || (selectedMethod === 'vnpay' ? !vndAmount : !amount)}
              className="w-full bg-emerald-500 hover:bg-emerald-400 text-black font-semibold h-12 rounded-xl"
            >
              {loading || stripeLoading ? 'Processing...' : 'Confirm Deposit'}
            </Button>
          </div>

          {/* Secure Transaction */}
          <div className="bg-gray-900/50 border border-gray-800 rounded-2xl p-6">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-10 h-10 rounded-full bg-emerald-500/10 flex items-center justify-center">
                <Shield className="w-5 h-5 text-emerald-400" />
              </div>
              <h3 className="font-semibold">Secure Transaction</h3>
            </div>
            <p className="text-sm text-gray-400 leading-relaxed">
              All transactions are encrypted and processed through secure payment gateways. Your financial information is never stored on our servers.
            </p>
          </div>

          {/* Why Deposit */}
          <div className="bg-gray-900/50 border border-gray-800 rounded-2xl p-6">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-10 h-10 rounded-full bg-emerald-500/10 flex items-center justify-center">
                <TrendingUp className="w-5 h-5 text-emerald-400" />
              </div>
              <h3 className="font-semibold">Why Deposit?</h3>
            </div>
            <ul className="space-y-3 text-sm text-gray-300">
              <li className="flex items-start gap-2">
                <CheckCircle2 className="w-4 h-4 text-emerald-400 mt-0.5 flex-shrink-0" />
                <span>Start trading immediately after deposit</span>
              </li>
              <li className="flex items-start gap-2">
                <CheckCircle2 className="w-4 h-4 text-emerald-400 mt-0.5 flex-shrink-0" />
                <span>Access to premium trading features</span>
              </li>
              <li className="flex items-start gap-2">
                <CheckCircle2 className="w-4 h-4 text-emerald-400 mt-0.5 flex-shrink-0" />
                <span>No minimum balance requirements</span>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}


