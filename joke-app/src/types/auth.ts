export interface User {
  userId: string;
  email: string;
  name: string;
  role: 'admin' | 'moderator' | 'user';
}

export interface AuthState {
  isAuthenticated: boolean;
  user: User | null;
  token: string | null;
  isLoading: boolean;
  error: string | null;
}

export interface LoginCredentials {
  email: string;
  name: string;
}

export interface AuthResponse {
  success: boolean;
  token?: string;
  user?: User;
  message?: string;
}

// Joke-related types
export interface Joke {
  id: string;
  content: string;
  type?: number;
  tags: string[];
  isActive: boolean;
  laughScore: number | null;
  createdAt: string;
  modifiedAt: string;
  author: {
    id: string;
    name: string;
    email: string;
  };
}

export interface JokeSearchParams {
  query?: string;
  type?: string;
  tags?: string[];
  isActive?: boolean;
  page?: number;
  limit?: number;
  sortBy?: 'laughScore' | 'createdAt' | 'modifiedAt';
  sortOrder?: 'asc' | 'desc';
}

export interface JokesResponse {
  jokes: Joke[];
  total: number;
  page: number;
  limit: number;
  totalPages: number;
}

export interface Tag {
  id: string;
  name: string;
  description?: string;
  createdAt: string;
  updatedAt: string;
}