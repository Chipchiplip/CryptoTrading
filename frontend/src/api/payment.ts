import { authFetch } from './http';

export type ApiResult<T> = { ok: true; data: T } | { ok: false; error: string };

async function apiGet<T>(url: string): Promise<ApiResult<T>> {
  try {
    const res = await authFetch(url, {
      method: 'GET',
      headers: { 'Content-Type': 'application/json' },
    });
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

export interface SubscriptionPlan {
  id: number;
  name: string;
  price: number;
  priceVnd: number;
  period: string;
  features: string[];
}

export interface SubscriptionPlansResponse {
  plans: SubscriptionPlan[];
}

export interface CreateSubscriptionCheckoutRequest {
  planType: number;
}

export interface CreateSubscriptionCheckoutResponse {
  paymentUrl?: string;
  orderId?: string;
  paymentId?: number;
  planType: number;
  amount?: number;
  message?: string;
}

export interface UserSubscription {
  planType: number;
  status: string;
  isActive: boolean;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  canceledAt?: string;
}

export const PaymentApi = {
  createVnpayDeposit: (request: CreateDepositRequest) =>
    apiPost<CreateDepositResponse>('/api/payment/deposit/vnpay', request),

  getPlans: () => apiGet<SubscriptionPlansResponse>('/api/payment/plans'),

  createSubscriptionCheckout: (request: CreateSubscriptionCheckoutRequest) =>
    apiPost<CreateSubscriptionCheckoutResponse>('/api/payment/subscription/checkout', request),

  getSubscription: () => apiGet<UserSubscription>('/api/payment/subscription'),

  cancelSubscription: () => apiPost<{ message: string }>('/api/payment/subscription/cancel', {}),
};

