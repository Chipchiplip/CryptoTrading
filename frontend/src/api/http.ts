import { UserInfo } from './auth';

export type ApiResult<T> = { ok: true; data: T } | { ok: false; error: string };

const TOKEN_STORAGE_KEY = 'crypto_trading_access_token';
const USER_INFO_KEY = 'crypto_trading_user_info';

let accessToken: string | null = null;
try {
  accessToken = localStorage.getItem(TOKEN_STORAGE_KEY);
  if (accessToken) {
    console.log('[http] Token loaded from storage');
  }
} catch (e) {
  console.warn('[http] Failed to load token from storage:', e);
}

export function setAccessToken(token: string | null, user: UserInfo | null) {
  accessToken = token;
  try {
    if (token && user) {
      localStorage.setItem(TOKEN_STORAGE_KEY, token);
      localStorage.setItem(USER_INFO_KEY, JSON.stringify(user));
      console.log('[http] Access token and user info set');
    } else {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
      localStorage.removeItem(USER_INFO_KEY);
      console.log('[http] Access token and user info cleared');
    }
  } catch (e) {
    console.warn('[http] Failed to save token/user to storage:', e);
  }
}

export function getAccessToken(): string | null {
  if (!accessToken) {
    try {
      accessToken = localStorage.getItem(TOKEN_STORAGE_KEY);
    } catch (e) {
      console.warn('[http] Failed to load token from storage:', e);
    }
  }
  return accessToken;
}

export function getUserInfo(): UserInfo | null {
  try {
    const info = localStorage.getItem(USER_INFO_KEY);
    return info ? JSON.parse(info) : null;
  } catch (e) {
    console.warn('[http] Failed to load user info from storage:', e);
    return null;
  }
}

// Thêm hàm helper GET
export async function authGetJson<T>(url: string): Promise<ApiResult<T>> {
  try {
    const res = await authFetch(url, { method: 'GET' });
    const json = await res.json().catch(() => ({}));
    if (!res.ok) return { ok: false, error: (json as any)?.message || `HTTP ${res.status}` };
    return { ok: true, data: json as T };
  } catch (e: any) {
    return { ok: false, error: e.message || 'Network error' };
  }
}

// Thêm hàm helper PUT
export async function authPutJson<T>(url: string, body: unknown): Promise<ApiResult<T>> {
  try {
    const res = await authFetch(url, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    const json = await res.json().catch(() => ({}));
    if (!res.ok) return { ok: false, error: (json as any)?.message || `HTTP ${res.status}` };
    return { ok: true, data: json as T };
  } catch (e: any) {
    return { ok: false, error: e.message || 'Network error' };
  }
}

export async function authFetch(input: RequestInfo | URL, init?: RequestInit) {
  const url = typeof input === 'string' ? input : input.toString();
  const token = getAccessToken();
  const headers = new Headers(init?.headers || {});
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  } else {
    console.warn('[http] No access token available for request:', url);
  }
  
  console.log('[http] Fetching:', url, { method: init?.method || 'GET', hasAuth: !!token });
  
  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 10000);
    
    const res = await fetch(input, { 
      ...init, 
      headers, 
      credentials: 'include',
      signal: controller.signal
    });
    
    clearTimeout(timeoutId);
    console.log('[http] Response:', url, { status: res.status, statusText: res.statusText });
    
    if (res.status === 401) {
      console.warn('[http] Unauthorized (401), clearing token and user');
      setAccessToken(null, null); 
      
      if (typeof window !== 'undefined' && !window.location.pathname.startsWith('/login') && !window.location.pathname.startsWith('/register')) {
        const traderRoutes = ['/trader-dashboard', '/watchlist', '/trade', '/orders', '/portfolio', '/wallets', '/deposit', '/withdraw', '/subscription', '/settings', '/market'];
        if (traderRoutes.some(route => window.location.pathname.startsWith(route))) {
          console.warn('[http] Redirecting to login due to 401');
          window.location.href = '/login';
        }
      }
    }
    
    return res;
  } catch (e: any) {
    if (e.name === 'AbortError') {
      console.error('[http] Request timeout:', url);
      throw new Error('Request timeout - server may be unavailable');
    }
    console.error('[http] Fetch error:', url, e);
    throw e;
  }
}