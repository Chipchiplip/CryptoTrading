import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import axios from 'axios';

const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

// Axios auth interceptor
axios.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers || {};
    config.headers['Authorization'] = `Bearer ${token}`;
  }
  return config;
});

// Auto refresh token on 401
let isRefreshing = false;
let pending = [];

axios.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config;
    if (error.response && error.response.status === 401 && !original._retry) {
      original._retry = true;
      try {
        if (!isRefreshing) {
          isRefreshing = true;
          const refreshToken = localStorage.getItem('refreshToken');
          if (!refreshToken) throw error;
          const res = await axios.post('/api/auth/refresh', { refreshToken });
          localStorage.setItem('accessToken', res.data.accessToken);
          localStorage.setItem('refreshToken', res.data.refreshToken);
          pending.forEach((cb) => cb(res.data.accessToken));
          pending = [];
        }
        return new Promise((resolve) => {
          pending.push((token) => {
            original.headers = original.headers || {};
            original.headers['Authorization'] = `Bearer ${token}`;
            resolve(axios(original));
          });
        });
      } finally {
        isRefreshing = false;
      }
    }
    return Promise.reject(error);
  }
);















