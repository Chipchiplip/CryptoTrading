import {
  Activity,
  Users,
  Shield,
  Coins,
  Radio,
  BarChart3,
  TrendingUp,
  Settings,
  ShoppingCart,
  ArrowDownUp,
  ArrowDownToLine,
  ArrowUpFromLine,
  CreditCard,
} from "lucide-react";

interface SidebarProps {
  currentPage: string;
  onNavigate: (page: string) => void;
}

export default function Sidebar({
  currentPage,
  onNavigate,
}: SidebarProps) {
  const menuItems = [
    { id: "dashboard", label: "Dashboard", icon: Activity },
    { id: "users", label: "Users", icon: Users },
    { id: "roles", label: "Roles & Permissions", icon: Shield },
    {
      id: "orders-monitor",
      label: "Orders Monitor",
      icon: ShoppingCart,
    },
    {
      id: "trades-monitor",
      label: "Trades Monitor",
      icon: ArrowDownUp,
    },
    {
      id: "deposits-approvals",
      label: "Deposits",
      icon: ArrowDownToLine,
    },
    {
      id: "withdrawals-approvals",
      label: "Withdrawals",
      icon: ArrowUpFromLine,
    },
    {
      id: "plans-subscriptions",
      label: "Plans & Subscriptions",
      icon: CreditCard,
    },
    { id: "coins", label: "Coins Catalog", icon: Coins },
    { id: "price-feeds", label: "Price Feeds", icon: Radio },
    {
      id: "market-stats",
      label: "Market Stats",
      icon: BarChart3,
    },
  ];

  return (
    <aside className="w-64 bg-black border-r border-gray-800 flex flex-col">
      {/* Logo */}
      <div className="p-6 border-b border-gray-800">
        <div className="flex items-center gap-2">
          <img 
            src="/logo.png" 
            alt="CryptoAdmin Logo" 
            className="w-12 h-12 object-contain"
          />
          <span className="font-semibold">CryptoAdmin</span>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 p-4 space-y-1">
        {menuItems.map((item) => {
          const Icon = item.icon;
          const isActive = currentPage === item.id;

          return (
            <button
              key={item.id}
              onClick={() => onNavigate(item.id)}
              className={`flex items-center gap-3 px-4 py-3 rounded-lg w-full transition-colors ${
                isActive
                  ? "bg-emerald-500 text-black"
                  : "hover:bg-gray-900 text-white"
              }`}
            >
              <Icon className="w-5 h-5" />
              <span>{item.label}</span>
            </button>
          );
        })}
      </nav>

      {/* Settings & Logout */}
    </aside>
  );
}