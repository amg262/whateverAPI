import { create } from 'zustand';
import { AuthState, LoginCredentials } from '../types/auth';
import { apiService } from '../services/api';

interface AuthStore extends AuthState {
  login: (credentials: LoginCredentials) => Promise<boolean>;
  logout: () => Promise<void>;
  checkAuthStatus: () => Promise<void>;
  handleOAuthCallback: (token: string) => Promise<boolean>;
  clearError: () => void;
  setLoading: (loading: boolean) => void;
}

export const useAuthStore = create<AuthStore>((set, get) => ({
  isAuthenticated: false,
  user: null,
  token: null,
  isLoading: false,
  error: null,

  login: async (credentials: LoginCredentials): Promise<boolean> => {
    set({ isLoading: true, error: null });
    
    try {
      const result = await apiService.login(credentials);
      
      if (result.success && result.user && result.token) {
        set({
          isAuthenticated: true,
          user: result.user,
          token: result.token,
          isLoading: false,
          error: null,
        });
        return true;
      } else {
        set({
          isAuthenticated: false,
          user: null,
          token: null,
          isLoading: false,
          error: result.message || 'Login failed',
        });
        return false;
      }
    } catch (error) {
      set({
        isAuthenticated: false,
        user: null,
        token: null,
        isLoading: false,
        error: error instanceof Error ? error.message : 'Login failed',
      });
      return false;
    }
  },

  logout: async (): Promise<void> => {
    set({ isLoading: true });
    
    try {
      await apiService.logout();
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      set({
        isAuthenticated: false,
        user: null,
        token: null,
        isLoading: false,
        error: null,
      });
    }
  },

  checkAuthStatus: async (): Promise<void> => {
    set({ isLoading: true });
    
    try {
      const result = await apiService.checkAuthStatus();
      
      if (result.isAuthenticated && result.user) {
        const token = localStorage.getItem('jwt_token') || 'cookie-auth';
        set({
          isAuthenticated: true,
          user: result.user,
          token,
          isLoading: false,
          error: null,
        });
      } else {
        localStorage.removeItem('jwt_token');
        set({
          isAuthenticated: false,
          user: null,
          token: null,
          isLoading: false,
          error: null,
        });
      }
    } catch (error) {
      localStorage.removeItem('jwt_token');
      set({
        isAuthenticated: false,
        user: null,
        token: null,
        isLoading: false,
        error: null,
      });
    }
  },

  handleOAuthCallback: async (token: string): Promise<boolean> => {
    set({ isLoading: true, error: null });
    
    try {
      const result = await apiService.handleOAuthCallback(token);
      
      if (result.success && result.user) {
        set({
          isAuthenticated: true,
          user: result.user,
          token: result.token || token,
          isLoading: false,
          error: null,
        });
        return true;
      } else {
        set({
          isAuthenticated: false,
          user: null,
          token: null,
          isLoading: false,
          error: result.message || 'OAuth authentication failed',
        });
        return false;
      }
    } catch (error) {
      set({
        isAuthenticated: false,
        user: null,
        token: null,
        isLoading: false,
        error: error instanceof Error ? error.message : 'OAuth authentication failed',
      });
      return false;
    }
  },

  clearError: () => set({ error: null }),
  setLoading: (loading: boolean) => set({ isLoading: loading }),
}));