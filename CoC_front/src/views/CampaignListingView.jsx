import PageHeader from "./components/PageHeader";
import { useCampaignListingController } from "../controllers/useCampaignListingController";
import {
  formatCampaignDate,
  formatCampaignDateTime,
  formatCampaignStatusLabel,
  normalizeCampaignTypes,
} from "../models/campaignModel";
import "./CampaignListingView.css";

/**
 * Campaign listing view with filter bar, search, column sorting,
 * status pills, and clickable campaign links to edit/details.
 */
export default function CampaignListingView() {
  const {
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
    handleRefresh,
    sortField,
    sortDirection,
    handleSort,
    getSortIndicator,
    goToCreate,
    goToEdit,
    goToAccounts,
    statusOptions,
    typeOptions,
  } = useCampaignListingController();

  return (
    <main className="app-page campaigns-page">
      <PageHeader title="Campaigns" />

      <section
        className="content-card campaigns-card"
        aria-labelledby="campaigns-title"
      >
        <div className="section-heading campaigns-heading">
          <div>
            <h1 id="campaigns-title">Campaigns list</h1>
          </div>
          {isAdmin && (
            <div className="heading-actions">
              <button
                className="back-button"
                type="button"
                onClick={goToAccounts}
              >
                <svg viewBox="0 0 24 24" aria-hidden="true">
                  <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                  <circle cx="9" cy="7" r="4" />
                  <path d="M23 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" />
                </svg>
                <span>Accounts</span>
              </button>
            </div>
          )}
        </div>

        {/* Filter Section */}
        <form
          className="campaigns-filter-form"
          onSubmit={handleApplyFilter}
        >
          <div className="filter-grid">
            <div className="filter-group">
              <label htmlFor="filter-name">Name</label>
              <input
                id="filter-name"
                type="text"
                placeholder="Search by name"
                value={nameFilter}
                onChange={(e) => setNameFilter(e.target.value)}
              />
            </div>

            <div className="filter-group">
              <label htmlFor="filter-status">Status</label>
              <select
                id="filter-status"
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value)}
              >
                <option value="">Select</option>
                {statusOptions.map((st) => (
                  <option key={st} value={st}>
                    {st}
                  </option>
                ))}
              </select>
            </div>

            <div className="filter-group">
              <label htmlFor="filter-type">Type</label>
              <select
                id="filter-type"
                value={typeFilter}
                onChange={(e) => setTypeFilter(e.target.value)}
              >
                <option value="">Select</option>
                {typeOptions.map((tp) => (
                  <option key={tp} value={tp}>
                    {tp}
                  </option>
                ))}
              </select>
            </div>

            <div className="filter-group">
              <label htmlFor="filter-date">Created At</label>
              <input
                id="filter-date"
                type="date"
                value={dateFilter}
                onChange={(e) => setDateFilter(e.target.value)}
              />
            </div>
          </div>

          <div className="filter-actions-row">
            <button className="filter-submit-btn" type="submit">
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <polyline points="20 6 9 17 4 12" />
              </svg>
              <span>Filter</span>
            </button>
          </div>
        </form>

        {/* Action bar below filters */}
        <div className="campaigns-toolbar">
          <div className="toolbar-left">
            <button
              className="primary-button create-campaign-btn"
              type="button"
              onClick={goToCreate}
            >
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path d="M12 5v14M5 12h14" />
              </svg>
              <span>Create Campaign</span>
            </button>
          </div>

          <div className="toolbar-right">
            <button
              className="refresh-btn"
              type="button"
              onClick={handleRefresh}
              disabled={isLoading}
              aria-label="Refresh campaigns"
              title="Refresh campaigns"
            >
              <svg
                className={isLoading ? "refresh-icon spinning" : "refresh-icon"}
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path d="M21 12a9 9 0 1 1-9-9c2.52 0 4.93 1 6.74 2.74L21 8" />
                <path d="M21 3v5h-5" />
              </svg>
              <span>Refresh</span>
            </button>
            <button
              className="secondary-btn clean-search-btn"
              type="button"
              onClick={handleCleanSearch}
            >
              Clean Search
            </button>
            <button
              className="search-btn"
              type="button"
              onClick={handleApplyFilter}
            >
              Search
            </button>
          </div>
        </div>

        {/* Campaigns Table */}
        <div className="table-wrap">
          <table className="campaigns-table">
            <thead>
              <tr>
                <th
                  scope="col"
                  aria-sort={sortField === "name" ? sortDirection : "none"}
                >
                  <button
                    className="sort-button"
                    type="button"
                    onClick={() => handleSort("name")}
                  >
                    Name{" "}
                    <span className="sort-indicator" aria-hidden="true">
                      {getSortIndicator("name")}
                    </span>
                  </button>
                </th>
                <th
                  scope="col"
                  aria-sort={sortField === "description" ? sortDirection : "none"}
                >
                  <button
                    className="sort-button"
                    type="button"
                    onClick={() => handleSort("description")}
                  >
                    Description{" "}
                    <span className="sort-indicator" aria-hidden="true">
                      {getSortIndicator("description")}
                    </span>
                  </button>
                </th>
                <th
                  scope="col"
                  aria-sort={sortField === "createdAt" ? sortDirection : "none"}
                >
                  <button
                    className="sort-button"
                    type="button"
                    onClick={() => handleSort("createdAt")}
                  >
                    Created At{" "}
                    <span className="sort-indicator" aria-hidden="true">
                      {getSortIndicator("createdAt")}
                    </span>
                  </button>
                </th>
                <th
                  scope="col"
                  aria-sort={
                    sortField === "publicationDate" ? sortDirection : "none"
                  }
                >
                  <button
                    className="sort-button"
                    type="button"
                    onClick={() => handleSort("publicationDate")}
                  >
                    Publication Date{" "}
                    <span className="sort-indicator" aria-hidden="true">
                      {getSortIndicator("publicationDate")}
                    </span>
                  </button>
                </th>
                <th
                  scope="col"
                  aria-sort={sortField === "status" ? sortDirection : "none"}
                >
                  <button
                    className="sort-button"
                    type="button"
                    onClick={() => handleSort("status")}
                  >
                    Status{" "}
                    <span className="sort-indicator" aria-hidden="true">
                      {getSortIndicator("status")}
                    </span>
                  </button>
                </th>
                <th
                  scope="col"
                  aria-sort={sortField === "type" ? sortDirection : "none"}
                >
                  <button
                    className="sort-button"
                    type="button"
                    onClick={() => handleSort("type")}
                  >
                    Type{" "}
                    <span className="sort-indicator" aria-hidden="true">
                      {getSortIndicator("type")}
                    </span>
                  </button>
                </th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="empty-state" colSpan={6}>
                    Loading campaigns...
                  </td>
                </tr>
              ) : loadError ? (
                <tr>
                  <td className="empty-state" colSpan={6}>
                    {loadError}
                  </td>
                </tr>
              ) : visibleCampaigns.length === 0 ? (
                <tr>
                  <td className="empty-state" colSpan={6}>
                    No campaigns match your search.
                  </td>
                </tr>
              ) : (
                visibleCampaigns.map((camp) => {
                  const statusClass = `status-pill status-pill--${String(
                    camp.status || ""
                  ).toLowerCase()}`;

                  return (
                    <tr key={camp.id}>
                      <td className="campaign-name-cell">
                        <button
                          type="button"
                          className="campaign-link"
                          onClick={() => goToEdit(camp)}
                          title={`Edit ${camp.name}`}
                        >
                          {camp.name}
                        </button>
                      </td>
                      <td className="campaign-desc-cell">
                        <span className="campaign-desc-text" title={camp.description}>
                          {camp.description || "—"}
                        </span>
                      </td>
                      <td className="campaign-date-cell">
                        {formatCampaignDate(camp.createdAt)}
                      </td>
                      <td className="campaign-date-cell">
                        {formatCampaignDateTime(camp.publicationDate)}
                      </td>
                      <td className="campaign-status-cell">
                        <span className={statusClass}>
                          {formatCampaignStatusLabel(camp.status, camp.publicationDate)}
                        </span>
                      </td>
                      <td className="campaign-type-cell">
                        <div className="type-badges">
                          {normalizeCampaignTypes(camp.types ?? camp.type).map((t) => (
                            <span key={t} className="type-badge">
                              {t}
                            </span>
                          ))}
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
        <p className="action-message" id="action-message" role="status" aria-live="polite">
          {statusMessage}
        </p>
      </section>
    </main>
  );
}
