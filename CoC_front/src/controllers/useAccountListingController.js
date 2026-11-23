import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { normalizeCharacteristics } from "../models/accountModel";
import { useAccounts } from "./AccountsController";

/**
 * Controller hook for the Account Listing view.
 */
export function useAccountListingController() {
  const navigate = useNavigate();
  const {
    accounts,
    flashMessage,
    clearFlashMessage,
    deactivateAccount,
  } = useAccounts();

  const [search, setSearch] = useState("");
  const [showInactive, setShowInactive] = useState(false);
  const [sortField, setSortField] = useState("name");
  const [sortDirection, setSortDirection] = useState("ascending");
  const [statusMessage, setStatusMessage] = useState("");
  const [pendingDeactivate, setPendingDeactivate] = useState(null);

  useEffect(() => {
    document.title = "Account Management - CoC";
  }, []);

  useEffect(() => {
    if (flashMessage) {
      setStatusMessage(flashMessage);
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

  const handleConfirmDeactivate = () => {
    if (pendingDeactivate && pendingDeactivate.active) {
      deactivateAccount(pendingDeactivate.id);
      setStatusMessage(`${pendingDeactivate.name} was deactivated in this demo.`);
    }
    setPendingDeactivate(null);
  };

  const goToCreate = () => navigate("/accounts/create");
  const goToEdit = (id) => navigate(`/accounts/${id}/edit`);
  const handleBackToCampaigns = () => navigate("/campaigns/edit");

  return {
    accounts,
    visibleAccounts,
    search,
    setSearch,
    showInactive,
    setShowInactive,
    sortField,
    sortDirection,
    statusMessage,
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
