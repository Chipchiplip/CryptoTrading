import { useState } from 'react';
import { Search, Plus, Edit, Eye, EyeOff } from 'lucide-react';
import { Card } from '../ui/card';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Badge } from '../ui/badge';
import { Switch } from '../ui/switch';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '../ui/dialog';
import { Label } from '../ui/label';
import { Textarea } from '../ui/textarea';

interface Coin {
  id: string;
  symbol: string;
  name: string;
  icon: string;
  price: string;
  change24h: number;
  marketCap: string;
  volume24h: string;
  isEnabled: boolean;
  description: string;
}

const mockCoins: Coin[] = [
  { id: '1', symbol: 'BTC', name: 'Bitcoin', icon: '₿', price: '$50,729', change24h: 2.34, marketCap: '$994.2B', volume24h: '$28.4B', isEnabled: true, description: 'Bitcoin is a decentralized digital currency' },
  { id: '2', symbol: 'ETH', name: 'Ethereum', icon: 'Ξ', price: '$2,041', change24h: -1.23, marketCap: '$245.3B', volume24h: '$15.2B', isEnabled: true, description: 'Ethereum is a decentralized platform' },
  { id: '3', symbol: 'SOL', name: 'Solana', icon: '◎', price: '$103.37', change24h: 5.67, marketCap: '$47.8B', volume24h: '$2.1B', isEnabled: true, description: 'Solana is a high-performance blockchain' },
  { id: '4', symbol: 'USDT', name: 'Tether', icon: '₮', price: '$1.00', change24h: 0.01, marketCap: '$112.5B', volume24h: '$45.3B', isEnabled: true, description: 'Tether is a stablecoin pegged to USD' },
  { id: '5', symbol: 'BNB', name: 'BNB', icon: 'BNB', price: '$312.45', change24h: 1.89, marketCap: '$48.2B', volume24h: '$1.8B', isEnabled: true, description: 'BNB is the native token of Binance' },
  { id: '6', symbol: 'ADA', name: 'Cardano', icon: '₳', price: '$0.38', change24h: -2.45, marketCap: '$13.4B', volume24h: '$340M', isEnabled: false, description: 'Cardano is a proof-of-stake blockchain' },
];

export default function CoinsCatalog() {
  const [coins, setCoins] = useState<Coin[]>(mockCoins);
  const [searchQuery, setSearchQuery] = useState('');
  const [showDialog, setShowDialog] = useState(false);
  const [editingCoin, setEditingCoin] = useState<Coin | null>(null);
  const [formData, setFormData] = useState({
    symbol: '',
    name: '',
    icon: '',
    description: '',
  });

  const filteredCoins = coins.filter(coin =>
    coin.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    coin.symbol.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const toggleCoinStatus = (coinId: string) => {
    setCoins(coins.map(coin =>
      coin.id === coinId ? { ...coin, isEnabled: !coin.isEnabled } : coin
    ));
  };

  const handleEdit = (coin: Coin) => {
    setEditingCoin(coin);
    setFormData({
      symbol: coin.symbol,
      name: coin.name,
      icon: coin.icon,
      description: coin.description,
    });
    setShowDialog(true);
  };

  const handleSave = () => {
    if (editingCoin) {
      setCoins(coins.map(c => c.id === editingCoin.id ? { ...c, ...formData } : c));
    } else {
      const newCoin: Coin = {
        id: String(coins.length + 1),
        symbol: formData.symbol,
        name: formData.name,
        icon: formData.icon,
        description: formData.description,
        price: '$0.00',
        change24h: 0,
        marketCap: '$0',
        volume24h: '$0',
        isEnabled: false,
      };
      setCoins([...coins, newCoin]);
    }
    setShowDialog(false);
    setEditingCoin(null);
    setFormData({ symbol: '', name: '', icon: '', description: '' });
  };

  return (
    <>
      {/* Action Bar */}
      <div className="flex items-center justify-between mb-6">
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
          <Input
            placeholder="Search coins..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-10 bg-gray-900 border-gray-800"
          />
        </div>
        <Button onClick={() => { setEditingCoin(null); setFormData({ symbol: '', name: '', icon: '', description: '' }); setShowDialog(true); }} className="bg-emerald-500 hover:bg-emerald-600 text-black">
          <Plus className="w-4 h-4 mr-2" />
          Add Coin
        </Button>
      </div>

      {/* Coins Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {filteredCoins.map((coin) => (
          <Card key={coin.id} className="bg-gray-900 border-gray-800 p-6">
            <div className="flex items-start justify-between mb-4">
              <div className="flex items-center gap-3">
                <div className="w-12 h-12 rounded-full bg-emerald-500/10 flex items-center justify-center">
                  <span className="text-2xl">{coin.icon}</span>
                </div>
                <div>
                  <div className="flex items-center gap-2">
                    <h3 className="text-lg">{coin.symbol}</h3>
                    <Badge variant={coin.isEnabled ? 'default' : 'secondary'} className={coin.isEnabled ? 'bg-emerald-500/10 text-emerald-500 border-0' : 'bg-gray-700 text-gray-400 border-0'}>
                      {coin.isEnabled ? 'Active' : 'Disabled'}
                    </Badge>
                  </div>
                  <div className="text-sm text-gray-400">{coin.name}</div>
                </div>
              </div>
              <Button size="sm" variant="ghost" onClick={() => handleEdit(coin)} className="hover:bg-gray-800">
                <Edit className="w-4 h-4" />
              </Button>
            </div>

            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-gray-400 text-sm">Price</span>
                <span className="text-lg">{coin.price}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-gray-400 text-sm">24h Change</span>
                <span className={`text-sm ${coin.change24h >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
                  {coin.change24h >= 0 ? '+' : ''}{coin.change24h}%
                </span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-gray-400 text-sm">Market Cap</span>
                <span className="text-sm">{coin.marketCap}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-gray-400 text-sm">24h Volume</span>
                <span className="text-sm">{coin.volume24h}</span>
              </div>
              
              <div className="pt-3 border-t border-gray-800 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  {coin.isEnabled ? <Eye className="w-4 h-4 text-emerald-500" /> : <EyeOff className="w-4 h-4 text-gray-500" />}
                  <span className="text-sm">Display on Platform</span>
                </div>
                <Switch
                  checked={coin.isEnabled}
                  onCheckedChange={() => toggleCoinStatus(coin.id)}
                />
              </div>
            </div>
          </Card>
        ))}
      </div>

      {/* Add/Edit Coin Dialog */}
      <Dialog open={showDialog} onOpenChange={setShowDialog}>
        <DialogContent className="bg-gray-900 border-gray-800">
          <DialogHeader>
            <DialogTitle>{editingCoin ? 'Edit Coin' : 'Add New Coin'}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div>
              <Label htmlFor="symbol">Symbol</Label>
              <Input
                id="symbol"
                value={formData.symbol}
                onChange={(e) => setFormData({ ...formData, symbol: e.target.value })}
                className="bg-gray-800 border-gray-700 mt-2"
                placeholder="e.g. BTC"
              />
            </div>
            <div>
              <Label htmlFor="name">Name</Label>
              <Input
                id="name"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                className="bg-gray-800 border-gray-700 mt-2"
                placeholder="e.g. Bitcoin"
              />
            </div>
            <div>
              <Label htmlFor="icon">Icon (Emoji or Text)</Label>
              <Input
                id="icon"
                value={formData.icon}
                onChange={(e) => setFormData({ ...formData, icon: e.target.value })}
                className="bg-gray-800 border-gray-700 mt-2"
                placeholder="e.g. ₿"
              />
            </div>
            <div>
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                className="bg-gray-800 border-gray-700 mt-2"
                placeholder="Brief description of the cryptocurrency"
                rows={3}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowDialog(false)} className="border-gray-700">
              Cancel
            </Button>
            <Button onClick={handleSave} className="bg-emerald-500 hover:bg-emerald-600 text-black">
              {editingCoin ? 'Save Changes' : 'Add Coin'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
