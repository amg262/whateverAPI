export type User = {
  id: string;
  email: string;
  name: string;
  roles: string[];
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
};

export type Joke = {
  id: string;
  title: string;
  content: string;
  type: string;
  tags: Tag[];
  laughScore: number;
  isActive: boolean;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
};

export type Tag = {
  id: string;
  name: string;
  description?: string;
  color?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
};

export type AuthState = {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
};

export type JokesState = {
  jokes: Joke[];
  isLoading: boolean;
  error: string | null;
  pagination: {
    page: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
  };
};

export type ApiResponse<T> = {
  data: T;
  success: boolean;
  message?: string;
};

export type OAuthProvider = 'google' | 'microsoft' | 'facebook'; 