import type { LoginDto, RegisterDto } from '../types/auth';
import { API_URL, headers, handleResponse } from '../shared/apiHelpers';

export async function login(credentials: LoginDto): Promise<string> {
  const response = await fetch(`${API_URL}/api/auth/login`, {
    method: 'POST',
    headers,
    body: JSON.stringify(credentials),
  });
  // 401 here means wrong login details, not that the user has to log in again.
  if (response.status === 401) {
    throw new Error('Wrong username/email or password. Please try again.');
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