import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../../controllers/AuthController";
import askblueLogo from "../assets/askblue-logo-oficial-rgb.png";

/**
 * Shared AskBlue top banner with session user info and Logout button.
 */
export default function PageHeader({ title }) {
  const navigate = useNavigate();
  const { currentUser, isAuthenticated, logout } = useAuth();

  const handleLogout = async () => {
    await logout();
    navigate("/login", { replace: true });
  };

  return (
    <header className="page-header">
      <Link
        className="brand"
        to={isAuthenticated ? "/campaigns" : "/login"}
        aria-label={
          isAuthenticated
            ? "Cockpit of Communications - Campaigns"
            : "Cockpit of Communications - Login"
        }
      >
        <img src={askblueLogo} alt="AskBlue" />
        <span className="brand-copy">
          <span className="brand-name">Cockpit of Communications</span>
          <span className="page-title">{title}</span>
        </span>
      </Link>

      {isAuthenticated && currentUser && (
        <div className="header-user-bar">
          <div className="header-user-info">
            <span className="header-user-meta">
              <span className="header-user-name">{currentUser.name}</span>
              {currentUser.email && (
                <span className="header-user-email">{currentUser.email}</span>
              )}
            </span>
          </div>
          <button
            className="header-logout-btn"
            type="button"
            onClick={handleLogout}
          >
            <svg viewBox="0 0 24 24" aria-hidden="true">
              <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
              <polyline points="16 17 21 12 16 7" />
              <line x1="21" y1="12" x2="9" y2="12" />
            </svg>
            <span>Logout</span>
          </button>
        </div>
      )}
    </header>
  );
}
