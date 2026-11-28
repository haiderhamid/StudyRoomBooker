export const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5246';

export const headers = {
  'Content-Type': 'application/json',
};

export function getAuthHeaders(): Record<string, string> {
  const token = localStorage.getItem('token');
  const authHeaders: Record<string, string> = {
    'Content-Type': 'application/json',
  };
  if (token) {
    authHeaders['Authorization'] = `Bearer ${token}`;
  }
  return authHeaders;
}

// Keeps the status code, so a 404 can be told apart from a 500
export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

// Turns API errors into a message the user can read
export async function handleResponse<T>(response: Response): Promise<T> {
  if (response.ok) {
    return response.json() as Promise<T>;
  }

  if (response.status === 401) {
    throw new ApiError('You must be logged in to do this. Please log in again.', response.status);
  }
  if (response.status === 403) {
    throw new ApiError('You do not have permission to do this.', response.status);
  }

  const contentType = response.headers.get('content-type') ?? '';
  if (contentType.includes('json')) {
    const problem = await response.json();
    if (problem.errors) {
      throw new ApiError(Object.values(problem.errors).flat().join(' '), response.status);
    }
    throw new ApiError(problem.title ?? 'Request failed', response.status);
  }

  const text = await response.text();
  if (text) {
    throw new ApiError(text, response.status);
  }
  throw new ApiError(`Request failed with status ${response.status}`, response.status);
}

export async function handleEmptyResponse(response: Response): Promise<void> {
  if (!response.ok) {
    await handleResponse<void>(response);
  }
}