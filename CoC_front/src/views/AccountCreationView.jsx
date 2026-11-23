import PageHeader from "./components/PageHeader";
import PasswordField from "./components/PasswordField";
import { useAccountCreationController } from "../controllers/useAccountCreationController";
import "./AccountCreationView.css";

/**
 * Account creation view with User model validation, AskBlue calendar indicator,
 * password visibility toggle, and role checkboxes.
 */
export default function AccountCreationView() {
  const {
    name,
    setName,
    email,
    setEmail,
    birthDate,
    setBirthDate,
    taxId,
    setTaxId,
    password,
    setPassword,
    isAdmin,
    setIsAdmin,
    isAprovando,
    setIsAprovando,
    isAprovador,
    setIsAprovador,
    errors,
    actionMessage,
    actionType,
    handleSubmit,
    handleClear,
    goBackToAccounts,
  } = useAccountCreationController();

  return (
    <main className="app-page create-account-page">
      <PageHeader title="Create Account" />

      <section
        className="content-card create-account-card"
        aria-labelledby="create-account-title"
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
            <h1 id="create-account-title">Create Account</h1>
            <p id="create-account-subtitle">
              Enter details to add a new account to your organization
            </p>
          </div>
          <span className="demo-badge">Demo data</span>
        </div>

        <form
          id="create-account-form"
          className="management-form create-account-form"
          onSubmit={handleSubmit}
          noValidate
        >
          <div className="form-grid">
            <div className="form-group">
              <label htmlFor="user-name">Name</label>
              <input
                id="user-name"
                type="text"
                placeholder="First and last name"
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

            <div className="form-group">
              <label htmlFor="user-birthdate">Birth date</label>
              <input
                id="user-birthdate"
                type="date"
                title="Click the calendar icon to select your birth date"
                value={birthDate}
                onChange={(e) => setBirthDate(e.target.value)}
                className={errors.birthDate ? "invalid" : undefined}
                required
              />
              <span className="error-message" id="birthdate-error">
                {errors.birthDate}
              </span>
            </div>

            <div className="form-group">
              <label htmlFor="user-taxid">Tax ID</label>
              <input
                id="user-taxid"
                type="text"
                placeholder="9 digits (e.g. 123456789)"
                maxLength={9}
                inputMode="numeric"
                value={taxId}
                onChange={(e) => setTaxId(e.target.value)}
                className={errors.taxId ? "invalid" : undefined}
                required
              />
              <span className="error-message" id="taxid-error">
                {errors.taxId}
              </span>
            </div>

            <div className="form-group form-group--full">
              <label htmlFor="user-password">Password</label>
              <PasswordField
                id="user-password"
                name="password"
                placeholder="Your password"
                autoComplete="new-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                invalid={Boolean(errors.password)}
                required
              />
              <span className="field-hint">
                Must be more than 8 characters, with uppercase, lowercase, digit and symbol.
              </span>
              <span className="error-message" id="password-error">
                {errors.password}
              </span>
            </div>
          </div>

          <div className="roles-section">
            <span className="roles-title">Roles &amp; Characteristics</span>
            <div className="characteristics-grid">
              <label className="characteristic-toggle">
                <input
                  type="checkbox"
                  id="role-admin"
                  value="Admin"
                  checked={isAdmin}
                  onChange={(e) => setIsAdmin(e.target.checked)}
                />
                <span className="checkbox-mark" aria-hidden="true" />
                <span className="characteristic-label">Admin</span>
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
            <button className="primary-button" type="submit">
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path d="M12 5v14M5 12h14" />
              </svg>
              <span>Create Account</span>
            </button>
            <button
              className="cancel-button"
              id="cancel-create"
              type="button"
              onClick={handleClear}
            >
              Clear
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
