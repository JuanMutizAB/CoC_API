import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { normalizeCharacteristics } from "../models/accountModel";
import { useAccounts } from "./AccountsController";
import { useAuth } from "./AuthController";

/**
 * Controller hook for the Account Listing view.
 */
export function useAccountListingController() {
  const navigate = useNavigate();
  const { currentUser, isAdmin } = useAuth();
  const {
    accounts,
    isLoading,
    loadError,
    flashMessage,
    clearFlashMessage,
    refreshAccounts,
    deactivateAccount,
  } = useAccounts();

  const [search, setSearch] = useState("");
  const [showInactive, setShowInactive] = useState(false);
  const [sortField, setSortField] = useState("name");
  const [sortDirection, setSortDirection] = useState("ascending");
  const [statusMessage, setStatusMessage] = useState("");
  const [statusType, setStatusType] = useState("");
  const [pendingDeactivate, setPendingDeactivate] = useState(null);

  useEffect(() => {
    document.title = "Account Management - CoC";
    refreshAccounts();
  }, [refreshAccounts]);

  useEffect(() => {
    if (flashMessage) {
      setStatusMessage(flashMessage);
      setStatusType("success");
      clearFlashMessage();
    }
  }, [flashMessage, clearFlashMessage]);

  const visibleAccounts = useMemo(() => {
    const query = search.trim().toLocaleLowerCase();

    return accounts
      .filter((account) => showInactive || account.active)
      .filter((account) => {
        const rolesStr = normalizeCharacteristics(account.caracteristics).join(" ");
        return `${account.name} ${account.email} ${rolesStr}`
          .toLocaleLowerCase()
          .includes(query);
      })
      .sort((left, right) => {
        const leftVal = String(left[sortField] ?? "");
        const rightVal = String(right[sortField] ?? "");
        const comparison = leftVal.localeCompare(rightVal, "pt-PT");
        return sortDirection === "ascending" ? comparison : -comparison;
      });
  }, [accounts, search, showInactive, sortField, sortDirection]);

  const handleSort = (field) => {
    if (sortField === field) {
      setSortDirection((prev) =>
        prev === "ascending" ? "descending" : "ascending"
      );
    } else {
      setSortField(field);
      setSortDirection("ascending");
    }
  };

  const getSortIndicator = (field) => {
    if (sortField !== field) {
      return "↕";
    }
    return sortDirection === "ascending" ? "↑" : "↓";
  };

  const handleConfirmDeactivate = async () => {
    if (!pendingDeactivate || !pendingDeactivate.active) {
      setPendingDeactivate(null);
      return;
    }

    if (
      currentUser &&
      String(pendingDeactivate.id) === String(currentUser.id)
    ) {
      setPendingDeactivate(null);
      setStatusType("error");
      setStatusMessage("You cannot deactivate your own account.");
      return;
    }

    const target = pendingDeactivate;
    setPendingDeactivate(null);

    try {
      await deactivateAccount(target.id);
      setStatusType("success");
      setStatusMessage(`${target.name} was deactivated.`);
    } catch (error) {
      setStatusType("error");
      setStatusMessage(error?.message || "Could not deactivate account.");
    }
  };

  const goToCreate = () => navigate("/accounts/create");
  const goToEdit = (id) => navigate(`/accounts/${id}/edit`);
  const handleBackToCampaigns = () => navigate("/campaigns");

  return {
    accounts,
    visibleAccounts,
    isLoading,
    loadError,
    isAdmin,
    currentUserId: currentUser?.id ?? null,
    search,
    setSearch,
    showInactive,
    setShowInactive,
    sortField,
    sortDirection,
    statusMessage,
    statusType,
    pendingDeactivate,
    setPendingDeactivate,
    handleSort,
    getSortIndicator,
    handleConfirmDeactivate,
    goToCreate,
    goToEdit,
    handleBackToCampaigns,
  };
}
