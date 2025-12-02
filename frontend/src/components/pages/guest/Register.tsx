import { useState } from 'react';
import { Mail, Lock, Eye, EyeOff, User, AlertCircle, CheckCircle2 } from 'lucide-react';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Label } from '../../ui/label';
import { Card } from '../../ui/card';
import { Alert, AlertDescription } from '../../ui/alert';
import { Checkbox } from '../../ui/checkbox';
import { AuthApi } from '../../../api/auth';
import { setAccessToken } from '../../../api/http';
import { GoogleLogin } from '@react-oauth/google';

interface RegisterProps {
  onNavigate?: (page: string) => void;
}



export default function Register({ onNavigate }: RegisterProps) {
  const [formData, setFormData] = useState({
    fullName: '',
    email: '',
    password: '',
    confirmPassword: ''
  });
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [agreeToTerms, setAgreeToTerms] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const passwordRequirements = [
    { label: 'At least 8 characters', met: formData.password.length >= 8 },
    { label: 'Contains uppercase letter', met: /[A-Z]/.test(formData.password) },
    { label: 'Contains lowercase letter', met: /[a-z]/.test(formData.password) },
    { label: 'Contains number', met: /[0-9]/.test(formData.password) },
  ];

  const handleChange = (field: string, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    setError('');
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!formData.fullName || !formData.email || !formData.password || !formData.confirmPassword) {
      setError('Please fill in all fields');
      return;
    }

    if (formData.password !== formData.confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    if (!passwordRequirements.every(req => req.met)) {
      setError('Password does not meet requirements');
      return;
    }

    if (!agreeToTerms) {
      setError('Please agree to the Terms of Service and Privacy Policy');
      return;
    }

    setLoading(true);

    try {
      const res = await AuthApi.register({ 
        email: formData.email, 
        password: formData.password, 
        confirmPassword: formData.confirmPassword,
        fullName: formData.fullName 
      });
      
      if (!res.ok) {
        setError(res.error);
        setLoading(false);
        return;
      }

      localStorage.setItem('pendingVerificationEmail', formData.email);
      
      if (res.data.accessToken) {
        setAccessToken(res.data.accessToken, res.data.user || null);
      }
      onNavigate?.('verify-email');
      setLoading(false);
    } catch (e: any) {
      setError('Network Error: Could not connect to the server. Please ensure the backend is running.');
      setLoading(false);
    }
  };
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
        setError(data.message || "Google login failed on server.");
        setLoading(false);
        return;
      }

      setAccessToken(data.accessToken, data.user);
      onNavigate?.('trader-dashboard');
    } catch (err: any) {
        console.error('[Login] Google login API error:', err);
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
          <h1 className="text-3xl mb-2">Create Account</h1>
          <p className="text-gray-400">Start your crypto trading journey today</p>
        </div>

        <Card className="bg-gray-900 border-gray-800 p-8">
          <form onSubmit={handleSubmit} className="space-y-6">
            {error && (
              <Alert className="bg-red-500/10 border-red-500/50 text-red-500">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <div className="space-y-2">
              <Label htmlFor="fullName">Full Name</Label>
              <div className="relative">
                <User className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
                <Input
                  id="fullName"
                  type="text"
                  placeholder="John Doe"
                  value={formData.fullName}
                  onChange={(e) => handleChange('fullName', e.target.value)}
                  className="pl-10 bg-gray-800 border-gray-700 text-white"
                  required
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="email">Email Address</Label>
              <div className="relative">
                <Mail className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
                <Input
                  id="email"
                  type="email"
                  placeholder="your.email@example.com"
                  value={formData.email}
                  onChange={(e) => handleChange('email', e.target.value)}
                  className="pl-10 bg-gray-800 border-gray-700 text-white"
                  required
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
                  placeholder="Create a strong password"
                  value={formData.password}
                  onChange={(e) => handleChange('password', e.target.value)}
                  className="pl-10 pr-10 bg-gray-800 border-gray-700 text-white"
                  required
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-white"
                >
                  {showPassword ? <EyeOff className="w-5 h-5" /> : <Eye className="w-5 h-5" />}
                </button>
              </div>
              {formData.password && (
                <div className="space-y-1 mt-2">
                  {passwordRequirements.map((req, index) => (
                    <div key={index} className="flex items-center gap-2 text-sm">
                      <CheckCircle2 
                        className={`w-4 h-4 ${req.met ? 'text-emerald-500' : 'text-gray-600'}`}
                      />
                      <span className={req.met ? 'text-emerald-500' : 'text-gray-400'}>
                        {req.label}
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="confirmPassword">Confirm Password</Label>
              <div className="relative">
                <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
                <Input
                  id="confirmPassword"
                  type={showConfirmPassword ? 'text' : 'password'}
                  placeholder="Confirm your password"
                  value={formData.confirmPassword}
                  onChange={(e) => handleChange('confirmPassword', e.target.value)}
                  className="pl-10 pr-10 bg-gray-800 border-gray-700 text-white"
                  required
                />
                <button
                  type="button"
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                  className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-white"
                >
                  {showConfirmPassword ? <EyeOff className="w-5 h-5" /> : <Eye className="w-5 h-5" />}
                </button>
              </div>
            </div>

            <div className="flex items-start gap-2">
              <Checkbox
                id="terms"
                checked={agreeToTerms}
                onCheckedChange={(checked: boolean | "indeterminate") => setAgreeToTerms(checked as boolean)}
                className="mt-1"
              />
              <Label htmlFor="terms" className="text-sm text-gray-400 cursor-pointer">
                I agree to the{' '}
                <a href="#" className="text-emerald-500 hover:text-emerald-400">Terms of Service</a>
                {' '}and{' '}
                <a href="#" className="text-emerald-500 hover:text-emerald-400">Privacy Policy</a>
              </Label>
            </div>

            <Button
              type="submit"
              className="w-full bg-emerald-500 text-black hover:bg-emerald-600"
              disabled={loading}
            >
              {loading ? 'Creating Account...' : 'Create Account'}
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
          <span className="text-gray-400">Already have an account? </span>
          <button
            onClick={() => onNavigate?.('login')}
            className="text-emerald-500 hover:text-emerald-400"
          >
            Sign in
          </button>
        </div>
      </div>
    </div>
  );
}