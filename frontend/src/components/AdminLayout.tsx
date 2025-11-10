import React from 'react';
import { Users, Shield, Award, LogOut } from 'lucide-react';

interface AdminLayoutProps {
  currentPage: string;
  onNavigate: (page: string) => void;
  children: React.ReactNode;
}

export default function AdminLayout({ currentPage, onNavigate, children }: AdminLayoutProps) {
  const handleLogout = () => {
    localStorage.removeItem('token');
    onNavigate('login');
  };

  return (
    <div className="min-h-screen bg-black text-white">
      <nav className="bg-zinc-900 border-b border-zinc-800 px-6 py-4">
        <div className="max-w-7xl mx-auto flex justify-between items-center">
          <h1 className="text-xl font-bold">Admin Panel</h1>
          <button
            onClick={handleLogout}
            className="flex items-center gap-2 px-4 py-2 bg-zinc-800 hover:bg-zinc-700 rounded-lg text-sm transition-colors"
          >
            <LogOut className="w-4 h-4" />
            Đăng xuất
          </button>
        </div>
      </nav>

      <div className="flex">
        <aside className="w-64 bg-zinc-900 border-r border-zinc-800 min-h-[calc(100vh-73px)] p-4">
          <div className="space-y-1">
            <button
              onClick={() => onNavigate('admin-users')}
              className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${
                currentPage === 'admin-users' ? 'bg-blue-900/30 text-blue-400' : 'text-zinc-400 hover:bg-zinc-800'
              }`}
            >
              <Users className="w-5 h-5" />
              Quản lý Users
            </button>
            <button
              onClick={() => onNavigate('admin-roles')}
              className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${
                currentPage === 'admin-roles' ? 'bg-blue-900/30 text-blue-400' : 'text-zinc-400 hover:bg-zinc-800'
              }`}
            >
              <Shield className="w-5 h-5" />
              Quản lý Roles
            </button>
            <button
              onClick={() => onNavigate('admin-levels')}
              className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${
                currentPage === 'admin-levels' ? 'bg-blue-900/30 text-blue-400' : 'text-zinc-400 hover:bg-zinc-800'
              }`}
            >
              <Award className="w-5 h-5" />
              Quản lý Levels
            </button>
          </div>
        </aside>

        <main className="flex-1">{children}</main>
      </div>
    </div>
  );
}