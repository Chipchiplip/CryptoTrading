import { useState, useEffect } from 'react';
import { Wallet, ArrowDownToLine, Eye, EyeOff, TrendingUp, TrendingDown, Loader2 } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Badge } from '../../ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../ui/tabs';
import { TradingApi, WalletBalance } from '../../../api/trading';
import { MarketApi } from '../../../api/market';
import { CoinIcon } from '../../ui/CoinIcon';

interface WalletsProps {
  onNavigate?: (page: string) => void;
}

interface CryptoWalletData extends WalletBalance {
  name: string;
  change24h: number;
  imageUrl?: string;
}

export default function Wallets({ onNavigate }: WalletsProps) {
  const [showBalances, setShowBalances] = useState(true);
  const [loading, setLoading] = useState(true);
  const [cryptoWallets, setCryptoWallets] = useState<CryptoWalletData[]>([]);
  const [fiatWallets, setFiatWallets] = useState<WalletBalance[]>([]);
  const [totalValue, setTotalValue] = useState(0);
  const [totalChange24h, setTotalChange24h] = useState(0);

  useEffect(() => {
    fetchWalletData();
  }, []);

  const fetchWalletData = async () => {
    try {
      setLoading(true);
      const [balancesRes, marketRes] = await Promise.all([
        TradingApi.getBalances(),
        MarketApi.getCryptocurrencies()
      ]);

      if (balancesRes.ok && balancesRes.data) {
        const wallets = balancesRes.data.wallets || [];
        const cryptoWalletsData: CryptoWalletData[] = [];
        const fiatWalletsData: WalletBalance[] = [];

        // Get crypto prices map for change24h
        const cryptoPriceMap = new Map<string, { name: string; change24h: number; image?: string }>();
        if (marketRes.ok && marketRes.data) {
          marketRes.data.forEach((crypto: any) => {
            cryptoPriceMap.set(crypto.symbol.toUpperCase(), {
              name: crypto.name || crypto.symbol,
              change24h: crypto.priceChangePercentage24h || 0,
              image: crypto.image || crypto.Image
            });
          });
        }

        wallets.forEach((wallet: WalletBalance) => {
          const cryptoData = cryptoPriceMap.get(wallet.symbol.toUpperCase());
          if (cryptoData) {
            // Crypto wallet
            cryptoWalletsData.push({
              ...wallet,
              name: cryptoData.name,
              change24h: cryptoData.change24h,
              imageUrl: cryptoData.image
            });
          } else {
            // Fiat wallet
            fiatWalletsData.push(wallet);
          }
        });

        setCryptoWallets(cryptoWalletsData);
        setFiatWallets(fiatWalletsData);
        setTotalValue(balancesRes.data.totalBalance || 0);
        setTotalChange24h(0); // Calculate from individual wallets if needed
      }
    } catch (error) {
      console.error('Error fetching wallet data:', error);
    } finally {
      setLoading(false);
    }
  };

  const recentMovements: Array<{ type: string; currency: string; amount: number; status: string; time: string; txId: string }> = [];

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
            {totalChange24h !== 0 && (
              <div className={`flex items-center gap-1 ${totalChange24h >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
                {totalChange24h >= 0 ? <TrendingUp className="w-4 h-4" /> : <TrendingDown className="w-4 h-4" />}
                <span>{totalChange24h >= 0 ? '+' : ''}{totalChange24h.toFixed(2)}% (24h)</span>
              </div>
            )}
          </div>
          <div className="flex gap-3">
            <Button
              variant="outline"
              className="border-gray-700 hover:bg-gray-800"
              onClick={() => onNavigate?.('deposit')}
            >
              <ArrowDownToLine className="w-4 h-4 mr-2" />
              Deposit
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
          {loading ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="w-8 h-8 text-emerald-500 animate-spin" />
            </div>
          ) : cryptoWallets.length === 0 ? (
            <Card className="bg-gray-900 border-gray-800 p-6">
              <div className="text-center text-gray-400">No crypto wallets found</div>
            </Card>
          ) : (
            cryptoWallets.map((wallet) => (
              <Card key={wallet.symbol} className="bg-gray-900 border-gray-800 p-6 hover:border-emerald-500/50 transition-colors">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-4">
                    <CoinIcon 
                      image={wallet.imageUrl} 
                      symbol={wallet.symbol} 
                      size="lg"
                    />
                    <div>
                      <h3 className="mb-1">{wallet.name}</h3>
                      <p className="text-gray-400 text-sm">{wallet.symbol}</p>
                    </div>
                  </div>

                  <div className="grid grid-cols-3 gap-8 text-right">
                    <div>
                      <div className="text-gray-400 text-sm mb-1">Total Balance</div>
                      <div className="text-white">
                        {showBalances ? `${wallet.total.toFixed(6)} ${wallet.symbol}` : '••••••'}
                      </div>
                      <div className="text-sm text-gray-400">
                        {showBalances ? `$${wallet.valueUsd.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '••••••'}
                      </div>
                    </div>
                    <div>
                      <div className="text-gray-400 text-sm mb-1">Available</div>
                      <div className="text-emerald-500">
                        {showBalances ? wallet.available.toFixed(6) : '••••••'}
                      </div>
                    </div>
                    <div>
                      <div className="text-gray-400 text-sm mb-1">In Orders</div>
                      <div className="text-yellow-500">
                        {showBalances ? wallet.locked.toFixed(6) : '••••••'}
                      </div>
                    </div>
                  </div>

                  {wallet.change24h !== undefined && (
                    <div className="flex items-center gap-2">
                      <Badge className={wallet.change24h >= 0 ? 'bg-emerald-500/10 text-emerald-500' : 'bg-red-500/10 text-red-500'}>
                        {wallet.change24h >= 0 ? <TrendingUp className="w-3 h-3 mr-1" /> : <TrendingDown className="w-3 h-3 mr-1" />}
                        {Math.abs(wallet.change24h).toFixed(2)}%
                      </Badge>
                    </div>
                  )}
                </div>
              </Card>
            ))
          )}
        </TabsContent>

        <TabsContent value="fiat" className="space-y-4">
          {loading ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="w-8 h-8 text-emerald-500 animate-spin" />
            </div>
          ) : fiatWallets.length === 0 ? (
            <Card className="bg-gray-900 border-gray-800 p-6">
              <div className="text-center text-gray-400">No fiat wallets found</div>
            </Card>
          ) : (
            fiatWallets.map((wallet) => (
              <Card key={wallet.symbol} className="bg-gray-900 border-gray-800 p-6">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-4">
                    <div className="w-12 h-12 bg-blue-500/10 rounded-full flex items-center justify-center">
                      <span className="text-blue-500">{wallet.symbol}</span>
                    </div>
                    <div>
                      <h3 className="mb-1">{wallet.symbol}</h3>
                      <p className="text-gray-400 text-sm">Fiat Currency</p>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-8 text-right">
                    <div>
                      <div className="text-gray-400 text-sm mb-1">Total Balance</div>
                      <div className="text-white text-xl">
                        {showBalances ? `${wallet.total.toFixed(2)} ${wallet.symbol}` : '••••••'}
                      </div>
                    </div>
                    <div>
                      <div className="text-gray-400 text-sm mb-1">Available</div>
                      <div className="text-emerald-500">
                        {showBalances ? wallet.available.toFixed(2) : '••••••'}
                      </div>
                    </div>
                  </div>
                </div>
              </Card>
            ))
          )}
        </TabsContent>

        <TabsContent value="movements">
          <Card className="bg-gray-900 border-gray-800 p-6">
            <h2 className="text-xl mb-6">Recent Movements</h2>
            {recentMovements.length === 0 ? (
              <div className="text-center text-gray-400 py-8">No recent movements</div>
            ) : (
              <div className="space-y-4">
                {recentMovements.map((movement, index) => (
                  <div key={index} className="flex items-center justify-between p-4 bg-gray-800 rounded-lg">
                    <div className="flex items-center gap-4">
                      <div className={`w-10 h-10 rounded-full flex items-center justify-center ${
                        movement.type === 'Withdrawal' ? 'bg-red-500/10' :
                        'bg-blue-500/10'
                      }`}>
                        {movement.type === 'Withdrawal' ? <ArrowUpFromLine className="w-5 h-5 text-red-500" /> :
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
            )}
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
