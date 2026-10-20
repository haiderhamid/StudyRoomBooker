import type { LoginDto, RegisterDto } from '../types/auth';
import { API_URL, headers, handleResponse } from '../shared/apiHelpers';

// Thrown when no account uses the username or email, so the login page can link to register.
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
  // 401 here means a wrong password, not that the user has to log in again.
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