import type { 
  Joke, 
  CreateJokeRequest, 
  UpdateJokeRequest,
  Tag,
  CreateTagRequest,
  UpdateTagRequest,
  FilterRequest,
  AuthStatus
} from '../types';

class ApiService {
  private readonly baseURL = import.meta.env.VITE_API_URL || 'https://localhost:8081';

  constructor() {
    console.log('API Service initialized with baseURL:', this.baseURL);
  }

  // Token management
  private getToken(): string | null {
    return localStorage.getItem('jwt_token');
  }

  public setToken(token: string): void {
    localStorage.setItem('jwt_token', token);
  }

  public removeToken(): void {
    localStorage.removeItem('jwt_token');
  }

  private async fetchWithAuth(endpoint: string, options: RequestInit = {}): Promise<Response> {
    const token = this.getToken();
    const headers = {
      'Content-Type': 'application/json',
      ...(token && { Authorization: `Bearer ${token}` }),
      ...options.headers,
    };

    const response = await fetch(`${this.baseURL}${endpoint}`, {
      ...options,
      headers,
    });

    if (response.status === 401) {
      this.removeToken();
      window.location.href = '/login';
      throw new Error('Unauthorized');
    }

    return response;
  }

  private async handleResponse<T>(response: Response): Promise<T> {
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`API Error: ${response.status} - ${errorText}`);
    }

    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
      return response.json();
    }
    
    return response.text() as unknown as T;
  }

  // Auth endpoints
  async getAuthStatus(): Promise<AuthStatus> {
    const response = await this.fetchWithAuth('/api/v1/auth/status');
    return this.handleResponse<AuthStatus>(response);
  }

  async googleLogin(): Promise<string> {
    const redirectUri = `${window.location.origin}/auth/callback`;
    const response = await this.fetchWithAuth(`/api/v1/auth/google/login?redirectUri=${encodeURIComponent(redirectUri)}`);
    return this.handleResponse<string>(response);
  }

  async microsoftLogin(): Promise<string> {
    const redirectUri = `${window.location.origin}/auth/callback`;
    const response = await this.fetchWithAuth(`/api/v1/auth/microsoft/login?redirectUri=${encodeURIComponent(redirectUri)}`);
    return this.handleResponse<string>(response);
  }

  async facebookLogin(): Promise<string> {
    const redirectUri = `${window.location.origin}/auth/callback`;
    const response = await this.fetchWithAuth(`/api/v1/auth/facebook/login?redirectUri=${encodeURIComponent(redirectUri)}`);
    return this.handleResponse<string>(response);
  }

  async logout(): Promise<void> {
    await this.fetchWithAuth('/api/v1/auth/logout', {
      method: 'POST',
    });
    this.removeToken();
  }



  // Joke endpoints
  async getJokes(filters?: FilterRequest): Promise<Joke[]> {
    const params = new URLSearchParams();
    if (filters) {
      Object.entries(filters).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          params.append(key, value.toString());
        }
      });
    }
    
    const response = await this.fetchWithAuth(
      `/api/v1/jokes${params.toString() ? `?${params.toString()}` : ''}`
    );
    return this.handleResponse<Joke[]>(response);
  }

  async getJoke(id: string): Promise<Joke> {
    const response = await this.fetchWithAuth(`/api/v1/jokes/${id}`);
    return this.handleResponse<Joke>(response);
  }

  async getRandomJoke(): Promise<Joke> {
    const response = await this.fetchWithAuth('/api/v1/jokes/random');
    return this.handleResponse<Joke>(response);
  }

  async searchJokes(query: string): Promise<Joke[]> {
    const response = await this.fetchWithAuth(`/api/v1/jokes/search?q=${encodeURIComponent(query)}`);
    return this.handleResponse<Joke[]>(response);
  }

  async createJoke(joke: CreateJokeRequest): Promise<Joke> {
    const response = await this.fetchWithAuth('/api/v1/jokes', {
      method: 'POST',
      body: JSON.stringify(joke),
    });
    return this.handleResponse<Joke>(response);
  }

  async updateJoke(id: string, joke: UpdateJokeRequest): Promise<Joke> {
    const response = await this.fetchWithAuth(`/api/v1/jokes/${id}`, {
      method: 'PUT',
      body: JSON.stringify(joke),
    });
    return this.handleResponse<Joke>(response);
  }

  async deleteJoke(id: string): Promise<void> {
    await this.fetchWithAuth(`/api/v1/jokes/${id}`, {
      method: 'DELETE',
    });
  }

  // Tag endpoints
  async getTags(): Promise<Tag[]> {
    const response = await this.fetchWithAuth('/api/v1/tags');
    return this.handleResponse<Tag[]>(response);
  }

  async getTag(id: string): Promise<Tag> {
    const response = await this.fetchWithAuth(`/api/v1/tags/${id}`);
    return this.handleResponse<Tag>(response);
  }

  async createTag(tag: CreateTagRequest): Promise<Tag> {
    const response = await this.fetchWithAuth('/api/v1/tags', {
      method: 'POST',
      body: JSON.stringify(tag),
    });
    return this.handleResponse<Tag>(response);
  }

  async updateTag(id: string, tag: UpdateTagRequest): Promise<Tag> {
    const response = await this.fetchWithAuth(`/api/v1/tags/${id}`, {
      method: 'PUT',
      body: JSON.stringify(tag),
    });
    return this.handleResponse<Tag>(response);
  }

  async deleteTag(id: string): Promise<void> {
    await this.fetchWithAuth(`/api/v1/tags/${id}`, {
      method: 'DELETE',
    });
  }
}

export const apiService = new ApiService();
export default apiService; 