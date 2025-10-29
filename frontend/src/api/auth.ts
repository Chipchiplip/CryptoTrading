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

export interface LoginDto { email: string; password: string; }
export interface RegisterDto { email: string; password: string; confirmPassword: string; }
export interface TwoFactorDto { email: string; code: string; }
export interface ConfirmEmailDto { email: string; token: string; }
export interface ForgotPasswordDto { email: string; }
export interface ResetPasswordDto { email: string; token: string; newPassword: string; }

export interface AuthResponse {
  accessToken: string;
  refreshToken?: string;
  requiresTwoFactor?: boolean;
}

export const AuthApi = {
  login: (dto: LoginDto) => postJson<AuthResponse>('/api/auth/login', dto),
  login2fa: (dto: TwoFactorDto) => postJson<AuthResponse>('/api/auth/login-2fa', dto),
  register: (dto: RegisterDto) => postJson<AuthResponse>('/api/auth/register', dto),
  confirmEmail: (dto: ConfirmEmailDto) => postJson<{ message: string }>('/api/auth/confirm-email', dto),
  forgotPassword: (dto: ForgotPasswordDto) => postJson<{ message: string }>('/api/auth/forgot-password', dto),
  resetPassword: (dto: ResetPasswordDto) => postJson<{ message: string }>('/api/auth/reset-password', dto),
  refresh: (refreshToken: string) => postJson<AuthResponse>('/api/auth/refresh', { refreshToken }),
  revoke: (refreshToken: string) => postJson<{ message: string }>('/api/auth/revoke', { refreshToken }),
};


