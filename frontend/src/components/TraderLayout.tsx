import { useState, useEffect, useRef } from 'react';
import {
  LayoutDashboard,
  Star,
  TrendingUp,
  ListOrdered,
  History,
  Briefcase,
  Wallet,
  ArrowDownToLine,
  CreditCard,
  Settings,
  Bell,
  Search,
  ChevronDown,
  Menu,
  X,
  BarChart3,
  LogOut,
  User,
  Loader2,
  Bot,
  Cpu
} from 'lucide-react';
import { Input } from './ui/input';
import { Avatar, AvatarFallback, AvatarImage } from './ui/avatar';
import { setAccessToken } from '../api/http';
import { useNavigate } from 'react-router-dom';
import { useDashboardSummary } from '../contexts/DashboardContext';
import NotificationsPanel from './NotificationsPanel';
import { AuthApi, UserProfileDto } from '../api/auth';
import { useSubscriptionPlan } from '../hooks/useSubscriptionPlan';

interface TraderLayoutProps {
  children: React.ReactNode;
  currentPage: string;
  onNavigate: (page: string) => void;
}

type MenuItem = {
  id: string;
  label: string;
  icon: typeof LayoutDashboard;
  requiresPremium?: boolean;
};

export default function TraderLayout({ children, currentPage, onNavigate }: TraderLayoutProps) {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [notificationsOpen, setNotificationsOpen] = useState(false);
  const [userProfile, setUserProfile] = useState<UserProfileDto | null>(null);
  const userMenuRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();
  const { planType, loading: subscriptionLoading } = useSubscriptionPlan();
  
  // ✅ Use shared dashboard context instead of direct API calls
  const { summary: dashboardSummary, loading: balanceLoading } = useDashboardSummary();

  // ✅ Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (userMenuRef.current && !userMenuRef.current.contains(event.target as Node)) {
        setUserMenuOpen(false);
      }
    };

    if (userMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [userMenuOpen]);

  // Load profile data so header/avatar reflects stored avatarUrl
  useEffect(() => {
    let isMounted = true;
    const fetchProfile = async () => {
      try {
        const result = await AuthApi.getProfile();
        if (result.ok && isMounted) {
          setUserProfile(result.data);
        } else if (!result.ok) {
          console.error('[TraderLayout] Failed to fetch profile:', result.error);
        }
      } catch (error) {
        console.error('[TraderLayout] Exception while fetching profile:', error);
      }
    };

    fetchProfile();
    return () => {
      isMounted = false;
    };
  }, []);

  useEffect(() => {
    const handleProfileUpdate = (event: Event) => {
      const detail = (event as CustomEvent<{ avatarUrl?: string; fullName?: string }>).detail;
      if (!detail) {
        return;
      }

      setUserProfile((prev) => {
        if (!prev) {
          return {
            id: 0,
            email: '',
            fullName: detail.fullName || '',
            role: '',
            level: 0,
            status: 0,
            createdAt: '',
            emailConfirmed: false,
            twoFactorEnabled: false,
            avatarUrl: detail.avatarUrl,
          } as UserProfileDto;
        }

        return {
          ...prev,
          avatarUrl: detail.avatarUrl ?? prev.avatarUrl,
          fullName: detail.fullName ?? prev.fullName,
        };
      });
    };

    window.addEventListener('profile:avatar-updated', handleProfileUpdate as EventListener);

    return () => {
      window.removeEventListener('profile:avatar-updated', handleProfileUpdate as EventListener);
    };
  }, []);

  // ✅ Handle logout
  const handleLogout = () => {
    setAccessToken(null, null);
    setUserMenuOpen(false);
    navigate('/login');
  };

  // ✅ Format currency
  const formatCurrency = (value: number | null | undefined) => {
    if (value === null || value === undefined || isNaN(value)) {
      return '$0.00';
    }
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value);
  };

  const menuItems: MenuItem[] = [
    { id: 'trader-dashboard', label: 'Dashboard', icon: LayoutDashboard },
    { id: 'watchlist', label: 'Watchlist', icon: Star },
    { id: 'market', label: 'Market', icon: BarChart3 },
    { id: 'trade', label: 'Trade', icon: TrendingUp },
    { id: 'ai-chat', label: 'AI Chat', icon: Bot, requiresPremium: true },
    { id: 'bots', label: 'Bots', icon: Cpu, requiresPremium: true },
    { id: 'orders', label: 'Orders', icon: ListOrdered },
    { id: 'trades-history', label: 'Trades', icon: History },
    { id: 'portfolio', label: 'Portfolio', icon: Briefcase, requiresPremium: true },
    { id: 'wallets', label: 'Wallets', icon: Wallet },
    { id: 'deposit', label: 'Deposit', icon: ArrowDownToLine },
    { id: 'subscription', label: 'Subscription', icon: CreditCard },
    { id: 'settings', label: 'Settings', icon: Settings },
  ];

  const handleMenuNavigate = (item: MenuItem) => {
    if (item.requiresPremium && planType !== 2) {
      onNavigate('subscription');
      setSidebarOpen(false);
      return;
    }
    onNavigate(item.id);
    setSidebarOpen(false);
  };

  const SidebarContent = () => (
    <div className="flex flex-col h-full">
      {/* Logo */}
      <div className="p-6 border-b border-gray-800 flex-shrink-0">
        <div className="flex items-center gap-2">
          <img 
            src="/logo.png" 
            alt="CryptoTrade Logo" 
            className="w-10 h-10 object-contain"
          />
          <span className="text-xl">CryptoTrade</span>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 p-4 space-y-1">
        <div className="text-xs text-gray-500 px-2 pb-2">
          {subscriptionLoading ? 'Đang kiểm tra gói...' : planType === 2 ? 'Premium plan active' : 'Free plan'}
        </div>
        {menuItems.map((item) => {
          const isLocked = item.requiresPremium && planType !== 2;
          return (
            <button
              key={item.id}
              onClick={() => handleMenuNavigate(item)}
              className={`w-full flex items-center justify-between gap-3 px-4 py-3 rounded-lg transition-colors ${
                currentPage === item.id
                  ? 'bg-emerald-500 text-black'
                  : 'text-gray-300 hover:bg-gray-800 hover:text-white'
              } ${isLocked ? 'opacity-70' : ''}`}
            >
              <div className="flex items-center gap-3">
                <item.icon className="w-5 h-5" />
                <span>{item.label}</span>
              </div>
              {item.requiresPremium && (
                <span
                  className={`text-xs px-2 py-0.5 rounded-full ${
                    isLocked ? 'bg-emerald-500/10 text-emerald-300' : 'bg-emerald-500/60 text-black'
                  }`}
                >
                  {isLocked ? 'Locked' : 'Premium'}
                </span>
              )}
            </button>
          );
        })}
      </nav>

      {/* Quick Stats - Fixed at bottom */}
      <div className="p-4 border-t border-gray-800 flex-shrink-0">
        <div className="bg-gray-900 rounded-lg p-4 space-y-2">
          <div className="text-gray-400 text-sm">Total Balance</div>
          {balanceLoading ? (
            <div className="flex items-center justify-center py-2">
              <Loader2 className="w-5 h-5 text-gray-400 animate-spin" />
            </div>
          ) : dashboardSummary ? (
            <>
              <div className="text-2xl text-white" data-testid="sidebar-total-balance">
                {formatCurrency(dashboardSummary.totalBalance)}
              </div>
              <div className={`text-sm ${
                dashboardSummary.totalBalanceChange >= 0 ? 'text-emerald-500' : 'text-red-500'
              }`} data-testid="sidebar-balance-change">
                {dashboardSummary.totalBalanceChange >= 0 ? '+' : ''}
                {formatCurrency(dashboardSummary.totalBalanceChange)} ({dashboardSummary.totalBalanceChangePercent >= 0 ? '+' : ''}{dashboardSummary.totalBalanceChangePercent.toFixed(2)}%)
              </div>
            </>
          ) : (
            <div className="text-sm text-gray-500">
              <div className="text-xl">--</div>
              <div className="text-xs mt-1">Unable to load</div>
            </div>
          )}
        </div>
      </div>
    </div>
  );

  const avatarSrc =
    userProfile?.avatarUrl ||
    'https://api.dicebear.com/7.x/avataaars/svg?seed=trader';
  const avatarFallbackText =
    userProfile?.fullName?.trim().slice(0, 2).toUpperCase() || 'TR';
  const displayName = userProfile?.fullName || 'Trader';

  return (
    <>
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
              <button
                className="relative p-2 hover:bg-gray-900 rounded-lg transition-colors"
                onClick={() => setNotificationsOpen(true)}
                aria-label="Notifications"
              >
                <Bell className="w-5 h-5" />
                <span className="absolute top-1 right-1 w-2 h-2 bg-emerald-500 rounded-full"></span>
              </button>
              
              {/* ✅ User Menu with Dropdown */}
              <div className="relative" ref={userMenuRef}>
                <div 
                  onClick={() => setUserMenuOpen(!userMenuOpen)}
                  className="flex items-center gap-2 cursor-pointer hover:bg-gray-900 rounded-lg px-2 py-1 transition-colors"
                >
                  <Avatar className="w-8 h-8">
                    <AvatarImage src={avatarSrc} />
                    <AvatarFallback>{avatarFallbackText}</AvatarFallback>
                  </Avatar>
                  <span className="hidden sm:inline">{displayName}</span>
                  <ChevronDown className={`w-4 h-4 text-gray-400 transition-transform ${userMenuOpen ? 'rotate-180' : ''}`} />
                </div>
                
                {/* ✅ Dropdown Menu */}
                {userMenuOpen && (
                  <div className="absolute right-0 mt-2 w-48 bg-gray-900 border border-gray-800 rounded-lg shadow-xl z-50">
                    <div className="py-1">
                      <button
                        onClick={() => {
                          onNavigate('settings');
                          setUserMenuOpen(false);
                        }}
                        className="w-full flex items-center gap-3 px-4 py-2 text-left text-gray-300 hover:bg-gray-800 hover:text-white transition-colors"
                      >
                        <User className="w-4 h-4" />
                        <span>Profile</span>
                      </button>
                      <button
                        onClick={() => {
                          onNavigate('settings');
                          setUserMenuOpen(false);
                        }}
                        className="w-full flex items-center gap-3 px-4 py-2 text-left text-gray-300 hover:bg-gray-800 hover:text-white transition-colors"
                      >
                        <Settings className="w-4 h-4" />
                        <span>Settings</span>
                      </button>
                      <div className="border-t border-gray-800 my-1"></div>
                      <button
                        onClick={handleLogout}
                        className="w-full flex items-center gap-3 px-4 py-2 text-left text-red-400 hover:bg-red-500/10 hover:text-red-300 transition-colors"
                      >
                        <LogOut className="w-4 h-4" />
                        <span>Logout</span>
                      </button>
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>
        </header>

        {/* Page Content */}
        {children}
      </main>
    </div>
    <NotificationsPanel
      open={notificationsOpen}
      onOpenChange={setNotificationsOpen}
      onNavigate={onNavigate}
    />
    </>
  );
}
