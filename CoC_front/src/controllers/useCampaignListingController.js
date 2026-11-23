import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  CampaignStatus,
  CampaignType,
  loadStoredCampaigns,
  saveStoredCampaign,
} from "../models/campaignModel";

/**
 * Controller hook for the Campaign Listing view.
 * Handles search, status/type/date filtering, sorting, and navigation to edit/create.
 */
export function useCampaignListingController() {
  const navigate = useNavigate();

  const [campaigns, setCampaigns] = useState(() => loadStoredCampaigns());

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
          const matchName = String(camp.name || "").toLocaleLowerCase().includes(query);
          const matchDesc = String(camp.description || "").toLocaleLowerCase().includes(query);
          if (!matchName && !matchDesc) return false;
        }

        // Status filter
        if (appliedFilters.status && appliedFilters.status !== "All") {
          if (String(camp.status).toLowerCase() !== appliedFilters.status.toLowerCase()) {
            return false;
          }
        }

        // Type filter
        if (appliedFilters.type && appliedFilters.type !== "All") {
          if (String(camp.type).toLowerCase() !== appliedFilters.type.toLowerCase()) {
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
        const leftVal = String(left[sortField] ?? "");
        const rightVal = String(right[sortField] ?? "");
        const comparison = leftVal.localeCompare(rightVal, "pt-PT", { numeric: true });
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
    navigate("/campaigns/edit");
  };

  const goToEdit = (campaign) => {
    // Persist current campaign in sessionStorage for EditCampaignView
    saveStoredCampaign({
      id: campaign.id,
      name: campaign.name,
      description: campaign.description,
      text: campaign.modelMsg || campaign.text || "",
      image: campaign.imageUrl || null,
      publishDate: campaign.publicationDate ? campaign.publicationDate.split("T")[0] : "",
      publishTime: campaign.publicationDate && campaign.publicationDate.includes("T")
        ? campaign.publicationDate.split("T")[1].slice(0, 5)
        : "",
      status: campaign.status,
      type: campaign.type,
    });
    navigate(`/campaigns/edit`);
  };

  const goToAccounts = () => {
    navigate("/accounts");
  };

  return {
    campaigns,
    visibleCampaigns,
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
