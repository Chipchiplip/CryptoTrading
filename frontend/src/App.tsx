import React from 'react';
import { BrowserRouter, Routes, Route, useNavigate } from 'react-router-dom';
import GuestLayout from './components/GuestLayout';
import Home from './components/pages/guest/Home';
import Markets from './components/pages/guest/Markets';
import Login from './components/pages/guest/Login';
import Register from './components/pages/guest/Register';
import ForgotPassword from './components/pages/guest/ForgotPassword';
import ResetPassword from './components/pages/guest/ResetPassword';
import VerifyEmail from './components/pages/guest/VerifyEmail';

const pathMap: Record<string, string> = {
  home: '/',
  markets: '/markets',
  login: '/login',
  register: '/register',
  'forgot-password': '/forgot-password',
  'reset-password': '/reset-password',
  'verify-email': '/verify-email',
};

function GuestPage({ current, children }: { current: string; children: React.ReactNode }) {
  const navigate = useNavigate();
  const onNavigate = (page: string) => navigate(pathMap[page] || '/');
  return (
    <div className="dark">
      <GuestLayout currentPage={current} onNavigate={onNavigate}>
        {children}
      </GuestLayout>
    </div>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<GuestPage current="home"><Home onNavigate={() => {}} /></GuestPage>} />
        <Route path="/markets" element={<GuestPage current="markets"><Markets onNavigate={() => {}} /></GuestPage>} />
        <Route path="/login" element={<GuestPage current="login"><Login onNavigate={() => {}} /></GuestPage>} />
        <Route path="/register" element={<GuestPage current="register"><Register onNavigate={() => {}} /></GuestPage>} />
        <Route path="/forgot-password" element={<GuestPage current="forgot-password"><ForgotPassword onNavigate={() => {}} /></GuestPage>} />
        <Route path="/reset-password" element={<GuestPage current="reset-password"><ResetPassword onNavigate={() => {}} /></GuestPage>} />
        <Route path="/verify-email" element={<GuestPage current="verify-email"><VerifyEmail onNavigate={() => {}} /></GuestPage>} />
      </Routes>
    </BrowserRouter>
  );
}
