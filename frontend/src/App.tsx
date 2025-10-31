import React from 'react';
import { BrowserRouter, Routes, Route, useNavigate } from 'react-router-dom';
import GuestLayout from './components/GuestLayout';
import TraderLayout from './components/TraderLayout';
import Home from './components/pages/guest/Home';
import Markets from './components/pages/guest/Markets';
import Login from './components/pages/guest/Login';
import Register from './components/pages/guest/Register';
import ForgotPassword from './components/pages/guest/ForgotPassword';
import ResetPassword from './components/pages/guest/ResetPassword';
import VerifyEmail from './components/pages/guest/VerifyEmail';
import TraderDashboard from './components/pages/trader/TraderDashboard';
import Watchlist from './components/pages/trader/Watchlist';
import Trade from './components/pages/trader/Trade';
import Orders from './components/pages/trader/Orders';
import TradesHistory from './components/pages/trader/TradesHistory';
import Portfolio from './components/pages/trader/Portfolio';
import Wallets from './components/pages/trader/Wallets';
import Deposit from './components/pages/trader/Deposit';
import Withdraw from './components/pages/trader/Withdraw';
import Subscription from './components/pages/trader/Subscription';
import Settings from './components/pages/trader/Settings';
import OrderDetail from './components/pages/trader/OrderDetail';

const guestPathMap: Record<string, string> = {
  home: '/',
  markets: '/markets',
  login: '/login',
  register: '/register',
  'forgot-password': '/forgot-password',
  'reset-password': '/reset-password',
  'verify-email': '/verify-email',
};

const traderPathMap: Record<string, string> = {
  'trader-dashboard': '/trader-dashboard',
  'watchlist': '/watchlist',
  'trade': '/trade',
  'orders': '/orders',
  'order-detail': '/order-detail',
  'trades-history': '/trades-history',
  'portfolio': '/portfolio',
  'wallets': '/wallets',
  'deposit': '/deposit',
  'withdraw': '/withdraw',
  'subscription': '/subscription',
  'settings': '/settings',
};

function GuestPage({ current, children }: { current: string; children: React.ReactNode }) {
  const navigate = useNavigate();
  const onNavigate = (page: string) => navigate(guestPathMap[page] || traderPathMap[page] || '/');
  
  // Clone children and inject onNavigate prop
  const childrenWithNavigate = React.Children.map(children, (child) => {
    if (React.isValidElement(child)) {
      return React.cloneElement(child, { onNavigate } as any);
    }
    return child;
  });

  return (
    <div className="dark">
      <GuestLayout currentPage={current} onNavigate={onNavigate}>
        {childrenWithNavigate}
      </GuestLayout>
    </div>
  );
}

function TraderPage({ current, children }: { current: string; children: React.ReactNode }) {
  const navigate = useNavigate();
  const onNavigate = (page: string) => navigate(traderPathMap[page] || guestPathMap[page] || '/');
  
  // Clone children and inject onNavigate prop
  const childrenWithNavigate = React.Children.map(children, (child) => {
    if (React.isValidElement(child)) {
      return React.cloneElement(child, { onNavigate } as any);
    }
    return child;
  });

  return (
    <div className="dark min-h-screen bg-black">
      <TraderLayout currentPage={current} onNavigate={onNavigate}>
        {childrenWithNavigate}
      </TraderLayout>
    </div>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Guest Routes */}
        <Route path="/" element={<GuestPage current="home"><Home /></GuestPage>} />
        <Route path="/markets" element={<GuestPage current="markets"><Markets /></GuestPage>} />
        <Route path="/login" element={<GuestPage current="login"><Login /></GuestPage>} />
        <Route path="/register" element={<GuestPage current="register"><Register /></GuestPage>} />
        <Route path="/forgot-password" element={<GuestPage current="forgot-password"><ForgotPassword /></GuestPage>} />
        <Route path="/reset-password" element={<GuestPage current="reset-password"><ResetPassword /></GuestPage>} />
        <Route path="/verify-email" element={<GuestPage current="verify-email"><VerifyEmail /></GuestPage>} />
        
        {/* Trader Routes */}
        <Route path="/trader-dashboard" element={<TraderPage current="trader-dashboard"><TraderDashboard /></TraderPage>} />
        <Route path="/watchlist" element={<TraderPage current="watchlist"><Watchlist /></TraderPage>} />
        <Route path="/trade" element={<TraderPage current="trade"><Trade /></TraderPage>} />
        <Route path="/orders" element={<TraderPage current="orders"><Orders /></TraderPage>} />
        <Route path="/order-detail" element={<TraderPage current="order-detail"><OrderDetail /></TraderPage>} />
        <Route path="/trades-history" element={<TraderPage current="trades-history"><TradesHistory /></TraderPage>} />
        <Route path="/portfolio" element={<TraderPage current="portfolio"><Portfolio /></TraderPage>} />
        <Route path="/wallets" element={<TraderPage current="wallets"><Wallets /></TraderPage>} />
        <Route path="/deposit" element={<TraderPage current="deposit"><Deposit /></TraderPage>} />
        <Route path="/withdraw" element={<TraderPage current="withdraw"><Withdraw /></TraderPage>} />
        <Route path="/subscription" element={<TraderPage current="subscription"><Subscription /></TraderPage>} />
        <Route path="/settings" element={<TraderPage current="settings"><Settings /></TraderPage>} />
      </Routes>
    </BrowserRouter>
  );
}
