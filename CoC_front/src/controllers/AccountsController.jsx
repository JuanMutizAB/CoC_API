import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
} from "react";
import {
  createAccountApi,
  deactivateAccountApi,
  fetchAccountById,
  fetchAccounts,
  updateAccountApi,
} from "../models/accountModel";
import { useAuth } from "./AuthController";

const AccountsContext = createContext(null);

/**
 * Provides account state management backed by CoC_API across MVC controllers.
 */
export function AccountsProvider({ children }) {
  const { currentUser, isAuthenticated, isAdmin, refreshSession } = useAuth();
  const [accounts, setAccounts] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [loadError, setLoadError] = useState("");
  const [flashMessage, setFlashMessage] = useState("");

  const clearFlashMessage = useCallback(() => {
    setFlashMessage("");
  }, []);

  const refreshAccounts = useCallback(async () => {
    setIsLoading(true);
    setLoadError("");
    try {
      const list = await fetchAccounts();
      setAccounts(list);
      return list;
    } catch (error) {
      setLoadError(error?.message || "Could not load accounts.");
      return [];
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (isAuthenticated && isAdmin) {
      refreshAccounts();
    } else {
      setAccounts([]);
      setLoadError("");
    }
  }, [isAuthenticated, isAdmin, refreshAccounts]);

  const getAccountById = useCallback(
    (id) => accounts.find((acc) => String(acc.id) === String(id)) || null,
    [accounts]
  );

  const fetchAccount = useCallback(async (id) => {
    const account = await fetchAccountById(id);
    if (account) {
      setAccounts((prev) => {
        const exists = prev.some((item) => String(item.id) === String(account.id));
        return exists
          ? prev.map((item) =>
              String(item.id) === String(account.id) ? account : item
            )
          : [...prev, account];
      });
    }
    return account;
  }, []);

  const createAccount = useCallback(
    async ({ name, email, birthDate, taxId, password, caracteristics }) => {
      const created = await createAccountApi({
        name,
        email,
        birthDate,
        taxId,
        password,
        caracteristics,
      });
      setAccounts((prev) => [...prev, created]);
      return created;
    },
    []
  );

  const updateAccount = useCallback(
    async (id, { name, email, password, caracteristics }) => {
      await updateAccountApi(id, {
        name,
        email,
        password: password || null,
        caracteristics,
      });
      if (currentUser && String(id) === String(currentUser.id)) {
        await refreshSession();
      }
      await refreshAccounts();
    },
    [currentUser, refreshAccounts, refreshSession]
  );

  const deactivateAccount = useCallback(
    async (id) => {
      await deactivateAccountApi(id);
      if (currentUser && String(id) === String(currentUser.id)) {
        await refreshSession();
        return;
      }
      await refreshAccounts();
    },
    [currentUser, refreshAccounts, refreshSession]
  );

  return (
    <AccountsContext.Provider
      value={{
        accounts,
        isLoading,
        loadError,
        flashMessage,
        setFlashMessage,
        clearFlashMessage,
        getAccountById,
        fetchAccount,
        refreshAccounts,
        createAccount,
        updateAccount,
        deactivateAccount,
      }}
    >
      {children}
    </AccountsContext.Provider>
  );
}

/**
 * Hook to access shared accounts controller state and actions.
 */
export function useAccounts() {
  const ctx = useContext(AccountsContext);
  if (!ctx) {
    throw new Error("useAccounts must be used within an AccountsProvider");
  }
  return ctx;
}
