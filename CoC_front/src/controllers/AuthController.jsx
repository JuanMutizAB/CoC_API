import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { hasRole, normalizeCharacteristics } from "../models/accountModel";
import { apiRequest, setUnauthorizedHandler } from "../models/apiClient";

const AuthContext = createContext(null);

function normalizeSessionUser(raw) {
  if (!raw) return null;
  return {
    id: raw.id ?? raw.Id,
    name: raw.name ?? raw.Name ?? "",
    email: raw.email ?? raw.Email ?? "",
    caracteristics: normalizeCharacteristics(
      raw.caracteristics ?? raw.Caracteristics
    ),
  };
}

/**
 * Provides authenticated user session state and role helpers backed by CoC_API cookies.
 */
export function AuthProvider({ children }) {
  const [currentUser, setCurrentUser] = useState(null);
  const [isAuthLoading, setIsAuthLoading] = useState(true);

  useEffect(() => {
    setUnauthorizedHandler(() => {
      setCurrentUser(null);
    });
    return () => setUnauthorizedHandler(null);
  }, []);

  const refreshSession = useCallback(async () => {
    try {
      const me = await apiRequest("/me", { skipUnauthorizedHandler: true });
      const normalized = normalizeSessionUser(me);
      setCurrentUser(normalized);
      return normalized;
    } catch {
      setCurrentUser(null);
      return null;
    } finally {
      setIsAuthLoading(false);
    }
  }, []);

  useEffect(() => {
    refreshSession();
  }, [refreshSession]);

  const login = useCallback(async (email, password) => {
    const data = await apiRequest("/login", {
      method: "POST",
      body: { email, password },
      skipUnauthorizedHandler: true,
    });
    const normalized = normalizeSessionUser(data);
    setCurrentUser(normalized);
    return normalized;
  }, []);

  const logout = useCallback(async () => {
    try {
      await apiRequest("/logout", {
        method: "POST",
        skipUnauthorizedHandler: true,
      });
    } finally {
      setCurrentUser(null);
    }
  }, []);

  const value = useMemo(() => {
    const roles = currentUser?.caracteristics ?? [];
    return {
      currentUser,
      isAuthenticated: Boolean(currentUser),
      isAuthLoading,
      isAdmin: hasRole(roles, "admin"),
      isAprovador: hasRole(roles, "aprovador"),
      isAprovando: hasRole(roles, "aprovando"),
      login,
      logout,
      refreshSession,
    };
  }, [currentUser, isAuthLoading, login, logout, refreshSession]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/**
 * Hook to access current authentication session, user roles, and login/logout actions.
 */
export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return ctx;
}
