import React, { useState, useEffect } from 'react';
import { Shield, User, Activity, Plus, Edit2, Trash2, Search, X, Check } from 'lucide-react';
import { adminApi } from '../../../api/admin';

interface Role {
  id: number;
  name: string;
  description?: string;
}

const Badge = ({ children, variant = 'default' }: { children: React.ReactNode; variant?: string }) => {
  const variants: Record<string, string> = {
    default: 'bg-zinc-800 text-zinc-300',
    admin: 'bg-red-900/30 text-red-400 border border-red-800',
    trader: 'bg-blue-900/30 text-blue-400 border border-blue-800',
    user: 'bg-green-900/30 text-green-400 border border-green-800',
    viewer: 'bg-purple-900/30 text-purple-400 border border-purple-800',
  };
  
  return (
    <span className={`px-2 py-1 rounded text-xs font-medium ${variants[variant] || variants.default}`}>
      {children}
    </span>
  );
};

const Modal = ({ isOpen, onClose, title, children }: { 
  isOpen: boolean; 
  onClose: () => void; 
  title: string;
  children: React.ReactNode;
}) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
      <div className="bg-black border border-zinc-700 rounded-lg w-full max-w-lg shadow-lg">
        <div className="flex items-center justify-between p-6 border-b border-zinc-800">
          <h2 className="text-xl font-semibold">{title}</h2>
          <button
            onClick={onClose}
            className="p-1 hover:bg-zinc-800 rounded transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>
        {children}
      </div>
    </div>
  );
};

export default function AdminRoles() {
  const [roles, setRoles] = useState<Role[]>([]);
  const [filteredRoles, setFilteredRoles] = useState<Role[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [selectedRole, setSelectedRole] = useState<Role | null>(null);
  const [formData, setFormData] = useState({ name: '', description: '' });
  const [processing, setProcessing] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    loadRoles();
  }, []);

  useEffect(() => {
    if (searchQuery.trim() === '') {
      setFilteredRoles(roles);
    } else {
      const query = searchQuery.toLowerCase();
      setFilteredRoles(
        roles.filter(role =>
          role.name.toLowerCase().includes(query) ||
          role.description?.toLowerCase().includes(query)
        )
      );
    }
  }, [searchQuery, roles]);

  const loadRoles = async () => {
    try {
      setLoading(true);
      const result = await adminApi.getRoles();
      if (result.ok) {
        setRoles(result.data.data.roles);
        setFilteredRoles(result.data.data.roles);
      } else {
        setError(result.error);
      }
    } catch (err: any) {
      setError(err.message || 'Không thể tải danh sách vai trò');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async () => {
    if (!formData.name.trim()) {
      setError('Tên vai trò không được để trống');
      return;
    }

    setProcessing(true);
    setError('');
    
    try {
      const result = await adminApi.createRole({
        name: formData.name.trim(),
        description: formData.description.trim() || undefined,
      });

      if (result.ok) {
        await loadRoles();
        setShowCreateModal(false);
        setFormData({ name: '', description: '' });
      } else {
        setError(result.error);
      }
    } catch (err: any) {
      setError(err.message || 'Không thể tạo vai trò');
    } finally {
      setProcessing(false);
    }
  };

  const handleEdit = async () => {
    if (!selectedRole || !formData.name.trim()) {
      setError('Tên vai trò không được để trống');
      return;
    }

    setProcessing(true);
    setError('');
    
    try {
      const result = await adminApi.updateRole(selectedRole.id, {
        name: formData.name.trim(),
        description: formData.description.trim() || undefined,
      });

      if (result.ok) {
        await loadRoles();
        setShowEditModal(false);
        setSelectedRole(null);
        setFormData({ name: '', description: '' });
      } else {
        setError(result.error);
      }
    } catch (err: any) {
      setError(err.message || 'Không thể cập nhật vai trò');
    } finally {
      setProcessing(false);
    }
  };

  const handleDelete = async () => {
    if (!selectedRole) return;

    setProcessing(true);
    setError('');
    
    try {
      const result = await adminApi.deleteRole(selectedRole.id);

      if (result.ok) {
        await loadRoles();
        setShowDeleteModal(false);
        setSelectedRole(null);
      } else {
        setError(result.error);
      }
    } catch (err: any) {
      setError(err.message || 'Không thể xóa vai trò');
    } finally {
      setProcessing(false);
    }
  };

  const openEditModal = (role: Role) => {
    setSelectedRole(role);
    setFormData({ name: role.name, description: role.description || '' });
    setError('');
    setShowEditModal(true);
  };

  const openDeleteModal = (role: Role) => {
    setSelectedRole(role);
    setError('');
    setShowDeleteModal(true);
  };

  const openCreateModal = () => {
    setFormData({ name: '', description: '' });
    setError('');
    setShowCreateModal(true);
  };

  const getRoleIcon = (roleName: string) => {
    if (roleName.toLowerCase() === 'admin') return Shield;
    if (roleName.toLowerCase() === 'trader') return User;
    return Activity;
  };

  const getRoleDescription = (role: Role) => {
    if (role.description) return role.description;
    
    const descriptions: Record<string, string> = {
      'admin': 'Toàn quyền quản trị hệ thống, quản lý người dùng và cấu hình',
      'trader': 'Thực hiện giao dịch, quản lý danh mục đầu tư cá nhân',
      'user': 'Xem thông tin, theo dõi thị trường',
      'viewer': 'Chỉ xem, không có quyền thao tác'
    };
    return descriptions[role.name.toLowerCase()] || 'Vai trò hệ thống';
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-zinc-950 flex items-center justify-center">
        <div className="text-zinc-400">Đang tải...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-zinc-950 text-zinc-100">
      <div className="max-w-7xl mx-auto p-6">
        <div className="mb-8">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h1 className="text-3xl font-bold mb-2 flex items-center gap-3">
                <Shield className="w-8 h-8 text-blue-400" />
                Quản lý vai trò
              </h1>
              <p className="text-zinc-400">Danh sách vai trò và quyền hạn trong hệ thống</p>
            </div>
            <button
              onClick={openCreateModal}
              className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors"
            >
              <Plus className="w-5 h-5" />
              Tạo vai trò mới
            </button>
          </div>

          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-zinc-500" />
            <input
              type="text"
              placeholder="Tìm kiếm vai trò..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-10 pr-4 py-3 bg-zinc-900 border border-zinc-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
          <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
            <div className="text-zinc-400 text-sm mb-1">Tổng vai trò</div>
            <div className="text-3xl font-bold">{roles.length}</div>
          </div>
          <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
            <div className="text-zinc-400 text-sm mb-1">Vai trò hệ thống</div>
            <div className="text-3xl font-bold text-blue-400">
              {roles.filter(r => ['admin', 'trader', 'user'].includes(r.name.toLowerCase())).length}
            </div>
          </div>
          <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
            <div className="text-zinc-400 text-sm mb-1">Vai trò tùy chỉnh</div>
            <div className="text-3xl font-bold text-green-400">
              {roles.filter(r => !['admin', 'trader', 'user'].includes(r.name.toLowerCase())).length}
            </div>
          </div>
        </div>

        {filteredRoles.length === 0 ? (
          <div className="text-center py-12 text-zinc-500">
            {searchQuery ? 'Không tìm thấy vai trò phù hợp' : 'Chưa có vai trò nào'}
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {filteredRoles.map(role => {
              const Icon = getRoleIcon(role.name);
              return (
                <div key={role.id} className="bg-zinc-900 border border-zinc-800 rounded-lg p-6 hover:border-zinc-700 transition-all group">
                  <div className="flex items-start justify-between mb-4">
                    <div className="flex items-start gap-4">
                      <div className="p-3 bg-zinc-800 rounded-lg group-hover:bg-zinc-700 transition-colors">
                        <Icon className="w-6 h-6 text-blue-400" />
                      </div>
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-2">
                          <h3 className="text-lg font-semibold">{role.name}</h3>
                          <Badge variant={role.name.toLowerCase()}>{role.name}</Badge>
                        </div>
                        <p className="text-zinc-400 text-sm line-clamp-2">{getRoleDescription(role)}</p>
                      </div>
                    </div>
                  </div>
                  
                  <div className="flex gap-2 pt-4 border-t border-zinc-800">
                    <button
                      onClick={() => openEditModal(role)}
                      className="flex-1 flex items-center justify-center gap-2 px-3 py-2 bg-zinc-800 hover:bg-zinc-700 rounded transition-colors text-sm"
                    >
                      <Edit2 className="w-4 h-4" />
                      Chỉnh sửa
                    </button>
                    <button
                      onClick={() => openDeleteModal(role)}
                      className="flex-1 flex items-center justify-center gap-2 px-3 py-2 bg-red-900/20 hover:bg-red-900/30 text-red-400 rounded transition-colors text-sm"
                    >
                      <Trash2 className="w-4 h-4" />
                      Xóa
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="Tạo vai trò mới">
        <div className="p-6">
          {error && (
            <div className="mb-4 p-3 bg-red-900/20 border border-red-800 rounded text-red-400 text-sm">
              {error}
            </div>
          )}
          
          <div className="mb-4">
            <label className="block text-sm font-medium mb-2">Tên vai trò</label>
            <input
              type="text"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              className="w-full px-3 py-2 bg-zinc-800 border border-zinc-700 rounded focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="VD: Manager, Editor..."
            />
          </div>

          <div className="mb-6">
            <label className="block text-sm font-medium mb-2">Mô tả (tùy chọn)</label>
            <textarea
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              className="w-full px-3 py-2 bg-zinc-800 border border-zinc-700 rounded focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
              rows={3}
              placeholder="Mô tả quyền hạn và trách nhiệm..."
            />
          </div>

          <div className="flex gap-3">
            <button
              onClick={() => setShowCreateModal(false)}
              className="flex-1 px-4 py-2 bg-zinc-800 hover:bg-zinc-700 rounded transition-colors"
              disabled={processing}
            >
              Hủy
            </button>
            <button
              onClick={handleCreate}
              className="flex-1 px-4 py-2 bg-blue-600 hover:bg-blue-700 rounded transition-colors flex items-center justify-center gap-2"
              disabled={processing}
            >
              {processing ? (
                'Đang tạo...'
              ) : (
                <>
                  <Check className="w-4 h-4" />
                  Tạo vai trò
                </>
              )}
            </button>
          </div>
        </div>
      </Modal>

      <Modal isOpen={showEditModal} onClose={() => setShowEditModal(false)} title="Chỉnh sửa vai trò">
        <div className="p-6">
          {error && (
            <div className="mb-4 p-3 bg-red-900/20 border border-red-800 rounded text-red-400 text-sm">
              {error}
            </div>
          )}
          
          <div className="mb-4">
            <label className="block text-sm font-medium mb-2">Tên vai trò</label>
            <input
              type="text"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              className="w-full px-3 py-2 bg-zinc-800 border border-zinc-700 rounded focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div className="mb-6">
            <label className="block text-sm font-medium mb-2">Mô tả</label>
            <textarea
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              className="w-full px-3 py-2 bg-zinc-800 border border-zinc-700 rounded focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
              rows={3}
            />
          </div>

          <div className="flex gap-3">
            <button
              onClick={() => setShowEditModal(false)}
              className="flex-1 px-4 py-2 bg-zinc-800 hover:bg-zinc-700 rounded transition-colors"
              disabled={processing}
            >
              Hủy
            </button>
            <button
              onClick={handleEdit}
              className="flex-1 px-4 py-2 bg-blue-600 hover:bg-blue-700 rounded transition-colors flex items-center justify-center gap-2"
              disabled={processing}
            >
              {processing ? (
                'Đang lưu...'
              ) : (
                <>
                  <Check className="w-4 h-4" />
                  Lưu thay đổi
                </>
              )}
            </button>
          </div>
        </div>
      </Modal>

      <Modal isOpen={showDeleteModal} onClose={() => setShowDeleteModal(false)} title="Xác nhận xóa">
        <div className="p-6">
          {error && (
            <div className="mb-4 p-3 bg-red-900/20 border border-red-800 rounded text-red-400 text-sm">
              {error}
            </div>
          )}
          
          <p className="text-zinc-400 mb-6">
            Bạn có chắc chắn muốn xóa vai trò <span className="font-semibold text-zinc-200">{selectedRole?.name}</span>? 
            Hành động này không thể hoàn tác.
          </p>

          <div className="flex gap-3">
            <button
              onClick={() => setShowDeleteModal(false)}
              className="flex-1 px-4 py-2 bg-zinc-800 hover:bg-zinc-700 rounded transition-colors"
              disabled={processing}
            >
              Hủy
            </button>
            <button
              onClick={handleDelete}
              className="flex-1 px-4 py-2 bg-red-600 hover:bg-red-700 rounded transition-colors"
              disabled={processing}
            >
              {processing ? 'Đang xóa...' : 'Xóa vai trò'}
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
}