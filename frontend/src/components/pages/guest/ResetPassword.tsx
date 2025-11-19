import { useState, useEffect } from 'react';
import { Lock, Eye, EyeOff, AlertCircle, CheckCircle2, Loader2, ShieldCheck } from 'lucide-react';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Label } from '../../ui/label';
import { Card } from '../../ui/card';
import { Alert, AlertDescription } from '../../ui/alert';
import { AuthApi } from '../../../api/auth';

interface ResetPasswordProps {
  onNavigate?: (page: string) => void;
}

export default function ResetPassword({ onNavigate }: ResetPasswordProps) {
  const [email, setEmail] = useState('');
  const [token, setToken] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);
  const [loading, setLoading] = useState(false);

  // Auto-extract email and token from URL params
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const emailParam = params.get('email');
    const tokenParam = params.get('token');

    if (emailParam) setEmail(decodeURIComponent(emailParam));
    if (tokenParam) setToken(decodeURIComponent(tokenParam));
  }, []);

  const passwordRequirements = [
    { label: 'At least 8 characters', met: password.length >= 8 },
    { label: 'Contains uppercase letter (A-Z)', met: /[A-Z]/.test(password) },
    { label: 'Contains lowercase letter (a-z)', met: /[a-z]/.test(password) },
    { label: 'Contains number (0-9)', met: /[0-9]/.test(password) },
  ];

  const getPasswordStrength = () => {
    const metCount = passwordRequirements.filter(req => req.met).length;
    if (metCount === 0) return { label: '', color: '', width: '0%' };
    if (metCount === 1) return { label: 'Weak', color: 'bg-red-500', width: '25%' };
    if (metCount === 2) return { label: 'Fair', color: 'bg-yellow-500', width: '50%' };
    if (metCount === 3) return { label: 'Good', color: 'bg-blue-500', width: '75%' };
    return { label: 'Strong', color: 'bg-emerald-500', width: '100%' };
  };

  const passwordStrength = getPasswordStrength();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!password || !confirmPassword) {
      setError('Please fill in all fields');
      return;
    }

    if (password !== confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    if (!passwordRequirements.every(req => req.met)) {
      setError('Password does not meet requirements');
      return;
    }

    setLoading(true);
    const res = await AuthApi.resetPassword({ email, token, newPassword: password });
    if (!res.ok) {
      setError(res.error);
      setLoading(false);
      return;
    }
    setSuccess(true);
    setLoading(false);
  };

  return (
    <div className="min-h-screen bg-black text-white flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-md">
        {/* Logo & Header */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center gap-3 mb-6">
            <div className="w-12 h-12 bg-gradient-to-br from-emerald-500 to-emerald-600 rounded-xl flex items-center justify-center">
              <ShieldCheck className="w-7 h-7 text-black" />
            </div>
            <span className="text-2xl font-bold bg-gradient-to-r from-emerald-500 to-emerald-600 bg-clip-text text-transparent">
              CryptoTrade
            </span>
          </div>
          <h1 className="text-3xl font-bold mb-2">Reset Your Password</h1>
          <p className="text-gray-400 text-sm">
            {success
              ? 'Your password has been reset successfully'
              : 'Create a new secure password for your account'
            }
          </p>
        </div>

        <Card className="bg-gray-900 border-gray-800 p-8">
          {success ? (
            <div className="text-center space-y-6 animate-in fade-in duration-500">
              <div className="w-20 h-20 bg-emerald-500/10 rounded-full flex items-center justify-center mx-auto border-2 border-emerald-500/20">
                <CheckCircle2 className="w-10 h-10 text-emerald-500" />
              </div>
              <div>
                <h3 className="text-xl font-semibold mb-2">Password Reset Successfully!</h3>
                <p className="text-gray-400 text-sm">
                  Your password has been updated. You can now log in with your new password.
                </p>
              </div>
              <Button
                className="w-full bg-emerald-500 text-black hover:bg-emerald-600 font-semibold"
                onClick={() => onNavigate?.('login')}
              >
                Continue to Login →
              </Button>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-5">
              {/* Email */}
              {!email && (
                <div className="space-y-2">
                  <Label htmlFor="email" className="text-sm font-medium">Email</Label>
                  <Input
                    id="email"
                    type="email"
                    placeholder="your.email@example.com"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className="bg-gray-800 border-gray-700 text-white focus:border-emerald-500 focus:ring-emerald-500"
                    required
                  />
                </div>
              )}

              {/* Token */}
              {!token && (
                <div className="space-y-2">
                  <Label htmlFor="token" className="text-sm font-medium">Reset Token</Label>
                  <Input
                    id="token"
                    type="text"
                    placeholder="Paste your reset token here"
                    value={token}
                    onChange={(e) => setToken(e.target.value)}
                    className="bg-gray-800 border-gray-700 text-white focus:border-emerald-500 focus:ring-emerald-500 font-mono text-xs"
                    required
                  />
                </div>
              )}

              {/* Info Box if email/token are pre-filled */}
              {email && token && (
                <Alert className="bg-emerald-500/10 border-emerald-500/30 text-emerald-500">
                  <CheckCircle2 className="h-4 w-4" />
                  <AlertDescription className="text-sm">
                    Reset link verified for <strong>{email}</strong>
                  </AlertDescription>
                </Alert>
              )}

              {/* Error Alert */}
              {error && (
                <Alert className="bg-red-500/10 border-red-500/50 text-red-500">
                  <AlertCircle className="h-4 w-4" />
                  <AlertDescription className="text-sm">{error}</AlertDescription>
                </Alert>
              )}

              {/* New Password */}
              <div className="space-y-2">
                <Label htmlFor="password" className="text-sm font-medium">New Password</Label>
                <div className="relative">
                  <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                  <Input
                    id="password"
                    type={showPassword ? 'text' : 'password'}
                    placeholder="Enter new password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className="pl-10 pr-10 bg-gray-800 border-gray-700 text-white focus:border-emerald-500 focus:ring-emerald-500"
                    required
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-white transition-colors z-10 p-1"
                    aria-label="Toggle password visibility"
                  >
                    {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>

                {/* Password Strength Indicator */}
                {password && (
                  <div className="space-y-3 mt-3">
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-gray-400">Password Strength</span>
                      {passwordStrength.label && (
                        <span className={`font-medium ${
                          passwordStrength.label === 'Weak' ? 'text-red-500' :
                          passwordStrength.label === 'Fair' ? 'text-yellow-500' :
                          passwordStrength.label === 'Good' ? 'text-blue-500' :
                          'text-emerald-500'
                        }`}>
                          {passwordStrength.label}
                        </span>
                      )}
                    </div>
                    <div className="h-2 bg-gray-800 rounded-full overflow-hidden">
                      <div
                        className={`h-full ${passwordStrength.color} transition-all duration-300`}
                        style={{ width: passwordStrength.width }}
                      />
                    </div>

                    {/* Requirements Box */}
                    <div className="bg-gray-800/50 border border-gray-700 rounded-lg p-3 space-y-2">
                      <p className="text-xs font-medium text-emerald-500">Password Requirements</p>
                      {passwordRequirements.map((req, index) => (
                        <div key={index} className="flex items-center gap-2 text-xs">
                          <div className={`w-4 h-4 rounded-full flex items-center justify-center transition-colors ${
                            req.met ? 'bg-emerald-500' : 'bg-gray-700'
                          }`}>
                            {req.met && <CheckCircle2 className="w-3 h-3 text-black" />}
                          </div>
                          <span className={`transition-colors ${req.met ? 'text-emerald-500' : 'text-gray-400'}`}>
                            {req.label}
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>

              {/* Confirm Password */}
              <div className="space-y-2">
                <Label htmlFor="confirmPassword" className="text-sm font-medium">Confirm New Password</Label>
                <div className="relative">
                  <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                  <Input
                    id="confirmPassword"
                    type={showConfirmPassword ? 'text' : 'password'}
                    placeholder="Re-enter new password"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    className="pl-10 pr-10 bg-gray-800 border-gray-700 text-white focus:border-emerald-500 focus:ring-emerald-500"
                    required
                  />
                  <button
                    type="button"
                    onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                    className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-white transition-colors z-10 p-1"
                    aria-label="Toggle confirm password visibility"
                  >
                    {showConfirmPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
                {confirmPassword && (
                  <div className="flex items-center gap-2 text-xs mt-2">
                    {password === confirmPassword ? (
                      <>
                        <CheckCircle2 className="w-4 h-4 text-emerald-500" />
                        <span className="text-emerald-500 font-medium">Passwords match</span>
                      </>
                    ) : (
                      <>
                        <AlertCircle className="w-4 h-4 text-red-500" />
                        <span className="text-red-500 font-medium">Passwords do not match</span>
                      </>
                    )}
                  </div>
                )}
              </div>

              {/* Submit Button */}
              <Button
                type="submit"
                className="w-full bg-emerald-500 text-black hover:bg-emerald-600 font-semibold transition-all"
                disabled={loading}
              >
                {loading ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin mr-2" />
                    Resetting Password...
                  </>
                ) : (
                  'Reset Password'
                )}
              </Button>
            </form>
          )}
        </Card>

        {/* Help Text */}
        {!success && (
          <div className="text-center mt-6 space-y-3">
            <p className="text-gray-400 text-sm">
              Remember your password?{' '}
              <button
                type="button"
                onClick={() => onNavigate?.('login')}
                className="text-emerald-500 hover:text-emerald-400 font-medium transition-colors"
              >
                Back to Login
              </button>
            </p>
            <div className="pt-3 border-t border-gray-800">
              <p className="text-gray-500 text-xs">
                Need help?{' '}
                <a
                  href="mailto:support@cryptotrade.com"
                  className="text-gray-400 hover:text-emerald-500 transition-colors"
                >
                  Contact Support
                </a>
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
