import PageHeader from "./components/PageHeader";
import PasswordField from "./components/PasswordField";
import { useEditUserController } from "../controllers/useEditUserController";
import "./EditUserView.css";

/**
 * Edit user view supporting name, email, optional password change,
 * pre-populated role checkboxes, and permanent Admin lock once granted.
 */
export default function EditUserView() {
  const {
    id,
    user,
    isLoading,
    loadError,
    name,
    setName,
    email,
    setEmail,
    password,
    setPassword,
    isAdmin,
    setIsAdmin,
    isAprovando,
    setIsAprovando,
    isAprovador,
    setIsAprovador,
    isOriginalAdmin,
    errors,
    actionMessage,
    actionType,
    isSubmitting,
    handleSubmit,
    goBackToAccounts,
  } = useEditUserController();

  if (isLoading) {
    return (
      <main className="app-page edit-user-page">
        <PageHeader title="Edit User" />
        <section
          className="content-card edit-user-card"
          aria-labelledby="edit-user-title"
        >
          <div className="section-heading">
            <div>
              <h1 id="edit-user-title">Edit User</h1>
              <p>Loading user details...</p>
            </div>
          </div>
        </section>
      </main>
    );
  }

  if (!user) {
    return (
      <main className="app-page edit-user-page">
        <PageHeader title="Edit User" />
        <section
          className="content-card edit-user-card"
          aria-labelledby="edit-user-title"
        >
          <div className="toolbar">
            <button
              className="back-button"
              id="back-to-accounts"
              type="button"
              onClick={goBackToAccounts}
            >
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path d="M19 12H5m7 7-7-7 7-7" />
              </svg>
              <span>Back to accounts</span>
            </button>
          </div>
          <div className="section-heading">
            <div>
              <h1 id="edit-user-title">User not found</h1>
              <p>{loadError || `The requested account (ID ${id}) could not be found.`}</p>
            </div>
          </div>
        </section>
      </main>
    );
  }

  return (
    <main className="app-page edit-user-page">
      <PageHeader title="Edit User" />

      <section
        className="content-card edit-user-card"
        aria-labelledby="edit-user-title"
      >
        <div className="toolbar">
          <button
            className="back-button"
            id="back-to-accounts"
            type="button"
            onClick={goBackToAccounts}
          >
            <svg viewBox="0 0 24 24" aria-hidden="true">
              <path d="M19 12H5m7 7-7-7 7-7" />
            </svg>
            <span>Back to accounts</span>
          </button>
        </div>

        <div className="section-heading">
          <div>
            <h1 id="edit-user-title">Edit User</h1>
            <p id="edit-user-subtitle">
              Editing {user.name} (ID {user.id})
            </p>
          </div>
        </div>

        <form
          id="edit-user-form"
          className="management-form edit-user-form"
          onSubmit={handleSubmit}
          noValidate
        >
          <div className="form-grid">
            <div className="form-group">
              <label htmlFor="user-name">Name</label>
              <input
                id="user-name"
                type="text"
                placeholder="Full name"
                autoComplete="name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                className={errors.name ? "invalid" : undefined}
                required
              />
              <span className="error-message" id="name-error">
                {errors.name}
              </span>
            </div>

            <div className="form-group">
              <label htmlFor="user-email">Email</label>
              <input
                id="user-email"
                type="email"
                placeholder="user@example.com"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className={errors.email ? "invalid" : undefined}
                required
              />
              <span className="error-message" id="email-error">
                {errors.email}
              </span>
            </div>

            <div className="form-group form-group--full">
              <label htmlFor="user-password">Password</label>
              <PasswordField
                id="user-password"
                name="password"
                placeholder="Leave blank to keep current password"
                autoComplete="new-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                invalid={Boolean(errors.password)}
              />
              <span className="field-hint">
                Leave blank to keep current password. To change: min. 9 chars, with uppercase, lowercase, digit and symbol.
              </span>
              <span className="error-message" id="password-error">
                {errors.password}
              </span>
            </div>
          </div>

          <div className="roles-section">
            <span className="roles-title">Roles &amp; Characteristics</span>
            <div className="characteristics-grid">
              <label
                className={`characteristic-toggle${isOriginalAdmin ? " is-locked" : ""}`}
                title={
                  isOriginalAdmin
                    ? "Admin status cannot be removed once granted"
                    : undefined
                }
              >
                <input
                  type="checkbox"
                  id="role-admin"
                  value="Admin"
                  checked={isOriginalAdmin || isAdmin}
                  disabled={isOriginalAdmin}
                  onChange={(e) => {
                    if (!isOriginalAdmin) {
                      setIsAdmin(e.target.checked);
                    }
                  }}
                />
                <span className="checkbox-mark" aria-hidden="true" />
                <span className="characteristic-label">Admin</span>
                {isOriginalAdmin && (
                  <span
                    className="locked-badge"
                    id="admin-locked-badge"
                    aria-hidden="true"
                  >
                    Locked
                  </span>
                )}
              </label>

              <label className="characteristic-toggle">
                <input
                  type="checkbox"
                  id="role-aprovando"
                  value="isAprovando"
                  checked={isAprovando}
                  onChange={(e) => setIsAprovando(e.target.checked)}
                />
                <span className="checkbox-mark" aria-hidden="true" />
                <span className="characteristic-label">Aprovando</span>
              </label>

              <label className="characteristic-toggle">
                <input
                  type="checkbox"
                  id="role-aprovador"
                  value="isAprovador"
                  checked={isAprovador}
                  onChange={(e) => setIsAprovador(e.target.checked)}
                />
                <span className="checkbox-mark" aria-hidden="true" />
                <span className="characteristic-label">Aprovador</span>
              </label>
            </div>
          </div>

          <div className="form-actions">
            <button className="primary-button" type="submit" disabled={isSubmitting}>
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path d="M5 12l5 5L20 7" />
              </svg>
              <span>{isSubmitting ? "Saving..." : "Save changes"}</span>
            </button>
          </div>

          <p
            className={`action-message${actionType ? ` ${actionType}` : ""}`}
            id="action-message"
            role="status"
            aria-live="polite"
          >
            {actionMessage}
          </p>
        </form>
      </section>
    </main>
  );
}
