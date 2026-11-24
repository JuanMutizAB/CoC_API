import DeactivateModal from "./components/DeactivateModal";
import PageHeader from "./components/PageHeader";
import { useAccountListingController } from "../controllers/useAccountListingController";
import { formatBirthDate, hasRole } from "../models/accountModel";
import "./AccountListingView.css";

/**
 * Account listing view with search, inactive filter, sorting, role badges,
 * and confirmation modal for deactivating accounts.
 */
export default function AccountListingView() {
  const {
    accounts,
    visibleAccounts,
    isLoading,
    loadError,
    isAdmin,
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
  } = useAccountListingController();

  const colCount = isAdmin ? 5 : 4;

  return (
    <main className="app-page accounts-page">
      <PageHeader title="Account Management" />

      <section
        className="content-card accounts-card"
        aria-labelledby="accounts-title"
      >
        <div className="toolbar">
          <label className="search-field">
            <span className="visually-hidden">Search accounts</span>
            <svg viewBox="0 0 24 24" aria-hidden="true">
              <circle cx="10.8" cy="10.8" r="6.8" />
              <path d="m16 16 4.5 4.5" />
            </svg>
            <input
              id="account-search"
              type="search"
              placeholder="Search"
              autoComplete="off"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </label>

          <label className="deleted-toggle">
            <input
              id="show-inactive"
              type="checkbox"
              checked={showInactive}
              onChange={(e) => setShowInactive(e.target.checked)}
            />
            <span className="checkbox-mark" aria-hidden="true" />
            <span>Show inactive</span>
          </label>

          {isAdmin && (
            <button
              className="primary-button"
              id="create-account"
              type="button"
              onClick={goToCreate}
            >
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path d="M12 5v14M5 12h14" />
              </svg>
              <span>Create Account</span>
            </button>
          )}

          <button
            className="back-button"
            id="back-to-campaigns"
            type="button"
            onClick={handleBackToCampaigns}
          >
            <svg viewBox="0 0 24 24" aria-hidden="true">
              <path d="M19 12H5m7 7-7-7 7-7" />
            </svg>
            <span>Back to campaigns</span>
          </button>
        </div>

        <div className="section-heading">
          <div>
            <h1 id="accounts-title">Accounts</h1>
            <p>Manage the accounts in your organization.</p>
          </div>
        </div>

        <div className="table-wrap">
          <table className="accounts-table">
            <thead>
              <tr>
                <th
                  id="name-heading"
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
                <th scope="col">Roles</th>
                <th
                  id="birthdate-heading"
                  scope="col"
                  aria-sort={sortField === "birthDate" ? sortDirection : "none"}
                >
                  <button
                    className="sort-button"
                    type="button"
                    onClick={() => handleSort("birthDate")}
                  >
                    Birth date{" "}
                    <span className="sort-indicator" aria-hidden="true">
                      {getSortIndicator("birthDate")}
                    </span>
                  </button>
                </th>
                <th scope="col">Email</th>
                {isAdmin && (
                  <th scope="col">
                    <span className="visually-hidden">Actions</span>
                  </th>
                )}
              </tr>
            </thead>
            <tbody id="accounts-list">
              {isLoading ? (
                <tr>
                  <td className="empty-state" colSpan={colCount}>
                    Loading accounts...
                  </td>
                </tr>
              ) : loadError ? (
                <tr>
                  <td className="empty-state" colSpan={colCount}>
                    {loadError}
                  </td>
                </tr>
              ) : visibleAccounts.length === 0 ? (
                <tr>
                  <td className="empty-state" colSpan={colCount}>
                    No accounts match your search.
                  </td>
                </tr>
              ) : (
                visibleAccounts.map((account) => {
                  const accountIsAdmin = hasRole(account.caracteristics, "admin");
                  const accountIsAprovador = hasRole(account.caracteristics, "aprovador");
                  const accountIsAprovando = hasRole(account.caracteristics, "aprovando");
                  const hasAnyRole =
                    accountIsAdmin || accountIsAprovador || accountIsAprovando;

                  return (
                    <tr key={account.id}>
                      <td>
                        <div className="account-name-cell">
                          <span className="account-name">{account.name}</span>
                          {!account.active && (
                            <span className="account-state">Inactive</span>
                          )}
                        </div>
                      </td>

                      <td className="roles-cell">
                        <div className="roles-badges">
                          {!hasAnyRole ? (
                            <span className="role-badge role-badge--none">—</span>
                          ) : (
                            <>
                              {accountIsAdmin && (
                                <span className="role-badge role-badge--admin">
                                  Admin
                                </span>
                              )}
                              {accountIsAprovador && (
                                <span className="role-badge role-badge--aprovador">
                                  Aprovador
                                </span>
                              )}
                              {accountIsAprovando && (
                                <span className="role-badge role-badge--aprovando">
                                  Aprovando
                                </span>
                              )}
                            </>
                          )}
                        </div>
                      </td>

                      <td>{formatBirthDate(account.birthDate)}</td>
                      <td className="account-email">{account.email}</td>

                      {isAdmin && (
                        <td>
                          <div className="row-actions">
                            <button
                              className="edit-button"
                              type="button"
                              aria-label={`Edit ${account.name}`}
                              onClick={() => goToEdit(account.id)}
                            >
                              Edit
                            </button>
                            <button
                              className="deactivate-button"
                              type="button"
                              disabled={!account.active}
                              aria-label={`Deactivate ${account.name}`}
                              onClick={() => {
                                if (account.active) {
                                  setPendingDeactivate(account);
                                }
                              }}
                            >
                              {account.active ? "Deactivate" : "Deactivated"}
                            </button>
                          </div>
                        </td>
                      )}
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>

        <div className="table-footer">
          <p id="results-count" role="status" aria-live="polite">
            Showing {visibleAccounts.length} of {accounts.length} accounts
          </p>
        </div>
        <p
          className={`action-message${statusType ? ` ${statusType}` : ""}`}
          id="action-message"
          role="status"
          aria-live="polite"
        >
          {statusMessage}
        </p>
      </section>

      <DeactivateModal
        account={pendingDeactivate}
        onCancel={() => setPendingDeactivate(null)}
        onConfirm={handleConfirmDeactivate}
      />
    </main>
  );
}
