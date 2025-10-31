const TOKEN_STORAGE_KEY = 'crypto_trading_access_token';

// Load token from localStorage on init
let accessToken: string | null = null;
try {
  accessToken = localStorage.getItem(TOKEN_STORAGE_KEY);
  if (accessToken) {
    console.log('[http] Token loaded from storage');
  }
} catch (e) {
  console.warn('[http] Failed to load token from storage:', e);
}

export function setAccessToken(token: string | null) {
  accessToken = token;
  try {
    if (token) {
      localStorage.setItem(TOKEN_STORAGE_KEY, token);
      console.log('[http] Access token set:', token.substring(0, 20) + '...');
    } else {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
      console.log('[http] Access token cleared');
    }
  } catch (e) {
    console.warn('[http] Failed to save token to storage:', e);
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

export async function authFetch(input: RequestInfo | URL, init?: RequestInit) {
  const url = typeof input === 'string' ? input : input.toString();
  
  // Ensure we have the latest token from localStorage
  const token = getAccessToken();
  
  const headers = new Headers(init?.headers || {});
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  } else {
    console.warn('[http] No access token available for request:', url);
  }
  
  console.log('[http] Fetching:', url, { method: init?.method || 'GET', hasAuth: !!token });
  
  try {
    const res = await fetch(input, { ...init, headers, credentials: 'include' });
    console.log('[http] Response:', url, { status: res.status, statusText: res.statusText });
    
    // If 401, clear token and redirect to login
    if (res.status === 401) {
      console.warn('[http] Unauthorized (401), clearing token');
      setAccessToken(null);
    }
    
    return res;
  } catch (e) {
    console.error('[http] Fetch error:', url, e);
    throw e;
  }
}


