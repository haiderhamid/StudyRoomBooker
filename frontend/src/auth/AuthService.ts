import type { LoginDto, RegisterDto } from '../types/auth';
import { API_URL, headers, handleResponse } from '../shared/apiHelpers';

// So the login page can show a link to register
export class NotRegisteredError extends Error {}

export async function login(credentials: LoginDto): Promise<string> {
  const response = await fetch(`${API_URL}/api/auth/login`, {
    method: 'POST',
    headers,
    body: JSON.stringify(credentials),
  });
  if (response.status === 404) {
    throw new NotRegisteredError('This username or email is not registered.');
  }
  // 401 here means wrong password, not that the session expired
  if (response.status === 401) {
    throw new Error('Wrong password. Please try again.');
  }
  const data = await handleResponse<{ token: string }>(response);
  return data.token;
}

export async function register(userData: RegisterDto): Promise<void> {
  const response = await fetch(`${API_URL}/api/auth/register`, {
    method: 'POST',
    headers,
    body: JSON.stringify(userData),
  });
  await handleResponse<{ message: string }>(response);
}