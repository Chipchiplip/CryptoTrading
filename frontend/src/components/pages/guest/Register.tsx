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


const GitHubIcon = () => (
  <svg className="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 24 24">
    <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
  </svg>
);

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

  const handleGitHubLogin = () => {
    setLoading(true);
    const githubClientId = import.meta.env.VITE_GITHUB_CLIENT_ID;
    const redirectUri = `${window.location.origin}/auth/github/callback`;
    
    window.location.href = `https://github.com/login/oauth/authorize?client_id=${githubClientId}&redirect_uri=${redirectUri}&scope=user:email`;
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
              <Button
                type="button"
                variant="outline"
                className="border-gray-700 hover:bg-gray-800 h-12 text-base"
                onClick={handleGitHubLogin}
                disabled={loading}
              >
                <GitHubIcon />
                Continue with GitHub
              </Button>
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