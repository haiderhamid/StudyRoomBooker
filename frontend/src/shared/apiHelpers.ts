export const API_URL = import.meta.env.VITE_API_URL;

export const headers = {
  'Content-Type': 'application/json',
};

export async function handleResponse<T>(response: Response): Promise<T> {
  if (response.ok) {
    return response.json() as Promise<T>;
  }

  const contentType = response.headers.get('content-type') ?? '';
  if (contentType.includes('json')) {
    const problem = await response.json();
    if (problem.errors) {
      throw new Error(Object.values(problem.errors).flat().join(' '));
    }
    throw new Error(problem.title ?? 'Request failed');
  }

  const text = await response.text();
  throw new Error(text || `Request failed with status ${response.status}`);
}

export async function handleEmptyResponse(response: Response): Promise<void> {
  if (!response.ok) {
    await handleResponse<void>(response);
  }
}