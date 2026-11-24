import askblueLogo from "./assets/askblue-logo-oficial-rgb.png";
import PasswordField from "./components/PasswordField";
import { useLoginController } from "../controllers/useLoginController";
import "./LoginView.css";

const TBLUE_URL = "https://tblue.askblue.pt/Account/Login?ReturnUrl=%2F";

/**
 * Login view with AskBlue split-panel branding.
 */
export default function LoginView() {
  const {
    email,
    setEmail,
    password,
    setPassword,
    emailError,
    passwordError,
    formStatus,
    statusRole,
    isSubmitting,
    emailInputRef,
    handleSubmit,
  } = useLoginController();

  return (
    <main className="login-page">
      <section className="brand-panel" aria-label="AskBlue">
        <img className="brand-panel-logo" src={askblueLogo} alt="AskBlue" />
        <h2 className="brand-slogan">
          <span>
            Where <strong>business</strong>
          </span>
          <span>
            meets <strong>Technology</strong>
          </span>
        </h2>
      </section>

      <section className="login-card" aria-labelledby="login-title">
        <div className="brand" aria-label="AskBlue - Cockpit of Communications">
          <img className="brand-logo" src={askblueLogo} alt="AskBlue" />
          <p className="brand-name">Cockpit of Communications</p>
        </div>

        <h1 id="login-title">Login</h1>
        <p className="subtitle">Input your credentials to access the cockpit</p>

        <form id="login-form" onSubmit={handleSubmit} noValidate>
          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              ref={emailInputRef}
              id="email"
              name="email"
              type="email"
              placeholder="body@example.com"
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className={emailError ? "invalid" : undefined}
              required
            />
            <span className="error-message" id="email-error">
              {emailError}
            </span>
          </div>

          <div className="form-group">
            <div className="password-label-row">
              <label htmlFor="password">Password</label>
            </div>
            <PasswordField
              id="password"
              name="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Your password"
              autoComplete="current-password"
              required
              invalid={Boolean(passwordError)}
              showLabel="Mostrar palavra-passe"
              hideLabel="Ocultar palavra-passe"
            />
            <span className="forgot-password">
              Forgot your credentials?{" "}
              <a href={TBLUE_URL} target="_blank" rel="noopener noreferrer">
                Open a ticket on TBlue
              </a>
            </span>
            <span className="error-message" id="password-error">
              {passwordError}
            </span>
          </div>

          <button className="login-button" type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Logging in..." : "Login"}
          </button>
          <p
            className={`form-status${statusRole === "alert" ? " error" : ""}`}
            id="form-status"
            role={statusRole}
          >
            {formStatus}
          </p>
        </form>

        <p className="signup-text">
          Don't have an account?{" "}
          <a href={TBLUE_URL} target="_blank" rel="noopener noreferrer">
            Open a ticket on TBlue
          </a>
        </p>
      </section>
    </main>
  );
}
