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

// DTOs
export interface Crypto {
  id: string;
  symbol: string;
  name: string;
  currentPrice: number;
  priceChange24h: number;
  priceChangePercentage24h: number;
  marketCap: number;
  totalVolume: number;
  image?: string;
}

export interface CoinPriceUpdate {
  symbol: string;
  currentPrice: number;
  priceChange24h: number;
  priceChangePercentage24h: number;
  updatedAt: string;
}

export interface WatchlistRealtimeUpdate {
  watchlistId: string;
  updates: CoinPriceUpdate[];
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

export interface PriceHistoryItem {
  coinId: string;
  price: number;
  timestamp: string;
}

export const MarketApi = {
  getCryptocurrencies: () => apiGet<Crypto[]>('/api/market/cryptocurrencies'),
  getCryptocurrency: (symbol: string) => apiGet<Crypto>(`/api/market/cryptocurrencies/${symbol}`),
  getPrices: (symbols: string[]) => apiGet<Crypto[]>(`/api/market/prices?${symbols.map(s => `symbols=${s}`).join('&')}`),
  getMarketStats: () => apiGet<any>('/api/market/stats'),
  getWatchlistRealtime: (watchlistId: string) => apiGet<WatchlistRealtimeUpdate>(`/api/portfolio/watchlists/${watchlistId}/realtime`),
  getPriceHistory: (coinId: string, days: number) => apiGet<PriceHistoryItem[]>(`/api/market/cryptocurrencies/${coinId}/history?days=${days}`),
};

