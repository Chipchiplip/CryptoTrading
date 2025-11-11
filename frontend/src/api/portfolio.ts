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
  getPortfolioOverview: () => apiGet<PortfolioOverview>('/api/portfolio/overview'),
};

