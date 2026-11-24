import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  CampaignType,
  combineDateAndTime,
  createCampaignInStore,
  formatPublishTimeInput,
  isValidPublishTime,
  setCampaignFlashMessage,
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
};

/**
 * Controller hook for the Create Campaign view.
 */
export function useCreateCampaignController() {
  const navigate = useNavigate();

  const [name, setName] = useState("");
  const [type, setType] = useState("");
  const [description, setDescription] = useState("");
  const [modelMsg, setModelMsg] = useState("");
  const [publishDate, setPublishDate] = useState("");
  const [publishTime, setPublishTime] = useState("");
  const [imageUrl, setImageUrl] = useState("");

  const [errors, setErrors] = useState(INITIAL_ERRORS);
  const [actionMessage, setActionMessage] = useState("");
  const [actionType, setActionType] = useState("");

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

  const handleSubmit = (event) => {
    event.preventDefault();

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

    if (Object.values(nextErrors).some(Boolean)) {
      setErrors(nextErrors);
      setActionMessage("Please fix the errors above.");
      setActionType("error");
      return;
    }

    const trimmedName = name.trim();
    createCampaignInStore({
      name: trimmedName,
      description: description.trim(),
      modelMsg: modelMsg.trim(),
      type,
      publicationDate: combineDateAndTime(publishDate, publishTime),
      imageUrl: imageUrl.trim() || null,
    });

    setCampaignFlashMessage(`Campaign "${trimmedName}" was created successfully.`);
    navigate("/campaigns");
  };

  const handleClear = () => {
    setName("");
    setType("");
    setDescription("");
    setModelMsg("");
    setPublishDate("");
    setPublishTime("");
    setImageUrl("");
    clearErrors();
    setActionMessage("Form cleared.");
    setActionType("");
  };

  const goBackToCampaigns = () => navigate("/campaigns");

  return {
    name,
    setName,
    type,
    setType,
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
    errors,
    actionMessage,
    actionType,
    handleSubmit,
    handleClear,
    goBackToCampaigns,
    typeOptions: Object.values(CampaignType),
  };
}
