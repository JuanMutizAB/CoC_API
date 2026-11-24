import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import { useAuth } from "./controllers/AuthController";
import AccountCreationView from "./views/AccountCreationView";
import AccountListingView from "./views/AccountListingView";
import CampaignFeedbackView from "./views/CampaignFeedbackView";
import CampaignListingView from "./views/CampaignListingView";
import CreateCampaignView from "./views/CreateCampaignView";
import EditCampaignView from "./views/EditCampaignView";
import EditUserView from "./views/EditUserView";
import LoginView from "./views/LoginView";

function AuthLoadingScreen() {
  return (
    <main className="app-page auth-loading-page">
      <section className="content-card auth-loading-card">
        <p role="status" aria-live="polite">
          Checking session...
        </p>
      </section>
    </main>
  );
}

function ProtectedRoute({ children }) {
  const { isAuthenticated, isAuthLoading } = useAuth();
  const location = useLocation();

  if (isAuthLoading) {
    return <AuthLoadingScreen />;
  }

  if (!isAuthenticated) {
    return (
      <Navigate
        to="/login"
        replace
        state={{ from: `${location.pathname}${location.search}` }}
      />
    );
  }

  return children;
}

function PublicOnlyRoute({ children }) {
  const { isAuthenticated, isAuthLoading } = useAuth();

  if (isAuthLoading) {
    return <AuthLoadingScreen />;
  }

  if (isAuthenticated) {
    return <Navigate to="/campaigns" replace />;
  }

  return children;
}

function AdminRoute({ children }) {
  const { isAuthenticated, isAuthLoading, isAdmin } = useAuth();
  const location = useLocation();

  if (isAuthLoading) {
    return <AuthLoadingScreen />;
  }

  if (!isAuthenticated) {
    return (
      <Navigate
        to="/login"
        replace
        state={{ from: `${location.pathname}${location.search}` }}
      />
    );
  }

  if (!isAdmin) {
    return <Navigate to="/campaigns" replace />;
  }

  return children;
}

/**
 * Main application router for MVC Views.
 */
export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/campaigns" replace />} />
      <Route
        path="/login"
        element={
          <PublicOnlyRoute>
            <LoginView />
          </PublicOnlyRoute>
        }
      />
      <Route
        path="/campaigns"
        element={
          <ProtectedRoute>
            <CampaignListingView />
          </ProtectedRoute>
        }
      />
      <Route
        path="/campaigns/create"
        element={
          <ProtectedRoute>
            <CreateCampaignView />
          </ProtectedRoute>
        }
      />
      <Route
        path="/campaigns/:id/edit"
        element={
          <ProtectedRoute>
            <EditCampaignView />
          </ProtectedRoute>
        }
      />
      <Route
        path="/campaigns/:id/details"
        element={
          <ProtectedRoute>
            <EditCampaignView readOnly />
          </ProtectedRoute>
        }
      />
      <Route
        path="/campaigns/:id/feedback"
        element={
          <ProtectedRoute>
            <CampaignFeedbackView />
          </ProtectedRoute>
        }
      />
      <Route
        path="/accounts"
        element={
          <AdminRoute>
            <AccountListingView />
          </AdminRoute>
        }
      />
      <Route
        path="/accounts/create"
        element={
          <AdminRoute>
            <AccountCreationView />
          </AdminRoute>
        }
      />
      <Route
        path="/accounts/:id/edit"
        element={
          <AdminRoute>
            <EditUserView />
          </AdminRoute>
        }
      />
      <Route path="*" element={<Navigate to="/campaigns" replace />} />
    </Routes>
  );
}
