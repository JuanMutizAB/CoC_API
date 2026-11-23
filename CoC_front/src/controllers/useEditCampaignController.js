import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  formatPublishTimeInput,
  isValidPublishTime,
  loadStoredCampaign,
  saveStoredCampaign,
} from "../models/campaignModel";

/**
 * Controller hook for the Edit Campaign view.
 */
export function useEditCampaignController() {
  const navigate = useNavigate();
  const initialCampaign = loadStoredCampaign();

  const [name, setName] = useState(initialCampaign?.name || "");
  const [description, setDescription] = useState(initialCampaign?.description || "");
  const [text, setText] = useState(initialCampaign?.text || "");
  const [imageName, setImageName] = useState(initialCampaign?.image || "");
  const [publishDate, setPublishDate] = useState(initialCampaign?.publishDate || "");
  const [publishTime, setPublishTime] = useState(initialCampaign?.publishTime || "");
  const [statusMessage, setStatusMessage] = useState("");
  const [statusRole, setStatusRole] = useState("status");

  useEffect(() => {
    document.title = "Edit Campaign - CoC";
  }, []);

  const handlePublishTimeChange = (value) => {
    setPublishTime(formatPublishTimeInput(value));
  };

  const handleImageChange = (file) => {
    setImageName(file?.name ?? "");
  };

  const handleSubmit = (event) => {
    event.preventDefault();

    if (!isValidPublishTime(publishTime)) {
      setStatusRole("alert");
      setStatusMessage("Introduza um horário válido entre 00:00 e 23:59.");
      return;
    }

    const campaignData = {
      name: name.trim(),
      description: description.trim(),
      text: text.trim(),
      image: imageName || null,
      publishDate,
      publishTime,
    };

    saveStoredCampaign(campaignData);
    console.log("Dados da campanha:", campaignData);
    setStatusRole("status");
    setStatusMessage(
      `Dados: ${campaignData.name} | ${campaignData.publishDate} às ${campaignData.publishTime}`
    );
  };

  const goToFeedback = () => navigate("/campaigns/feedback");
  const goToAccounts = () => navigate("/accounts");
  const goToCampaigns = () => navigate("/campaigns");

  return {
    name,
    setName,
    description,
    setDescription,
    text,
    setText,
    imageName,
    publishDate,
    setPublishDate,
    publishTime,
    statusMessage,
    statusRole,
    handlePublishTimeChange,
    handleImageChange,
    handleSubmit,
    goToFeedback,
    goToAccounts,
    goToCampaigns,
  };
}
