import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { User, AuthStatus } from '../types';
import apiService from '../services/api';
import toast from 'react-hot-toast';

type AuthState = {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (provider: 'google' | 'microsoft' | 'facebook') => Promise<void>;
  logout: () => Promise<void>;
  checkAuthStatus: () => Promise<void>;
  setLoading: (loading: boolean) => void;
  setUser: (user: User | null) => void;
  setAuthenticated: (authenticated: boolean) => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      isAuthenticated: false,
      isLoading: true,

      setLoading: (loading: boolean) => set({ isLoading: loading }),
      
      setUser: (user: User | null) => set({ user }),
      
      setAuthenticated: (authenticated: boolean) => set({ isAuthenticated: authenticated }),

      checkAuthStatus: async () => {
        try {
          set({ isLoading: true });
          const authStatus: AuthStatus = await apiService.getAuthStatus();
          
          if (authStatus.isAuthenticated && authStatus.userId) {
            const user: User = {
              id: authStatus.userId,
              email: authStatus.email || '',
              name: authStatus.name || '',
              role: authStatus.role,
            };
            set({ 
              user, 
              isAuthenticated: true, 
              isLoading: false 
            });
          } else {
            set({ 
              user: null, 
              isAuthenticated: false, 
              isLoading: false 
            });
          }
        } catch (error) {
          console.error('Auth status check failed:', error);
          set({ 
            user: null, 
            isAuthenticated: false, 
            isLoading: false 
          });
          apiService.removeToken();
        }
      },

      login: async (provider: 'google' | 'microsoft' | 'facebook') => {
        try {
          set({ isLoading: true });
          let authUrl: string;
          
          switch (provider) {
            case 'google':
              authUrl = await apiService.googleLogin();
              break;
            case 'microsoft':
              authUrl = await apiService.microsoftLogin();
              break;
            case 'facebook':
              authUrl = await apiService.facebookLogin();
              break;
            default:
              throw new Error('Invalid provider');
          }

          // Redirect to OAuth provider
          console.log('Redirecting to OAuth URL:', authUrl);
          window.location.href = authUrl;
        } catch (error) {
          console.error('Login failed:', error);
          toast.error('Login failed. Please try again.');
          set({ isLoading: false });
        }
      },

      logout: async () => {
        try {
          await apiService.logout();
          set({ 
            user: null, 
            isAuthenticated: false 
          });
          toast.success('Logged out successfully');
        } catch (error) {
          console.error('Logout failed:', error);
          // Still clear local state even if API call fails
          set({ 
            user: null, 
            isAuthenticated: false 
          });
          apiService.removeToken();
        }
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({ 
        user: state.user, 
        isAuthenticated: state.isAuthenticated 
      }),
    }
  )
); 