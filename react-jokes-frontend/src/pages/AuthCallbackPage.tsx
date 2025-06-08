import React, { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useAuthStore } from '../stores/authStore';
import { Loader2 } from 'lucide-react';
import toast from 'react-hot-toast';

const AuthCallbackPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { checkAuthStatus } = useAuthStore();
  const [status, setStatus] = useState<'processing' | 'success' | 'error'>('processing');

  useEffect(() => {
    const handleCallback = async () => {
      try {
        // Debug: Log all URL parameters
        console.log('AuthCallback URL parameters:', Object.fromEntries(searchParams.entries()));
        console.log('Current URL:', window.location.href);

        // Check for error from OAuth provider or backend
        const error = searchParams.get('error');
        if (error) {
          console.error('OAuth error:', error);
          setStatus('error');
          toast.error(`Authentication failed: ${error}`);
          setTimeout(() => navigate('/login'), 2000);
          return;
        }

        // Check if token is provided by backend redirect
        const urlToken = searchParams.get('token');
        const provider = searchParams.get('provider');
        
        console.log('Token found:', !!urlToken);
        console.log('Provider:', provider);
        
        if (urlToken) {
          // Token provided by backend redirect - store it and authenticate
          localStorage.setItem('jwt_token', urlToken);
          setStatus('success');
          toast.success(`Authentication with ${provider || 'OAuth provider'} successful!`);
          await checkAuthStatus();
          navigate('/create-joke');
        } else {
          // No token provided - authentication failed
          setStatus('error');
          const errorMessage = 'No authentication token received from server';
          console.error(errorMessage);
          toast.error(errorMessage);
          setTimeout(() => navigate('/login'), 2000);
        }
      } catch (error) {
        console.error('Callback handling error:', error);
        setStatus('error');
        toast.error('Authentication failed. Please try again.');
        setTimeout(() => navigate('/login'), 2000);
      }
    };

    handleCallback();
  }, [searchParams, navigate, checkAuthStatus]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100">
      <div className="max-w-md w-full text-center p-8">
        {status === 'processing' && (
          <>
            <Loader2 className="h-16 w-16 text-blue-600 animate-spin mx-auto mb-4" />
            <h2 className="text-2xl font-bold text-gray-900 mb-2">Authenticating...</h2>
            <p className="text-gray-600">Please wait while we complete your sign-in.</p>
          </>
        )}
        
        {status === 'success' && (
          <>
            <div className="h-16 w-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg className="h-8 w-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-gray-900 mb-2">Success!</h2>
            <p className="text-gray-600">Redirecting you to the app...</p>
          </>
        )}
        
        {status === 'error' && (
          <>
            <div className="h-16 w-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg className="h-8 w-8 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-gray-900 mb-2">Authentication Failed</h2>
            <p className="text-gray-600">Redirecting you back to login...</p>
          </>
        )}
      </div>
    </div>
  );
};

export default AuthCallbackPage; 