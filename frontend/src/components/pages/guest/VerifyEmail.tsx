import React, { useState, useEffect } from 'react';
import { Mail, CheckCircle2, AlertCircle, Loader2 } from 'lucide-react';
import { Button } from '../../ui/button';
import { Card } from '../../ui/card';
import { Alert, AlertDescription } from '../../ui/alert';
import { Input } from '../../ui/input';
import { Label } from '../../ui/label';
import { AuthApi } from '../../../api/auth';

interface VerifyEmailProps {
  onNavigate?: (page: string) => void;
}

export default function VerifyEmail({ onNavigate }: VerifyEmailProps) {
  const [status, setStatus] = useState<'sending' | 'sent' | 'verified' | 'error'>('sending');
  const [resendCooldown, setResendCooldown] = useState(0);
  const [email, setEmail] = useState('');
  const [token, setToken] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    // Simulate sending verification email
    const timer = setTimeout(() => {
      setStatus('sent');
      setResendCooldown(60);
    }, 2000);

    return () => clearTimeout(timer);
  }, []);

  useEffect(() => {
    if (resendCooldown > 0) {
      const timer = setTimeout(() => {
        setResendCooldown(resendCooldown - 1);
      }, 1000);
      return () => clearTimeout(timer);
    }
  }, [resendCooldown]);

  const handleResend = () => {
    setStatus('sending');
    setTimeout(() => {
      setStatus('sent');
      setResendCooldown(60);
    }, 2000);
  };

  const handleVerify = async () => {
    setError('');
    if (!email || !token) {
      setError('Please provide email and token');
      return;
    }
    const res = await AuthApi.confirmEmail({ email, token });
    if (!res.ok) {
      setError(res.error);
      return;
    }
    setStatus('verified');
  };

  return (
    <div className="min-h-screen bg-black text-white flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-md">
        {/* Logo */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center gap-2 mb-4">
            <div className="w-12 h-12 bg-emerald-500 rounded-lg flex items-center justify-center">
              <span className="text-black text-xl">CT</span>
            </div>
          </div>
          <h1 className="text-3xl mb-2">
            {status === 'verified' ? 'Email Verified!' : 'Verify Your Email'}
          </h1>
          <p className="text-gray-400">
            {status === 'verified' 
              ? 'Your account is ready to use'
              : 'We need to verify your email address'
            }
          </p>
        </div>

        <Card className="bg-gray-900 border-gray-800 p-8">
          {status === 'sending' && (
            <div className="text-center space-y-6">
              <div className="w-16 h-16 bg-emerald-500/10 rounded-full flex items-center justify-center mx-auto">
                <Loader2 className="w-8 h-8 text-emerald-500 animate-spin" />
              </div>
              <div>
                <h3 className="mb-2">Sending Verification Email</h3>
                <p className="text-gray-400">Please wait while we send you the verification link...</p>
              </div>
            </div>
          )}

          {status === 'sent' && (
            <div className="space-y-6">
              <div className="text-center">
                <div className="w-16 h-16 bg-emerald-500/10 rounded-full flex items-center justify-center mx-auto mb-4">
                  <Mail className="w-8 h-8 text-emerald-500" />
                </div>
                <h3 className="mb-2">Check Your Email</h3>
                <p className="text-gray-400 mb-2">
                  We've sent a verification link to
                </p>
                <p className="text-white">{email || 'your email'}</p>
              </div>

              <div className="bg-gray-800 border border-gray-700 rounded-lg p-4 space-y-3">
                <p className="text-sm text-gray-300">To complete your registration:</p>
                <ol className="text-sm text-gray-400 space-y-2 list-decimal list-inside">
                  <li>Open the email we sent you</li>
                  <li>Click on the verification link</li>
                  <li>You'll be redirected to login</li>
                </ol>
              </div>

              <Alert className="bg-yellow-500/10 border-yellow-500/50">
                <AlertCircle className="h-4 w-4 text-yellow-500" />
                <AlertDescription className="text-yellow-500">
                  The verification link will expire in 24 hours
                </AlertDescription>
              </Alert>

              {error && (
                <Alert className="bg-red-500/10 border-red-500/50">
                  <AlertCircle className="h-4 w-4 text-red-500" />
                  <AlertDescription className="text-red-500">{error}</AlertDescription>
                </Alert>
              )}

              <div className="grid gap-3">
                <div className="space-y-2">
                  <Label htmlFor="email">Email</Label>
                  <Input id="email" value={email} onChange={(e) => setEmail(e.target.value)} className="bg-gray-800 border-gray-700 text-white" placeholder="your.email@example.com" />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="token">Verification Token</Label>
                  <Input id="token" value={token} onChange={(e) => setToken(e.target.value)} className="bg-gray-800 border-gray-700 text-white" placeholder="Paste token here" />
                </div>
              </div>

              {/* Demo Verify Button - for testing */}
              <Button
                className="w-full bg-emerald-500 text-black hover:bg-emerald-600"
                onClick={handleVerify}
              >
                Verify Email (Demo)
              </Button>

              <div className="text-center space-y-3">
                <p className="text-sm text-gray-400">Didn't receive the email?</p>
                <Button
                  variant="outline"
                  className="w-full border-gray-700 hover:bg-gray-800"
                  onClick={handleResend}
                  disabled={resendCooldown > 0}
                >
                  {resendCooldown > 0 
                    ? `Resend in ${resendCooldown}s` 
                    : 'Resend Verification Email'
                  }
                </Button>
              </div>

              <div className="bg-gray-800 border border-gray-700 rounded-lg p-4">
                <p className="text-sm text-gray-300 mb-2">Email not in inbox?</p>
                <ul className="text-sm text-gray-400 space-y-1 list-disc list-inside">
                  <li>Check your spam or junk folder</li>
                  <li>Add noreply@cryptotrade.com to contacts</li>
                  <li>Make sure the email address is correct</li>
                </ul>
              </div>
            </div>
          )}

          {status === 'verified' && (
            <div className="text-center space-y-6">
              <div className="w-16 h-16 bg-emerald-500/10 rounded-full flex items-center justify-center mx-auto">
                <CheckCircle2 className="w-8 h-8 text-emerald-500" />
              </div>
              <div>
                <h3 className="mb-2">Email Verified Successfully!</h3>
                <p className="text-gray-400">
                  Your account is now active. You can start trading right away.
                </p>
              </div>

              <div className="bg-emerald-500/10 border border-emerald-500/20 rounded-lg p-4">
                <p className="text-emerald-500 text-sm">
                  🎉 Welcome to CryptoTrade! Get started with our beginner's guide.
                </p>
              </div>

              <div className="space-y-3">
                <Button
                  className="w-full bg-emerald-500 text-black hover:bg-emerald-600"
                  onClick={() => onNavigate?.('login')}
                >
                  Go to Login
                </Button>
                <p className="text-sm text-gray-400">
                  You'll receive a welcome email with tips to get started
                </p>
              </div>
            </div>
          )}

          {status === 'error' && (
            <div className="text-center space-y-6">
              <div className="w-16 h-16 bg-red-500/10 rounded-full flex items-center justify-center mx-auto">
                <AlertCircle className="w-8 h-8 text-red-500" />
              </div>
              <div>
                <h3 className="mb-2">Verification Failed</h3>
                <p className="text-gray-400">
                  We couldn't send the verification email. Please try again.
                </p>
              </div>
              <Button
                className="w-full bg-emerald-500 text-black hover:bg-emerald-600"
                onClick={handleResend}
              >
                Try Again
              </Button>
            </div>
          )}
        </Card>

        {/* Help Text */}
        {status !== 'verified' && (
          <div className="text-center mt-6">
            <p className="text-gray-400 text-sm">
              Need help?{' '}
              <a href="#" className="text-emerald-500 hover:text-emerald-400">
                Contact Support
              </a>
            </p>
          </div>
        )}

        {/* Back to Login */}
        {status === 'sent' && (
          <div className="text-center mt-4">
            <button
              onClick={() => onNavigate?.('login')}
              className="text-gray-400 hover:text-white text-sm"
            >
              I'll verify later, take me to login
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
