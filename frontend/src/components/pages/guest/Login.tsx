import { useState } from 'react';
import { Mail, Lock, Eye, EyeOff, AlertCircle } from 'lucide-react';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Label } from '../../ui/label';
import { Card } from '../../ui/card';
import { Alert, AlertDescription } from '../../ui/alert';
import { Checkbox } from '../../ui/checkbox';
import { AuthApi } from '../../../api/auth';
import { setAccessToken } from '../../../api/http';
import { GoogleLogin } from '@react-oauth/google';

interface LoginProps {
  onNavigate?: (page: string) => void;
}


export default function Login({ onNavigate }: LoginProps) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [rememberMe, setRememberMe] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [twoFARequired, setTwoFARequired] = useState(false);
  const [twoFACode, setTwoFACode] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      console.log('[Login] Attempting login for:', email);
      const res = await AuthApi.login({ email, password });
      
      if (!res.ok) {
        console.error('[Login] Login failed:', res.error);
        setError(res.error || 'Login failed');
        setLoading(false);
        return;
      }

      if (res.data.requiresTwoFactor) {
        console.log('[Login] 2FA required, showing 2FA form');
        setTwoFARequired(true);
        setLoading(false);
        return;
      }

      if (!res.data.accessToken) {
        console.error('[Login] No access token in response');
        setError('Login failed: No access token received');
        setLoading(false);
        return;
      }

      console.log('[Login] Login successful, setting access token');
      setAccessToken(res.data.accessToken, res.data.user || null);

      setAccessToken(res.data.accessToken, res.data.user || null, res.data.refreshToken || null);
      onNavigate?.('trader-dashboard');
    } catch (err: any) {
      console.error('[Login] Unexpected error:', err);
      setError(err?.message || 'An unexpected error occurred');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit2FA = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    if (!twoFACode.trim()) {
      setError('Please enter the 2FA code');
      setLoading(false);
      return;
    }

    try {
      console.log('[Login] Verifying 2FA code for:', email);
      const res = await AuthApi.login2fa({ email, code: twoFACode });
      
      if (!res.ok) {
        console.error('[Login] 2FA verification failed:', res.error);
        setError(res.error || 'Invalid 2FA code');
        setLoading(false);
        return;
      }

      if (!res.data.accessToken) {
        console.error('[Login] No access token after 2FA verification');
        setError('Login failed: No access token received');
        setLoading(false);
        return;
      }

      console.log('[Login] 2FA verification successful');
      setAccessToken(res.data.accessToken, res.data.user || null);
      onNavigate?.('trader-dashboard');
    } catch (err: any) {
      console.error('[Login] Unexpected error during 2FA:', err);
      setError(err?.message || 'An unexpected error occurred');
    } finally {
      setLoading(false);
    }
  };

  // === SOCIAL LOGIN HANDLERS ===
  const handleGoogleLoginSuccess = async (credentialResponse: any) => {
    setLoading(true);
    setError('');
    
    const idToken = credentialResponse.credential;
    if (!idToken) {
      setError("Google login failed: No ID token received.");
      setLoading(false);
      return;
    }

    try {
      const res = await fetch('/api/auth/google', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ token: idToken })
      });

      const data = await res.json();

      if (!res.ok) {
        throw new Error(data.message || "Google login failed on server.");
      }

      setAccessToken(data.accessToken, data.user);


      onNavigate?.('trader-dashboard');
    } catch (err: any) {
        console.error('[Login] Google login error:', err);
        setError(err.message || "An error occurred during Google login.");
    } finally {
        setLoading(false);
    }
  };

  const handleGoogleLoginError = () => {
    setError("Google login failed. Please try again.");
  };


  return (
    <div className="min-h-screen bg-black text-white flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <div className="inline-flex items-center gap-2 mb-4">
            <img 
              src="/logo.png" 
              alt="CryptoTrade Logo" 
              className="w-20 h-20 object-contain"
            />
          </div>
          <h1 className="text-3xl mb-2">Welcome Back</h1>
          <p className="text-gray-400">Login to your CryptoTrade account</p>
        </div>

        <Card className="bg-gray-900 border-gray-800 p-8">
          <form onSubmit={twoFARequired ? handleSubmit2FA : handleSubmit} className="space-y-6">
            {error && (
              <Alert className="bg-red-500/10 border-red-500/50 text-red-500">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <div className="space-y-2">
              <Label htmlFor="email">Email Address</Label>
              <div className="relative">
                <Mail className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
                <Input
                  id="email"
                  type="email"
                  placeholder="your.email@example.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="pl-10 bg-gray-800 border-gray-700 text-white"
                  required
                  disabled={twoFARequired || loading}
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <div className="relative">
                <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
                <Input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  placeholder="Enter your password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="pl-10 pr-10 bg-gray-800 border-gray-700 text-white"
                  required
                  disabled={twoFARequired || loading}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-white"
                  disabled={twoFARequired || loading}
                >
                  {showPassword ? <EyeOff className="w-5 h-5" /> : <Eye className="w-5 h-5" />}
                </button>
              </div>
            </div>

            {twoFARequired && (
              <div className="space-y-2">
                <Label htmlFor="twofa">Two-Factor Code</Label>
                <Input
                  id="twofa"
                  type="text"
                  placeholder="Enter 2FA code"
                  value={twoFACode}
                  onChange={(e) => setTwoFACode(e.target.value)}
                  className="bg-gray-800 border-gray-700 text-white"
                  required
                  disabled={loading}
                />
              </div>
            )}

            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Checkbox
                  id="remember"
                  checked={rememberMe}
                  onCheckedChange={(checked: boolean | "indeterminate") => setRememberMe(checked as boolean)}
                  disabled={twoFARequired || loading}
                />
                <Label htmlFor="remember" className="text-sm text-gray-400 cursor-pointer">
                  Remember me
                </Label>
              </div>
              <button
                type="button"
                onClick={() => onNavigate?.('forgot-password')}
                className="text-sm text-emerald-500 hover:text-emerald-400"
                disabled={loading}
              >
                Forgot password?
              </button>
            </div>

            <Button
              type="submit"
              className="w-full bg-emerald-500 text-black hover:bg-emerald-600"
              disabled={loading}
            >
              {loading ? (twoFARequired ? 'Verifying...' : 'Signing in...') : (twoFARequired ? 'Verify 2FA' : 'Sign In')}
            </Button>

            <div className="relative">
              <div className="absolute inset-0 flex items-center">
                <div className="w-full border-t border-gray-800"></div>
              </div>
              <div className="relative flex justify-center text-sm">
                <span className="px-2 bg-gray-900 text-gray-400">Or continue with</span>
              </div>
            </div>

            <div className="grid grid-cols-1 gap-4">
              <GoogleLogin
                onSuccess={handleGoogleLoginSuccess}
                onError={handleGoogleLoginError}
                theme="outline"
                size="large"
                shape="rectangular"
                width="100%"
                logo_alignment="left"
              />
              
            </div>
          </form>
        </Card>

        <div className="text-center mt-6">
          <span className="text-gray-400">Don't have an account? </span>
          <button
            onClick={() => onNavigate?.('register')}
            className="text-emerald-500 hover:text-emerald-400"
            disabled={loading}
          >
            Sign up for free
          </button>
        </div>
      </div>
    </div>
  );
}
