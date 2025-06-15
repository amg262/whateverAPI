import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { AuthState, User, OAuthProvider } from '../types';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || 'https://localhost:8081';

type AuthStore = AuthState & {
  login: (email: string, name: string) => Promise<void>;
  loginWithOAuth: (provider: OAuthProvider) => Promise<void>;
  logout: () => Promise<void>;
  checkAuth: () => void;
  setToken: (token: string) => void;
  setUser: (user: User) => void;
};

export const useAuthStore = create<AuthStore>()(
  persist(
    (set, get) => ({
      user: null,
      token: null,
      isAuthenticated: false,
      isLoading: false,

      login: async (email: string, name: string) => {
        set({ isLoading: true });
        try {
          const response = await fetch(`${API_BASE_URL}/api/v1/user/login`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify({ email, name }),
          });

          if (!response.ok) {
            throw new Error('Login failed');
          }

          const data = await response.json();
          
          set({
            token: data.token,
            user: data.user,
            isAuthenticated: true,
            isLoading: false,
          });
        } catch (error) {
          console.error('Login error:', error);
          set({ isLoading: false });
          throw error;
        }
      },

      loginWithOAuth: async (provider: OAuthProvider) => {
        try {
          set({ isLoading: true });
          // Fetch the OAuth URL from the backend
          const response = await fetch(`${API_BASE_URL}/api/v1/auth/${provider}/login`, {
            method: 'GET',
            headers: {
              'Content-Type': 'application/json',
            },
          });

          if (!response.ok) {
            throw new Error(`Failed to get OAuth URL: ${response.statusText}`);
          }

          // Get the OAuth URL from the response
          let oauthUrl: string;
          
          const contentType = response.headers.get('content-type');
          if (contentType && contentType.includes('application/json')) {
            // Response is JSON, parse it
            oauthUrl = await response.json();
          } else {
            // Response is plain text, but might have quotes
            oauthUrl = await response.text();
            // Remove quotes if the URL is wrapped in JSON quotes
            if (oauthUrl.startsWith('"') && oauthUrl.endsWith('"')) {
              oauthUrl = oauthUrl.slice(1, -1);
            }
          }
          
          // Redirect to the OAuth provider
          window.location.href = oauthUrl;
        } catch (error) {
          console.error('OAuth login error:', error);
          set({ isLoading: false });
          throw error;
        }
      },

      logout: async () => {
        const { token } = get();
        if (token) {
          try {
            await fetch(`${API_BASE_URL}/api/v1/user/logout`, {
              method: 'POST',
              headers: {
                'Authorization': `Bearer ${token}`,
              },
            });
          } catch (error) {
            console.error('Logout error:', error);
          }
        }

        set({
          user: null,
          token: null,
          isAuthenticated: false,
        });
      },

      setToken: (token: string) => {
        set({ token, isAuthenticated: true });
      },

      setUser: (user: User) => {
        set({ user });
      },

      checkAuth: () => {
        const { token } = get();
        if (token) {
          // Verify token validity here if needed
          set({ isAuthenticated: true });
        }
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        token: state.token,
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
); 