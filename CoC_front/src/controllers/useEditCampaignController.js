import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  CampaignStatus,
  CampaignType,
  combineDateAndTime,
  extractDateAndTime,
  formatPublishTimeInput,
  getCampaignById,
  isValidPublishTime,
  loadStoredCampaign,
  setCampaignFlashMessage,
  updateCampaignInStore,
} from "../models/campaignModel";
import { validateImageUrl } from "../models/validationModel";

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
 * Manages field editing (with imageUrl), ForChange -> InReview transition,
 * workflow actions (Cancel -> feedback, Approve, Send for change), and read-only details mode.
 */
export function useEditCampaignController({ readOnly = false } = {}) {
  const { id } = useParams();
  const navigate = useNavigate();

  const resolvedCampaign = id
    ? getCampaignById(id)
    : getCampaignById(loadStoredCampaign()?.id);

  const [campaign, setCampaign] = useState(resolvedCampaign);
  const [name, setName] = useState("");
  const [type, setType] = useState(CampaignType.Email);
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

  useEffect(() => {
    const fresh = id
      ? getCampaignById(id)
      : getCampaignById(loadStoredCampaign()?.id);
    setCampaign(fresh);

    if (!fresh) {
      return;
    }

    const { publishDate: initDate, publishTime: initTime } =
      extractDateAndTime(fresh.publicationDate);

    setName(fresh.name || "");
    setType(fresh.type || CampaignType.Email);
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
  }, [id]);

  const isCanceled = status === CampaignStatus.Canceled;
  const isClosed = status === CampaignStatus.Closed;
  const isDetailsMode = Boolean(readOnly) || isCanceled || isClosed;
  const canCancel = !isDetailsMode && !isClosed && !isCanceled;
  const isInReview = !isDetailsMode && status === CampaignStatus.InReview;
  const isForChange = status === CampaignStatus.ForChange;
  const showFeedbackBox = isForChange || isCanceled;

  useEffect(() => {
    document.title = isDetailsMode ? "Campaign Details - CoC" : "Edit Campaign - CoC";
  }, [isDetailsMode]);

  const handlePublishTimeChange = (value) => {
    setPublishTime(formatPublishTimeInput(value));
  };

  const handleSubmit = (event) => {
    event.preventDefault();
    if (!campaign || isDetailsMode) {
      return;
    }

    const nextErrors = { ...INITIAL_ERRORS };
    if (!name.trim()) {
      nextErrors.name = "Campaign name is required.";
    }
    if (!type) {
      nextErrors.type = "Campaign type is required.";
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

    // TODO: once backend auth/sessions are connected, skip InReview transition when saved by an Admin
    const wasForChange = status === CampaignStatus.ForChange;
    const nextStatus = wasForChange ? CampaignStatus.InReview : status;

    const updatedName = name.trim();
    updateCampaignInStore(campaign.id, {
      name: updatedName,
      description: description.trim(),
      modelMsg: modelMsg.trim(),
      type,
      imageUrl: imageUrl.trim() || null,
      publicationDate: combineDateAndTime(publishDate, publishTime),
      status: nextStatus,
      ...(wasForChange ? { motivo: null, justificationText: "" } : {}),
    });

    setCampaignFlashMessage(
      wasForChange
        ? `Campaign "${updatedName}" updated and sent back to InReview.`
        : `Campaign "${updatedName}" updated successfully.`
    );
    navigate("/campaigns");
  };

  const handleCancelCampaign = () => {
    if (!campaign || !canCancel) {
      return;
    }
    // TODO: once backend auth/sessions are connected, restrict canceling ForChange campaigns to Admins
    navigate(`/campaigns/${campaign.id}/feedback?mode=cancel`);
  };

  const handleApproveCampaign = () => {
    if (!campaign || !isInReview) {
      return;
    }
    updateCampaignInStore(campaign.id, { status: CampaignStatus.Approved });
    setCampaignFlashMessage(`Campaign "${campaign.name}" was approved.`);
    navigate("/campaigns");
  };

  const handleSendForChange = () => {
    if (!campaign || !isInReview) {
      return;
    }
    navigate(`/campaigns/${campaign.id}/feedback?mode=change`);
  };

  const goToCampaigns = () => navigate("/campaigns");

  return {
    id,
    campaign,
    name,
    setName,
    type,
    setType,
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
    canCancel,
    isInReview,
    isForChange,
    showFeedbackBox,
    errors,
    statusMessage,
    statusRole,
    handlePublishTimeChange,
    handleSubmit,
    handleCancelCampaign,
    handleApproveCampaign,
    handleSendForChange,
    goToCampaigns,
    typeOptions: Object.values(CampaignType),
  };
}
