import { authFetch } from './http';

export type ApiResult<T> = { ok: true; data: T } | { ok: false; error: string };

async function apiGet<T>(url: string): Promise<ApiResult<T>> {
  try {
    const res = await authFetch(url);
    const json = await res.json().catch(() => ({}));
    if (!res.ok) {
      const errorMsg = (json as any)?.message || `HTTP ${res.status} ${res.statusText}`;
      console.error(`API Error [${url}]:`, { status: res.status, error: errorMsg, json });
      return { ok: false, error: errorMsg };
    }
    return { ok: true, data: json as T };
  } catch (e: any) {
    console.error(`Network Error [${url}]:`, e);
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
export interface WalletBalance {
  symbol: string;
  available: number;
  locked: number;
  total: number;
  valueUsd: number;
}

export interface TradingBalances {
  totalBalance: number;
  availableBalance: number;
  lockedBalance: number;
  wallets: WalletBalance[];
}

export interface Order {
  id: string;
  symbol: string;
  side: 'Buy' | 'Sell';
  type: 'Market' | 'Limit';
  quantity: number;
  price?: number;
  filled: number;
  remaining: number;
  status: 'Open' | 'Filled' | 'Partial' | 'Canceled';
  createdAt: string;
  updatedAt: string;
}

export interface PlaceOrderDto {
  symbol: string;
  side: 'Buy' | 'Sell';
  type: 'Market' | 'Limit';
  quantity: number;
  price?: number;
}

export interface DashboardData {
  totalBalance: number;
  totalBalanceChange: number;
  totalBalanceChangePercent: number;
  todayPnl: number;
  todayPnlPercent: number;
  availableBalance: number;
  availableBalancePercent: number;
  openOrders: number;
  openOrdersBuy: number;
  openOrdersSell: number;
  recentOrders: Order[];
  topHoldings: Array<{
    symbol: string;
    name: string;
    amount: number;
    valueUsd: number;
    change24h: number;
  }>;
  navHistory: Array<{
    date: string;
    value: number;
  }>;
  pnlHistory: Array<{
    time: string;
    pnl: number;
  }>;
}

export interface OrderBook {
  symbol: string;
  currentPrice: number;
  priceChange24h: number;
  priceChangePercentage24h: number;
  asks: OrderBookLevel[];
  bids: OrderBookLevel[];
  lastUpdated: string;
}

export interface OrderBookLevel {
  price: number;
  amount: number;
  total: number;
}

export const TradingApi = {
  getBalances: () => apiGet<TradingBalances>('/api/trading/balances'),
  getOrders: () => apiGet<Order[]>('/api/trading/orders'),
  getOrder: (id: string) => apiGet<Order>(`/api/trading/orders/${id}`),
  placeOrder: (dto: PlaceOrderDto) => apiPost<Order>('/api/trading/orders', dto),
  getDashboard: () => apiGet<DashboardData>('/api/trading/dashboard'),
  getOrderBook: (symbol: string) => apiGet<OrderBook>(`/api/trading/orderbook/${encodeURIComponent(symbol)}`),
};

