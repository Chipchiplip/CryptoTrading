import { useEffect, useMemo, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { Lock, Eye, EyeOff, AlertCircle, AlertTriangle, CheckCircle2 } from 'lucide-react';
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

  const location = useLocation();

  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const linkEmail = params.get('email');
    const linkToken = params.get('token');

    if (linkEmail) {
      setEmail(decodeURIComponent(linkEmail));
    }
    if (linkToken) {
      setToken(decodeURIComponent(linkToken));
    }
  }, [location.search]);

  const passwordRequirements = useMemo(
    () => [
      { label: 'At least 8 characters', met: password.length >= 8 },
      { label: 'At least 1 uppercase letter', met: /[A-Z]/.test(password) },
      { label: 'At least 1 lowercase letter', met: /[a-z]/.test(password) },
      { label: 'At least 1 number', met: /[0-9]/.test(password) },
    ],
    [password],
  );

  const isReadyToSubmit = !!email && !!token;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!isReadyToSubmit) {
      setError('Reset link is missing required details. Please retry from your email link.');
      return;
    }

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
    <div className="min-h-screen bg-[#030712] text-white flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-xl">
        <Card className="bg-[#0b1220] border border-white/5 rounded-3xl p-8 shadow-[0_40px_80px_rgba(0,0,0,0.45)]">
          {success ? (
            <div className="text-center space-y-8">
              <div className="w-20 h-20 rounded-[28px] bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center mx-auto">
                <CheckCircle2 className="w-10 h-10 text-emerald-400" />
              </div>
              <div>
                <p className="uppercase text-xs tracking-[0.3em] text-emerald-400">CryptoTrade</p>
                <h3 className="mt-3 text-3xl font-semibold">Password Reset!</h3>
                <p className="mt-2 text-gray-400">
                  Your password has been updated. You can now sign in using your new credentials.
                </p>
              </div>
              <Button
                className="w-full bg-emerald-500 text-black hover:bg-emerald-400 text-lg py-6 rounded-2xl"
                onClick={() => onNavigate?.('login')}
              >
                Continue to Login
              </Button>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-8">
              <div className="space-y-6">
                <div className="flex items-center gap-4">
                  <div className="w-14 h-14 rounded-2xl bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center">
                    <img src="/logo.png" alt="CryptoTrade" className="w-10 h-10 object-contain" />
                  </div>
                  <div>
                    <p className="uppercase text-xs tracking-[0.4em] text-emerald-400">CryptoTrade</p>
                    <h1 className="text-3xl font-semibold">Reset Your Password</h1>
                  </div>
                </div>
                <p className="text-gray-400 text-lg">Enter your new password below</p>
                {email && (
                  <p className="text-sm text-gray-500">
                    Resetting password for <span className="text-white">{email}</span>
                  </p>
                )}
              </div>

              <div className="relative rounded-3xl border border-yellow-500/40 bg-[#080f1e] px-6 py-5">
                <div className="flex items-start gap-4">
                  <div className="mt-1 text-yellow-400">
                    <AlertTriangle className="w-6 h-6" />
                  </div>
                  <div className="space-y-3">
                    <p className="text-sm uppercase tracking-wide text-gray-400">Your password must include</p>
                    <div className="space-y-2">
                      {passwordRequirements.map((req) => (
                        <div key={req.label} className="flex items-center gap-3 text-sm">
                          <span
                            className={`inline-flex h-2.5 w-2.5 rounded-full ${
                              req.met ? 'bg-emerald-400' : 'bg-gray-600'
                            }`}
                          />
                          <span className={req.met ? 'text-emerald-300' : 'text-gray-400'}>
                            {req.label}
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              </div>

              {error && (
                <Alert className="bg-red-500/10 border border-red-500/50 text-red-400">
                  <AlertCircle className="h-4 w-4" />
                  <AlertDescription className="text-sm">{error}</AlertDescription>
                </Alert>
              )}

              <div className="space-y-6">
                <div className="space-y-2">
                  <Label htmlFor="password" className="text-gray-300">
                    Enter new password
                  </Label>
                  <div className="relative">
                    <Lock className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-500" />
                    <Input
                      id="password"
                      type={showPassword ? 'text' : 'password'}
                      placeholder="Enter new password"
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      className="pl-12 pr-12 py-6 bg-[#050914] border-white/5 rounded-2xl text-white placeholder:text-gray-600"
                      required
                    />
                    <button
                      type="button"
                      onClick={() => setShowPassword(!showPassword)}
                      className="absolute right-4 top-1/2 -translate-y-1/2 text-gray-500 hover:text-white"
                    >
                      {showPassword ? <EyeOff className="w-5 h-5" /> : <Eye className="w-5 h-5" />}
                    </button>
                  </div>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="confirmPassword" className="text-gray-300">
                    Re-enter password
                  </Label>
                  <div className="relative">
                    <Lock className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-500" />
                    <Input
                      id="confirmPassword"
                      type={showConfirmPassword ? 'text' : 'password'}
                      placeholder="Re-enter password"
                      value={confirmPassword}
                      onChange={(e) => setConfirmPassword(e.target.value)}
                      className="pl-12 pr-12 py-6 bg-[#050914] border-white/5 rounded-2xl text-white placeholder:text-gray-600"
                      required
                    />
                    <button
                      type="button"
                      onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                      className="absolute right-4 top-1/2 -translate-y-1/2 text-gray-500 hover:text-white"
                    >
                      {showConfirmPassword ? <EyeOff className="w-5 h-5" /> : <Eye className="w-5 h-5" />}
                    </button>
                  </div>
                  {confirmPassword && (
                    <div className="flex items-center gap-2 text-sm mt-2">
                      {password === confirmPassword ? (
                        <>
                          <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                          <span className="text-emerald-300">Passwords match</span>
                        </>
                      ) : (
                        <>
                          <AlertCircle className="w-4 h-4 text-red-400" />
                          <span className="text-red-400">Passwords do not match</span>
                        </>
                      )}
                    </div>
                  )}
                </div>
              </div>

              {!isReadyToSubmit && (
                <div className="text-sm text-yellow-300/90 bg-yellow-500/10 border border-yellow-500/30 rounded-2xl px-4 py-3">
                  The reset link is missing some details. Please open the password reset link directly from your email.
                </div>
              )}

              <Button
                type="submit"
                className="w-full bg-emerald-500 text-black hover:bg-emerald-400 text-lg py-6 rounded-2xl"
                disabled={loading || !isReadyToSubmit}
              >
                {loading ? 'Resetting Password...' : 'Reset Password'}
              </Button>

              <p className="text-center text-sm text-gray-500">
                Remember your password?{' '}
                <button
                  type="button"
                  className="text-emerald-400 hover:text-emerald-300"
                  onClick={() => onNavigate?.('login')}
                >
                  Back to Login
                </button>
              </p>
            </form>
          )}
        </Card>
      </div>
    </div>
  );
}
