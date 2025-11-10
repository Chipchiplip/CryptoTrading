import React, { useEffect, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { Loader2, AlertCircle } from 'lucide-react';
import { Button } from '../../ui/button';
import { setAccessToken } from '../../../api/http';

export default function GitHubCallback() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const code = searchParams.get('code');

    if (code) {
      // Gửi 'code' này lên API của bạn
      const exchangeCodeForJwt = async (githubCode: string) => {
        try {
          const res = await fetch('/api/auth/github', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ token: githubCode }) // Gửi code trong trường 'token'
          });

          const data = await res.json();

          if (!res.ok) {
            throw new Error(data.message || "GitHub login failed on server.");
          }

          // Đăng nhập API thành công
          setAccessToken(data.accessToken, data.user);
          navigate('/trader-dashboard'); // Chuyển hướng đến trang dashboard

        } catch (err: any) {
          setError(err.message || "An unexpected error occurred.");
        }
      };

      exchangeCodeForJwt(code);
    } else {
      setError("GitHub login failed: No authorization code received.");
    }
  }, [searchParams, navigate]);

  return (
    <div className="min-h-screen bg-black text-white flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-md text-center">
        {error ? (
          <>
            <AlertCircle className="w-16 h-16 text-red-500 mx-auto mb-4" />
            <h1 className="text-2xl font-bold mb-2">Login Failed</h1>
            <p className="text-gray-400 mb-4">{error}</p>
            <Button
              onClick={() => navigate('/login')}
              className="bg-emerald-500 text-black hover:bg-emerald-600"
            >
              Back to Login
            </Button>
          </>
        ) : (
          <>
            <Loader2 className="w-16 h-16 text-emerald-500 mx-auto mb-4 animate-spin" />
            <h1 className="text-2xl font-bold mb-2">Verifying your identity...</h1>
            <p className="text-gray-400">Please wait while we log you in with GitHub.</p>
          </>
        )}
      </div>
    </div>
  );
}