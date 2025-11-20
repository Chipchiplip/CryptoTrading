import { createContext, useContext, useState, useEffect, useCallback, ReactNode } from 'react';
import { DashboardSummary, DashboardApi } from '../services/dashboard';

interface DashboardContextType {
  summary: DashboardSummary | null;
  loading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
  lastFetch: number;
}

const DashboardContext = createContext<DashboardContextType | undefined>(undefined);

const CACHE_DURATION = 10000; // 10 seconds deduplication window
const REFETCH_INTERVAL = 30000; // Auto refetch every 30 seconds

interface DashboardProviderProps {
  children: ReactNode;
}

export function DashboardProvider({ children }: DashboardProviderProps) {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastFetch, setLastFetch] = useState(0);
  const [isFetching, setIsFetching] = useState(false);

  const fetchSummary = useCallback(async (force = false) => {
    const now = Date.now();
    
    // Deduplication: Skip if recently fetched (unless forced)
    if (!force && isFetching) {
      console.log('[DashboardContext] Fetch already in progress, skipping duplicate call');
      return;
    }
    
    if (!force && now - lastFetch < CACHE_DURATION) {
      console.log('[DashboardContext] Using cached data (fetched', now - lastFetch, 'ms ago)');
      return;
    }

    setIsFetching(true);
    setLoading(true);
    setError(null);

    try {
      console.log('[DashboardContext] Fetching dashboard summary...');
      const result = await DashboardApi.getSummary();
      
      if (result.ok && result.data) {
        setSummary(result.data);
        setLastFetch(Date.now());
        console.log('[DashboardContext] Summary loaded successfully');
      } else {
        const errorMsg = !result.ok ? (result as any).error : 'Failed to fetch dashboard summary';
        setError(errorMsg);
        console.error('[DashboardContext] Failed to fetch summary:', errorMsg);
        
        // Set default summary on error to prevent UI breakage
        setSummary(prev => prev || {
          totalBalance: 0,
          totalBalanceChange: 0,
          totalBalanceChangePercent: 0,
          todayPnl: 0,
          todayPnlPercent: 0,
          availableBalance: 0,
          availableBalancePercent: 0,
          openOrdersCount: 0,
          openOrdersBuy: 0,
          openOrdersSell: 0
        });
      }
    } catch (e: any) {
      const errorMsg = e.message || 'Network error';
      setError(errorMsg);
      console.error('[DashboardContext] Exception while fetching summary:', e);
      
      // Set default summary on error to prevent UI breakage
      setSummary(prev => prev || {
        totalBalance: 0,
        totalBalanceChange: 0,
        totalBalanceChangePercent: 0,
        todayPnl: 0,
        todayPnlPercent: 0,
        availableBalance: 0,
        availableBalancePercent: 0,
        openOrdersCount: 0,
        openOrdersBuy: 0,
        openOrdersSell: 0
      });
    } finally {
      setLoading(false);
      setIsFetching(false);
    }
  }, [lastFetch, isFetching]);

  // Initial fetch on mount
  useEffect(() => {
    fetchSummary(true);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Auto refetch interval - use ref to avoid recreating interval on every render
  useEffect(() => {
    const interval = setInterval(() => {
      fetchSummary(false);
    }, REFETCH_INTERVAL);
    
    return () => clearInterval(interval);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const value: DashboardContextType = {
    summary,
    loading,
    error,
    refetch: () => fetchSummary(true),
    lastFetch
  };

  return (
    <DashboardContext.Provider value={value}>
      {children}
    </DashboardContext.Provider>
  );
}

export function useDashboardSummary() {
  const context = useContext(DashboardContext);
  if (context === undefined) {
    throw new Error('useDashboardSummary must be used within a DashboardProvider');
  }
  return context;
}

