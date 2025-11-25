import { authFetch } from './http';

export type ApiResult<T> = { ok: true; data: T } | { ok: false; error: string };

async function apiGet<T>(url: string): Promise<ApiResult<T>> {
  try {
    const res = await authFetch(url, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'Cache-Control': 'no-cache, no-store, must-revalidate',
        'Pragma': 'no-cache',
        'Expires': '0'
      },
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
    if (!res.ok) {
      // Return the full error object if it has additional fields (like required/available for insufficient balance)
      const errorObj = json as any;
      if (errorObj && typeof errorObj === 'object' && (errorObj.required !== undefined || errorObj.available !== undefined)) {
        return { ok: false, error: JSON.stringify(errorObj) };
      }
      return { ok: false, error: errorObj?.message || `HTTP ${res.status}` };
    }
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
  message?: string;
  planType: number;
  planName?: string;
  amountVnd?: number;
  amountUsd?: number;
  subscriptionId?: number;
  paymentId?: number;
  periodStart?: string;
  periodEnd?: string;
  // Legacy fields for backward compatibility
  paymentUrl?: string;
  orderId?: string;
  amount?: number;
}

export interface UserSubscription {
  planType: number;
  status: string;
  isActive: boolean;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  canceledAt?: string;
}

export interface BillingHistoryItem {
  id: string;
  invoiceId: string;
  date: string;
  plan: string;
  planType: number;
  amount: number;
  amountUsd: number;
  currency: string;
  status: string;
  paymentMethod: string;
  transactionId?: string;
}

export interface BillingHistoryResponse {
  billingHistory: BillingHistoryItem[];
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

  getPlans: () => apiGet<SubscriptionPlansResponse>('/api/payment/plans'),

  createSubscriptionCheckout: (request: CreateSubscriptionCheckoutRequest) =>
    apiPost<CreateSubscriptionCheckoutResponse>('/api/payment/subscription/checkout', request),

  getSubscription: () => apiGet<UserSubscription>('/api/payment/subscription'),

  cancelSubscription: () => apiPost<{ message: string }>('/api/payment/subscription/cancel', {}),

  getBillingHistory: () => apiGet<BillingHistoryResponse>('/api/payment/billing-history'),

  createStripeDeposit: (request: CreateStripeDepositRequest) =>
    apiPost<CreateStripeDepositResponse>('/api/payment/deposit/stripe', request),
  getStripeSession: (sessionId: string) =>
    apiGet<StripeSessionInfo>(`/api/payment/stripe/session/${sessionId}`),
  confirmStripeDeposit: (sessionId: string) =>
    apiPost<ConfirmStripeDepositResponse>('/api/payment/deposit/stripe/confirm', { sessionId }),
};
