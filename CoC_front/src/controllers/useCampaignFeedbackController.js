import { useEffect, useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import {
  cancelCampaignApi,
  fetchCampaignById,
  Motivo,
  requestCampaignChangesApi,
} from "../models/campaignModel";

/**
 * Controller hook for the Campaign Feedback view.
 * Submits cancellation (PUT /campaigns/{id}/cancel) or change requests (PUT /campaigns/{id}/review/request-changes).
 */
export function useCampaignFeedbackController() {
  const { id } = useParams();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const isCancelMode = searchParams.get("mode") === "cancel";
  const [campaign, setCampaign] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState("");

  const reasons = Object.values(Motivo).filter((m) => m !== Motivo.None);
  const [reason, setReason] = useState("");
  const [feedbackText, setFeedbackText] = useState("");
  const [statusMessage, setStatusMessage] = useState("");
  const [statusRole, setStatusRole] = useState("status");
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    document.title = isCancelMode
      ? "Cancel Campaign Feedback - CoC"
      : "Campaign Feedback - CoC";
  }, [isCancelMode]);

  useEffect(() => {
    if (!id) {
      setCampaign(null);
      setIsLoading(false);
      return;
    }

    let cancelled = false;
    setIsLoading(true);
    setLoadError("");

    fetchCampaignById(id)
      .then((loaded) => {
        if (!cancelled) {
          setCampaign(loaded);
        }
      })
      .catch((error) => {
        if (!cancelled) {
          setCampaign(null);
          setLoadError(error?.message || "Could not load campaign.");
        }
      })
      .finally(() => {
        if (!cancelled) {
          setIsLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [id]);

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!id || isSubmitting) {
      return;
    }

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

    setStatusMessage("");
    setStatusRole("status");
    setIsSubmitting(true);

    try {
      if (isCancelMode) {
        await cancelCampaignApi(id, {
          motivo: reason,
          justificationText: trimmedFeedback,
        });
      } else {
        await requestCampaignChangesApi(id, {
          motivo: reason,
          justificationText: trimmedFeedback,
        });
      }

      const label = campaign?.name ? `"${campaign.name}"` : "Campaign";
      navigate("/campaigns", {
        state: {
          flashMessage: isCancelMode
            ? `Campaign ${label} was canceled (${reason}).`
            : `Campaign ${label} sent for change (${reason}).`,
        },
      });
    } catch (error) {
      setStatusRole("alert");
      setStatusMessage(error?.message || "Could not submit feedback.");
    } finally {
      setIsSubmitting(false);
    }
  };

  const goBackToCampaign = () => {
    if (id) {
      navigate(`/campaigns/${id}/edit`);
    } else {
      navigate("/campaigns");
    }
  };

  return {
    campaign,
    isLoading,
    loadError,
    isCancelMode,
    reasons,
    reason,
    setReason,
    feedbackText,
    setFeedbackText,
    statusMessage,
    statusRole,
    isSubmitting,
    handleSubmit,
    goBackToCampaign,
  };
}
