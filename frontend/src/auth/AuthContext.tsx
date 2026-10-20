import { useState } from 'react';
import type { ReactNode } from 'react';
import { jwtDecode } from 'jwt-decode';
import { ROLE_CLAIM } from '../types/user';
import type { User } from '../types/user';
import type { LoginDto } from '../types/auth';
import * as AuthService from './AuthService';
import { AuthContext } from './useAuth';

interface AuthProviderProps {
  children: ReactNode;
}

// Reads the saved token and throws it away if it is expired or broken.
function getValidToken(): string | null {
  const storedToken = localStorage.getItem('token');
  if (!storedToken) {
    return null;
  }

  try {
    const decodedUser = jwtDecode<User>(storedToken);
    if (decodedUser.exp * 1000 > Date.now()) {
      return storedToken;
    }
    console.warn('Token expired');
  } catch (error) {
    console.error('Invalid token', error);
  }

  localStorage.removeItem('token');
  return null;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [token, setToken] = useState<string | null>(getValidToken);
  const [user, setUser] = useState<User | null>(token ? jwtDecode<User>(token) : null);

  const login = async (credentials: LoginDto) => {
    const receivedToken = await AuthService.login(credentials);
    localStorage.setItem('token', receivedToken);
    setUser(jwtDecode<User>(receivedToken));
    setToken(receivedToken);
  };

  const logout = () => {
    localStorage.removeItem('token');
    setUser(null);
    setToken(null);
  };

  // The role comes from the token the API made at login.
  const isAdmin = user !== null && user[ROLE_CLAIM] === 'Admin';

  return (
    <AuthContext.Provider value={{ user, token, isAdmin, isLoading: false, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}
