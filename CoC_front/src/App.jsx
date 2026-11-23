import { Navigate, Route, Routes } from "react-router-dom";
import AccountCreationView from "./views/AccountCreationView";
import AccountListingView from "./views/AccountListingView";
import CampaignFeedbackView from "./views/CampaignFeedbackView";
import CampaignListingView from "./views/CampaignListingView";
import EditCampaignView from "./views/EditCampaignView";
import EditUserView from "./views/EditUserView";
import LoginView from "./views/LoginView";

/**
 * Main application router for MVC Views.
 */
export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/campaigns" replace />} />
      <Route path="/login" element={<LoginView />} />
      <Route path="/campaigns" element={<CampaignListingView />} />
      <Route path="/campaigns/edit" element={<EditCampaignView />} />
      <Route path="/campaigns/:id/edit" element={<EditCampaignView />} />
      <Route path="/campaigns/feedback" element={<CampaignFeedbackView />} />
      <Route path="/accounts" element={<AccountListingView />} />
      <Route path="/accounts/create" element={<AccountCreationView />} />
      <Route path="/accounts/:id/edit" element={<EditUserView />} />
      <Route path="*" element={<Navigate to="/campaigns" replace />} />
    </Routes>
  );
}
