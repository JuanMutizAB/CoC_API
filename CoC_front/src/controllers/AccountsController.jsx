import { createContext, useCallback, useContext, useState } from "react";
import {
  hasRole,
  loadStoredAccounts,
  normalizeCharacteristics,
  saveStoredAccounts,
} from "../models/accountModel";

const AccountsContext = createContext(null);

/**
 * Provides account state management and demo persistence across MVC controllers.
 */
export function AccountsProvider({ children }) {
  const [accounts, setAccounts] = useState(() => loadStoredAccounts());
  const [flashMessage, setFlashMessage] = useState("");

  const clearFlashMessage = useCallback(() => {
    setFlashMessage("");
  }, []);

  const getAccountById = useCallback(
    (id) => accounts.find((acc) => String(acc.id) === String(id)) || null,
    [accounts]
  );

  const createAccount = useCallback(
    ({ name, email, birthDate, taxId, password, caracteristics }) => {
      const nextId =
        accounts.length > 0
          ? Math.max(...accounts.map((a) => Number(a.id) || 0)) + 1
          : 1;

      const normalized = normalizeCharacteristics(caracteristics);
      if (!hasRole(normalized, "isActive")) {
        normalized.unshift("isActive");
      }

      const newAccount = {
        id: nextId,
        name,
        email,
        birthDate,
        taxId,
        password,
        active: true,
        caracteristics: normalized,
      };

      const nextList = [...accounts, newAccount];
      setAccounts(nextList);
      saveStoredAccounts(nextList);
      return newAccount;
    },
    [accounts]
  );

  const updateAccount = useCallback(
    (id, { name, email, password, caracteristics }) => {
      const nextList = accounts.map((acc) => {
        if (String(acc.id) !== String(id)) {
          return acc;
        }

        const updatedChars = normalizeCharacteristics(caracteristics);
        const hadIsActive = hasRole(acc.caracteristics, "isActive");
        if ((hadIsActive || acc.active) && !hasRole(updatedChars, "isActive")) {
          updatedChars.push("isActive");
        }

        const updated = {
          ...acc,
          name,
          email,
          caracteristics: updatedChars,
        };

        if (password) {
          updated.password = password;
        }

        return updated;
      });

      setAccounts(nextList);
      saveStoredAccounts(nextList);
    },
    [accounts]
  );

  const deactivateAccount = useCallback(
    (id) => {
      const nextList = accounts.map((acc) =>
        String(acc.id) === String(id) ? { ...acc, active: false } : acc
      );
      setAccounts(nextList);
      saveStoredAccounts(nextList);
    },
    [accounts]
  );

  return (
    <AccountsContext.Provider
      value={{
        accounts,
        flashMessage,
        setFlashMessage,
        clearFlashMessage,
        getAccountById,
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
