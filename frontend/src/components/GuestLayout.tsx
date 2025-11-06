import { Menu, X } from 'lucide-react';
import { Button } from './ui/button';
import { useState } from 'react';

interface GuestLayoutProps {
  children: React.ReactNode;
  currentPage?: string;
  onNavigate?: (page: string) => void;
}

export default function GuestLayout({ children, currentPage, onNavigate }: GuestLayoutProps) {
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const navItems = [
    { id: 'home', label: 'Home' },
    { id: 'markets', label: 'Markets' },
  ];

  return (
    <div className="min-h-screen bg-black text-white">
      {/* Navigation Bar */}
      <nav className="border-b border-gray-800 bg-black sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16">
            {/* Logo */}
            <div className="flex items-center">
              <button 
                onClick={() => onNavigate?.('home')}
                className="flex items-center gap-2"
              >
                <img 
                  src="/logo.png" 
                  alt="CryptoTrade Logo" 
                  className="w-12 h-12 object-contain"
                />
                <span className="text-xl">CryptoTrade</span>
              </button>
            </div>

            {/* Desktop Navigation */}
            <div className="hidden md:flex items-center gap-6">
              {navItems.map((item) => (
                <button
                  key={item.id}
                  onClick={() => onNavigate?.(item.id)}
                  className={`px-3 py-2 rounded-lg transition-colors ${
                    currentPage === item.id
                      ? 'text-emerald-500 bg-emerald-500/10'
                      : 'text-gray-300 hover:text-white hover:bg-gray-900'
                  }`}
                >
                  {item.label}
                </button>
              ))}
            </div>

            {/* Auth Buttons */}
            <div className="hidden md:flex items-center gap-3">
              <Button
                variant="ghost"
                onClick={() => onNavigate?.('login')}
                className="text-gray-300 hover:text-white hover:bg-gray-900"
              >
                Login
              </Button>
              <Button
                onClick={() => onNavigate?.('register')}
                className="bg-emerald-500 text-black hover:bg-emerald-600"
              >
                Sign Up
              </Button>
            </div>

            {/* Mobile Menu Button */}
            <button
              className="md:hidden p-2 rounded-lg hover:bg-gray-900"
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            >
              {mobileMenuOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
            </button>
          </div>
        </div>

        {/* Mobile Menu */}
        {mobileMenuOpen && (
          <div className="md:hidden border-t border-gray-800 bg-black">
            <div className="px-4 py-4 space-y-2">
              {navItems.map((item) => (
                <button
                  key={item.id}
                  onClick={() => {
                    onNavigate?.(item.id);
                    setMobileMenuOpen(false);
                  }}
                  className={`w-full text-left px-4 py-2 rounded-lg transition-colors ${
                    currentPage === item.id
                      ? 'text-emerald-500 bg-emerald-500/10'
                      : 'text-gray-300 hover:text-white hover:bg-gray-900'
                  }`}
                >
                  {item.label}
                </button>
              ))}
              <div className="pt-4 border-t border-gray-800 space-y-2">
                <Button
                  variant="outline"
                  className="w-full"
                  onClick={() => {
                    onNavigate?.('login');
                    setMobileMenuOpen(false);
                  }}
                >
                  Login
                </Button>
                <Button
                  className="w-full bg-emerald-500 text-black hover:bg-emerald-600"
                  onClick={() => {
                    onNavigate?.('register');
                    setMobileMenuOpen(false);
                  }}
                >
                  Sign Up
                </Button>
              </div>
            </div>
          </div>
        )}
      </nav>

      {/* Content */}
      <main>{children}</main>

      {/* Footer */}
      <footer className="border-t border-gray-800 bg-black mt-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
            <div>
              <div className="flex items-center gap-2 mb-4">
                <img 
                  src="/logo.png" 
                  alt="CryptoTrade Logo" 
                  className="w-12 h-12 object-contain"
                />
                <span className="text-xl">CryptoTrade</span>
              </div>
              <p className="text-gray-400 text-sm">
                Your trusted cryptocurrency trading platform
              </p>
            </div>
            <div>
              <h3 className="mb-4">Platform</h3>
              <ul className="space-y-2 text-sm text-gray-400">
                <li><button onClick={() => onNavigate?.('markets')} className="hover:text-emerald-500">Markets</button></li>
                <li><a href="#" className="hover:text-emerald-500">Trading</a></li>
                <li><a href="#" className="hover:text-emerald-500">Staking</a></li>
              </ul>
            </div>
            <div>
              <h3 className="mb-4">Support</h3>
              <ul className="space-y-2 text-sm text-gray-400">
                <li><a href="#" className="hover:text-emerald-500">Help Center</a></li>
                <li><a href="#" className="hover:text-emerald-500">API Docs</a></li>
                <li><a href="#" className="hover:text-emerald-500">Contact Us</a></li>
              </ul>
            </div>
            <div>
              <h3 className="mb-4">Legal</h3>
              <ul className="space-y-2 text-sm text-gray-400">
                <li><a href="#" className="hover:text-emerald-500">Terms of Service</a></li>
                <li><a href="#" className="hover:text-emerald-500">Privacy Policy</a></li>
                <li><a href="#" className="hover:text-emerald-500">Cookie Policy</a></li>
              </ul>
            </div>
          </div>
          <div className="mt-8 pt-8 border-t border-gray-800 text-center text-sm text-gray-400">
            <p>&copy; 2025 CryptoTrade. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  );
}
