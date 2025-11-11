import { authFetch } from './http';

export type ApiResult<T> = { ok: true; data: T } | { ok: false; error: string };

async function apiGet<T>(url: string): Promise<ApiResult<T>> {
  try {
    const res = await authFetch(url);
    const json = await res.json().catch(() => ({}));
    if (!res.ok) return { ok: false, error: (json as any)?.message || `HTTP ${res.status}` };
    return { ok: true, data: json as T };
  } catch (e: any) {
    return { ok: false, error: e?.message || 'Network error' };
  }
}

// Helper function for portfolio overview with longer timeout (30s) and retry logic
// This endpoint does heavy processing (multiple DB queries, CoinGecko API calls, FIFO calculations)
async function apiGetWithLongTimeout<T>(url: string, timeoutMs: number = 30000, retries: number = 2): Promise<ApiResult<T>> {
  let lastError: any = null;
  
  for (let attempt = 0; attempt <= retries; attempt++) {
    try {
      if (attempt > 0) {
        // Wait before retry: 1s, 2s, 3s...
        await new Promise(resolve => setTimeout(resolve, 1000 * attempt));
        console.log(`[portfolio] Retrying request (attempt ${attempt + 1}/${retries + 1}):`, url);
      }
      
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), timeoutMs);
      
      const token = localStorage.getItem('crypto_trading_access_token');
      const headers = new Headers();
      if (token) {
        headers.set('Authorization', `Bearer ${token}`);
      }
      
      const res = await fetch(url, {
        method: 'GET',
        headers,
        credentials: 'include',
        signal: controller.signal,
      });
      
      clearTimeout(timeoutId);
      
      // Try to parse JSON response
      let json: any = {};
      try {
        const text = await res.text();
        if (text) {
          json = JSON.parse(text);
        } else {
          // Empty response body
          throw new Error('Empty response from server - backend may have crashed or timed out');
        }
      } catch (parseError: any) {
        console.error('[portfolio] Failed to parse JSON response:', parseError);
        // If response is not ok and we can't parse JSON, return error
        if (!res.ok) {
          throw new Error(`Server error (${res.status}): ${res.statusText || 'Unable to parse response'}`);
        }
        throw new Error('Invalid JSON response from server');
      }
      
      if (!res.ok) {
        return { ok: false, error: (json as any)?.message || `HTTP ${res.status}` };
      }
      
      return { ok: true, data: json as T };
    } catch (e: any) {
      lastError = e;
      
      // Don't retry on abort (timeout)
      if (e.name === 'AbortError') {
        return { ok: false, error: 'Request timeout - server may be processing. Please try again.' };
      }
      
      // Don't retry on last attempt
      if (attempt === retries) {
        // Check for specific error types
        if (e.message?.includes('Failed to fetch') || e.message?.includes('ERR_EMPTY_RESPONSE')) {
          return { ok: false, error: 'Server connection error - backend may be unavailable or processing. Please check if the server is running.' };
        }
        return { ok: false, error: e?.message || 'Network error' };
      }
      
      // Log retry attempt
      console.warn(`[portfolio] Request failed (attempt ${attempt + 1}/${retries + 1}):`, e.message);
    }
  }
  
  return { ok: false, error: lastError?.message || 'Network error after retries' };
}

async function apiPost<T>(url: string, body: unknown): Promise<ApiResult<T>> {
  try {
    const res = await authFetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    const json = await res.json().catch(() => ({}));
    if (!res.ok) return { ok: false, error: (json as any)?.message || `HTTP ${res.status}` };
    return { ok: true, data: json as T };
  } catch (e: any) {
    return { ok: false, error: e?.message || 'Network error' };
  }
}

async function apiPut<T>(url: string, body: unknown): Promise<ApiResult<T>> {
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
    return { ok: false, error: e?.message || 'Network error' };
  }
}

async function apiDelete<T>(url: string): Promise<ApiResult<T>> {
  try {
    const res = await authFetch(url, { method: 'DELETE' });
    const json = await res.json().catch(() => ({}));
    if (!res.ok) return { ok: false, error: (json as any)?.message || `HTTP ${res.status}` };
    return { ok: true, data: json as T };
  } catch (e: any) {
    return { ok: false, error: e?.message || 'Network error' };
  }
}

// DTOs
export interface Watchlist {
  id: string;
  name: string;
  isDefault: boolean;
  coinCount: number;
  createdAt: string;
  updatedAt: string;
  coins: WatchlistCoin[];
}

export interface WatchlistCoin {
  symbol: string;
  name: string;
  iconUrl: string;
  currentPrice: number;
  priceChange24h: number;
  priceChangePercent24h: number;
  addedAt: string;
}

export interface WatchlistItem {
  id: string;
  coinSymbol: string;
  addedAt: string;
}

export interface CreateWatchlistDto {
  name: string;
}

export interface RenameWatchlistDto {
  name: string;
}

export interface AddCoinDto {
  coinSymbol: string;
}

export interface WatchlistQuota {
  maxAllowed: number;
  current: number;
  canCreateMore: boolean;
  subscriptionTier: string;
}

// Portfolio DTOs
export interface PortfolioHolding {
  symbol: string;
  name: string;
  amount: number;
  avgPrice: number;
  currentPrice: number;
  value: number;
  pnl: number;
  pnlPercent: number;
  allocation: number;
  cost: number;
  imageUrl?: string;
}

export interface NavDataPoint {
  date: string;
  value: number;
}

export interface PortfolioOverview {
  totalValue: number;
  totalCost: number;
  unrealizedPnL: number;
  unrealizedPnLPercent: number;
  realizedPnL: number;
  holdings: PortfolioHolding[];
  navHistory: NavDataPoint[];
}

export const PortfolioApi = {
  getWatchlists: () => apiGet<Watchlist[]>('/api/portfolio/watchlists'),
  createWatchlist: (dto: CreateWatchlistDto) => apiPost<Watchlist>('/api/portfolio/watchlists', dto),
  getWatchlist: (id: string) => apiGet<Watchlist>(`/api/portfolio/watchlists/${id}`),
  getDefaultWatchlist: () => apiGet<Watchlist>('/api/portfolio/watchlists/default'),
  renameWatchlist: (id: string, dto: RenameWatchlistDto) => apiPut<Watchlist>(`/api/portfolio/watchlists/${id}/rename`, dto),
  deleteWatchlist: (id: string) => apiDelete<void>(`/api/portfolio/watchlists/${id}`),
  addCoinToDefault: (dto: AddCoinDto) => apiPost<void>('/api/portfolio/watchlists/default/coins', dto),
  addCoinToWatchlist: (id: string, dto: AddCoinDto) => apiPost<void>(`/api/portfolio/watchlists/${id}/coins`, dto),
  removeCoinFromWatchlist: (id: string, symbol: string) => apiDelete<void>(`/api/portfolio/watchlists/${id}/coins/${symbol}`),
  getWatchlistQuota: () => apiGet<WatchlistQuota>('/api/portfolio/watchlists/quota'),
  getPortfolioOverview: () => apiGetWithLongTimeout<PortfolioOverview>('/api/portfolio/overview', 30000), // 30s timeout for heavy processing
};

