import React, { useState } from 'react';
import axios from 'axios';

function Auth({ onSuccess }) {
  const [mode, setMode] = useState('login'); // login | register | confirm | forgot | reset | login2fa
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fullName, setFullName] = useState('');
  const [token, setToken] = useState(''); // confirm/reset token or 2FA code
  const [newPassword, setNewPassword] = useState('');
  const [message, setMessage] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setMessage('');
    try {
      if (mode === 'login') {
        const res = await axios.post('/api/auth/login', { email, password });
        localStorage.setItem('accessToken', res.data.accessToken);
        localStorage.setItem('refreshToken', res.data.refreshToken);
        onSuccess?.(res.data);
        setMessage('Đăng nhập thành công');
      } else if (mode === 'register') {
        await axios.post('/api/auth/register', { email, password, confirmPassword: password, fullName });
        setMessage('Đăng ký thành công. Vui lòng xác nhận email trước khi đăng nhập.');
        setMode('login');
      } else if (mode === 'confirm') {
        await axios.post('/api/auth/confirm-email', { email, token });
        setMessage('Xác nhận email thành công. Bạn có thể đăng nhập.');
        setMode('login');
      } else if (mode === 'forgot') {
        await axios.post('/api/auth/forgot-password', { email });
        setMessage('Đã gửi email hướng dẫn đặt lại mật khẩu.');
      } else if (mode === 'reset') {
        await axios.post('/api/auth/reset-password', { email, token, newPassword });
        setMessage('Đặt lại mật khẩu thành công.');
        setMode('login');
      } else if (mode === 'login2fa') {
        const res = await axios.post('/api/auth/login-2fa', { email, code: token });
        localStorage.setItem('accessToken', res.data.accessToken);
        localStorage.setItem('refreshToken', res.data.refreshToken);
        onSuccess?.(res.data);
        setMessage('Đăng nhập 2FA thành công');
      }
    } catch (err) {
      setMessage(err?.response?.data?.message || 'Có lỗi xảy ra');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ maxWidth: 420, margin: '40px auto', background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 2px 12px rgba(0,0,0,0.08)' }}>
      <h2 style={{ marginBottom: 16 }}>
        {mode === 'login' && 'Đăng nhập'}
        {mode === 'register' && 'Đăng ký'}
        {mode === 'confirm' && 'Xác nhận email'}
        {mode === 'forgot' && 'Quên mật khẩu'}
        {mode === 'reset' && 'Đặt lại mật khẩu'}
        {mode === 'login2fa' && 'Đăng nhập 2FA'}
      </h2>
      <form onSubmit={handleSubmit}>
        {(mode === 'register') && (
          <div style={{ marginBottom: 12 }}>
            <label>Họ tên</label>
            <input value={fullName} onChange={(e) => setFullName(e.target.value)} style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid #e5e7eb' }} />
          </div>
        )}
        {(mode !== 'confirm' && mode !== 'reset' && mode !== 'login2fa') && (
          <div style={{ marginBottom: 12 }}>
            <label>Email</label>
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid #e5e7eb' }} />
          </div>
        )}
        {(mode === 'login' || mode === 'register') && (
          <div style={{ marginBottom: 12 }}>
            <label>Mật khẩu</label>
            <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid #e5e7eb' }} />
          </div>
        )}
        {(mode === 'confirm' || mode === 'reset' || mode === 'login2fa') && (
          <div style={{ marginBottom: 12 }}>
            <label>{mode === 'login2fa' ? 'Mã 2FA' : 'Token'}</label>
            <input value={token} onChange={(e) => setToken(e.target.value)} style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid #e5e7eb' }} />
          </div>
        )}
        {mode === 'reset' && (
          <div style={{ marginBottom: 12 }}>
            <label>Mật khẩu mới</label>
            <input type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid #e5e7eb' }} />
          </div>
        )}
        {message && <div style={{ marginBottom: 12, color: '#dc2626' }}>{message}</div>}
        <button disabled={loading} type="submit" style={{ width: '100%', padding: 12, borderRadius: 8, border: 'none', background: '#2563eb', color: '#fff', fontWeight: 700 }}>
          {loading ? 'Đang xử lý...' : (
            mode === 'login' ? 'Đăng nhập' :
            mode === 'register' ? 'Đăng ký' :
            mode === 'confirm' ? 'Xác nhận' :
            mode === 'forgot' ? 'Gửi email đặt lại' :
            mode === 'reset' ? 'Đặt lại mật khẩu' :
            'Đăng nhập 2FA'
          )}
        </button>
      </form>
      <div style={{ marginTop: 12, textAlign: 'center' }}>
        {mode === 'login' && (
          <>
            <span>Chưa có tài khoản? <button onClick={() => setMode('register')} style={{ color: '#2563eb', background: 'transparent', border: 'none', cursor: 'pointer' }}>Đăng ký</button></span>
            <div style={{ marginTop: 8 }}>
              <button onClick={() => setMode('forgot')} style={{ color: '#2563eb', background: 'transparent', border: 'none', cursor: 'pointer' }}>Quên mật khẩu?</button>
            </div>
            <div style={{ marginTop: 8 }}>
              <button onClick={() => setMode('login2fa')} style={{ color: '#2563eb', background: 'transparent', border: 'none', cursor: 'pointer' }}>Đăng nhập 2FA</button>
            </div>
          </>
        )}
        {mode !== 'login' && (
          <span>Quay lại <button onClick={() => setMode('login')} style={{ color: '#2563eb', background: 'transparent', border: 'none', cursor: 'pointer' }}>Đăng nhập</button></span>
        )}
      </div>
    </div>
  );
}

export default Auth;


