import { create } from 'zustand';
import { JokesState, Joke } from '../types';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || 'https://localhost:8081';

type JokesStore = JokesState & {
  fetchJokes: (token: string, page?: number, pageSize?: number) => Promise<void>;
  searchJokes: (token: string, query: string) => Promise<void>;
  addJoke: (token: string, joke: Partial<Joke>) => Promise<void>;
  updateJoke: (token: string, id: string, joke: Partial<Joke>) => Promise<void>;
  deleteJoke: (token: string, id: string) => Promise<void>;
  clearError: () => void;
  setPage: (page: number) => void;
};

export const useJokesStore = create<JokesStore>((set, get) => ({
  jokes: [],
  isLoading: false,
  error: null,
  pagination: {
    page: 1,
    pageSize: 20,
    totalItems: 0,
    totalPages: 0,
  },

  fetchJokes: async (token: string, page = 1, pageSize = 20) => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch(
        `${API_BASE_URL}/api/v1/jokes?page=${page}&pageSize=${pageSize}`,
        {
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
        }
      );

      if (!response.ok) {
        throw new Error(`Failed to fetch jokes: ${response.statusText}`);
      }

      const data = await response.json();
      
      set({
        jokes: data.jokes || data.data || data,
        pagination: {
          page,
          pageSize,
          totalItems: data.totalItems || data.total || 0,
          totalPages: data.totalPages || Math.ceil((data.totalItems || data.total || 0) / pageSize),
        },
        isLoading: false,
      });
    } catch (error) {
      console.error('Fetch jokes error:', error);
      set({
        error: error instanceof Error ? error.message : 'Failed to fetch jokes',
        isLoading: false,
      });
    }
  },

  searchJokes: async (token: string, query: string) => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch(
        `${API_BASE_URL}/api/v1/jokes/search?query=${encodeURIComponent(query)}`,
        {
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
        }
      );

      if (!response.ok) {
        throw new Error(`Failed to search jokes: ${response.statusText}`);
      }

      const data = await response.json();
      
      set({
        jokes: data.jokes || data.data || data,
        isLoading: false,
      });
    } catch (error) {
      console.error('Search jokes error:', error);
      set({
        error: error instanceof Error ? error.message : 'Failed to search jokes',
        isLoading: false,
      });
    }
  },

  addJoke: async (token: string, joke: Partial<Joke>) => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch(`${API_BASE_URL}/api/v1/jokes`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(joke),
      });

      if (!response.ok) {
        throw new Error(`Failed to add joke: ${response.statusText}`);
      }

      const newJoke = await response.json();
      const { jokes } = get();
      
      set({
        jokes: [newJoke, ...jokes],
        isLoading: false,
      });
    } catch (error) {
      console.error('Add joke error:', error);
      set({
        error: error instanceof Error ? error.message : 'Failed to add joke',
        isLoading: false,
      });
    }
  },

  updateJoke: async (token: string, id: string, joke: Partial<Joke>) => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch(`${API_BASE_URL}/api/v1/jokes/${id}`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(joke),
      });

      if (!response.ok) {
        throw new Error(`Failed to update joke: ${response.statusText}`);
      }

      const updatedJoke = await response.json();
      const { jokes } = get();
      
      set({
        jokes: jokes.map(j => j.id === id ? updatedJoke : j),
        isLoading: false,
      });
    } catch (error) {
      console.error('Update joke error:', error);
      set({
        error: error instanceof Error ? error.message : 'Failed to update joke',
        isLoading: false,
      });
    }
  },

  deleteJoke: async (token: string, id: string) => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch(`${API_BASE_URL}/api/v1/jokes/${id}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error(`Failed to delete joke: ${response.statusText}`);
      }

      const { jokes } = get();
      
      set({
        jokes: jokes.filter(j => j.id !== id),
        isLoading: false,
      });
    } catch (error) {
      console.error('Delete joke error:', error);
      set({
        error: error instanceof Error ? error.message : 'Failed to delete joke',
        isLoading: false,
      });
    }
  },

  clearError: () => set({ error: null }),

  setPage: (page: number) => {
    set(state => ({
      pagination: { ...state.pagination, page }
    }));
  },
})); 