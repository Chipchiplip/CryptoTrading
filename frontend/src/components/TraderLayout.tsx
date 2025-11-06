import { useState } from 'react';
import { 
  LayoutDashboard, 
  Star, 
  TrendingUp, 
  ListOrdered, 
  History, 
  Briefcase, 
  Wallet, 
  ArrowDownToLine, 
  ArrowUpFromLine,
  CreditCard,
  Settings,
  Bell,
  Search,
  ChevronDown,
  Menu,
  X,
  BarChart3 
} from 'lucide-react';
import { Input } from './ui/input';
import { Avatar, AvatarFallback, AvatarImage } from './ui/avatar';
import { Badge } from './ui/badge';
import { Button } from './ui/button';

interface TraderLayoutProps {
  children: React.ReactNode;
  currentPage: string;
  onNavigate: (page: string) => void;
}

export default function TraderLayout({ children, currentPage, onNavigate }: TraderLayoutProps) {
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const menuItems = [
    { id: 'trader-dashboard', label: 'Dashboard', icon: LayoutDashboard },
    { id: 'watchlist', label: 'Watchlist', icon: Star },
    { id: 'market', label: 'Market', icon: BarChart3 },
    { id: 'trade', label: 'Trade', icon: TrendingUp },
    { id: 'orders', label: 'Orders', icon: ListOrdered },
    { id: 'trades-history', label: 'Trades', icon: History },
    { id: 'portfolio', label: 'Portfolio', icon: Briefcase },
    { id: 'wallets', label: 'Wallets', icon: Wallet },
    { id: 'deposit', label: 'Deposit', icon: ArrowDownToLine },
    { id: 'withdraw', label: 'Withdraw', icon: ArrowUpFromLine },
    { id: 'subscription', label: 'Subscription', icon: CreditCard },
    { id: 'settings', label: 'Settings', icon: Settings },

  ];

  const SidebarContent = () => (
    <div className="flex flex-col h-full">
      {/* Logo */}
      <div className="p-6 border-b border-gray-800 flex-shrink-0">
        <div className="flex items-center gap-2">
          <img 
            src="/logo.png" 
            alt="CryptoTrade Logo" 
            className="w-12 h-12 object-contain"
          />
          <span className="text-xl">CryptoTrade</span>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 p-4 space-y-1 overflow-y-auto">
        {menuItems.map((item) => (
          <button
            key={item.id}
            onClick={() => {
              onNavigate(item.id);
              setSidebarOpen(false);
            }}
            className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${
              currentPage === item.id
                ? 'bg-emerald-500 text-black'
                : 'text-gray-300 hover:bg-gray-800 hover:text-white'
            }`}
          >
            <item.icon className="w-5 h-5" />
            <span>{item.label}</span>
          </button>
        ))}
      </nav>

      {/* Quick Stats - Fixed at bottom */}
      <div className="p-4 border-t border-gray-800 flex-shrink-0">
        <div className="bg-gray-900 rounded-lg p-4 space-y-2">
          <div className="text-gray-400 text-sm">Total Balance</div>
          <div className="text-2xl text-white">$12,458.32</div>
          <div className="text-emerald-500 text-sm">+$234.12 (1.9%)</div>
        </div>
      </div>
    </div>
  );

  return (
    <div className="flex h-screen bg-black text-white">
      {/* Desktop Sidebar */}
      <aside className="hidden lg:flex lg:flex-col w-64 bg-black border-r border-gray-800 h-screen">
        <SidebarContent />
      </aside>

      {/* Mobile Sidebar */}
      {sidebarOpen && (
        <div className="fixed inset-0 z-50 lg:hidden">
          <div className="absolute inset-0 bg-black/50" onClick={() => setSidebarOpen(false)}></div>
          <aside className="absolute left-0 top-0 bottom-0 w-64 bg-black border-r border-gray-800 flex flex-col">
            <div className="p-6 border-b border-gray-800 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <img 
                  src="/logo.png" 
                  alt="CryptoTrade Logo" 
                  className="w-8 h-8 object-contain"
                />
                <span className="text-xl">CryptoTrade</span>
              </div>
              <button onClick={() => setSidebarOpen(false)}>
                <X className="w-6 h-6" />
              </button>
            </div>
            <SidebarContent />
          </aside>
        </div>
      )}

      {/* Main Content */}
      <main className="flex-1 overflow-auto">
        {/* Header */}
        <header className="bg-black border-b border-gray-800 px-4 lg:px-8 py-4 sticky top-0 z-10">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <button
                onClick={() => setSidebarOpen(true)}
                className="lg:hidden p-2 hover:bg-gray-900 rounded-lg"
              >
                <Menu className="w-6 h-6" />
              </button>
              <div className="relative hidden md:block">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                <Input 
                  placeholder="Search coins, orders..." 
                  className="pl-10 bg-gray-900 border-gray-800 w-64"
                />
              </div>
            </div>
            
            <div className="flex items-center gap-4">
              <button className="relative p-2 hover:bg-gray-900 rounded-lg transition-colors">
                <Bell className="w-5 h-5" />
                <span className="absolute top-1 right-1 w-2 h-2 bg-emerald-500 rounded-full"></span>
              </button>
              <div className="flex items-center gap-2 cursor-pointer hover:bg-gray-900 rounded-lg px-2 py-1 transition-colors">
                <Avatar className="w-8 h-8">
                  <AvatarImage src="https://api.dicebear.com/7.x/avataaars/svg?seed=trader" />
                  <AvatarFallback>TR</AvatarFallback>
                </Avatar>
                <span className="hidden sm:inline">Trader</span>
                <ChevronDown className="w-4 h-4 text-gray-400" />
              </div>
            </div>
          </div>
        </header>

        {/* Page Content */}
        {children}
      </main>
    </div>
  );
}
