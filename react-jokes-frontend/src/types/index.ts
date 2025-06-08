export type User = {
  id: string;
  email: string;
  name: string;
  role?: string;
  createdAt?: string;
  isActive?: boolean;
}

export type AuthResponse = {
  token: string;
  user: User;
}

export type AuthStatus = {
  isAuthenticated: boolean;
  userId?: string;
  email?: string;
  name?: string;
  role?: string;
}

export enum JokeType {
  Default = 0,
  Personal = 1,
  Api = 2,
  UserSubmission = 3,
  ThirdParty = 4,
  SocialMedia = 5,
  Unknown = 6,
  Joke = 7,
  FunnySaying = 8,
  Discouragement = 9,
  SelfDeprecating = 10
}

export type Tag = {
  id: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  modifiedAt: string;
}

export type Joke = {
  id: string;
  content: string;
  type: JokeType;
  tags: Tag[];
  laughScore?: number;
  isActive: boolean;
  createdAt: string;
  modifiedAt: string;
  user?: User;
}

export type CreateJokeRequest = {
  content: string;
  type: JokeType;
  tags?: string[];
  laughScore?: number;
  isActive?: boolean;
}

export type UpdateJokeRequest = {
  content: string;
  type: JokeType;
  tags?: string[];
  laughScore?: number;
  isActive?: boolean;
}

export type CreateTagRequest = {
  name: string;
}

export type UpdateTagRequest = {
  name: string;
  isActive: boolean;
}

export type FilterRequest = {
  active?: boolean;
  type?: JokeType;
  query?: string;
  pageSize?: number;
  pageNumber?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export type ApiResponse<T> = {
  data: T;
  success: boolean;
  message?: string;
} 