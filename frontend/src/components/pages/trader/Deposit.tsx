import { useState } from 'react';
import { Copy, Upload, CheckCircle2, Info } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Label } from '../../ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../ui/select';
import { Alert, AlertDescription } from '../../ui/alert';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../ui/tabs';
import { Badge } from '../../ui/badge';

export default function Deposit() {
  const [selectedCurrency, setSelectedCurrency] = useState('BTC');
  const [amount, setAmount] = useState('');
  const [uploadedFile, setUploadedFile] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  const depositAddress = '1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa';

  const cryptoCurrencies = [
    { symbol: 'BTC', name: 'Bitcoin', network: 'Bitcoin', minDeposit: 0.0001, fee: 0 },
    { symbol: 'ETH', name: 'Ethereum', network: 'Ethereum (ERC20)', minDeposit: 0.01, fee: 0 },
    { symbol: 'USDT', name: 'Tether', network: 'Ethereum (ERC20)', minDeposit: 10, fee: 0 },
  ];

  const fiatMethods = [
    { method: 'Bank Transfer', currency: 'USD', fee: '0%', processing: '1-3 business days' },
    { method: 'Credit Card', currency: 'USD', fee: '2.5%', processing: 'Instant' },
    { method: 'Debit Card', currency: 'USD', fee: '1.5%', processing: 'Instant' },
  ];

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

  const currentCurrency = cryptoCurrencies.find(c => c.symbol === selectedCurrency) || cryptoCurrencies[0];

  return (
    <div className="p-4 lg:p-8">
      <div className="mb-6">
        <h1 className="text-3xl mb-2">Deposit Funds</h1>
        <p className="text-gray-400">Add funds to your account</p>
      </div>

      <div className="grid lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2 bg-gray-900 border-gray-800 p-6">
          <Tabs defaultValue="crypto">
            <TabsList className="bg-gray-800 w-full mb-6">
              <TabsTrigger value="crypto" className="flex-1 data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
                Cryptocurrency
              </TabsTrigger>
              <TabsTrigger value="fiat" className="flex-1 data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
                Fiat (Bank/Card)
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
                      className="flex items-center justify-between p-4 bg-gray-800 rounded-lg hover:bg-gray-700 cursor-pointer transition-colors"
                    >
                      <div>
                        <div className="text-white mb-1">{method.method}</div>
                        <div className="text-sm text-gray-400">Processing: {method.processing}</div>
                      </div>
                      <Badge variant="outline" className="border-gray-700">
                        Fee: {method.fee}
                      </Badge>
                    </div>
                  ))}
                </div>
              </div>

              <div>
                <Label htmlFor="amount">Amount (USD)</Label>
                <Input
                  id="amount"
                  type="number"
                  placeholder="0.00"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  className="bg-gray-800 border-gray-700 text-white"
                />
                <p className="text-sm text-gray-400 mt-2">Minimum: $10.00</p>
              </div>

              <div>
                <Label htmlFor="receipt">Upload Receipt (Optional)</Label>
                <div className="mt-2">
                  <label className="flex items-center justify-center w-full h-32 border-2 border-dashed border-gray-700 rounded-lg cursor-pointer hover:border-emerald-500 transition-colors">
                    <div className="text-center">
                      <Upload className="w-8 h-8 text-gray-400 mx-auto mb-2" />
                      <span className="text-gray-400">
                        {uploadedFile || 'Click to upload or drag and drop'}
                      </span>
                    </div>
                    <input
                      id="receipt"
                      type="file"
                      className="hidden"
                      accept="image/*,.pdf"
                      onChange={handleFileUpload}
                    />
                  </label>
                </div>
              </div>

              <Button className="w-full bg-emerald-500 text-black hover:bg-emerald-600">
                Submit Deposit Request
              </Button>
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
