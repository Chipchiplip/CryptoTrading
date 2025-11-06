import { authFetch } from '../api/http';

export type ApiResult<T> = { ok: true; data: T } | { ok: false; error: string };

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';
const USE_MOCK = import.meta.env.VITE_USE_MOCK === 'true';

async function apiGet<T>(endpoint: string): Promise<ApiResult<T>> {
  try {
    // Use relative path if API_BASE_URL is empty (uses vite proxy)
    const url = API_BASE_URL ? `${API_BASE_URL}${endpoint}` : endpoint;
    const res = await authFetch(url);
    const json = await res.json().catch(() => ({}));
    
    if (!res.ok) {
      const errorMsg = (json as any)?.message || `HTTP ${res.status} ${res.statusText}`;
      console.error(`API Error [${endpoint}]:`, { status: res.status, error: errorMsg, json });
      return { ok: false, error: errorMsg };
    }
    return { ok: true, data: json as T };
  } catch (e: any) {
    console.error(`Network Error [${endpoint}]:`, e);
    return { ok: false, error: e?.message || 'Network error' };
  }
}

// DTOs
export interface DashboardSummary {
  totalBalance: number;
  totalBalanceChange: number;
  totalBalanceChangePercent: number;
  todayPnl: number;
  todayPnlPercent: number;
  availableBalance: number;
  availableBalancePercent: number;
  openOrdersCount: number;
  openOrdersBuy: number;
  openOrdersSell: number;
}

export interface NavDataPoint {
  date: string;
  value: number;
}

export interface NavHistory {
  from: string;
  to: string;
  data: NavDataPoint[];
}

export interface PnlDataPoint {
  time: string;
  pnl: number;
}

export interface PnlHistory {
  granularity: 'hourly' | 'daily' | 'weekly';
  date: string;
  data: PnlDataPoint[];
}

// Mock data generators (used when VITE_USE_MOCK=true)
const generateMockSummary = (): DashboardSummary => ({
  totalBalance: 12458.32,
  totalBalanceChange: 234.12,
  totalBalanceChangePercent: 1.9,
  todayPnl: 234.12,
  todayPnlPercent: 3.45,
  availableBalance: 8234.56,
  availableBalancePercent: 66.1,
  openOrdersCount: 3,
  openOrdersBuy: 2,
  openOrdersSell: 1,
});

const generateMockNavHistory = (from: string): NavHistory => {
  const fromDate = new Date(from);
  const data: NavDataPoint[] = [];
  
  for (let i = 29; i >= 0; i--) {
    const date = new Date(fromDate);
    date.setDate(date.getDate() - i);
    const value = 10000 + Math.random() * 3000;
    data.push({
      date: date.toISOString().split('T')[0],
      value: Math.round(value * 100) / 100,
    });
  }
  
  return {
    from,
    to: new Date().toISOString().split('T')[0],
    data,
  };
};

const generateMockPnlHistory = (granularity: 'hourly' | 'daily' | 'weekly', date: string): PnlHistory => {
  const data: PnlDataPoint[] = [];
  
  if (granularity === 'hourly') {
    for (let hour = 0; hour < 24; hour++) {
      data.push({
        time: `${String(hour).padStart(2, '0')}:00`,
        pnl: Math.round((Math.random() * 500 - 250) * 100) / 100,
      });
    }
  }
  
  return {
    granularity,
    date,
    data,
  };
};

// API functions
export const DashboardApi = {
  /**
   * Get dashboard summary (NAV, TodayPnL, AvailableBalance, OpenOrdersCount)
   */
  getSummary: async (): Promise<ApiResult<DashboardSummary>> => {
    if (USE_MOCK) {
      await new Promise(resolve => setTimeout(resolve, 500)); // Simulate API delay
      return { ok: true, data: generateMockSummary() };
    }
    return apiGet<DashboardSummary>('/api/trading/dashboard/summary');
  },

  /**
   * Get NAV history for the last 30 days
   * @param from Start date in YYYY-MM-DD format
   */
  getNavHistory: async (from?: string): Promise<ApiResult<NavHistory>> => {
    if (USE_MOCK) {
      await new Promise(resolve => setTimeout(resolve, 500));
      const fromDate = from || new Date(Date.now() - 29 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
      return { ok: true, data: generateMockNavHistory(fromDate) };
    }
    
    const fromDate = from || new Date(Date.now() - 29 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
    return apiGet<NavHistory>(`/api/trading/dashboard/nav?from=${fromDate}`);
  },

  /**
   * Get PnL history for a specific date
   * @param granularity hourly, daily, or weekly
   * @param date Date in YYYY-MM-DD format (defaults to today)
   */
  getPnlHistory: async (granularity: 'hourly' | 'daily' | 'weekly' = 'hourly', date?: string): Promise<ApiResult<PnlHistory>> => {
    if (USE_MOCK) {
      await new Promise(resolve => setTimeout(resolve, 500));
      const targetDate = date || new Date().toISOString().split('T')[0];
      return { ok: true, data: generateMockPnlHistory(granularity, targetDate) };
    }
    
    const targetDate = date || new Date().toISOString().split('T')[0];
    return apiGet<PnlHistory>(`/api/trading/dashboard/pnl?granularity=${granularity}&date=${targetDate}`);
  },
};

