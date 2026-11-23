import { Navigate, Route, Routes } from "react-router-dom";
import AccountCreationView from "./views/AccountCreationView";
import AccountListingView from "./views/AccountListingView";
import EditUserView from "./views/EditUserView";
import LoginView from "./views/LoginView";

/**
 * Main application router for MVC Views.
 */
export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/accounts" replace />} />
      <Route path="/login" element={<LoginView />} />
      <Route path="/accounts" element={<AccountListingView />} />
      <Route path="/accounts/create" element={<AccountCreationView />} />
      <Route path="/accounts/:id/edit" element={<EditUserView />} />
      <Route path="*" element={<Navigate to="/accounts" replace />} />
    </Routes>
  );
}
