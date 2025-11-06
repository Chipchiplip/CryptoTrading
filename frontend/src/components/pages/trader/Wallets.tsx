import { useState } from 'react';
import { Wallet, ArrowDownToLine, ArrowUpFromLine, Eye, EyeOff, TrendingUp, TrendingDown } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Badge } from '../../ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../ui/tabs';

interface WalletsProps {
  onNavigate?: (page: string) => void;
}

export default function Wallets({ onNavigate }: WalletsProps) {
  const [showBalances, setShowBalances] = useState(true);

  const cryptoWallets = [
    { symbol: 'BTC', name: 'Bitcoin', balance: 0.2341, available: 0.2341, locked: 0, usdValue: 11759.82, change24h: 2.34 },
    { symbol: 'ETH', name: 'Ethereum', balance: 4.5678, available: 3.5678, locked: 1.0, usdValue: 12993.50, change24h: 1.82 },
    { symbol: 'SOL', name: 'Solana', balance: 125.34, available: 100.34, locked: 25.0, usdValue: 12339.73, change24h: -0.45 },
    { symbol: 'BNB', name: 'BNB', balance: 10.5, available: 10.5, locked: 0, usdValue: 3285.35, change24h: 3.12 },
    { symbol: 'USDT', name: 'Tether', balance: 5234.56, available: 5234.56, locked: 0, usdValue: 5234.56, change24h: 0 },
  ];

  const fiatWallets = [
    { currency: 'USD', balance: 2500.00, available: 2500.00, locked: 0 },
    { currency: 'EUR', balance: 1200.00, available: 1200.00, locked: 0 },
  ];

  const recentMovements = [
    { type: 'Deposit', currency: 'USDT', amount: 1000, status: 'Completed', time: '2025-01-15 14:23', txId: '0x1234...5678' },
    { type: 'Withdrawal', currency: 'BTC', amount: 0.05, status: 'Completed', time: '2025-01-14 10:15', txId: '0x9876...4321' },
    { type: 'Trade', currency: 'ETH', amount: 1.0, status: 'Completed', time: '2025-01-14 09:30', txId: 'TRD-001' },
    { type: 'Deposit', currency: 'USD', amount: 500, status: 'Pending', time: '2025-01-13 16:45', txId: 'DEP-123' },
  ];

  const totalValue = cryptoWallets.reduce((sum, w) => sum + w.usdValue, 0) + fiatWallets.reduce((sum, w) => sum + w.balance, 0);

  return (
    <div className="p-4 lg:p-8">
      <div className="mb-6">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h1 className="text-3xl mb-2">Wallets & Balances</h1>
            <p className="text-gray-400">Manage your crypto and fiat balances</p>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setShowBalances(!showBalances)}
          >
            {showBalances ? <EyeOff className="w-4 h-4 mr-2" /> : <Eye className="w-4 h-4 mr-2" />}
            {showBalances ? 'Hide' : 'Show'} Balances
          </Button>
        </div>
      </div>

      {/* Total Balance */}
      <Card className="bg-gradient-to-r from-emerald-500/10 to-transparent border-emerald-500/20 p-8 mb-6">
        <div className="flex items-center justify-between">
          <div>
            <div className="text-gray-400 mb-2">Total Balance</div>
            <div className="text-5xl text-white mb-2">
              {showBalances ? `$${totalValue.toLocaleString()}` : '••••••'}
            </div>
            <div className="flex items-center gap-1 text-emerald-500">
              <TrendingUp className="w-4 h-4" />
              <span>+2.1% (24h)</span>
            </div>
          </div>
          <div className="flex gap-3">
            <Button
              className="bg-emerald-500 text-black hover:bg-emerald-600"
              onClick={() => onNavigate?.('deposit')}
            >
              <ArrowDownToLine className="w-4 h-4 mr-2" />
              Deposit
            </Button>
            <Button
              variant="outline"
              className="border-gray-700 hover:bg-gray-800"
              onClick={() => onNavigate?.('withdraw')}
            >
              <ArrowUpFromLine className="w-4 h-4 mr-2" />
              Withdraw
            </Button>
          </div>
        </div>
      </Card>

      <Tabs defaultValue="crypto" className="space-y-6">
        <TabsList className="bg-gray-900 border border-gray-800">
          <TabsTrigger value="crypto" className="data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
            Crypto Wallets
          </TabsTrigger>
          <TabsTrigger value="fiat" className="data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
            Fiat Wallets
          </TabsTrigger>
          <TabsTrigger value="movements" className="data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
            Recent Movements
          </TabsTrigger>
        </TabsList>

        <TabsContent value="crypto" className="space-y-4">
          {cryptoWallets.map((wallet) => (
            <Card key={wallet.symbol} className="bg-gray-900 border-gray-800 p-6 hover:border-emerald-500/50 transition-colors">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                  <div className="w-12 h-12 bg-emerald-500/10 rounded-full flex items-center justify-center">
                    <span className="text-emerald-500">{wallet.symbol}</span>
                  </div>
                  <div>
                    <h3 className="mb-1">{wallet.name}</h3>
                    <p className="text-gray-400 text-sm">{wallet.symbol}</p>
                  </div>
                </div>

                <div className="grid grid-cols-3 gap-8 text-right">
                  <div>
                    <div className="text-gray-400 text-sm mb-1">Total Balance</div>
                    <div className="text-white">
                      {showBalances ? `${wallet.balance.toFixed(4)} ${wallet.symbol}` : '••••••'}
                    </div>
                    <div className="text-sm text-gray-400">
                      {showBalances ? `$${wallet.usdValue.toLocaleString()}` : '••••••'}
                    </div>
                  </div>
                  <div>
                    <div className="text-gray-400 text-sm mb-1">Available</div>
                    <div className="text-emerald-500">
                      {showBalances ? wallet.available.toFixed(4) : '••••••'}
                    </div>
                  </div>
                  <div>
                    <div className="text-gray-400 text-sm mb-1">In Orders</div>
                    <div className="text-yellow-500">
                      {showBalances ? wallet.locked.toFixed(4) : '••••••'}
                    </div>
                  </div>
                </div>

                <div className="flex items-center gap-2">
                  <Badge className={wallet.change24h >= 0 ? 'bg-emerald-500/10 text-emerald-500' : 'bg-red-500/10 text-red-500'}>
                    {wallet.change24h >= 0 ? <TrendingUp className="w-3 h-3 mr-1" /> : <TrendingDown className="w-3 h-3 mr-1" />}
                    {Math.abs(wallet.change24h)}%
                  </Badge>
                </div>
              </div>
            </Card>
          ))}
        </TabsContent>

        <TabsContent value="fiat" className="space-y-4">
          {fiatWallets.map((wallet) => (
            <Card key={wallet.currency} className="bg-gray-900 border-gray-800 p-6">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                  <div className="w-12 h-12 bg-blue-500/10 rounded-full flex items-center justify-center">
                    <span className="text-blue-500">{wallet.currency}</span>
                  </div>
                  <div>
                    <h3 className="mb-1">{wallet.currency}</h3>
                    <p className="text-gray-400 text-sm">Fiat Currency</p>
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-8 text-right">
                  <div>
                    <div className="text-gray-400 text-sm mb-1">Total Balance</div>
                    <div className="text-white text-xl">
                      {showBalances ? `${wallet.balance.toFixed(2)} ${wallet.currency}` : '••••••'}
                    </div>
                  </div>
                  <div>
                    <div className="text-gray-400 text-sm mb-1">Available</div>
                    <div className="text-emerald-500">
                      {showBalances ? `${wallet.available.toFixed(2)}` : '••••••'}
                    </div>
                  </div>
                </div>
              </div>
            </Card>
          ))}
        </TabsContent>

        <TabsContent value="movements">
          <Card className="bg-gray-900 border-gray-800 p-6">
            <h2 className="text-xl mb-6">Recent Movements</h2>
            <div className="space-y-4">
              {recentMovements.map((movement, index) => (
                <div key={index} className="flex items-center justify-between p-4 bg-gray-800 rounded-lg">
                  <div className="flex items-center gap-4">
                    <div className={`w-10 h-10 rounded-full flex items-center justify-center ${
                      movement.type === 'Deposit' ? 'bg-emerald-500/10' :
                      movement.type === 'Withdrawal' ? 'bg-red-500/10' :
                      'bg-blue-500/10'
                    }`}>
                      {movement.type === 'Deposit' ? <ArrowDownToLine className="w-5 h-5 text-emerald-500" /> :
                       movement.type === 'Withdrawal' ? <ArrowUpFromLine className="w-5 h-5 text-red-500" /> :
                       <Wallet className="w-5 h-5 text-blue-500" />}
                    </div>
                    <div>
                      <div className="text-white mb-1">{movement.type} - {movement.currency}</div>
                      <div className="text-sm text-gray-400">{movement.time}</div>
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="text-white mb-1">{movement.amount} {movement.currency}</div>
                    <Badge className={movement.status === 'Completed' ? 'bg-emerald-500/10 text-emerald-500' : 'bg-yellow-500/10 text-yellow-500'}>
                      {movement.status}
                    </Badge>
                  </div>
                  <div className="text-gray-400 text-sm">
                    {movement.txId}
                  </div>
                </div>
              ))}
            </div>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
