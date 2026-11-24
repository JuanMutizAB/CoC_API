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
          <span className="header-user-name">{currentUser.name}</span>
          <button
            className="header-logout-btn"
            type="button"
            onClick={handleLogout}
          >
            Logout
          </button>
        </div>
      )}
    </header>
  );
}
