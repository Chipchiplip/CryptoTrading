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

export interface CreateDepositRequest {
  amount: number;
}

export interface CreateDepositResponse {
  paymentUrl: string;
  orderId: string;
  depositId: number;
}

export interface CreateStripeDepositRequest {
  amount: number;
  currency?: string;
}

export interface CreateStripeDepositResponse {
  sessionId: string;
  checkoutUrl: string;
  orderId: string;
  depositId: number;
}

export interface StripeSessionInfo {
  sessionId: string;
  sessionStatus: string;
  sessionAmountTotal?: number;
  sessionCurrency?: string;
  depositId: number;
  depositStatus: string;
  depositAmount: number;
  depositCurrency: string;
  orderId: string;
}

export interface ConfirmStripeDepositResponse {
  depositId: number;
  status: string;
  creditedAmount: number;
}

export const PaymentApi = {
  createVnpayDeposit: (request: CreateDepositRequest) =>
    apiPost<CreateDepositResponse>('/api/payment/deposit/vnpay', request),
  createStripeDeposit: (request: CreateStripeDepositRequest) =>
    apiPost<CreateStripeDepositResponse>('/api/payment/deposit/stripe', request),
  getStripeSession: (sessionId: string) =>
    apiGet<StripeSessionInfo>(`/api/payment/stripe/session/${sessionId}`),
  confirmStripeDeposit: (sessionId: string) =>
    apiPost<ConfirmStripeDepositResponse>('/api/payment/deposit/stripe/confirm', { sessionId }),
};
