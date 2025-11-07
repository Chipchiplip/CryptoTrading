import React, { useState, useEffect, useCallback } from 'react';
import { Users, Search, Edit2, Lock, Trash2, X, RefreshCw, BarChart3, ChevronLeft, ChevronRight } from 'lucide-react';
import { adminApi, ApiResponse, PaginatedResponse, UserStatistics } from '../../../api/admin';

// ========== INTERFACES ==========
interface User {
  id: number;
  fullName: string;
  email: string;
  role: string;
  roleId: number;
  level: number;
  levelId: number;
  status: string;
  isActive: boolean;
  createdAt?: string;
  lastLogin?: string;
}

interface Role {
  id: number;
  name: string;
  description?: string;
}

interface Level {
  id: number;
  name: string;
  number: number;
  minBalance?: number;
  maxBalance?: number;
}

interface FilterState {
  role: string;
  isActive: string;
  level: string;
}

interface ToastState {
  message: string;
  type: 'success' | 'error' | 'info';
}

// ========== COMPONENTS ==========
const Badge = ({ children, variant = 'default' }: { children: React.ReactNode; variant?: string }) => {
  const variants: Record<string, string> = {
    default: 'bg-zinc-800 text-zinc-300',
    admin: 'bg-red-900/30 text-red-400 border border-red-800',
    trader: 'bg-blue-900/30 text-blue-400 border border-blue-800',
    user: 'bg-green-900/30 text-green-400 border border-green-800',
    active: 'bg-emerald-900/30 text-emerald-400 border border-emerald-800',
    locked: 'bg-gray-900/30 text-gray-400 border border-gray-800',
    inactive: 'bg-gray-900/30 text-gray-400 border border-gray-800',
    gold: 'bg-yellow-900/30 text-yellow-400 border border-yellow-800',
  };

  return (
    <span className={`px-2 py-1 rounded text-xs font-medium ${variants[variant.toLowerCase()] || variants.default}`}>
      {children}
    </span>
  );
};

const Toast = ({ message, type = 'success', onClose }: {
  message: string;
  type?: 'success' | 'error' | 'info';
  onClose: () => void;
}) => {
  useEffect(() => {
    const timer = setTimeout(onClose, 4000);
    return () => clearTimeout(timer);
  }, [onClose]);

  const types: Record<string, string> = {
    success: 'bg-green-900/90 border-green-700 text-green-100',
    error: 'bg-red-900/90 border-red-700 text-red-100',
    info: 'bg-blue-900/90 border-blue-700 text-blue-100',
  };

  return (
    <div className={`fixed top-4 right-4 px-4 py-3 rounded-lg border ${types[type]} shadow-lg z-50 flex items-center gap-2 animate-slide-in`}>
      {message}
      <button onClick={onClose} className="ml-2 hover:opacity-70 transition-opacity">
        <X className="w-4 h-4" />
      </button>
    </div>
  );
};

const Modal = ({
  isOpen,
  onClose,
  title,
  children,
}: {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
}) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-[9999] flex items-center justify-center bg-black/70 backdrop-blur-sm" onClick={onClose}>
      <div
        className="relative bg-zinc-900 border border-zinc-700 rounded-2xl p-6 w-full max-w-md mx-4 shadow-2xl text-zinc-100 animate-scale-in"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between mb-5 border-b border-zinc-800 pb-3">
          <h3 className="text-lg font-semibold">{title}</h3>
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-100 transition-colors">
            <X className="w-5 h-5" />
          </button>
        </div>
        <div className="space-y-6">{children}</div>
      </div>
    </div>
  );
};

const LoadingSpinner = () => (
  <div className="flex items-center justify-center p-8">
    <RefreshCw className="w-8 h-8 text-blue-500 animate-spin" />
  </div>
);

const EmptyState = ({ message }: { message: string }) => (
  <div className="text-center py-12 text-zinc-400">
    <Users className="w-16 h-16 mx-auto mb-4 opacity-30" />
    <p className="text-lg">{message}</p>
  </div>
);

const Pagination = ({
  currentPage,
  totalPages,
  onPageChange,
  totalItems,
}: {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  totalItems: number;
}) => {
  if (totalPages <= 1) return null;

  return (
    <div className="flex items-center justify-between px-4 py-3 bg-zinc-800/30 border-t border-zinc-700">
      <div className="text-sm text-zinc-400">
        Tổng <span className="font-medium text-zinc-200">{totalItems}</span> người dùng
      </div>
      <div className="flex items-center gap-2">
        <button
          onClick={() => onPageChange(currentPage - 1)}
          disabled={currentPage === 1}
          className="p-2 rounded-lg hover:bg-zinc-700 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
        >
          <ChevronLeft className="w-4 h-4" />
        </button>
        <span className="text-sm text-zinc-300">
          Trang {currentPage} / {totalPages}
        </span>
        <button
          onClick={() => onPageChange(currentPage + 1)}
          disabled={currentPage === totalPages}
          className="p-2 rounded-lg hover:bg-zinc-700 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
        >
          <ChevronRight className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
};

const StatisticsCard = ({ stats }: { stats: UserStatistics | null }) => {
  if (!stats) return null;

  return (
    <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-4 mb-4">
      <div className="flex items-center gap-2 mb-3">
        <BarChart3 className="w-5 h-5 text-blue-400" />
        <h3 className="font-semibold">Thống kê</h3>
      </div>
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="text-center">
          <p className="text-2xl font-bold text-blue-400">{stats.totalUsers}</p>
          <p className="text-xs text-zinc-400">Tổng</p>
        </div>
        <div className="text-center">
          <p className="text-2xl font-bold text-green-400">{stats.activeUsers}</p>
          <p className="text-xs text-zinc-400">Hoạt động</p>
        </div>
        <div className="text-center">
          <p className="text-2xl font-bold text-gray-400">{stats.inactiveUsers}</p>
          <p className="text-xs text-zinc-400">Khóa</p>
        </div>
        <div className="text-center">
          <p className="text-2xl font-bold text-purple-400">{stats.usersByRole.length}</p>
          <p className="text-xs text-zinc-400">Vai trò</p>
        </div>
      </div>
    </div>
  );
};

// ========== MAIN COMPONENT ==========
export default function AdminUsers() {
  // State management
  const [users, setUsers] = useState<User[]>([]);
  const [roles, setRoles] = useState<Role[]>([]);
  const [levels, setLevels] = useState<Level[]>([]);
  const [statistics, setStatistics] = useState<UserStatistics | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState<number | null>(null);
  const [toast, setToast] = useState<ToastState | null>(null);
  const [editModal, setEditModal] = useState<{ isOpen: boolean; user: User | null }>({
    isOpen: false,
    user: null,
  });
  const [filters, setFilters] = useState<FilterState>({ role: '', isActive: '', level: '' });
  const [search, setSearch] = useState('');
  const [editFormData, setEditFormData] = useState({ roleId: 0, levelId: 0 });
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const pageSize = 20;

  // Load initial data
  useEffect(() => {
    loadData();
    loadStatistics();
  }, [currentPage, filters]);

  // Debounced search
  useEffect(() => {
    const timer = setTimeout(() => {
      if (currentPage !== 1) {
        setCurrentPage(1);
      } else {
        loadData();
      }
    }, 500);
    return () => clearTimeout(timer);
  }, [search]);

  const showToast = useCallback((message: string, type: 'success' | 'error' | 'info' = 'success') => {
    setToast({ message, type });
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);

      // Build filters
      const apiFilters: any = {
        page: currentPage,
        pageSize,
      };

      if (filters.role) apiFilters.role = filters.role;
      if (filters.isActive) apiFilters.isActive = filters.isActive === 'true';

      // Fetch users with pagination
      const usersResult = await adminApi.getUsers(apiFilters);

      if (usersResult.ok) {
        const responseData = usersResult.data.data;
        const usersData = responseData.users || [];

        // Filter by search locally (since backend doesn't support search)
        const filteredUsers = search
          ? usersData.filter(
            (user: User) =>
              user.fullName?.toLowerCase().includes(search.toLowerCase()) ||
              user.email?.toLowerCase().includes(search.toLowerCase())
          )
          : usersData;

        // Filter by level locally
        const finalUsers = filters.level
          ? filteredUsers.filter((user: User) => user.level === parseInt(filters.level))
          : filteredUsers;

        setUsers(finalUsers);

        if (responseData.pagination) {
          setTotalPages(responseData.pagination.totalPages);
          setTotalItems(responseData.pagination.totalItems);
        }
      } else {
        showToast(`Lỗi tải users: ${usersResult.error}`, 'error');
        setUsers([]);
      }

      // Load roles and levels if not loaded
      if (roles.length === 0) {
        const rolesResult = await adminApi.getRoles();
        if (rolesResult.ok) {
          setRoles(rolesResult.data.data?.roles || []);
        }
      }

      if (levels.length === 0) {
        const levelsResult = await adminApi.getLevels();
        if (levelsResult.ok) {
          setLevels(levelsResult.data.data?.levels || []);
        }
      }
    } catch (err: any) {
      showToast(err.message || 'Không thể tải dữ liệu', 'error');
      setUsers([]);
    } finally {
      setLoading(false);
    }
  };

  const loadStatistics = async () => {
    try {
      const result = await adminApi.getUserStatistics();
      if (result.ok) {
        setStatistics(result.data.data);
      }
    } catch (err) {
      console.error('Failed to load statistics:', err);
    }
  };

  //
  // === 🚀 PHẦN ĐÃ SỬA 🚀 ===
  //
  // Hàm này được sửa để gửi `roleId` và `levelId` (number) trực tiếp,
  // khớp với `admin.ts` và `AdminUsersController.cs` đã sửa.
  //
  const handleUpdateUser = async () => {
    if (!editModal.user) return;

    try {
      setActionLoading(editModal.user.id);
      const { id } = editModal.user;
      
      // Lấy ID trực tiếp từ state của form
      const { roleId, levelId } = editFormData;

      // ✅ 1. Gửi Role ID (number)
      // (Khớp với `adminApi.updateUserRole(id: number, roleId: number)`)
      const roleResult = await adminApi.updateUserRole(id, roleId);
      if (!roleResult.ok) {
        throw new Error(roleResult.error);
      }

      // ✅ 2. Gửi Level ID (number)
      // (Khớp với `adminApi.updateUserLevel(id: number, levelId: number)`)
      const levelResult = await adminApi.updateUserLevel(id, levelId);
      if (!levelResult.ok) {
        throw new Error(levelResult.error);
      }

      showToast('Cập nhật người dùng thành công!', 'success');
      setEditModal({ isOpen: false, user: null });
      await loadData();
      await loadStatistics();
    } catch (err: any) {
      showToast(err.message || 'Cập nhật thất bại', 'error');
    } finally {
      setActionLoading(null);
    }
  };
  //
  // === KẾT THÚC PHẦN SỬA ===
  //

  const handleToggleStatus = async (userId: number, currentIsActive: boolean) => {
    try {
      setActionLoading(userId);
      const result = await adminApi.updateUserStatus(userId, !currentIsActive);

      if (!result.ok) {
        throw new Error(result.error);
      }

      const newStatus = !currentIsActive ? 'kích hoạt' : 'khóa';
      showToast(`Đã ${newStatus} người dùng`, 'success');
      await loadData();
      await loadStatistics();
    } catch (err: any) {
      showToast(err.message || 'Cập nhật trạng thái thất bại', 'error');
    } finally {
      setActionLoading(null);
    }
  };

  const handleDeleteUser = async (userId: number) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa người dùng này?')) return;

    try {
      setActionLoading(userId);
      const result = await adminApi.deleteUser(userId);

      if (!result.ok) {
        throw new Error(result.error);
      }

      showToast('Đã xóa người dùng', 'success');
      await loadData();
      await loadStatistics();
    } catch (err: any) {
      showToast(err.message || 'Xóa thất bại', 'error');
    } finally {
      setActionLoading(null);
    }
  };

  const openEditModal = (user: User) => {
    // State `editFormData` đã lưu đúng `roleId` và `levelId`
    setEditFormData({ roleId: user.roleId, levelId: user.levelId });
    setEditModal({ isOpen: true, user });
  };

  const handleFilterChange = (key: keyof FilterState, value: string) => {
    setFilters({ ...filters, [key]: value });
    setCurrentPage(1); // Reset to first page
  };

  const handleRefresh = () => {
    setCurrentPage(1);
    loadData();
    loadStatistics();
  };

  // Render
  return (
    <div className="min-h-screen bg-[#111827] text-white p-6">
      {toast && <Toast {...toast} onClose={() => setToast(null)} />}

      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold mb-2 flex items-center gap-2">
              <Users className="w-7 h-7" />
              Quản lý người dùng
            </h1>
            <p className="text-zinc-400">Quản lý tài khoản và phân quyền</p>
          </div>
          <button
            onClick={handleRefresh}
            className="p-2 bg-zinc-800 hover:bg-zinc-700 rounded-lg transition-colors"
            title="Làm mới"
          >
            <RefreshCw className="w-5 h-5" />
          </button>
        </div>

        {/* Statistics */}
        <StatisticsCard stats={statistics} />

        {/* Filters */}
        <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-4 mb-4">
          <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
            <div className="relative md:col-span-2">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-zinc-500" />
              <input
                type="text"
                placeholder="Tên hoặc Email"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="w-full bg-zinc-800 border border-zinc-700 rounded-lg pl-10 pr-4 py-2 focus:outline-none focus:border-blue-500 transition-colors"
              />
            </div>
            <select
              value={filters.role}
              onChange={(e) => handleFilterChange('role', e.target.value)}
              className="bg-black border border-zinc-700 rounded-lg px-4 py-2 focus:outline-none focus:border-blue-500 transition-colors"
            >
              <option value="">Tất cả vai trò</option>
              {roles.map((role) => (
                <option key={role.id} value={role.name}>
                  {role.name}
                </option>
              ))}
            </select>
            <select
              value={filters.level}
              onChange={(e) => handleFilterChange('level', e.target.value)}
              className="bg-black border border-zinc-700 rounded-lg px-4 py-2 focus:outline-none focus:border-blue-500 transition-colors"
            >
              <option value="">Tất cả cấp độ</option>
              {levels.map((level) => (
                <option key={level.id} value={level.number}>
                  {level.name}
                </option>
              ))}
            </select>
            <select
              value={filters.isActive}
              onChange={(e) => handleFilterChange('isActive', e.target.value)}
              className="bg-black border border-zinc-700 rounded-lg px-4 py-2 focus:outline-none focus:border-blue-500 transition-colors"
            >
              <option value="">Tất cả trạng thái</option>
              <option value="true">Hoạt động</option>
              <option value="false">Khóa</option>
            </select>
          </div>
        </div>

        {/* Table */}
        <div className="bg-zinc-900 border border-zinc-800 rounded-lg overflow-hidden">
          {loading ? (
            <LoadingSpinner />
          ) : users.length === 0 ? (
            <EmptyState message="Không tìm thấy người dùng nào" />
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead className="bg-zinc-800/50 border-b border-zinc-700">
                    <tr>
                      <th className="text-left px-4 py-3 text-sm font-medium">Người dùng</th>
                      <th className="text-left px-4 py-3 text-sm font-medium">Vai trò</th>
                      <th className="text-left px-4 py-3 text-sm font-medium">Cấp độ</th>
                      <th className="text-left px-4 py-3 text-sm font-medium">Trạng thái</th>
                      <th className="text-right px-4 py-3 text-sm font-medium">Hành động</th>
                    </tr>
                  </thead>
                  <tbody>
                    {users.map((user) => (
                      <tr key={user.id} className="border-b border-zinc-800 hover:bg-zinc-800/30 transition-colors">
                        <td className="px-4 py-3">
                          <div className="flex items-center gap-3">
                            <div className="w-10 h-10 bg-gradient-to-br from-blue-600 to-purple-600 rounded-full flex items-center justify-center font-bold text-sm">
                              {user.fullName?.charAt(0)?.toUpperCase() || 'U'}
                            </div>
                            <div>
                              <p className="font-medium">{user.fullName}</p>
                              <p className="text-sm text-zinc-400">{user.email}</p>
                            </div>
                          </div>
                        </td>
                        <td className="px-4 py-3">
                          <Badge variant={user.role?.toLowerCase()}>{user.role}</Badge>
                        </td>
                        <td className="px-4 py-3">
                          <Badge variant="gold">Level {user.level}</Badge>
                        </td>
                        <td className="px-4 py-3">
                          <Badge variant={user.isActive ? 'active' : 'inactive'}>
                            {user.isActive ? 'Hoạt động' : 'Khóa'}
                          </Badge>
                        </td>
                        <td className="px-4 py-3">
                          <div className="flex justify-end gap-2">
                            <button
                              onClick={() => openEditModal(user)}
                              disabled={actionLoading === user.id}
                              className="p-2 hover:bg-zinc-700 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                              title="Chỉnh sửa"
                            >
                              <Edit2 className="w-4 h-4" />
                            </button>
                            <button
                              onClick={() => handleToggleStatus(user.id, user.isActive)}
                              disabled={actionLoading === user.id}
                              className="p-2 hover:bg-zinc-700 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                              title={user.isActive ? 'Khóa' : 'Mở khóa'}
                            >
                              {actionLoading === user.id ? (
                                <RefreshCw className="w-4 h-4 animate-spin" />
                              ) : (
                                <Lock className="w-4 h-4" />
                              )}
                            </button>
                            <button
                              onClick={() => handleDeleteUser(user.id)}
                              disabled={actionLoading === user.id}
                              className="p-2 hover:bg-red-900/30 text-red-400 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                              title="Xóa"
                            >
                              <Trash2 className="w-4 h-4" />
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <Pagination
                currentPage={currentPage}
                totalPages={totalPages}
                totalItems={totalItems}
                onPageChange={setCurrentPage}
              />
            </>
          )}
        </div>
      </div>

      {/* Edit Modal */}
      <Modal isOpen={editModal.isOpen} onClose={() => setEditModal({ isOpen: false, user: null })} title="Chỉnh sửa người dùng">
        {editModal.user && (
          <div className="bg-black space-y-6">
            <div className="flex flex-col space-y-2">
              <label className="text-sm font-medium text-zinc-300">Vai trò</label>
              <select
                value={editFormData.roleId}
                onChange={(e) => setEditFormData({ ...editFormData, roleId: parseInt(e.target.value) })}
                className="bg-black border border-zinc-700 rounded-xl px-4 py-2 text-zinc-100 focus:outline-none focus:border-blue-500 transition"
              >
                {roles.map((role) => (
                  <option key={role.id} value={role.id}>
                    {role.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="flex flex-col space-y-2">
              <label className="text-sm font-medium text-zinc-300">Cấp độ</label>
              <select
                value={editFormData.levelId}
                onChange={(e) => setEditFormData({ ...editFormData, levelId: parseInt(e.target.value) })}
                className="bg-black border border-zinc-700 rounded-xl px-4 py-2 text-zinc-100 focus:outline-none focus:border-blue-500 transition"
              >
                {levels.map((level) => (
                  <option key={level.id} value={level.id}>
                    {level.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="pt-2">
              <button
                onClick={handleUpdateUser}
                disabled={actionLoading === editModal.user.id}
                className="w-full bg-blue-600 hover:bg-blue-700 px-4 py-2 rounded-xl font-semibold text-white transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
              >
                {actionLoading === editModal.user.id ? (
                  <>
                    <RefreshCw className="w-4 h-4 animate-spin" />
                    Đang lưu...
                  </>
                ) : (
                  'Lưu thay đổi'
                )}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}