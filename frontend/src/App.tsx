import React, { useEffect } from 'react';
import { BrowserRouter, Routes, Route, useNavigate, useLocation } from 'react-router-dom';
import { getAccessToken } from './api/http';
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
import Market from './components/pages/trader/Market';
import ChartTest from './components/pages/test/ChartTest';

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
  'market': '/market',
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

// Protected route wrapper - redirect to login if no token
function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const location = useLocation();
  const navigate = useNavigate();
  
  useEffect(() => {
    const token = getAccessToken();
    if (!token) {
      console.warn('[ProtectedRoute] No token found, redirecting to login');
      navigate('/login', { state: { from: location.pathname } });
    }
  }, [navigate, location]);
  
  const token = getAccessToken();
  if (!token) {
    return null; // Don't render children if no token
  }
  
  return <>{children}</>;
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
        
        {/* Test Route - Public */}
        <Route path="/test/chart" element={
          <div className="dark min-h-screen bg-black">
            <ChartTest />
          </div>
        } />
        
        {/* Trader Routes - Protected */}
        <Route path="/trader-dashboard" element={<ProtectedRoute><TraderPage current="trader-dashboard"><TraderDashboard /></TraderPage></ProtectedRoute>} />
        <Route path="/watchlist" element={<ProtectedRoute><TraderPage current="watchlist"><Watchlist /></TraderPage></ProtectedRoute>} />
        <Route path="/trade" element={<ProtectedRoute><TraderPage current="trade"><Trade /></TraderPage></ProtectedRoute>} />
        <Route path="/orders" element={<ProtectedRoute><TraderPage current="orders"><Orders /></TraderPage></ProtectedRoute>} />
        <Route path="/order-detail" element={<ProtectedRoute><TraderPage current="order-detail"><OrderDetail /></TraderPage></ProtectedRoute>} />
        <Route path="/trades-history" element={<ProtectedRoute><TraderPage current="trades-history"><TradesHistory /></TraderPage></ProtectedRoute>} />
        <Route path="/portfolio" element={<ProtectedRoute><TraderPage current="portfolio"><Portfolio /></TraderPage></ProtectedRoute>} />
        <Route path="/wallets" element={<ProtectedRoute><TraderPage current="wallets"><Wallets /></TraderPage></ProtectedRoute>} />
        <Route path="/deposit" element={<ProtectedRoute><TraderPage current="deposit"><Deposit /></TraderPage></ProtectedRoute>} />
        <Route path="/withdraw" element={<ProtectedRoute><TraderPage current="withdraw"><Withdraw /></TraderPage></ProtectedRoute>} />
        <Route path="/subscription" element={<ProtectedRoute><TraderPage current="subscription"><Subscription /></TraderPage></ProtectedRoute>} />
        <Route path="/settings" element={<ProtectedRoute><TraderPage current="settings"><Settings /></TraderPage></ProtectedRoute>} />
        <Route path="/market" element={<ProtectedRoute><TraderPage current="market"><Market /></TraderPage></ProtectedRoute>} />
        
      </Routes>
    </BrowserRouter>
  );
}
