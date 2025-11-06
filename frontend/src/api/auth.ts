import { authFetch } from './http';

export type ApiResult<T> = { ok: true; data: T } | { ok: false; error: string };

async function postJson<T>(url: string, body: unknown): Promise<ApiResult<T>> {
  try {
    const res = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(body),
    });
    const json = await res.json().catch(() => ({}));
    if (!res.ok) return { ok: false, error: (json as any)?.message || `HTTP ${res.status}` };
    return { ok: true, data: json as T };
  } catch (e: any) {
    return { ok: false, error: e?.message || 'Network error' };
  }
}

async function authPostJson<T>(url: string, body: unknown): Promise<ApiResult<T>> {
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

export interface LoginDto { email: string; password: string; }
export interface RegisterDto { email: string; password: string; confirmPassword: string; fullName?: string; }
export interface TwoFactorDto { email: string; code: string; }
export interface ConfirmEmailDto { email: string; token: string; }
export interface ForgotPasswordDto { email: string; }
export interface ResetPasswordDto { email: string; token: string; newPassword: string; }

export interface UserInfo {
  id: number;
  email: string;
  fullName?: string;
  twoFactorEnabled: boolean;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken?: string;
  expiresAt?: string;
  requiresTwoFactor?: boolean;
  user?: UserInfo;
}

export interface Enable2FAResponse {
  secret: string;
  qrCodeUrl: string;
  message: string;
}

export interface Verify2FAResponse {
  message: string;
  twoFactorEnabled: boolean;
}

export interface VerifyTwoFactorDto { 
  code: string; 
}

export const AuthApi = {
  login: (dto: LoginDto) => postJson<AuthResponse>('/api/auth/login', dto),
  login2fa: (dto: TwoFactorDto) => postJson<AuthResponse>('/api/auth/login-2fa', dto),
  register: (dto: RegisterDto) => postJson<AuthResponse>('/api/auth/register', dto),
  confirmEmail: (dto: ConfirmEmailDto) => postJson<{ message: string }>('/api/auth/confirm-email', dto),
  forgotPassword: (dto: ForgotPasswordDto) => postJson<{ message: string }>('/api/auth/forgot-password', dto),
  resetPassword: (dto: ResetPasswordDto) => postJson<{ message: string }>('/api/auth/reset-password', dto),
  refresh: (refreshToken: string) => postJson<AuthResponse>('/api/auth/refresh', { refreshToken }),
  revoke: (refreshToken: string) => authPostJson<{ message: string }>('/api/auth/revoke', { refreshToken }),
  
  // 2FA Management (require authentication)
  enable2FA: () => authPostJson<Enable2FAResponse>('/api/auth/enable-2fa', {}),
  verify2FA: (dto: VerifyTwoFactorDto) => authPostJson<Verify2FAResponse>('/api/auth/verify-2fa', dto),
  
  // Test endpoints (for development)
  testGenerate2FACode: (email: string) => postJson<{ code: string }>('/api/auth/test-generate-2fa-code', { email }),
};



