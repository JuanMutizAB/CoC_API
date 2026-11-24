import { useEffect, useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import {
  CampaignStatus,
  getCampaignById,
  loadStoredCampaign,
  Motivo,
  saveStoredFeedback,
  setCampaignFlashMessage,
  updateCampaignInStore,
} from "../models/campaignModel";

/**
 * Controller hook for the Campaign Feedback view.
 * In default/change mode, sets status to ForChange; in cancel mode (?mode=cancel), sets status to Canceled.
 */
export function useCampaignFeedbackController() {
  const { id } = useParams();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const isCancelMode = searchParams.get("mode") === "cancel";
  const campaign = id
    ? getCampaignById(id)
    : getCampaignById(loadStoredCampaign()?.id);

  const reasons = Object.values(Motivo).filter((m) => m !== Motivo.None);
  const [reason, setReason] = useState("");
  const [feedbackText, setFeedbackText] = useState("");
  const [statusMessage, setStatusMessage] = useState("");
  const [statusRole, setStatusRole] = useState("status");

  useEffect(() => {
    document.title = isCancelMode
      ? "Cancel Campaign Feedback - CoC"
      : "Campaign Feedback - CoC";
  }, [isCancelMode]);

  const handleSubmit = (event) => {
    event.preventDefault();

    if (!reason) {
      setStatusRole("alert");
      setStatusMessage("Please select a reason.");
      return;
    }

    const trimmedFeedback = feedbackText.trim();
    if (!trimmedFeedback) {
      setStatusRole("alert");
      setStatusMessage(
        isCancelMode
          ? "Please provide feedback describing the reason for cancellation."
          : "Please provide feedback describing the requested changes."
      );
      return;
    }

    const targetStatus = isCancelMode
      ? CampaignStatus.Canceled
      : CampaignStatus.ForChange;

    saveStoredFeedback({
      campaignId: campaign?.id ?? null,
      mode: isCancelMode ? "cancel" : "change",
      reason,
      feedback: trimmedFeedback,
      createdAt: new Date().toISOString(),
    });

    if (campaign) {
      updateCampaignInStore(campaign.id, {
        status: targetStatus,
        motivo: reason,
        justificationText: trimmedFeedback,
      });
      setCampaignFlashMessage(
        isCancelMode
          ? `Campaign "${campaign.name}" was canceled (${reason}).`
          : `Campaign "${campaign.name}" sent for change (${reason}).`
      );
    } else {
      setCampaignFlashMessage(`Feedback submitted (${reason}).`);
    }

    navigate("/campaigns");
  };

  const goBackToCampaign = () => {
    if (campaign) {
      navigate(`/campaigns/${campaign.id}/edit`);
    } else {
      navigate("/campaigns");
    }
  };

  return {
    campaign,
    isCancelMode,
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
