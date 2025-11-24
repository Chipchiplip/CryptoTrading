import { authFetch } from './http';

export type ApiResult<T> =
  | { ok: true; data: T }
  | { ok: false; error: string };

export interface ApiResponse<T = any> {
  success: boolean;
  message: string;
  data: T;
  statusCode: number;
  timestamp: string;
}

export interface PaginationParams {
  page?: number;
  pageSize?: number;
}

export interface PaginationMeta {
  currentPage: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface PaginatedResponse<T> {
  users?: T[];
  levels?: T[];
  roles?: T[];
  pagination: PaginationMeta;
}

export interface UserFilters extends PaginationParams {
  role?: string;  
  isActive?: boolean; 
}

export interface UserStatistics {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  usersByRole: Array<{ role: string; count: number }>;
  usersByLevel: Array<{ level: number; count: number }>;
}

export interface UserListDto {
  id: number;
  fullName: string;
  email: string;
  role: string;
  roleId: number;
  level: string;
  levelId: number;
  status: string;
  isActive: boolean;
  createdAt?: string;
  lastLogin?: string;
}

export interface Role {
  id: number;
  name: string;
  description?: string;
}

export interface Level {
  id: number;
  name: string;
  number: number;
  minBalance?: number;
  maxBalance?: number;
}

async function handleResponse<T>(res: Response): Promise<ApiResult<T>> {
  try {
    const json = await res.json().catch(() => ({}));

    if (!res.ok) {
      const apiRes = json as ApiResponse;
      const errorMsg =
        apiRes?.message ||
        (apiRes?.data as any)?.errors?.[0] ||
        `HTTP ${res.status}: ${res.statusText}`;
      return { ok: false, error: errorMsg };
    }

    return { ok: true, data: json as T };
  } catch (e: any) {
    return { ok: false, error: e?.message || 'Failed to parse response' };
  }
}

async function apiGet<T>(url: string): Promise<ApiResult<T>> {
  try {
    const res = await authFetch(url);
    return handleResponse<T>(res);
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
    return handleResponse<T>(res);
  } catch (e: any) {
    return { ok: false, error: e?.message || 'Network error' };
  }
}

async function apiPut<T>(url: string, body: unknown): Promise<ApiResult<T>> {
  try {
    const res = await authFetch(url, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    return handleResponse<T>(res);
  } catch (e: any) {
    return { ok: false, error: e?.message || 'Network error' };
  }
}

async function apiDelete<T>(url: string): Promise<ApiResult<T>> {
  try {
    const res = await authFetch(url, { method: 'DELETE' });
    return handleResponse<T>(res);
  } catch (e: any) {
    return { ok: false, error: e?.message || 'Network error' };
  }
}

function buildQueryString(params: Record<string, any>): string {
  const filtered = Object.entries(params)
    .filter(([_, value]) => value !== undefined && value !== null && value !== '')
    .reduce((acc, [key, value]) => ({ ...acc, [key]: value }), {});

  const searchParams = new URLSearchParams(filtered as any);
  return searchParams.toString() ? `?${searchParams.toString()}` : '';
}

function validateId(id: number, entityName: string = 'Entity'): void {
  if (!id || id <= 0) {
    throw new Error(`Invalid ${entityName} ID: ${id}`);
  }
}

function validatePagination(params?: PaginationParams): void {
  if (params?.page && params.page < 1) {
    throw new Error('Page number must be at least 1');
  }
  if (params?.pageSize && (params.pageSize < 1 || params.pageSize > 100)) {
    throw new Error('Page size must be between 1 and 100');
  }
}

const API_BASE = '/api/admin';

export const adminApi = {
  getUsers: async (
    filters: UserFilters = {}
  ): Promise<ApiResult<ApiResponse<PaginatedResponse<UserListDto>>>> => {
    try {
      validatePagination(filters);
      const queryString = buildQueryString(filters);
      return apiGet<ApiResponse<PaginatedResponse<UserListDto>>>(
        `${API_BASE}/users${queryString}`
      );
    } catch (e: any) {
      return { ok: false, error: e?.message || 'Failed to fetch users' };
    }
  },

  getUserById: async (id: number): Promise<ApiResult<ApiResponse<UserListDto>>> => {
    try {
      validateId(id, 'User');
      return apiGet<ApiResponse<UserListDto>>(`${API_BASE}/users/${id}`);
    } catch (e: any) {
      return { ok: false, error: e?.message || 'Failed to fetch user' };
    }
  },

  updateUserRole: async (
    id: number,
    roleId: number 
  ): Promise<ApiResult<ApiResponse>> => {
    try {
      validateId(id, 'User');
      return apiPut<ApiResponse>(
        `${API_BASE}/users/${id}/role`,
        { RoleId: roleId }
      );
    } catch (e: any) {
      return { ok: false, error: e?.message || 'Failed to update user role' };
    }
  },

  updateUserLevel: async (
    id: number,
    levelId: number
  ): Promise<ApiResult<ApiResponse>> => {
    try {
      validateId(id, 'User');
      if (levelId < 1) {
        throw new Error('Invalid level ID');
      }
      return apiPut<ApiResponse>(
        `${API_BASE}/users/${id}/level`,
        { LevelId: levelId }
      );
    } catch (e: any) {
      return { ok: false, error: e?.message || 'Failed to update user level' };
    }
  },

  updateUserStatus: async (
    id: number,
    status: string | boolean
  ): Promise<ApiResult<ApiResponse>> => {
    try {
      validateId(id, 'User');
      const isActive = typeof status === 'boolean'
        ? status
        : status === 'active' || status === 'true';
        
      return apiPut<ApiResponse>(
        `${API_BASE}/users/${id}/status`,
        { isActive }
      );
    } catch (e: any) {
      return { ok: false, error: e?.message || 'Failed to update user status' };
    }
  },

  deleteUser: async (id: number): Promise<ApiResult<ApiResponse>> => {
    try {
      validateId(id, 'User');
      return apiDelete<ApiResponse>(`${API_BASE}/users/${id}`);
    } catch (e: any) {
      return { ok: false, error: e?.message || 'Failed to delete user' };
    }
  },

  getUserStatistics: async (): Promise<ApiResult<ApiResponse<UserStatistics>>> => {
    return apiGet<ApiResponse<UserStatistics>>(`${API_BASE}/users/statistics`);
  },

  getRoles: async (): Promise<ApiResult<ApiResponse<{ total: number; roles: Role[] }>>> => {
    return apiGet<ApiResponse<{ total: number; roles: Role[] }>>(`${API_BASE}/roles`);
  },

  getRoleById: async (id: number): Promise<ApiResult<ApiResponse<Role>>> => {
    try {
      validateId(id, 'Role');
      return apiGet<ApiResponse<Role>>(`${API_BASE}/roles/${id}`);
    } catch (e: any) {
      return { ok: false, error: e?.message || 'Failed to fetch role' };
    }
  },

  createRole: async (data: {
    name: string;
    description?: string;
  }): Promise<ApiResult<ApiResponse>> => {
    try {
      if (!data.name || data.name.trim().length === 0) {
        throw new Error('Role name is required');
      }
      return apiPost<ApiResponse>(`${API_BASE}/roles`, data);
    } catch (e: any) {
      return { ok: false, error: e?.message || 'Failed to create role' };
    }
  },

  updateRole: async (
    id: number,
    data: { name?: string; description?: string }
  ): Promise<ApiResult<ApiResponse>> => {
    try {
      validateId(id, 'Role');
      
      if (data.name !== undefined && data.name.trim().length === 0) {
        throw new Error('Role name cannot be empty');
      }

      return apiPut<ApiResponse>(`${API_BASE}/roles/${id}`, data);
    } catch (e: any) {
      return { ok: false, error: e?.message || 'Failed to update role' };
    }
  },

  deleteRole: async (id: number): Promise<ApiResult<ApiResponse>> => {
    try {
      validateId(id, 'Role');
      return apiDelete<ApiResponse>(`${API_BASE}/roles/${id}`);
    } catch (e: any) {
      return { ok: false, error: e?.message || 'Failed to delete role' };
    }
  },

  getLevels: async (): Promise<ApiResult<ApiResponse<{ total: number; levels: Level[] }>>> => {
    return apiGet<ApiResponse<{ total: number; levels: Level[] }>>(`${API_BASE}/levels`);
  },

  getLevelById: async (id: number): Promise<ApiResult<ApiResponse<Level>>> => {
    try {
      validateId(id, 'Level');
      return apiGet<ApiResponse<Level>>(`${API_BASE}/levels/${id}`);
    } catch (e: any) {
      return { ok: false, error: e?.message || 'Failed to fetch level' };
    }
  },

  createLevel: async (data: {
    name: string;
    description?: string;
    minBalance?: number;
    maxBalance?: number;
  }): Promise<ApiResult<ApiResponse>> => {
    try {
      if (!data.name || data.name.trim().length === 0) {
        throw new Error('Level name is required');
      }
      if (data.minBalance !== undefined && data.minBalance < 0) {
        throw new Error('Min balance cannot be negative');
      }
      if (data.maxBalance !== undefined && data.maxBalance < 0) {
        throw new Error('Max balance cannot be negative');
      }
      if (
        data.minBalance !== undefined &&
        data.maxBalance !== undefined &&
        data.minBalance > data.maxBalance
      ) {
        throw new Error('Min balance cannot be greater than max balance');
      }

      return apiPost<ApiResponse>(`${API_BASE}/levels`, data);
    } catch (e: any) {
      return { ok: false, error: e?.message || 'Failed to create level' };
    }
  },

  updateLevel: async (
    id: number,
    data: {
      name?: string;
      description?: string;
      minBalance?: number;
      maxBalance?: number;
    }
  ): Promise<ApiResult<ApiResponse>> => {
    try {
      validateId(id, 'Level');
      
      if (data.name !== undefined && data.name.trim().length === 0) {
        throw new Error('Level name cannot be empty');
      }
      if (data.minBalance !== undefined && data.minBalance < 0) {
        throw new Error('Min balance cannot be negative');
      }
      if (data.maxBalance !== undefined && data.maxBalance < 0) {
        throw new Error('Max balance cannot be negative');
      }
      if (
        data.minBalance !== undefined &&
        data.maxBalance !== undefined &&
        data.minBalance > data.maxBalance
      ) {
        throw new Error('Min balance cannot be greater than max balance');
      }

      return apiPut<ApiResponse>(`${API_BASE}/levels/${id}`, data);
    } catch (e: any) {
      return { ok: false, error: e?.message || 'Failed to update level' };
    }
  },

  deleteLevel: async (id: number): Promise<ApiResult<ApiResponse>> => {
    try {
      validateId(id, 'Level');
      return apiDelete<ApiResponse>(`${API_BASE}/levels/${id}`);
    } catch (e: any) {
      return { ok: false, error: e?.message || 'Failed to delete level' };
    }
  },

  getUserSubscription: async (userId: number): Promise<ApiResult<ApiResponse<{
    planType: number;
    status: string;
    isActive: boolean;
    currentPeriodStart: string;
    currentPeriodEnd: string;
    canceledAt?: string;
  }>>> => {
    try {
      validateId(userId, 'User');
      return apiGet<ApiResponse<{
        planType: number;
        status: string;
        isActive: boolean;
        currentPeriodStart: string;
        currentPeriodEnd: string;
        canceledAt?: string;
      }>>(`${API_BASE}/users/${userId}/subscription`);
    } catch (e: any) {
      return { ok: false, error: e?.message || 'Failed to fetch user subscription' };
    }
  },
};

export const adminUtils = {
  buildQueryString,
  validateId,
  validatePagination,
};