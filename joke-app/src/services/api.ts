import { LoginCredentials, AuthResponse, User, Joke, JokesResponse, JokeSearchParams, Tag } from '../types/auth';

class ApiService {
  private baseURL = 'https://localhost:8081';

  private async fetchWithAuth(endpoint: string, options: RequestInit = {}): Promise<Response> {
    const token = localStorage.getItem('jwt_token');
    const headers = {
      'Content-Type': 'application/json',
      ...(token && { Authorization: `Bearer ${token}` }),
      ...options.headers,
    };

    const response = await fetch(`${this.baseURL}${endpoint}`, {
      ...options,
      headers,
      credentials: 'include', // Include cookies
    });

    if (response.status === 401) {
      localStorage.removeItem('jwt_token');
      throw new Error('Unauthorized');
    }

    return response;
  }

  async login(credentials: LoginCredentials): Promise<AuthResponse> {
    try {
      const response = await fetch(`${this.baseURL}/api/v1/auth/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include',
        body: JSON.stringify(credentials),
      });

      const data = await response.json();
      
      if (response.ok && data.token) {
        localStorage.setItem('jwt_token', data.token);
        return { 
          success: true, 
          token: data.token, 
          user: data.user 
        };
      }

      return { 
        success: false, 
        message: data.message || 'Login failed' 
      };
    } catch (error) {
      return { 
        success: false, 
        message: error instanceof Error ? error.message : 'Network error' 
      };
    }
  }

  async logout(): Promise<void> {
    try {
      await this.fetchWithAuth('/api/v1/auth/logout', {
        method: 'POST',
      });
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      localStorage.removeItem('jwt_token');
    }
  }

  async checkAuthStatus(): Promise<{ isAuthenticated: boolean; user?: User }> {
    try {
      const response = await this.fetchWithAuth('/api/v1/auth/status');
      
      if (response.ok) {
        const data = await response.json();
        
        // Handle the actual API response format
        if (data.isAuthenticated) {
          const user: User = {
            userId: data.userId,
            email: data.email,
            name: data.name || data.email, // Use email as fallback if name is null
            role: data.role
          };
          
          return { 
            isAuthenticated: true, 
            user: user 
          };
        }
      }
      
      return { isAuthenticated: false };
    } catch (error) {
      return { isAuthenticated: false };
    }
  }

  async getOAuthUrl(provider: 'google' | 'microsoft' | 'facebook'): Promise<string> {
    try {
      const response = await fetch(`${this.baseURL}/api/v1/auth/${provider}/login`, {
        method: 'GET',
        credentials: 'include',
      });

      // Check if response is ok
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      // Try to get the content type
      const contentType = response.headers.get('content-type');
      
      if (contentType && contentType.includes('application/json')) {
        // Backend returns JSON response
        const data = await response.json();
        
        // Check for various possible JSON response formats
        if (data.url) {
          return data.url;
        } else if (data.authUrl) {
          return data.authUrl;
        } else if (data.oauthUrl) {
          return data.oauthUrl;
        } else if (data.redirectUrl) {
          return data.redirectUrl;
        } else if (typeof data === 'string') {
          return data;
        } else {
          throw new Error('OAuth URL not found in response');
        }
      } else {
        // Backend returns plain text
        const textData = await response.text();
        if (textData && textData.startsWith('http')) {
          return textData.trim();
        } else {
          throw new Error('Invalid OAuth URL format');
        }
      }
    } catch (error) {
      console.error('OAuth URL fetch error:', error);
      throw new Error(`OAuth URL fetch failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  // Alternative method: direct redirect to backend OAuth endpoint
  // Use this if the backend is configured to redirect directly
  redirectToOAuth(provider: 'google' | 'microsoft' | 'facebook'): void {
    window.location.href = `${this.baseURL}/api/v1/auth/${provider}/login`;
  }

  async handleOAuthCallback(token: string): Promise<AuthResponse> {
    try {
      localStorage.setItem('jwt_token', token);
      
      const statusResult = await this.checkAuthStatus();
      
      if (statusResult.isAuthenticated && statusResult.user) {
        return {
          success: true,
          token,
          user: statusResult.user
        };
      }
      
      return { 
        success: false, 
        message: 'Authentication failed' 
      };
    } catch (error) {
      return { 
        success: false, 
        message: error instanceof Error ? error.message : 'OAuth callback failed' 
      };
    }
  }

  // Joke Management Methods
  async getJokes(params?: JokeSearchParams): Promise<JokesResponse> {
    try {
      const queryParams = new URLSearchParams();
      
      if (params?.query) queryParams.append('query', params.query);
      if (params?.type) queryParams.append('type', params.type);
      if (params?.tags) queryParams.append('tags', params.tags.join(','));
      if (params?.isActive !== undefined) queryParams.append('isActive', params.isActive.toString());
      if (params?.page) queryParams.append('page', params.page.toString());
      if (params?.limit) queryParams.append('limit', params.limit.toString());
      if (params?.sortBy) queryParams.append('sortBy', params.sortBy);
      if (params?.sortOrder) queryParams.append('sortOrder', params.sortOrder);

      const url = `/api/v1/jokes${queryParams.toString() ? `?${queryParams.toString()}` : ''}`;
      const response = await this.fetchWithAuth(url);

      if (response.ok) {
        const data = await response.json();
        
        // Handle the actual API response format (direct array of jokes)
        if (Array.isArray(data)) {
          const page = params?.page || 1;
          const limit = params?.limit || 10;
          
          return {
            jokes: data,
            total: data.length,
            page: page,
            limit: limit,
            totalPages: Math.ceil(data.length / limit)
          };
        }
        
        // If it's already in the expected format
        return data;
      }

      throw new Error(`Failed to fetch jokes: ${response.status}`);
    } catch (error) {
      console.error('Get jokes error:', error);
      throw new Error(`Failed to fetch jokes: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  async getJoke(id: string): Promise<Joke> {
    try {
      const response = await this.fetchWithAuth(`/api/v1/jokes/${id}`);

      if (response.ok) {
        const data = await response.json();
        return data;
      }

      throw new Error(`Failed to fetch joke: ${response.status}`);
    } catch (error) {
      console.error('Get joke error:', error);
      throw new Error(`Failed to fetch joke: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  async createJoke(joke: Omit<Joke, 'id' | 'createdAt' | 'modifiedAt' | 'author'>): Promise<Joke> {
    try {
      const response = await this.fetchWithAuth('/api/v1/jokes', {
        method: 'POST',
        body: JSON.stringify(joke),
      });

      if (response.ok) {
        const data = await response.json();
        return data;
      }

      throw new Error(`Failed to create joke: ${response.status}`);
    } catch (error) {
      console.error('Create joke error:', error);
      throw new Error(`Failed to create joke: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  async updateJoke(id: string, joke: Partial<Omit<Joke, 'id' | 'createdAt' | 'updatedAt' | 'userId'>>): Promise<Joke> {
    try {
      const response = await this.fetchWithAuth(`/api/v1/jokes/${id}`, {
        method: 'PUT',
        body: JSON.stringify(joke),
      });

      if (response.ok) {
        const data = await response.json();
        return data;
      }

      throw new Error(`Failed to update joke: ${response.status}`);
    } catch (error) {
      console.error('Update joke error:', error);
      throw new Error(`Failed to update joke: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  async deleteJoke(id: string): Promise<void> {
    try {
      const response = await this.fetchWithAuth(`/api/v1/jokes/${id}`, {
        method: 'DELETE',
      });

      if (!response.ok) {
        throw new Error(`Failed to delete joke: ${response.status}`);
      }
    } catch (error) {
      console.error('Delete joke error:', error);
      throw new Error(`Failed to delete joke: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  async searchJokes(params: JokeSearchParams): Promise<JokesResponse> {
    try {
      const response = await this.fetchWithAuth('/api/v1/jokes/search', {
        method: 'POST',
        body: JSON.stringify(params),
      });

      if (response.ok) {
        const data = await response.json();
        return data;
      }

      throw new Error(`Failed to search jokes: ${response.status}`);
    } catch (error) {
      console.error('Search jokes error:', error);
      throw new Error(`Failed to search jokes: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  async getRandomJoke(): Promise<Joke> {
    try {
      const response = await this.fetchWithAuth('/api/v1/jokes/whatever');

      if (response.ok) {
        const data = await response.json();
        return data;
      }

      throw new Error(`Failed to fetch random joke: ${response.status}`);
    } catch (error) {
      console.error('Get random joke error:', error);
      throw new Error(`Failed to fetch random joke: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  async getKlumpJoke(): Promise<Joke> {
    try {
      const response = await this.fetchWithAuth('/api/v1/jokes/klump');

      if (response.ok) {
        const data = await response.json();
        return data;
      }

      throw new Error(`Failed to fetch klump joke: ${response.status}`);
    } catch (error) {
      console.error('Get klump joke error:', error);
      throw new Error(`Failed to fetch klump joke: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  // Tag Management Methods
  async getTags(): Promise<Tag[]> {
    try {
      const response = await this.fetchWithAuth('/api/v1/tag');

      if (response.ok) {
        const data = await response.json();
        return data;
      }

      throw new Error(`Failed to fetch tags: ${response.status}`);
    } catch (error) {
      console.error('Get tags error:', error);
      throw new Error(`Failed to fetch tags: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }
}

export const apiService = new ApiService();