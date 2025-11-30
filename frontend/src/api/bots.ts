import { authFetch } from './http';

export type ApiResult<T> = { ok: true; data: T } | { ok: false; error: string };

export interface StrategyInfo {
  id: string;
  key: string;
  version: string;
  displayName: string;
}

export interface BotRuntimeInfo {
  nextRunAt?: string;
  openPositions: number;
  unrealizedPnl: number;
  realizedPnl: number;
  totalFees: number;
  totalOrders: number;
  filledOrders: number;
  lastSignal?: string;
  lastExecutionAt?: string;
}

export interface BotSummary {
  id: string;
  name: string;
  status: string;
  baseAsset: string;
  quoteAsset: string;
  strategy: StrategyInfo;
  runtime?: BotRuntimeInfo;
  createdAt: string;
  updatedAt?: string;
}

export interface BotDetail extends BotSummary {
  userId?: number;
  riskProfile?: string;
  parameters?: Record<string, unknown>;
  positionSizing?: Record<string, unknown>;
  executionIntervalSeconds?: number;
  nextRunAt?: string;
  lastStatusReason?: string;
}

export interface PaginatedBotsResponse {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  data: BotSummary[];
}

async function apiGet<T>(url: string): Promise<ApiResult<T>> {
  try {
    const res = await authFetch(url);
    const json = await res.json().catch(() => ({}));
    if (!res.ok) return { ok: false, error: (json as any)?.message || `HTTP ${res.status}` };
    return { ok: true, data: json as T };
  } catch (error: any) {
    return { ok: false, error: error?.message || 'Network error' };
  }
}

async function apiRequest<T>(url: string, method: string, body?: unknown): Promise<ApiResult<T>> {
  try {
    const res = await authFetch(url, {
      method,
      ...(body !== undefined
        ? {
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body),
          }
        : {}),
    });
    const json = await res.json().catch(() => ({}));
    if (!res.ok) return { ok: false, error: (json as any)?.message || `HTTP ${res.status}` };
    return { ok: true, data: json as T };
  } catch (error: any) {
    return { ok: false, error: error?.message || 'Network error' };
  }
}

export interface BotListOptions {
  status?: string;
  strategyKey?: string;
  baseAsset?: string;
  page?: number;
  pageSize?: number;
}

export interface BotOrder {
  id: string;
  botId: string;
  orderId: string;
  intent: string;
  signalId?: string;
  createdAt: string;
  orderDetails?: {
    id: string;
    symbol: string;
    side: string;
    type: string;
    quantity: number;
    price?: number;
    filled: number;
    remaining: number;
    status: string;
    createdAt: string;
    updatedAt?: string;
  };
}

export interface PaginatedBotOrdersResponse {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  data: BotOrder[];
}

export const BotApi = {
  getBots: (options: BotListOptions = {}) => {
    const params = new URLSearchParams();
    params.set('page', String(options.page ?? 1));
    params.set('pageSize', String(options.pageSize ?? 50));
    if (options.status) params.set('status', options.status);
    if (options.strategyKey) params.set('strategyKey', options.strategyKey);
    if (options.baseAsset) params.set('baseAsset', options.baseAsset);
    return apiGet<PaginatedBotsResponse>(`/api/bots?${params.toString()}`);
  },
  getBot: (id: string) => apiGet<BotDetail>(`/api/bots/${id}`),
  getBotOrders: (id: string, page = 1, pageSize = 20) => {
    const params = new URLSearchParams();
    params.set('page', String(page));
    params.set('pageSize', String(pageSize));
    return apiGet<PaginatedBotOrdersResponse>(`/api/bots/${id}/orders?${params.toString()}`);
  },
  startBot: (id: string, payload: Record<string, unknown> = {}) =>
    apiRequest<BotSummary>(`/api/bots/${id}/start`, 'POST', payload),
  stopBot: (id: string, payload: Record<string, unknown> = {}) =>
    apiRequest<BotSummary>(`/api/bots/${id}/stop`, 'POST', payload),
  deleteBot: (id: string) => apiRequest<{ success: boolean }>(`/api/bots/${id}`, 'DELETE'),
  closePositions: (id: string) =>
    apiRequest<{ message: string; closedQuantity: number; ordersCreated: number; remainingInventory: number }>(
      `/api/bots/${id}/close-positions`,
      'POST'
    ),
};

