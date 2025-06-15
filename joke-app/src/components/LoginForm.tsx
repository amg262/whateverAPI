import React from 'react';
import { useAuthStore } from '../store/authStore';
import { apiService } from '../services/api';
import { Chrome, Shield, Facebook } from 'lucide-react';

export const LoginForm: React.FC = () => {
  const { error } = useAuthStore();

  const handleSocialLogin = async (provider: 'google' | 'microsoft' | 'facebook') => {
    try {
      const url = await apiService.getOAuthUrl(provider);
      window.location.href = url;
    } catch (error) {
      console.error('OAuth login error:', error);
      // You could also show an error message to the user here
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50 flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        {/* Logo and Header */}
        <div className="text-center mb-8">
          <div className="bg-gradient-to-r from-blue-600 to-purple-600 w-16 h-16 rounded-2xl flex items-center justify-center mx-auto mb-4 shadow-lg">
            <Shield className="w-8 h-8 text-white" />
          </div>
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Welcome Back</h1>
          <p className="text-gray-600">Sign in to access your account</p>
        </div>

        {/* Main Form */}
        <div className="bg-white/70 backdrop-blur-sm rounded-2xl p-8 shadow-xl border border-white/20">
          {/* Error Message */}
          {error && (
            <div className="bg-red-50 border border-red-200 rounded-xl p-4 mb-6">
              <p className="text-red-600 text-sm">{error}</p>
            </div>
          )}

          {/* Sign in with OAuth providers */}
          <div className="text-center mb-6">
            <p className="text-gray-600">Choose your preferred sign-in method</p>
          </div>

          {/* Social Login Buttons */}
          <div className="space-y-3">
            <button
              onClick={() => handleSocialLogin('google')}
              className="w-full bg-white/50 backdrop-blur-sm border border-gray-200 text-gray-700 py-3 px-4 rounded-xl font-medium hover:bg-white/70 focus:ring-2 focus:ring-gray-500 focus:ring-offset-2 transition-all flex items-center justify-center space-x-3"
            >
              <Chrome className="w-5 h-5 text-red-500" />
              <span>Continue with Google</span>
            </button>

            <button
              onClick={() => handleSocialLogin('microsoft')}
              className="w-full bg-white/50 backdrop-blur-sm border border-gray-200 text-gray-700 py-3 px-4 rounded-xl font-medium hover:bg-white/70 focus:ring-2 focus:ring-gray-500 focus:ring-offset-2 transition-all flex items-center justify-center space-x-3"
            >
              <div className="w-5 h-5 bg-blue-600 rounded-sm flex items-center justify-center">
                <div className="w-3 h-3 bg-white rounded-sm"></div>
              </div>
              <span>Continue with Microsoft</span>
            </button>

            <button
              onClick={() => handleSocialLogin('facebook')}
              className="w-full bg-white/50 backdrop-blur-sm border border-gray-200 text-gray-700 py-3 px-4 rounded-xl font-medium hover:bg-white/70 focus:ring-2 focus:ring-gray-500 focus:ring-offset-2 transition-all flex items-center justify-center space-x-3"
            >
              <Facebook className="w-5 h-5 text-blue-600" />
              <span>Continue with Facebook</span>
            </button>
          </div>
        </div>

        {/* Footer */}
        <div className="text-center mt-8">
          <p className="text-sm text-gray-500">
            Protected by WhateverAPI Authentication System
          </p>
        </div>
      </div>
    </div>
  );
};