import { useState } from 'react';
import { Shield, AlertCircle, Info } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Label } from '../../ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../ui/select';
import { Alert, AlertDescription } from '../../ui/alert';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '../../ui/dialog';
import { Badge } from '../../ui/badge';

export default function Withdraw() {
  const [selectedCurrency, setSelectedCurrency] = useState('BTC');
  const [address, setAddress] = useState('');
  const [amount, setAmount] = useState('');
  const [twoFACode, setTwoFACode] = useState('');
  const [showConfirm, setShowConfirm] = useState(false);

  const wallets = [
    { symbol: 'BTC', name: 'Bitcoin', available: 0.2341, minWithdraw: 0.001, fee: 0.0005, network: 'Bitcoin' },
    { symbol: 'ETH', name: 'Ethereum', available: 3.5678, minWithdraw: 0.01, fee: 0.005, network: 'Ethereum (ERC20)' },
    { symbol: 'USDT', name: 'Tether', available: 5234.56, minWithdraw: 10, fee: 1, network: 'Ethereum (ERC20)' },
  ];

  const currentWallet = wallets.find(w => w.symbol === selectedCurrency) || wallets[0];
  const amountNum = parseFloat(amount) || 0;
  const receiveAmount = Math.max(0, amountNum - currentWallet.fee);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setShowConfirm(true);
  };

  const confirmWithdraw = () => {
    // Handle withdrawal
    setShowConfirm(false);
  };

  const recentWithdrawals = [
    { id: 'WD-001', currency: 'BTC', amount: 0.05, status: 'Completed', time: '2025-01-14 10:15', address: '1A1z...DivfNa' },
    { id: 'WD-002', currency: 'ETH', amount: 1.0, status: 'Processing', time: '2025-01-13 16:45', address: '0x742...46eBc' },
  ];

  return (
    <div className="p-4 lg:p-8">
      <div className="mb-6">
        <h1 className="text-3xl mb-2">Withdraw Funds</h1>
        <p className="text-gray-400">Withdraw funds to your external wallet</p>
      </div>

      <div className="grid lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2 bg-gray-900 border-gray-800 p-6">
          <form onSubmit={handleSubmit} className="space-y-6">
            <div>
              <Label>Select Currency</Label>
              <Select value={selectedCurrency} onValueChange={setSelectedCurrency}>
                <SelectTrigger className="bg-gray-800 border-gray-700">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent className="bg-gray-800 border-gray-700 text-white">
                  {wallets.map((wallet) => (
                    <SelectItem key={wallet.symbol} value={wallet.symbol}>
                      {wallet.name} ({wallet.symbol})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <div className="mt-2 flex items-center justify-between text-sm">
                <span className="text-gray-400">Available Balance</span>
                <span className="text-white">{currentWallet.available} {currentWallet.symbol}</span>
              </div>
            </div>

            <Alert className="bg-blue-500/10 border-blue-500/50">
              <Info className="h-4 w-4 text-blue-500" />
              <AlertDescription className="text-blue-500">
                <strong>Network:</strong> {currentWallet.network}<br />
                <strong>Minimum Withdrawal:</strong> {currentWallet.minWithdraw} {currentWallet.symbol}<br />
                <strong>Network Fee:</strong> {currentWallet.fee} {currentWallet.symbol}
              </AlertDescription>
            </Alert>

            <div>
              <Label htmlFor="address">Withdrawal Address</Label>
              <Input
                id="address"
                placeholder={`Enter ${currentWallet.symbol} address`}
                value={address}
                onChange={(e) => setAddress(e.target.value)}
                className="bg-gray-800 border-gray-700 text-white"
                required
              />
              <p className="text-sm text-gray-400 mt-2">
                Make sure this address supports {currentWallet.network}
              </p>
            </div>

            <div>
              <div className="flex items-center justify-between mb-2">
                <Label htmlFor="amount">Amount</Label>
                <button
                  type="button"
                  onClick={() => setAmount(currentWallet.available.toString())}
                  className="text-sm text-emerald-500 hover:text-emerald-400"
                >
                  Max
                </button>
              </div>
              <Input
                id="amount"
                type="number"
                step="any"
                placeholder="0.00"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                className="bg-gray-800 border-gray-700 text-white"
                required
              />
              <div className="mt-2 space-y-1 text-sm">
                <div className="flex justify-between text-gray-400">
                  <span>Withdrawal Amount</span>
                  <span>{amountNum || 0} {currentWallet.symbol}</span>
                </div>
                <div className="flex justify-between text-gray-400">
                  <span>Network Fee</span>
                  <span>-{currentWallet.fee} {currentWallet.symbol}</span>
                </div>
                <div className="flex justify-between text-white pt-2 border-t border-gray-800">
                  <span>You Will Receive</span>
                  <span>{receiveAmount.toFixed(8)} {currentWallet.symbol}</span>
                </div>
              </div>
            </div>

            <Alert className="bg-yellow-500/10 border-yellow-500/50">
              <AlertCircle className="h-4 w-4 text-yellow-500" />
              <AlertDescription className="text-yellow-500">
                <strong>Security Notice:</strong> Always verify the withdrawal address. Transactions cannot be reversed.
              </AlertDescription>
            </Alert>

            <div>
              <Label htmlFor="2fa">2FA Code</Label>
              <Input
                id="2fa"
                placeholder="Enter 6-digit code"
                value={twoFACode}
                onChange={(e) => setTwoFACode(e.target.value)}
                className="bg-gray-800 border-gray-700 text-white"
                maxLength={6}
                required
              />
              <p className="text-sm text-gray-400 mt-2">
                <Shield className="w-4 h-4 inline mr-1" />
                Enter your 2FA code to confirm withdrawal
              </p>
            </div>

            <Button
              type="submit"
              disabled={!address || !amount || !twoFACode || amountNum < currentWallet.minWithdraw}
              className="w-full bg-emerald-500 text-black hover:bg-emerald-600"
            >
              Submit Withdrawal Request
            </Button>
          </form>
        </Card>

        <div className="space-y-6">
          <Card className="bg-gray-900 border-gray-800 p-6">
            <h3 className="mb-4">Withdrawal Limits</h3>
            <div className="space-y-3 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-400">Daily Limit</span>
                <span className="text-white">10 BTC</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-400">Used Today</span>
                <span className="text-emerald-500">0.05 BTC</span>
              </div>
              <div className="flex justify-between pt-3 border-t border-gray-800">
                <span className="text-gray-400">Remaining</span>
                <span className="text-white">9.95 BTC</span>
              </div>
            </div>
          </Card>

          <Card className="bg-gray-900 border-gray-800 p-6">
            <h3 className="mb-4">Recent Withdrawals</h3>
            <div className="space-y-3">
              {recentWithdrawals.map((withdrawal) => (
                <div key={withdrawal.id} className="p-3 bg-gray-800 rounded-lg">
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-white">{withdrawal.amount} {withdrawal.currency}</span>
                    <Badge className={withdrawal.status === 'Completed' ? 'bg-emerald-500/10 text-emerald-500' : 'bg-yellow-500/10 text-yellow-500'}>
                      {withdrawal.status}
                    </Badge>
                  </div>
                  <div className="text-sm text-gray-400">{withdrawal.time}</div>
                  <div className="text-sm text-gray-500">{withdrawal.address}</div>
                </div>
              ))}
            </div>
          </Card>
        </div>
      </div>

      {/* Confirmation Dialog */}
      <Dialog open={showConfirm} onOpenChange={setShowConfirm}>
        <DialogContent className="bg-gray-900 border-gray-800 text-white">
          <DialogHeader>
            <DialogTitle>Confirm Withdrawal</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <Alert className="bg-yellow-500/10 border-yellow-500/50">
              <AlertCircle className="h-4 w-4 text-yellow-500" />
              <AlertDescription className="text-yellow-500">
                Please verify all details carefully. This action cannot be undone.
              </AlertDescription>
            </Alert>
            <div className="space-y-3 p-4 bg-gray-800 rounded-lg">
              <div className="flex justify-between">
                <span className="text-gray-400">Currency</span>
                <span className="text-white">{selectedCurrency}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-400">Address</span>
                <span className="text-white text-sm">{address.substring(0, 20)}...</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-400">Amount</span>
                <span className="text-white">{amount} {selectedCurrency}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-400">Network Fee</span>
                <span className="text-white">-{currentWallet.fee} {selectedCurrency}</span>
              </div>
              <div className="flex justify-between border-t border-gray-700 pt-3">
                <span className="text-white">You Will Receive</span>
                <span className="text-emerald-500 text-lg">{receiveAmount.toFixed(8)} {selectedCurrency}</span>
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowConfirm(false)} className="border-gray-700">
              Cancel
            </Button>
            <Button onClick={confirmWithdraw} className="bg-emerald-500 text-black hover:bg-emerald-600">
              Confirm Withdrawal
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
