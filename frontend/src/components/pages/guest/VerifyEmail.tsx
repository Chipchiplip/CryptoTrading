import React, { useState, useEffect } from 'react';
import { CheckCircle2, AlertCircle, Loader2 } from 'lucide-react';
import { Button } from '../../ui/button';
import { Card } from '../../ui/card';
import { AuthApi } from '../../../api/auth';

interface VerifyEmailProps {
  onNavigate?: (page: string) => void;
}

export default function VerifyEmail({ onNavigate }: VerifyEmailProps) {
  const [status, setStatus] = useState<'sending' | 'sent' | 'verified' | 'error'>('sending');
  const [resendCooldown, setResendCooldown] = useState(0);
  const [email, setEmail] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    // Get email from localStorage if available (from registration)
    const savedEmail = localStorage.getItem('pendingVerificationEmail');
    if (savedEmail) {
      setEmail(savedEmail);
      localStorage.removeItem('pendingVerificationEmail'); // Clear after reading
    }
    
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

  const handleResend = async () => {
    if (!email) {
      setError('Please enter your email address');
      return;
    }
    
    setError('');
    setStatus('sending');
    // Call resend email API here if available
    setTimeout(() => {
      setStatus('sent');
      setResendCooldown(60);
    }, 2000);
  };

  return (
    <div className="min-h-screen bg-black text-white flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-md">
        {/* Logo */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center gap-2 mb-4">
            <div className="w-16 h-16 bg-emerald-500 rounded-full flex items-center justify-center">
              <span className="text-black text-xl font-bold">CT</span>
            </div>
          </div>
          <h1 className="text-3xl font-bold mb-2">Verify Your Email</h1>
          <p className="text-gray-400">
            {status === 'sent' 
              ? 'Check your email for verification instructions'
              : status === 'verified'
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
            <div className="text-center space-y-6">
              {/* Success Checkmark */}
              <div className="w-16 h-16 bg-emerald-500/10 rounded-full flex items-center justify-center mx-auto">
                <CheckCircle2 className="w-8 h-8 text-emerald-500" />
              </div>
              
              {/* Email Sent Message */}
              <div>
                <h3 className="text-xl font-bold mb-2">Email Sent!</h3>
                <p className="text-gray-400">
                  We've sent a verification link to <strong className="text-emerald-500">{email || 'your email'}</strong>
                </p>
              </div>

              {/* Troubleshooting Section */}
              <div className="bg-gray-800 border border-gray-700 rounded-lg p-4 text-left">
                <p className="text-sm text-gray-300 mb-3">Didn't receive the email?</p>
                <ul className="text-sm text-gray-400 space-y-1 list-disc list-inside">
                  <li>Check your spam folder</li>
                  <li>Make sure the email address is correct</li>
                  <li>Wait a few minutes and check again</li>
                </ul>
              </div>

              {error && (
                <div className="bg-red-500/10 border border-red-500/50 rounded-lg p-4 text-left">
                  <div className="flex items-center gap-2 text-red-400">
                    <AlertCircle className="w-4 h-4" />
                    <p className="text-sm">{error}</p>
                  </div>
                </div>
              )}

              {/* Action Buttons */}
              <div className="space-y-3">
                <Button
                  className="w-full bg-emerald-500 text-black hover:bg-emerald-600 font-medium py-6"
                  onClick={() => onNavigate?.('login')}
                >
                  Back to Login
                </Button>
                <Button
                  variant="outline"
                  className="w-full border-gray-700 hover:bg-gray-800 text-white font-medium py-6"
                  onClick={handleResend}
                  disabled={resendCooldown > 0}
                >
                  {resendCooldown > 0 
                    ? `Resend Email (${resendCooldown}s)`
                    : 'Resend Email'
                  }
                </Button>
              </div>
            </div>
          )}

          {status === 'verified' && (
            <div className="text-center space-y-6">
              <div className="w-16 h-16 bg-emerald-500/10 rounded-full flex items-center justify-center mx-auto">
                <CheckCircle2 className="w-8 h-8 text-emerald-500" />
              </div>
              <div>
                <h3 className="text-xl font-bold mb-2">Email Verified Successfully!</h3>
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
                  className="w-full bg-emerald-500 text-black hover:bg-emerald-600 font-medium py-6"
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
                <h3 className="text-xl font-bold mb-2">Verification Failed</h3>
                <p className="text-gray-400">
                  We couldn't send the verification email. Please try again.
                </p>
              </div>
              <Button
                className="w-full bg-emerald-500 text-black hover:bg-emerald-600 font-medium py-6"
                onClick={handleResend}
              >
                Try Again
              </Button>
            </div>
          )}
        </Card>

        {/* Help Text */}
        {status !== 'verified' && status !== 'error' && (
          <div className="text-center mt-6">
            <p className="text-gray-400 text-sm">
              Need help?{' '}
              <a href="#" className="text-emerald-500 hover:text-emerald-400">
                Contact Support
              </a>
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
