import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  approveCampaignApi,
  CampaignStatus,
  CampaignType,
  combineDateAndTime,
  editCampaignApi,
  extractDateAndTime,
  fetchCampaignById,
  formatPublishTimeInput,
  isValidPublishTime,
  normalizeCampaignTypes,
} from "../models/campaignModel";
import { validateImageUrl } from "../models/validationModel";
import { useAuth } from "./AuthController";

const INITIAL_ERRORS = {
  name: "",
  type: "",
  description: "",
  modelMsg: "",
  imageUrl: "",
  publishDate: "",
  publishTime: "",
};

/**
 * Controller hook for the Edit Campaign and Campaign Details views.
 */
export function useEditCampaignController({ readOnly = false } = {}) {
  const { id } = useParams();
  const navigate = useNavigate();
  const { currentUser, isAdmin, isAprovador, isAprovando } = useAuth();

  const [campaign, setCampaign] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [name, setName] = useState("");
  const [types, setTypes] = useState([]);
  const [description, setDescription] = useState("");
  const [modelMsg, setModelMsg] = useState("");
  const [imageUrl, setImageUrl] = useState("");
  const [publishDate, setPublishDate] = useState("");
  const [publishTime, setPublishTime] = useState("");
  const [status, setStatus] = useState(CampaignStatus.New);
  const [motivo, setMotivo] = useState("");
  const [justificationText, setJustificationText] = useState("");
  const [errors, setErrors] = useState(INITIAL_ERRORS);
  const [statusMessage, setStatusMessage] = useState("");
  const [statusRole, setStatusRole] = useState("status");
  const [isSubmitting, setIsSubmitting] = useState(false);

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
      .then((fresh) => {
        if (cancelled) return;
        setCampaign(fresh);

        if (!fresh) return;

        const { publishDate: initDate, publishTime: initTime } =
          extractDateAndTime(fresh.publicationDate);

        setName(fresh.name || "");
        setTypes(normalizeCampaignTypes(fresh.types ?? fresh.type));
        setDescription(fresh.description || "");
        setModelMsg(fresh.modelMsg || "");
        setImageUrl(fresh.imageUrl || "");
        setPublishDate(initDate);
        setPublishTime(initTime);
        setStatus(fresh.status || CampaignStatus.New);
        setMotivo(fresh.motivo || "");
        setJustificationText(fresh.justificationText || "");
        setErrors(INITIAL_ERRORS);
        setStatusMessage("");
        setStatusRole("status");
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

  const isCanceled = status === CampaignStatus.Canceled;
  const isClosed = status === CampaignStatus.Closed;
  const isForChange = status === CampaignStatus.ForChange;
  const isInReview = status === CampaignStatus.InReview;
  const isDetailsMode = Boolean(readOnly) || isCanceled || isClosed;
  const isOwner = Boolean(
    currentUser && campaign && Number(campaign.userId) === Number(currentUser.id)
  );
  const canEditFields =
    !isDetailsMode &&
    (((isForChange || isInReview) && (isAdmin || isAprovador)) ||
      ((isOwner || isAdmin) && !(isForChange && !isAprovando)));
  const canReview =
    !isDetailsMode && isInReview && isAprovador;
  const canCancel =
    !isDetailsMode &&
    status !== CampaignStatus.Sending &&
    (isOwner || isAdmin) &&
    (!isForChange || isAdmin);
  const showFeedbackBox = isForChange || isCanceled;

  useEffect(() => {
    document.title = isDetailsMode ? "Campaign Details - CoC" : "Edit Campaign - CoC";
  }, [isDetailsMode]);

  const handlePublishTimeChange = (value) => {
    setPublishTime(formatPublishTimeInput(value));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!campaign || !canEditFields || isSubmitting) {
      return;
    }

    const nextErrors = { ...INITIAL_ERRORS };
    if (!name.trim()) {
      nextErrors.name = "Campaign name is required.";
    }
    if (!types || types.length === 0) {
      nextErrors.type = "Select at least one campaign type.";
    }
    if (!description.trim()) {
      nextErrors.description = "Description is required.";
    }
    if (!modelMsg.trim()) {
      nextErrors.modelMsg = "Model message is required.";
    }
    if (!publishDate) {
      nextErrors.publishDate = "Publication date is required.";
    }
    if (!publishTime || !isValidPublishTime(publishTime)) {
      nextErrors.publishTime =
        "Introduza um horário válido entre 00:00 e 23:59.";
    }
    nextErrors.imageUrl = validateImageUrl(imageUrl);

    if (Object.values(nextErrors).some(Boolean)) {
      setErrors(nextErrors);
      setStatusRole("alert");
      setStatusMessage("Please fix the errors above.");
      return;
    }

    setErrors(INITIAL_ERRORS);
    setStatusMessage("");
    setStatusRole("status");

    const updatedName = name.trim();
    setIsSubmitting(true);
    try {
      const updated = await editCampaignApi(campaign.id, {
        name: updatedName,
        description: description.trim(),
        modelMsg: modelMsg.trim(),
        types,
        imageUrl: imageUrl.trim(),
        publicationDate: combineDateAndTime(publishDate, publishTime),
      });

      let flashMessage = `Campaign "${updatedName}" updated successfully.`;
      if (isForChange && updated?.status === CampaignStatus.InReview) {
        flashMessage = `Campaign "${updatedName}" updated and sent back to InReview.`;
      } else if (
        (isForChange || isInReview) &&
        (updated?.status === CampaignStatus.Scheduled ||
          updated?.status === CampaignStatus.Sending)
      ) {
        flashMessage = `Campaign "${updatedName}" updated and moved to ${updated.status}.`;
      }

      navigate("/campaigns", {
        state: {
          flashMessage,
        },
      });
    } catch (error) {
      setStatusRole("alert");
      setStatusMessage(error?.message || "Could not update campaign.");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCancelCampaign = () => {
    if (!campaign || !canCancel) {
      return;
    }
    navigate(`/campaigns/${campaign.id}/feedback?mode=cancel`);
  };

  const handleApproveCampaign = async () => {
    if (!campaign || !canReview || isSubmitting) {
      return;
    }

    setIsSubmitting(true);
    setStatusMessage("");
    setStatusRole("status");

    try {
      await approveCampaignApi(campaign.id);
      navigate("/campaigns", {
        state: {
          flashMessage: `Campaign "${campaign.name}" was approved.`,
        },
      });
    } catch (error) {
      setStatusRole("alert");
      setStatusMessage(error?.message || "Could not approve campaign.");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSendForChange = () => {
    if (!campaign || !canReview) {
      return;
    }
    navigate(`/campaigns/${campaign.id}/feedback?mode=change`);
  };

  const goToCampaigns = () => navigate("/campaigns");

  return {
    id,
    campaign,
    isLoading,
    loadError,
    name,
    setName,
    types,
    setTypes,
    description,
    setDescription,
    modelMsg,
    setModelMsg,
    imageUrl,
    setImageUrl,
    publishDate,
    setPublishDate,
    publishTime,
    status,
    motivo,
    justificationText,
    isDetailsMode,
    isCanceled,
    canEditFields,
    canReview,
    canCancel,
    isForChange,
    showFeedbackBox,
    errors,
    statusMessage,
    statusRole,
    isSubmitting,
    handlePublishTimeChange,
    handleSubmit,
    handleCancelCampaign,
    handleApproveCampaign,
    handleSendForChange,
    goToCampaigns,
    typeOptions: Object.values(CampaignType),
  };
}
