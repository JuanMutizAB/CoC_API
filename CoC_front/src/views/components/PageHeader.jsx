import { Link } from "react-router-dom";
import askblueLogo from "../assets/askblue-logo-oficial-rgb.png";

/**
 * Shared AskBlue top banner linking back to the Login view.
 */
export default function PageHeader({ title }) {
  return (
    <header className="page-header">
      <Link
        className="brand"
        to="/login"
        aria-label="Cockpit of Communications - Login"
      >
        <img src={askblueLogo} alt="AskBlue" />
        <span className="brand-copy">
          <span className="brand-name">Cockpit of Communications</span>
          <span className="page-title">{title}</span>
        </span>
      </Link>
    </header>
  );
}
