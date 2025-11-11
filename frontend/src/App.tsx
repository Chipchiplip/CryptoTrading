import React, { useEffect } from 'react';
import { BrowserRouter, Routes, Route, useNavigate, useLocation } from 'react-router-dom';
import { getAccessToken, getUserInfo } from './api/http';
import GuestLayout from './components/GuestLayout';
import TraderLayout from './components/TraderLayout';
import Home from './components/pages/guest/Home';
import Markets from './components/pages/guest/Markets';
import Login from './components/pages/guest/Login';
import Register from './components/pages/guest/Register';
import ForgotPassword from './components/pages/guest/ForgotPassword';
import ResetPassword from './components/pages/guest/ResetPassword';
import VerifyEmail from './components/pages/guest/VerifyEmail';
import GitHubCallback from './components/pages/guest/GitHubCallback'; // ĐÃ THÊM

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

import AdminLayout from './components/AdminLayout';
import AdminUsers from './components/pages/admin/AdminUsers';
import AdminRoles from './components/pages/admin/AdminRoles';
import AdminLevels from './components/pages/admin/AdminLevels';

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

const adminPathMap: Record<string, string> = {
  'admin-users': '/admin/users',
  'admin-roles': '/admin/roles',
  'admin-levels': '/admin/levels',
};

function GuestPage({ current, children }: { current: string; children: React.ReactNode }) {
  const navigate = useNavigate();
  const onNavigate = (page: string) => navigate(guestPathMap[page] || traderPathMap[page] || adminPathMap[page] || '/');
  
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

function ProtectedRoute({ children, adminOnly = false }: { children: React.ReactNode, adminOnly?: boolean }) {
  const location = useLocation();
  const navigate = useNavigate();
  
  useEffect(() => {
    const token = getAccessToken();
    if (!token) {
      console.warn('[ProtectedRoute] No token found, redirecting to login');
      navigate('/login', { state: { from: location.pathname } });
      return;
    }
    
    if (adminOnly) {
      const user = getUserInfo();
      if (user?.role !== 'Admin') {
        console.warn('[ProtectedRoute] Admin access required, redirecting to dashboard');
        navigate('/trader-dashboard');
      }
    }
  }, [navigate, location, adminOnly]);
  
  const token = getAccessToken();
  if (!token) {
    return null;
  }
  
  if (adminOnly) {
    const user = getUserInfo();
    if (user?.role !== 'Admin') {
      return null;
    }
  }
  
  return <>{children}</>;
}

function TraderPage({ current, children }: { current: string; children: React.ReactNode }) {
  const navigate = useNavigate();
  const onNavigate = (page: string, orderId?: string) => {
    if (page === 'order-detail' && orderId) {
      navigate(`${traderPathMap[page]}?id=${orderId}`);
    } else {
      navigate(traderPathMap[page] || guestPathMap[page] || adminPathMap[page] || '/');
    }
  };
  
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

function AdminPage({ current, children }: { current: string; children: React.ReactNode }) {
  const navigate = useNavigate();
  const onNavigate = (page: string) => navigate(adminPathMap[page] || traderPathMap[page] || guestPathMap[page] || '/');
  
  const childrenWithNavigate = React.Children.map(children, (child) => {
    if (React.isValidElement(child)) {
      return React.cloneElement(child, { onNavigate } as any);
    }
    return child;
  });

  return (
    <div className="dark min-h-screen bg-black">
      <AdminLayout currentPage={current} onNavigate={onNavigate}>
        {childrenWithNavigate}
      </AdminLayout>
    </div>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<GuestPage current="home"><Home /></GuestPage>} />
        <Route path="/markets" element={<GuestPage current="markets"><Markets /></GuestPage>} />
        <Route path="/login" element={<GuestPage current="login"><Login /></GuestPage>} />
        <Route path="/register" element={<GuestPage current="register"><Register /></GuestPage>} />
        <Route path="/forgot-password" element={<GuestPage current="forgot-password"><ForgotPassword /></GuestPage>} />
        <Route path="/reset-password" element={<GuestPage current="reset-password"><ResetPassword /></GuestPage>} />
        <Route path="/verify-email" element={<GuestPage current="verify-email"><VerifyEmail /></GuestPage>} />
        
        {/* THÊM ROUTE MỚI CHO GITHUB CALLBACK */}
        <Route path="/auth/github/callback" element={<GitHubCallback />} />
        
        <Route path="/test/chart" element={
          <div className="dark min-h-screen bg-black">
            <ChartTest />
          </div>
        } />
        
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
        
        <Route path="/admin/users" element={<ProtectedRoute adminOnly={true}><AdminPage current="admin-users"><AdminUsers /></AdminPage></ProtectedRoute>} />
        <Route path="/admin/roles" element={<ProtectedRoute adminOnly={true}><AdminPage current="admin-roles"><AdminRoles /></AdminPage></ProtectedRoute>} />
        <Route path="/admin/levels" element={<ProtectedRoute adminOnly={true}><AdminPage current="admin-levels"><AdminLevels /></AdminPage></ProtectedRoute>} />
      </Routes>
    </BrowserRouter>
  );
}