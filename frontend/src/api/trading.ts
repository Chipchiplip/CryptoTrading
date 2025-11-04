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

// DTOs - aligned with backend Models/DTOs/TradingDtos.cs
export type OrderStatus = 'NEW' | 'PARTIAL' | 'FILLED' | 'CANCELED' | 'REJECTED';
export type OrderSide = 'BUY' | 'SELL';
export type OrderType = 'MARKET' | 'LIMIT';

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
  side: OrderSide;
  type: OrderType;
  quantity: number;
  price?: number;
  filled: number;
  remaining: number;
  status: OrderStatus;
  createdAt: string;
  updatedAt: string;
}

export interface OrderDetail extends Order {
  avgPrice?: number;
  totalFees: number;
  trades: Trade[];
}

export interface Trade {
  id: string;
  orderId: string;
  symbol: string;
  price: number;
  quantity: number;
  fee: number;
  createdAt: string;
}

export interface PlaceOrderDto {
  symbol: string;
  side: OrderSide;
  type: OrderType;
  quantity: number;
  price?: number;
}

export interface PaginatedResponse<T> {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  data: T[];
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
  quantity: number;  // Backend uses 'quantity', not 'amount'
  total: number;
  orderCount: number;
}

export const TradingApi = {
  getBalances: () => apiGet<TradingBalances>('/api/trading/balances'),
  
  // Orders - with pagination support
  getOrders: (params?: {
    symbol?: string;
    side?: OrderSide;
    type?: OrderType;
    status?: OrderStatus[];
    fromDate?: string;
    toDate?: string;
    page?: number;
    pageSize?: number;
  }) => {
    const query = new URLSearchParams();
    if (params?.symbol) query.append('symbol', params.symbol);
    if (params?.side) query.append('side', params.side);
    if (params?.type) query.append('type', params.type);
    if (params?.status) params.status.forEach(s => query.append('status', s));
    if (params?.fromDate) query.append('fromDate', params.fromDate);
    if (params?.toDate) query.append('toDate', params.toDate);
    if (params?.page) query.append('page', params.page.toString());
    if (params?.pageSize) query.append('pageSize', params.pageSize.toString());
    
    const queryStr = query.toString();
    return apiGet<PaginatedResponse<Order>>(`/api/trading/orders${queryStr ? `?${queryStr}` : ''}`);
  },
  
  getOrder: (id: string) => apiGet<OrderDetail>(`/api/trading/orders/${id}`),
  
  placeOrder: (dto: PlaceOrderDto) => apiPost<OrderDetail>('/api/trading/orders', dto),
  
  cancelOrder: (id: string) => apiDelete<Order>(`/api/trading/orders/${id}`),
  
  // Trades - with pagination support
  getTrades: (params?: {
    symbol?: string;
    orderId?: string;
    fromDate?: string;
    toDate?: string;
    page?: number;
    pageSize?: number;
  }) => {
    const query = new URLSearchParams();
    if (params?.symbol) query.append('symbol', params.symbol);
    if (params?.orderId) query.append('orderId', params.orderId);
    if (params?.fromDate) query.append('fromDate', params.fromDate);
    if (params?.toDate) query.append('toDate', params.toDate);
    if (params?.page) query.append('page', params.page.toString());
    if (params?.pageSize) query.append('pageSize', params.pageSize.toString());
    
    const queryStr = query.toString();
    return apiGet<PaginatedResponse<Trade>>(`/api/trading/trades${queryStr ? `?${queryStr}` : ''}`);
  },
  
  getDashboard: () => apiGet<DashboardData>('/api/trading/dashboard'),
  
  getOrderBook: (symbol: string) => apiGet<OrderBook>(`/api/trading/orderbook/${encodeURIComponent(symbol)}`),
};

