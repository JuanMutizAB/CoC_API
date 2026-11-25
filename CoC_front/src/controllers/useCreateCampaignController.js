import { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import {
  CampaignType,
  combineDateAndTime,
  createCampaignApi,
  formatPublishTimeInput,
  isValidPublishTime,
  normalizeCampaignTypes,
  validateEmailRecipients,
} from "../models/campaignModel";
import { validateImageUrl } from "../models/validationModel";

const INITIAL_ERRORS = {
  name: "",
  type: "",
  description: "",
  modelMsg: "",
  publishDate: "",
  publishTime: "",
  imageUrl: "",
  emailRecipients: "",
};

/**
 * Controller hook for the Create Campaign view.
 */
export function useCreateCampaignController() {
  const navigate = useNavigate();
  const location = useLocation();
  const duplicateCampaign = location.state?.duplicateCampaign || null;

  const [name, setName] = useState(() => duplicateCampaign?.name || "");
  const [types, setTypes] = useState(() =>
    duplicateCampaign
      ? normalizeCampaignTypes(duplicateCampaign.types ?? duplicateCampaign.type)
      : []
  );
  const [description, setDescription] = useState(
    () => duplicateCampaign?.description || ""
  );
  const [modelMsg, setModelMsg] = useState(
    () => duplicateCampaign?.modelMsg || ""
  );
  const [publishDate, setPublishDate] = useState("");
  const [publishTime, setPublishTime] = useState("");
  const [imageUrl, setImageUrl] = useState(
    () => duplicateCampaign?.imageUrl || ""
  );
  const [emailRecipients, setEmailRecipients] = useState(
    () => duplicateCampaign?.emailRecipients || ""
  );

  const [errors, setErrors] = useState(INITIAL_ERRORS);
  const [actionMessage, setActionMessage] = useState("");
  const [actionType, setActionType] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    document.title = "Create Campaign - CoC";
  }, []);

  const handlePublishTimeChange = (value) => {
    setPublishTime(formatPublishTimeInput(value));
  };

  const clearErrors = () => {
    setErrors(INITIAL_ERRORS);
    setActionMessage("");
    setActionType("");
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (isSubmitting) {
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
    } else {
      const selectedDate = new Date(`${publishDate}T00:00:00`);
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      if (selectedDate < today) {
        nextErrors.publishDate = "Publication date cannot be in the past.";
      }
    }

    if (!publishTime) {
      nextErrors.publishTime = "Publication time is required.";
    } else if (!isValidPublishTime(publishTime)) {
      nextErrors.publishTime = "Enter a valid time between 00:00 and 23:59.";
    }

    nextErrors.imageUrl = validateImageUrl(imageUrl);
    if (types.includes(CampaignType.Email)) {
      nextErrors.emailRecipients = validateEmailRecipients(emailRecipients);
    }

    if (Object.values(nextErrors).some(Boolean)) {
      setErrors(nextErrors);
      setActionMessage("Please fix the errors above.");
      setActionType("error");
      return;
    }

    setErrors(INITIAL_ERRORS);
    setActionMessage("");
    setActionType("");

    const trimmedName = name.trim();
    const includesEmail = types.includes(CampaignType.Email);
    setIsSubmitting(true);
    try {
      await createCampaignApi({
        name: trimmedName,
        description: description.trim(),
        modelMsg: modelMsg.trim(),
        types,
        publicationDate: combineDateAndTime(publishDate, publishTime),
        imageUrl: imageUrl.trim() || null,
        emailRecipients: includesEmail ? emailRecipients.trim() || null : null,
      });

      navigate("/campaigns", {
        state: {
          flashMessage: `Campaign "${trimmedName}" was created successfully.`,
        },
      });
    } catch (error) {
      setActionMessage(error?.message || "Could not create campaign.");
      setActionType("error");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClear = () => {
    setName("");
    setTypes([]);
    setDescription("");
    setModelMsg("");
    setPublishDate("");
    setPublishTime("");
    setImageUrl("");
    setEmailRecipients("");
    clearErrors();
    setActionMessage("Form cleared.");
    setActionType("");
  };

  const goBackToCampaigns = () => navigate("/campaigns");

  return {
    name,
    setName,
    types,
    setTypes,
    description,
    setDescription,
    modelMsg,
    setModelMsg,
    publishDate,
    setPublishDate,
    publishTime,
    handlePublishTimeChange,
    imageUrl,
    setImageUrl,
    emailRecipients,
    setEmailRecipients,
    showEmailRecipients: types.includes(CampaignType.Email),
    showFacebookOnlyImageNotice:
      types.length === 1 && types[0] === CampaignType.Facebook,
    errors,
    actionMessage,
    actionType,
    isSubmitting,
    handleSubmit,
    handleClear,
    goBackToCampaigns,
    typeOptions: Object.values(CampaignType),
  };
}
