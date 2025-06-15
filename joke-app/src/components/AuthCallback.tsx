import React, { useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { Loader2, AlertCircle } from 'lucide-react';

export const AuthCallback: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { handleOAuthCallback, isLoading, error } = useAuthStore();

  useEffect(() => {
    const processCallback = async () => {
      const token = searchParams.get('token');
      const errorParam = searchParams.get('error');

      if (errorParam) {
        navigate('/login', { 
          state: { 
            error: `Authentication failed: ${errorParam}` 
          } 
        });
        return;
      }

      if (token) {
        const success = await handleOAuthCallback(token);
        if (success) {
          navigate('/jokes', { replace: true });
        } else {
          navigate('/login', { 
            state: { 
              error: 'OAuth authentication failed. Please try again.' 
            } 
          });
        }
      } else {
        navigate('/login', { 
          state: { 
            error: 'No authentication token received.' 
          } 
        });
      }
    };

    processCallback();
  }, [searchParams, navigate, handleOAuthCallback]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50 flex items-center justify-center p-4">
      <div className="bg-white/70 backdrop-blur-sm rounded-2xl p-8 shadow-xl border border-white/20 text-center max-w-md w-full">
        {isLoading ? (
          <>
            <Loader2 className="w-12 h-12 animate-spin text-blue-600 mx-auto mb-4" />
            <h2 className="text-2xl font-bold text-gray-900 mb-2">Completing Authentication</h2>
            <p className="text-gray-600">Please wait while we verify your credentials...</p>
          </>
        ) : error ? (
          <>
            <AlertCircle className="w-12 h-12 text-red-500 mx-auto mb-4" />
            <h2 className="text-2xl font-bold text-gray-900 mb-2">Authentication Failed</h2>
            <p className="text-gray-600 mb-4">{error}</p>
            <button
              onClick={() => navigate('/login')}
              className="bg-blue-600 hover:bg-blue-700 text-white px-6 py-2 rounded-lg font-medium transition-colors"
            >
              Try Again
            </button>
          </>
        ) : (
          <>
            <Loader2 className="w-12 h-12 animate-spin text-blue-600 mx-auto mb-4" />
            <h2 className="text-2xl font-bold text-gray-900 mb-2">Processing Authentication</h2>
            <p className="text-gray-600">Redirecting you to your dashboard...</p>
          </>
        )}
      </div>
    </div>
  );
};