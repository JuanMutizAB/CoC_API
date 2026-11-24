import { useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import {
  CampaignStatus,
  CampaignType,
  fetchCampaigns,
  normalizeCampaignTypes,
} from "../models/campaignModel";
import { useAuth } from "./AuthController";

/**
 * Controller hook for the Campaign Listing view.
 * Loads campaigns from CoC_API and handles filtering, sorting, and navigation.
 */
export function useCampaignListingController() {
  const navigate = useNavigate();
  const location = useLocation();
  const { isAdmin } = useAuth();

  const [campaigns, setCampaigns] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [statusMessage, setStatusMessage] = useState("");

  // Form input filter values
  const [nameFilter, setNameFilter] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [typeFilter, setTypeFilter] = useState("");
  const [dateFilter, setDateFilter] = useState("");

  // Applied filter criteria (applied when user clicks "Filter" or submits)
  const [appliedFilters, setAppliedFilters] = useState({
    name: "",
    status: "",
    type: "",
    date: "",
  });

  // Sorting
  const [sortField, setSortField] = useState("createdAt");
  const [sortDirection, setSortDirection] = useState("descending");

  useEffect(() => {
    document.title = "Campaigns - CoC";

    if (location.state?.flashMessage) {
      setStatusMessage(location.state.flashMessage);
      navigate(location.pathname, { replace: true, state: null });
    }
  }, [location.pathname, location.state, navigate]);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setLoadError("");

    fetchCampaigns()
      .then((list) => {
        if (!cancelled) {
          setCampaigns(list);
        }
      })
      .catch((error) => {
        if (!cancelled) {
          setLoadError(error?.message || "Could not load campaigns.");
        }
      })
      .finally(() => {
        if (!cancelled) {
          setIsLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const handleApplyFilter = (e) => {
    if (e && typeof e.preventDefault === "function") {
      e.preventDefault();
    }
    setAppliedFilters({
      name: nameFilter.trim(),
      status: statusFilter,
      type: typeFilter,
      date: dateFilter,
    });
  };

  const handleCleanSearch = () => {
    setNameFilter("");
    setStatusFilter("");
    setTypeFilter("");
    setDateFilter("");
    setAppliedFilters({
      name: "",
      status: "",
      type: "",
      date: "",
    });
  };

  const visibleCampaigns = useMemo(() => {
    return campaigns
      .filter((camp) => {
        // Name filter
        if (appliedFilters.name) {
          const query = appliedFilters.name.toLocaleLowerCase();
          const matchName = String(camp.name || "")
            .toLocaleLowerCase()
            .includes(query);
          const matchDesc = String(camp.description || "")
            .toLocaleLowerCase()
            .includes(query);
          if (!matchName && !matchDesc) return false;
        }

        // Status filter
        if (appliedFilters.status && appliedFilters.status !== "All") {
          if (
            String(camp.status).toLowerCase() !==
            appliedFilters.status.toLowerCase()
          ) {
            return false;
          }
        }

        // Type filter
        if (appliedFilters.type && appliedFilters.type !== "All") {
          const campTypes = normalizeCampaignTypes(camp.types ?? camp.type);
          const matchesType = campTypes.some(
            (t) => t.toLowerCase() === appliedFilters.type.toLowerCase()
          );
          if (!matchesType) {
            return false;
          }
        }

        // Date filter (matches YYYY-MM-DD from createdAt)
        if (appliedFilters.date) {
          const campDateStr = String(camp.createdAt || "").split("T")[0];
          if (campDateStr !== appliedFilters.date) {
            return false;
          }
        }

        return true;
      })
      .sort((left, right) => {
        if (sortField === "createdAt" || sortField === "publicationDate") {
          const leftTime = Date.parse(left[sortField] || "") || 0;
          const rightTime = Date.parse(right[sortField] || "") || 0;
          const comparison = leftTime - rightTime;
          return sortDirection === "ascending" ? comparison : -comparison;
        }

        const leftVal = String(left[sortField] ?? "");
        const rightVal = String(right[sortField] ?? "");
        const comparison = leftVal.localeCompare(rightVal, "pt-PT", {
          numeric: true,
        });
        return sortDirection === "ascending" ? comparison : -comparison;
      });
  }, [campaigns, appliedFilters, sortField, sortDirection]);

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

  const goToCreate = () => {
    navigate("/campaigns/create");
  };

  const goToEdit = (campaign) => {
    if (
      campaign.status === CampaignStatus.Canceled ||
      campaign.status === CampaignStatus.Closed
    ) {
      navigate(`/campaigns/${campaign.id}/details`);
    } else {
      navigate(`/campaigns/${campaign.id}/edit`);
    }
  };

  const goToAccounts = () => {
    navigate("/accounts");
  };

  return {
    campaigns,
    visibleCampaigns,
    isAdmin,
    isLoading,
    loadError,
    statusMessage,
    nameFilter,
    setNameFilter,
    statusFilter,
    setStatusFilter,
    typeFilter,
    setTypeFilter,
    dateFilter,
    setDateFilter,
    handleApplyFilter,
    handleCleanSearch,
    sortField,
    sortDirection,
    handleSort,
    getSortIndicator,
    goToCreate,
    goToEdit,
    goToAccounts,
    statusOptions: Object.values(CampaignStatus),
    typeOptions: Object.values(CampaignType),
  };
}
