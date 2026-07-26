const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";
const API_BASE = `${API_URL}/api/v1`;

export type JokeType = "Joke" | "FunnySaying" | "Discouragement" | "SelfDeprecating";

export interface JokeAuthor {
  id: string;
  name: string;
  email: string;
}

export interface Joke {
  id: string;
  content: string | null;
  type: JokeType | null;
  createdAt: string;
  modifiedAt: string;
  tags: string[] | null;
  laughScore: number | null;
  isActive: boolean;
  author: JokeAuthor | null;
}

export interface CreateJokeRequest {
  content: string;
  type: JokeType;
  tags?: string[];
  laughScore?: number;
  isActive: boolean;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
}

export class ApiError extends Error {
  constructor(
    public status: number,
    public problem: ProblemDetails | null,
  ) {
    super(problem?.detail ?? problem?.title ?? `Request failed with status ${status}`);
  }
}

const TOKEN_KEY = "authToken";
const EMAIL_KEY = "authEmail";

function authHeaders(): HeadersInit {
  if (typeof window === "undefined") return {};
  const token = window.localStorage.getItem(TOKEN_KEY);
  return token ? { Authorization: `Bearer ${token}` } : {};
}

const listeners = new Set<() => void>();
let storageBound = false;

function notify() {
  for (const listener of listeners) listener();
}

export const auth = {
  /** useSyncExternalStore subscribe — also picks up sign-in/out in other tabs. */
  subscribe(listener: () => void) {
    if (!storageBound && typeof window !== "undefined") {
      window.addEventListener("storage", notify);
      storageBound = true;
    }
    listeners.add(listener);
    return () => {
      listeners.delete(listener);
    };
  },

  /** Email of the signed-in user, or null. Strings compare by value, so this is a stable snapshot. */
  getEmail: () =>
    typeof window === "undefined" ? null : window.localStorage.getItem(EMAIL_KEY),

  /** Nothing is signed in during SSR — localStorage only exists in the browser. */
  getServerEmail: () => null,

  async signIn(email: string, name: string) {
    const { token } = await request<{ email: string; token: string }>("/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, name }),
    });
    window.localStorage.setItem(TOKEN_KEY, token);
    window.localStorage.setItem(EMAIL_KEY, email);
    notify();
  },

  signOut() {
    window.localStorage.removeItem(TOKEN_KEY);
    window.localStorage.removeItem(EMAIL_KEY);
    notify();
  },
};

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...authHeaders(),
      ...init?.headers,
    },
  });

  if (!res.ok) {
    let problem: ProblemDetails | null = null;
    try {
      problem = await res.json();
    } catch {
      // response had no JSON body
    }
    throw new ApiError(res.status, problem);
  }

  if (res.status === 204) {
    return undefined as T;
  }

  return res.json() as Promise<T>;
}

export const jokesApi = {
  getRandom: () => request<Joke>("/jokes/random"),
  getAll: () => request<Joke[]>("/jokes"),
  getById: (id: string) => request<Joke>(`/jokes/${id}`),
  search: (query: string) => request<Joke[]>(`/jokes/search?q=${encodeURIComponent(query)}`),
  create: (data: CreateJokeRequest) =>
    request<Joke>("/jokes", {
      method: "POST",
      body: JSON.stringify(data),
    }),
  update: (id: string, data: CreateJokeRequest) =>
    request<Joke>(`/jokes/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    }),
  remove: (id: string) =>
    request<void>(`/jokes/${id}`, {
      method: "DELETE",
    }),
};
