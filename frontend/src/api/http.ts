import { UserInfo } from './auth';

export type ApiResult<T> = { ok: true; data: T } | { ok: false; error: string };

const TOKEN_STORAGE_KEY = 'crypto_trading_access_token';
const REFRESH_TOKEN_STORAGE_KEY = 'crypto_trading_refresh_token';
const USER_INFO_KEY = 'crypto_trading_user_info';

let accessToken: string | null = null;
let refreshToken: string | null = null;

try {
  accessToken = localStorage.getItem(TOKEN_STORAGE_KEY);
  refreshToken = localStorage.getItem(REFRESH_TOKEN_STORAGE_KEY);
  if (accessToken) {
    console.log('[http] Token loaded from storage');
  }
} catch (e) {
  console.warn('[http] Failed to load token from storage:', e);
}

// ✅ Giữ logic từ feature/mysql-database-integration
// Lắng nghe sự thay đổi localStorage từ các tab khác để đồng bộ token
if (typeof window !== 'undefined') {
  window.addEventListener('storage', (e) => {
    if (e.key === TOKEN_STORAGE_KEY) {
      console.log('[http] Token changed in another tab, updating...');
      accessToken = e.newValue;
      // Nếu token bị xóa ở tab khác, redirect về login
      if (
        !e.newValue &&
        !window.location.pathname.startsWith('/login') &&
        !window.location.pathname.startsWith('/register')
      ) {
        const traderRoutes = [
          '/trader-dashboard',
          '/watchlist',
          '/trade',
          '/orders',
          '/portfolio',
          '/wallets',
          '/deposit',
          '/withdraw',
          '/subscription',
          '/settings',
          '/market',
        ];
        if (traderRoutes.some((route) => window.location.pathname.startsWith(route))) {
          console.warn('[http] Token cleared in another tab, redirecting to login');
          window.location.href = '/login';
        }
      }
    }
  });
}

// ✅ Hợp nhất với fix từ feature/watchlist-portfolio
export function setAccessToken(
  token: string | null,
  user: UserInfo | null,
  newRefreshToken?: string | null
) {
  accessToken = token;
  refreshToken = newRefreshToken || null;

  try {
    if (token) {
      localStorage.setItem(TOKEN_STORAGE_KEY, token);
      if (newRefreshToken) {
        localStorage.setItem(REFRESH_TOKEN_STORAGE_KEY, newRefreshToken);
      }
      if (user) {
        localStorage.setItem(USER_INFO_KEY, JSON.stringify(user));
      }
      console.log('[http] Access token saved (user optional)');
    } else {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
      localStorage.removeItem(REFRESH_TOKEN_STORAGE_KEY);
      localStorage.removeItem(USER_INFO_KEY);
    }
  } catch (e) {
    console.warn('[http] Failed to save token/user to storage:', e);
  }
}

export function getAccessToken(): string | null {
  try {
    const token = localStorage.getItem(TOKEN_STORAGE_KEY);
    accessToken = token;
    return token;
  } catch (e) {
    console.warn('[http] Failed to load token from storage:', e);
    accessToken = null;
    return null;
  }
}

export function getRefreshToken(): string | null {
  if (!refreshToken) {
    try {
      refreshToken = localStorage.getItem(REFRESH_TOKEN_STORAGE_KEY);
    } catch (e) {
      console.warn('[http] Failed to load refresh token from storage:', e);
    }
  }
  return refreshToken;
}

async function refreshAuthToken(): Promise<boolean> {
  const currentRefreshToken = getRefreshToken();
  if (!currentRefreshToken) return false;

  try {
    const res = await fetch('/api/auth/refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: currentRefreshToken }),
    });

    const json = await res.json().catch(() => ({}));

    if (!res.ok || !json.accessToken) {
      setAccessToken(null, null);
      return false;
    }

    // ✅ Giữ fix thêm refreshToken
    setAccessToken(json.accessToken, json.user, json.refreshToken);
    return true;
  } catch (e) {
    console.error('[http] Refresh Token API failed:', e);
    setAccessToken(null, null);
    return false;
  }
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
      signal: controller.signal,
    });

    clearTimeout(timeoutId);
    console.log('[http] Response:', url, { status: res.status, statusText: res.statusText });

    if (res.status === 401) {
      console.warn('[http] Unauthorized (401), attempting token refresh...');
      const refreshed = await refreshAuthToken();

      if (refreshed) {
        const newToken = getAccessToken();
        if (newToken) {
          headers.set('Authorization', `Bearer ${newToken}`);
          return fetch(input, { ...init, headers, credentials: 'include' });
        }
      }

      setAccessToken(null, null);

      if (
        typeof window !== 'undefined' &&
        !window.location.pathname.startsWith('/login') &&
        !window.location.pathname.startsWith('/register')
      ) {
        const traderRoutes = [
          '/trader-dashboard',
          '/watchlist',
          '/trade',
          '/orders',
          '/portfolio',
          '/wallets',
          '/deposit',
          '/withdraw',
          '/subscription',
          '/settings',
          '/market',
        ];
        if (traderRoutes.some((route) => window.location.pathname.startsWith(route))) {
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
