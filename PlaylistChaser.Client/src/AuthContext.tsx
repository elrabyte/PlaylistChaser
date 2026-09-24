import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
import { api, getToken, setToken, type AuthResponse } from "./api";

interface AuthState {
  userName: string | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [userName, setUserName] = useState<string | null>(() => localStorage.getItem("playlistchaser.userName"));

  function applyAuth(auth: AuthResponse) {
    setToken(auth.token);
    localStorage.setItem("playlistchaser.userName", auth.userName);
    setUserName(auth.userName);
  }

  const value = useMemo<AuthState>(
    () => ({
      userName,
      isAuthenticated: !!getToken(),
      login: async (email, password) => applyAuth(await api.login(email, password)),
      register: async (email, password) => applyAuth(await api.register(email, password)),
      logout: () => {
        setToken(null);
        localStorage.removeItem("playlistchaser.userName");
        setUserName(null);
      },
    }),
    [userName],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}
