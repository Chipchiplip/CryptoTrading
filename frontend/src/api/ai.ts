import { authFetch } from './http';
import { BotDetail } from './bots';

export type ApiResult<T> = { ok: true; data: T } | { ok: false; error: string };

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

export interface AiChatMessageRequest {
  userId: number;
  sessionId?: string;
  message: string;
}

export interface AiTradeSuggestionDto {
  decision: 'BUY' | 'SELL' | 'NO_TRADE';
  symbol: string;
  amountUsdt: number;
  expectedReturnPct: number | null;
  confidence: number;
  timeHorizon: string;
}

export interface AiChatBotSuggestion {
  suggestionId: string;
  name: string;
  symbols: string[];
  strategyType: string;
  riskMode: string;
  maxCapitalPerTrade: number;
  maxDailyExposure: number;
  timeHorizon: string;
  expectedReturnPct?: number | null;
  riskNote?: string | null;
}

export interface AiChatResponseDto {
  sessionId: string;
  reply: string;
  bots: AiChatBotSuggestion[];
  tradeSuggestion?: AiTradeSuggestionDto | null;
}

export const AiApi = {
  chat: (payload: AiChatMessageRequest) => apiPost<AiChatResponseDto>('/api/ai/chat', payload),
  applyBotSuggestion: async (suggestionId: string, userId: number): Promise<ApiResult<BotDetail>> => {
    try {
      const res = await authFetch(`/api/ai/chat/bot-suggestions/${suggestionId}/apply?userId=${userId}`, {
        method: 'POST',
      });
      const json = await res.json().catch(() => ({}));
      if (!res.ok) return { ok: false, error: (json as any)?.message || `HTTP ${res.status}` };
      return { ok: true, data: json as BotDetail };
    } catch (e: any) {
      return { ok: false, error: e?.message || 'Network error' };
    }
  },
};

