import { authFetch } from './http';

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

export interface CreateDepositRequest {
  amount: number;
}

export interface CreateDepositResponse {
  paymentUrl: string;
  orderId: string;
  depositId: number;
}

export const PaymentApi = {
  createVnpayDeposit: (request: CreateDepositRequest) =>
    apiPost<CreateDepositResponse>('/api/payment/deposit/vnpay', request),
};

