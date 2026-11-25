export const API_URL = import.meta.env.VITE_API_URL;

export const headers = {
  'Content-Type': 'application/json',
};

// Adds the token to the request, so the API knows who is logged in.
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

// An error from the API, with the HTTP status code, so callers can tell a 404 from a server error.
export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

// Ids in the URL must be whole numbers. Anything else can't be a room, building or booking.
export function isValidId(id: string | undefined): boolean {
  return !!id && /^\d+$/.test(id);
}

// Turns error replies from the API into messages the user can read.
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