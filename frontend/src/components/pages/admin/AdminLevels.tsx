import React, { useState, useEffect } from 'react';
import { Award, Plus, Edit2, Trash2, X, Check, Search, TrendingUp } from 'lucide-react';
import { adminApi } from '../../../api/admin';

interface Level {
  id: number;
  name: string;
  number: number;
  description?: string;
  minBalance?: number;
  maxBalance?: number;
}

const Badge = ({ children, variant = 'default' }: { children: React.ReactNode; variant?: string }) => {
  const variants: Record<string, string> = {
    default: 'bg-zinc-800 text-zinc-300',
    bronze: 'bg-orange-900/30 text-orange-400 border border-orange-800',
    silver: 'bg-slate-700/30 text-slate-300 border border-slate-600',
    gold: 'bg-yellow-900/30 text-yellow-400 border border-yellow-800',
    platinum: 'bg-cyan-900/30 text-cyan-400 border border-cyan-800',
    diamond: 'bg-purple-900/30 text-purple-400 border border-purple-800',
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
  children: React.ReactNode 
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

export default function AdminLevels() {
  const [levels, setLevels] = useState<Level[]>([]);
  const [filteredLevels, setFilteredLevels] = useState<Level[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [selectedLevel, setSelectedLevel] = useState<Level | null>(null);
  const [formData, setFormData] = useState({
    name: '',
    number: '',
    description: '',
    minBalance: '',
    maxBalance: '',
  });
  const [processing, setProcessing] = useState(false);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  useEffect(() => {
    loadLevels();
  }, []);

  useEffect(() => {
    if (searchQuery.trim() === '') {
      setFilteredLevels(levels);
    } else {
      const query = searchQuery.toLowerCase();
      setFilteredLevels(
        levels.filter(level =>
          level.name.toLowerCase().includes(query) ||
          level.description?.toLowerCase().includes(query) ||
          level.number.toString().includes(query)
        )
      );
    }
  }, [searchQuery, levels]);

  useEffect(() => {
    if (successMessage) {
      const timer = setTimeout(() => setSuccessMessage(''), 3000);
      return () => clearTimeout(timer);
    }
  }, [successMessage]);

  const loadLevels = async () => {
    try {
      setLoading(true);
      const result = await adminApi.getLevels();
      if (result.ok) {
        const sortedLevels = result.data.data.levels.sort((a, b) => a.number - b.number);
        setLevels(sortedLevels);
        setFilteredLevels(sortedLevels);
      } else {
        setError(result.error);
      }
    } catch (err: any) {
      setError(err.message || 'Không thể tải danh sách cấp độ');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async () => {
    if (!formData.name.trim()) {
      setError('Tên cấp độ không được để trống');
      return;
    }

    setProcessing(true);
    setError('');
    
    try {
      const payload: any = {
      name: formData.name.trim(),
      number: parseInt(formData.number) || 1,
      description: formData.description.trim() || undefined,
      minBalance: formData.minBalance ? parseFloat(formData.minBalance) : undefined,
      maxBalance: formData.maxBalance ? parseFloat(formData.maxBalance) : undefined,
    };

      if (formData.minBalance) {
        payload.minBalance = parseFloat(formData.minBalance);
      }
      if (formData.maxBalance) {
        payload.maxBalance = parseFloat(formData.maxBalance);
      }

      const result = await adminApi.createLevel(payload);

      if (result.ok) {
      await loadLevels();
      setShowCreateModal(false);
      setFormData({ name: '', number: '', description: '', minBalance: '', maxBalance: '' });
      setSuccessMessage('Tạo cấp độ thành công!');
    } else {
      setError(result.error);
    }
    } catch (err: any) {
      setError(err.message || 'Không thể tạo cấp độ');
    } finally {
      setProcessing(false);
    }
  };

  const handleEdit = async () => {
    if (!selectedLevel || !formData.name.trim()) {
      setError('Tên cấp độ không được để trống');
      return;
    }

    setProcessing(true);
    setError('');
    
    try {
      const payload: any = {
      name: formData.name.trim(),
      number: parseInt(formData.number) || 1,
      description: formData.description.trim() || undefined,
    };

    // Gửi cả 0, không bị loại bỏ
    if (formData.minBalance !== '') {
      payload.minBalance = parseFloat(formData.minBalance);
    }
    if (formData.maxBalance !== '') {
      payload.maxBalance = parseFloat(formData.maxBalance);
    }
      const result = await adminApi.updateLevel(selectedLevel.id, payload);

      if (result.ok) {
      await loadLevels();
      setShowEditModal(false);
      setSelectedLevel(null);
      setFormData({ name: '', number: '', description: '', minBalance: '', maxBalance: '' });
      setSuccessMessage('Cập nhật cấp độ thành công!');
    } else {
      setError(result.error);
    }
    } catch (err: any) {
      setError(err.message || 'Không thể cập nhật cấp độ');
    } finally {
      setProcessing(false);
    }
  };

  const handleDelete = async () => {
    if (!selectedLevel) return;

    setProcessing(true);
    setError('');
    
    try {
      const result = await adminApi.deleteLevel(selectedLevel.id);

      if (result.ok) {
        await loadLevels();
        setShowDeleteModal(false);
        setSelectedLevel(null);
        setSuccessMessage('Xóa cấp độ thành công!');
      } else {
        setError(result.error);
      }
    } catch (err: any) {
      setError(err.message || 'Không thể xóa cấp độ');
    } finally {
      setProcessing(false);
    }
  };

  const openEditModal = (level: Level) => {
    setSelectedLevel(level);
    setFormData({
      name: level.name,
      number: level.number.toString(),
      description: level.description || '',
      minBalance: level.minBalance?.toString() || '',
      maxBalance: level.maxBalance?.toString() || '',
    });
    setError('');
    setShowEditModal(true);
  };

  const openDeleteModal = (level: Level) => {
    setSelectedLevel(level);
    setError('');
    setShowDeleteModal(true);
  };

  const openCreateModal = () => {
    setFormData({ name: '', number: '', description: '', minBalance: '', maxBalance: '' });
    setError('');
    setShowCreateModal(true);
  };

  const getLevelVariant = (number: number) => {
    if (number <= 2) return 'bronze';
    if (number <= 4) return 'silver';
    if (number <= 6) return 'gold';
    if (number <= 8) return 'platinum';
    return 'diamond';
  };

  const formatCurrency = (amount?: number) => {
    if (!amount) return 'N/A';
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(amount);
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
        {successMessage && (
          <div className="fixed top-4 right-4 bg-green-900/90 border border-green-700 text-green-100 px-4 py-3 rounded-lg shadow-lg z-50 flex items-center gap-2">
            <Check className="w-5 h-5" />
            {successMessage}
            <button onClick={() => setSuccessMessage('')} className="ml-2 hover:opacity-70">
              <X className="w-4 h-4" />
            </button>
          </div>
        )}

        <div className="mb-8">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h1 className="text-3xl font-bold mb-2 flex items-center gap-3">
                <Award className="w-8 h-8 text-yellow-400" />
                Quản lý cấp độ
              </h1>
              <p className="text-zinc-400">Cấu hình cấp bậc và phân loại người dùng</p>
            </div>
            <button
              onClick={openCreateModal}
              className="flex items-center gap-2 px-4 py-2 bg-yellow-600 hover:bg-yellow-700 rounded-lg transition-colors"
            >
              <Plus className="w-5 h-5" />
              Tạo cấp độ mới
            </button>
          </div>

          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-zinc-500" />
            <input
              type="text"
              placeholder="Tìm kiếm cấp độ..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-10 pr-4 py-3 bg-zinc-900 border border-zinc-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-yellow-500 focus:border-transparent"
            />
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
          <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
            <div className="text-zinc-400 text-sm mb-1">Tổng cấp độ</div>
            <div className="text-3xl font-bold">{levels.length}</div>
          </div>
          <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
            <div className="text-zinc-400 text-sm mb-1">Cấp cao nhất</div>
            <div className="text-3xl font-bold text-yellow-400">
              {levels.length > 0 ? Math.max(...levels.map(l => l.number)) : 0}
            </div>
          </div>
          <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
            <div className="text-zinc-400 text-sm mb-1">Có giới hạn số dư</div>
            <div className="text-3xl font-bold text-green-400">
              {levels.filter(l => l.minBalance || l.maxBalance).length}
            </div>
          </div>
        </div>

        {filteredLevels.length === 0 ? (
          <div className="text-center py-12 text-zinc-500">
            {searchQuery ? 'Không tìm thấy cấp độ phù hợp' : 'Chưa có cấp độ nào'}
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {filteredLevels.map(level => (
              <div key={level.id} className="bg-zinc-900 border border-zinc-800 rounded-lg p-6 hover:border-zinc-700 transition-all group">
                <div className="flex items-start justify-between mb-4">
                  <div className="flex items-start gap-3">
                    <div className="p-3 bg-zinc-800 rounded-lg group-hover:bg-zinc-700 transition-colors">
                      <TrendingUp className="w-6 h-6 text-yellow-400" />
                    </div>
                    <div className="flex-1">
                      <Badge variant={getLevelVariant(level.number)}>Level {level.number}</Badge>
                      <h3 className="text-lg font-semibold mt-2">{level.name}</h3>
                    </div>
                  </div>
                </div>
                
                <p className="text-zinc-400 text-sm mb-4 line-clamp-2">
                  {level.description || 'Không có mô tả'}
                </p>

                {(level.minBalance || level.maxBalance) && (
                  <div className="mb-4 p-3 bg-zinc-800 rounded border border-zinc-700">
                  <div className="text-xs text-zinc-500 mb-1">Giới hạn số dư</div>
                  <div className="text-sm text-zinc-300">
                    {level.minBalance
                      ? formatCurrency(level.minBalance)
                      : 'Không giới hạn'} 
                    {' - '}
                    {level.maxBalance
                      ? formatCurrency(level.maxBalance)
                      : 'Không giới hạn'}
                  </div>
                </div>
                )}
                
                <div className="flex gap-2 pt-4 border-t border-zinc-800">
                  <button
                    onClick={() => openEditModal(level)}
                    className="flex-1 flex items-center justify-center gap-2 px-3 py-2 bg-zinc-800 hover:bg-zinc-700 rounded transition-colors text-sm"
                  >
                    <Edit2 className="w-4 h-4" />
                    Chỉnh sửa
                  </button>
                  <button
                    onClick={() => openDeleteModal(level)}
                    className="flex-1 flex items-center justify-center gap-2 px-3 py-2 bg-red-900/20 hover:bg-red-900/30 text-red-400 rounded transition-colors text-sm"
                  >
                    <Trash2 className="w-4 h-4" />
                    Xóa
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="Tạo cấp độ mới">
        <div className="p-6">
          {error && (
            <div className="mb-4 p-3 bg-red-900/20 border border-red-800 rounded text-red-400 text-sm">
              {error}
            </div>
          )}
          
          <div className="space-y-4">
  <div>
    <label className="block text-sm font-medium mb-2">Tên cấp độ</label>
    <input
      type="text"
      value={formData.name}
      onChange={(e) => setFormData({ ...formData, name: e.target.value })}
      className="w-full px-3 py-2 bg-zinc-800 border border-zinc-700 rounded focus:outline-none focus:ring-2 focus:ring-yellow-500"
    />
  </div>

  <div>
    <label className="block text-sm font-medium mb-2">Thứ tự cấp độ</label>
    <input
      type="number"
      value={formData.number}
      onChange={(e) => setFormData({ ...formData, number: e.target.value })}
      className="w-full px-3 py-2 bg-zinc-800 border border-zinc-700 rounded focus:outline-none focus:ring-2 focus:ring-yellow-500"
      min="1"
    />
  </div>

  <div>
    <label className="block text-sm font-medium mb-2">Mô tả</label>
    <textarea
      value={formData.description}
      onChange={(e) => setFormData({ ...formData, description: e.target.value })}
      className="w-full px-3 py-2 bg-zinc-800 border border-zinc-700 rounded focus:outline-none focus:ring-2 focus:ring-yellow-500 resize-none"
      rows={3}
    />
  </div>

  <div className="grid grid-cols-2 gap-4">
    <div>
      <label className="block text-sm font-medium mb-2">Số dư tối thiểu</label>
      <input
        type="number"
        value={formData.minBalance}
        onChange={(e) => setFormData({ ...formData, minBalance: e.target.value })}
        className="w-full px-3 py-2 bg-zinc-800 border border-zinc-700 rounded focus:outline-none focus:ring-2 focus:ring-yellow-500"
        min="0"
      />
    </div>
    <div>
      <label className="block text-sm font-medium mb-2">Số dư tối đa</label>
      <input
        type="number"
        value={formData.maxBalance}
        onChange={(e) => setFormData({ ...formData, maxBalance: e.target.value })}
        className="w-full px-3 py-2 bg-zinc-800 border border-zinc-700 rounded focus:outline-none focus:ring-2 focus:ring-yellow-500"
        min="0"
      />
    </div>
  </div>
</div>
          <div className="flex gap-3 mt-6">
            <button
              onClick={() => setShowCreateModal(false)}
              className="flex-1 px-4 py-2 bg-zinc-800 hover:bg-zinc-700 rounded transition-colors"
              disabled={processing}
            >
              Hủy
            </button>
            <button
              onClick={handleCreate}
              className="flex-1 px-4 py-2 bg-yellow-600 hover:bg-yellow-700 rounded transition-colors flex items-center justify-center gap-2"
              disabled={processing}
            >
              {processing ? (
                'Đang tạo...'
              ) : (
                <>
                  <Check className="w-4 h-4" />
                  Tạo cấp độ
                </>
              )}
            </button>
          </div>
        </div>
      </Modal>

      <Modal isOpen={showEditModal} onClose={() => setShowEditModal(false)} title="Chỉnh sửa cấp độ">
        <div className="p-6">
          {error && (
            <div className="mb-4 p-3 bg-red-900/20 border border-red-800 rounded text-red-400 text-sm">
              {error}
            </div>
          )}
          
          <div className="space-y-4">
  <div>
    <label className="block text-sm font-medium mb-2">Tên cấp độ</label>
    <input
      type="text"
      value={formData.name}
      onChange={(e) => setFormData({ ...formData, name: e.target.value })}
      className="w-full px-3 py-2 bg-zinc-800 border border-zinc-700 rounded focus:outline-none focus:ring-2 focus:ring-yellow-500"
    />
  </div>

  <div>
    <label className="block text-sm font-medium mb-2">Thứ tự cấp độ</label>
    <input
      type="number"
      value={formData.number}
      onChange={(e) => setFormData({ ...formData, number: e.target.value })}
      className="w-full px-3 py-2 bg-zinc-800 border border-zinc-700 rounded focus:outline-none focus:ring-2 focus:ring-yellow-500"
      min="1"
    />
  </div>

  <div>
    <label className="block text-sm font-medium mb-2">Mô tả</label>
    <textarea
      value={formData.description}
      onChange={(e) => setFormData({ ...formData, description: e.target.value })}
      className="w-full px-3 py-2 bg-zinc-800 border border-zinc-700 rounded focus:outline-none focus:ring-2 focus:ring-yellow-500 resize-none"
      rows={3}
    />
  </div>

  <div className="grid grid-cols-2 gap-4">
    <div>
      <label className="block text-sm font-medium mb-2">Số dư tối thiểu</label>
      <input
        type="number"
        value={formData.minBalance}
        onChange={(e) => setFormData({ ...formData, minBalance: e.target.value })}
        className="w-full px-3 py-2 bg-zinc-800 border border-zinc-700 rounded focus:outline-none focus:ring-2 focus:ring-yellow-500"
        min="0"
      />
    </div>
    <div>
      <label className="block text-sm font-medium mb-2">Số dư tối đa</label>
      <input
        type="number"
        value={formData.maxBalance}
        onChange={(e) => setFormData({ ...formData, maxBalance: e.target.value })}
        className="w-full px-3 py-2 bg-zinc-800 border border-zinc-700 rounded focus:outline-none focus:ring-2 focus:ring-yellow-500"
        min="0"
      />
    </div>
  </div>
</div>

          <div className="flex gap-3 mt-6">
            <button
              onClick={() => setShowEditModal(false)}
              className="flex-1 px-4 py-2 bg-zinc-800 hover:bg-zinc-700 rounded transition-colors"
              disabled={processing}
            >
              Hủy
            </button>
            <button
              onClick={handleEdit}
              className="flex-1 px-4 py-2 bg-yellow-600 hover:bg-yellow-700 rounded transition-colors flex items-center justify-center gap-2"
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
            Bạn có chắc chắn muốn xóa cấp độ <span className="font-semibold text-zinc-200">{selectedLevel?.name}</span>? 
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
              {processing ? 'Đang xóa...' : 'Xóa cấp độ'}
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
}