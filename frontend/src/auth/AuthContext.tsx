import { createContext, useContext, useEffect, useState } from 'react';
import type { ReactNode } from 'react';
import { jwtDecode } from 'jwt-decode';
import { ROLE_CLAIM } from '../types/user';
import type { User } from '../types/user';
import type { LoginDto } from '../types/auth';
import * as AuthService from './AuthService';

interface AuthContextType {
  user: User | null;
  token: string | null;
  isAdmin: boolean;
  isLoading: boolean;
  login: (credentials: LoginDto) => Promise<void>;
  logout: () => void;
}

interface AuthProviderProps {
  children: ReactNode;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: AuthProviderProps) {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(localStorage.getItem('token'));
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    if (token) {
      try {
        const decodedUser = jwtDecode<User>(token);
        if (decodedUser.exp * 1000 > Date.now()) {
          setUser(decodedUser);
        } else {
          console.warn('Token expired');
          localStorage.removeItem('token');
          setUser(null);
          setToken(null);
        }
      } catch (error) {
        console.error('Invalid token', error);
        localStorage.removeItem('token');
        setUser(null);
        setToken(null);
      }
    }
    setIsLoading(false);
  }, [token]);

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

  const isAdmin = user !== null && user[ROLE_CLAIM] === 'Admin';

  return (
    <AuthContext.Provider value={{ user, token, isAdmin, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
