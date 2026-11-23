import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Motivo, saveStoredFeedback } from "../models/campaignModel";

/**
 * Controller hook for the Campaign Feedback view.
 */
export function useCampaignFeedbackController() {
  const navigate = useNavigate();

  const reasons = Object.values(Motivo);
  const [reason, setReason] = useState("");
  const [feedbackText, setFeedbackText] = useState("");
  const [statusMessage, setStatusMessage] = useState("");
  const [statusRole, setStatusRole] = useState("status");

  useEffect(() => {
    document.title = "Campaign Feedback - CoC";
  }, []);

  const handleSubmit = (event) => {
    event.preventDefault();

    if (!reason) {
      setStatusRole("alert");
      setStatusMessage("Please select a reason.");
      return;
    }

    const submittedReason = reason;
    saveStoredFeedback({
      reason: submittedReason,
      feedback: feedbackText.trim(),
      createdAt: new Date().toISOString(),
    });

    setReason("");
    setFeedbackText("");
    setStatusRole("status");
    setStatusMessage(`Feedback submitted (${submittedReason}).`);
  };

  const goBackToCampaign = () => navigate("/campaigns/edit");

  return {
    reasons,
    reason,
    setReason,
    feedbackText,
    setFeedbackText,
    statusMessage,
    statusRole,
    handleSubmit,
    goBackToCampaign,
  };
}
