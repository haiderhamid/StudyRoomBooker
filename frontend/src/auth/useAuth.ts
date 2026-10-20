import { createContext, useContext } from 'react';
import type { User } from '../types/user';
import type { LoginDto } from '../types/auth';

export interface AuthContextType {
  user: User | null;
  token: string | null;
  isAdmin: boolean;
  isLoading: boolean;
  login: (credentials: LoginDto) => Promise<void>;
  logout: () => void;
}

// Shared login state for the whole app.
export const AuthContext = createContext<AuthContextType | undefined>(undefined);

// Shortcut, so components can write useAuth() instead of useContext(AuthContext).
export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
