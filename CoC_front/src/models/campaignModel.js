const CAMPAIGN_STORAGE_KEY = "coc_campaign";
const CAMPAIGNS_STORAGE_KEY = "coc_campaigns";
const FEEDBACKS_STORAGE_KEY = "coc_campaign_feedbacks";
const CAMPAIGN_FLASH_KEY = "coc_campaign_flash";

export const CampaignStatus = Object.freeze({
  New: "New",
  Scheduled: "Scheduled",
  Sending: "Sending",
  Closed: "Closed",
  InReview: "InReview",
  ForChange: "ForChange",
  Approved: "Approved",
  Canceled: "Canceled",
  Failed: "Failed",
});

export const CampaignType = Object.freeze({
  Email: "Email",
  Messaging: "Messaging",
  Instagram: "Instagram",
  Facebook: "Facebook",
  LinkedIn: "LinkedIn",
});

export const Motivo = Object.freeze({
  None: "None",
  InadequateContent: "InadequateContent",
  InadequateLanguage: "InadequateLanguage",
  InadequateImage: "InadequateImage",
  InadequateTargetAudience: "InadequateTargetAudience",
  Other: "Other",
});

export const NAMED_CAMPAIGNS = [
  {
    id: "7239-a1b2-4c3d-8e9f",
    name: "teste parametrização de no...",
    description: "Campanha de teste de parametrização e integração multicanal",
    modelMsg: "Mensagem modelo de teste para parametrização do canal",
    type: "Email",
    status: "New",
    createdAt: "2026-10-13T09:28:52Z",
    publicationDate: "2026-11-20T10:00:00Z",
    imageUrl: null,
    motivo: null,
    justificationText: "",
  },
  {
    id: "7235-b2c3-4d4e-9f0a",
    name: "Assunto Ticket Teste NGT-4...",
    description: "Lançamento oficial da nova funcionalidade e atualização de sistema",
    modelMsg: "Confira as principais novidades implementadas nesta versão.",
    type: "Messaging",
    status: "New",
    createdAt: "2026-10-09T13:01:00Z",
    publicationDate: "2026-11-15T14:30:00Z",
    imageUrl: null,
    motivo: null,
    justificationText: "",
  },
  {
    id: "7234-c3d4-4e5f-0a1b",
    name: "NGT-AUTO 4424-SR-01",
    description: "Divulgação de automações de processos e melhorias no fluxo",
    modelMsg: "Otimize os seus processos corporativos com rapidez e segurança.",
    type: "LinkedIn",
    status: "InReview",
    createdAt: "2026-10-07T18:19:32Z",
    publicationDate: "2026-11-25T11:00:00Z",
    imageUrl: null,
    motivo: null,
    justificationText: "",
  },
  {
    id: "7230-d4e5-5f6a-1b2c",
    name: "Campanha de Natal AskBlue 2026",
    description: "Comunicação especial de Boas Festas e retrospectiva anual",
    modelMsg: "Desejamos a todos os nossos clientes e parceiros um Feliz Natal!",
    type: "Instagram",
    status: "Approved",
    createdAt: "2026-10-01T10:15:00Z",
    publicationDate: "2026-12-24T09:00:00Z",
    imageUrl: null,
    motivo: null,
    justificationText: "",
  },
  {
    id: "7228-e5f6-6a7b-2c3d",
    name: "Recrutamento Tech Academy",
    description: "Divulgação do programa de estágios e aceleração de carreiras",
    modelMsg: "Estão abertas as candidaturas para a DotNet Academy AskBlue.",
    type: "Facebook",
    status: "Scheduled",
    createdAt: "2026-09-28T16:40:00Z",
    publicationDate: "2026-11-10T09:30:00Z",
    imageUrl: null,
    motivo: null,
    justificationText: "",
  },
  {
    id: "7225-f6a7-7b8c-3d4e",
    name: "Manutenção Preventiva de Servidores",
    description: "Aviso de manutenção técnica programada da infraestrutura",
    modelMsg: "Haverá indisponibilidade temporária no próximo domingo às 02h.",
    type: "Messaging",
    status: "Closed",
    createdAt: "2026-09-20T11:05:00Z",
    publicationDate: "2026-09-22T02:00:00Z",
    imageUrl: null,
    motivo: null,
    justificationText: "",
  },
  {
    id: "7220-a7b8-8c9d-4e5f",
    name: "Revisão de Conteúdo Institucional",
    description: "Solicitação de ajustes no texto da proposta comercial",
    modelMsg: "Texto aguardando revisão do aprovador responsável.",
    type: "Email",
    status: "ForChange",
    createdAt: "2026-09-15T08:50:00Z",
    publicationDate: "2026-10-05T17:00:00Z",
    imageUrl: null,
    motivo: "InadequateContent",
    justificationText:
      "Por favor rever o segundo parágrafo da proposta comercial e alinhar o tom institucional.",
  },
];

/**
 * Strips non-digits and formats up to 4 digits as HH:mm while typing.
 */
export function formatPublishTimeInput(value) {
  const digits = String(value || "").replace(/\D/g, "").slice(0, 4);
  return digits.length > 2 ? `${digits.slice(0, 2)}:${digits.slice(2)}` : digits;
}

/**
 * Validates 24-hour time string in HH:mm format (00:00 to 23:59).
 */
export function isValidPublishTime(time) {
  return /^([01][0-9]|2[0-3]):[0-5][0-9]$/.test(String(time || ""));
}

/**
 * Formats a date string (ISO / Date) to DD-MM-YYYY format.
 */
export function formatCampaignDate(dateValue) {
  if (!dateValue) return "—";
  const date = new Date(dateValue);
  if (isNaN(date.getTime())) {
    const parts = String(dateValue).split("T")[0].split("-");
    if (parts.length === 3) {
      return `${parts[2]}-${parts[1]}-${parts[0]}`;
    }
    return String(dateValue);
  }
  const day = String(date.getDate()).padStart(2, "0");
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const year = date.getFullYear();
  return `${day}-${month}-${year}`;
}

/**
 * Safely extracts { publishDate: "YYYY-MM-DD", publishTime: "HH:mm" } from an ISO value.
 */
export function extractDateAndTime(isoValue) {
  if (!isoValue || typeof isoValue !== "string") {
    return { publishDate: "", publishTime: "" };
  }
  const [datePart, timePart = ""] = isoValue.split("T");
  return {
    publishDate: /^\d{4}-\d{2}-\d{2}$/.test(datePart) ? datePart : "",
    publishTime: timePart.length >= 5 ? timePart.slice(0, 5) : "",
  };
}

/**
 * Combines YYYY-MM-DD and HH:mm into an ISO-8601 UTC timestamp string.
 */
export function combineDateAndTime(publishDate, publishTime) {
  if (!publishDate || !isValidPublishTime(publishTime)) {
    return null;
  }
  return `${publishDate}T${publishTime}:00Z`;
}

/**
 * Normalizes campaign object fields from PascalCase/camelCase into uniform front-end keys.
 */
export function normalizeCampaign(camp) {
  if (!camp) return null;
  return {
    id: camp.id || camp.Id,
    name: camp.name || camp.Name || "",
    description: camp.description || camp.Description || "",
    modelMsg: camp.modelMsg || camp.ModelMsg || camp.text || camp.Text || "",
    type: camp.type || camp.Type || "Email",
    status: camp.status || camp.Status || "New",
    createdAt: camp.createdAt || camp.CreatedAt || new Date().toISOString(),
    publicationDate: camp.publicationDate || camp.PublicationDate || null,
    imageUrl: camp.imageUrl ?? camp.ImageUrl ?? camp.image ?? null,
    userId: camp.userId || camp.UserId || null,
    motivo: camp.motivo || camp.Motivo || null,
    justificationText:
      camp.justificationText || camp.JustificationText || "",
  };
}

/**
 * Loads stored campaigns list from sessionStorage, falling back to mock data.
 */
export function loadStoredCampaigns() {
  try {
    const stored = sessionStorage.getItem(CAMPAIGNS_STORAGE_KEY);
    if (stored) {
      const parsed = JSON.parse(stored);
      if (Array.isArray(parsed) && parsed.length > 0) {
        const normalized = parsed.map(normalizeCampaign).map((item) => {
          if (
            String(item.id) === "7220-a7b8-8c9d-4e5f" &&
            item.status === CampaignStatus.ForChange &&
            !item.motivo &&
            !item.justificationText
          ) {
            const demoItem = NAMED_CAMPAIGNS.find(
              (n) => n.id === "7220-a7b8-8c9d-4e5f"
            );
            return {
              ...item,
              motivo: demoItem?.motivo ?? null,
              justificationText: demoItem?.justificationText ?? "",
            };
          }
          return item;
        });
        return normalized;
      }
    }
  } catch (e) {
    console.warn("Could not read coc_campaigns from sessionStorage", e);
  }
  saveStoredCampaigns(NAMED_CAMPAIGNS);
  return NAMED_CAMPAIGNS;
}

/**
 * Persists campaigns list to sessionStorage.
 */
export function saveStoredCampaigns(campaigns) {
  try {
    sessionStorage.setItem(CAMPAIGNS_STORAGE_KEY, JSON.stringify(campaigns));
  } catch (e) {
    console.warn("Could not save coc_campaigns to sessionStorage", e);
  }
}

/**
 * Finds a campaign by ID from stored campaigns.
 */
export function getCampaignById(id) {
  const campaigns = loadStoredCampaigns();
  return campaigns.find((c) => String(c.id) === String(id)) || null;
}

/**
 * Creates a new campaign in sessionStorage and returns it.
 */
export function createCampaignInStore({
  name,
  description,
  modelMsg,
  type,
  publicationDate,
  imageUrl,
}) {
  const campaigns = loadStoredCampaigns();
  const id =
    typeof crypto !== "undefined" && typeof crypto.randomUUID === "function"
      ? crypto.randomUUID()
      : `camp-${Date.now()}`;

  const created = {
    id,
    name,
    description,
    modelMsg,
    type,
    status: CampaignStatus.New,
    createdAt: new Date().toISOString(),
    publicationDate,
    imageUrl: imageUrl || null,
    motivo: null,
    justificationText: "",
  };

  saveStoredCampaigns([created, ...campaigns]);
  return created;
}

/**
 * Updates an existing campaign in sessionStorage and returns the updated item.
 */
export function updateCampaignInStore(id, patch) {
  const campaigns = loadStoredCampaigns();
  let updatedCampaign = null;

  const next = campaigns.map((item) => {
    if (String(item.id) !== String(id)) {
      return item;
    }
    updatedCampaign = normalizeCampaign({ ...item, ...patch });
    return updatedCampaign;
  });

  if (updatedCampaign) {
    saveStoredCampaigns(next);
  }
  return updatedCampaign;
}

/**
 * Stores a one-time campaign flash status message in sessionStorage.
 */
export function setCampaignFlashMessage(message) {
  try {
    sessionStorage.setItem(CAMPAIGN_FLASH_KEY, message);
  } catch (e) {
    console.warn("Could not save campaign flash message", e);
  }
}

/**
 * Reads and clears any pending campaign flash message from sessionStorage.
 */
export function consumeCampaignFlashMessage() {
  try {
    const message = sessionStorage.getItem(CAMPAIGN_FLASH_KEY);
    if (message) {
      sessionStorage.removeItem(CAMPAIGN_FLASH_KEY);
      return message;
    }
  } catch (e) {
    console.warn("Could not read campaign flash message", e);
  }
  return "";
}

/**
 * Loads stored campaign draft/saved data from sessionStorage.
 */
export function loadStoredCampaign() {
  try {
    const stored = sessionStorage.getItem(CAMPAIGN_STORAGE_KEY);
    if (stored) {
      return JSON.parse(stored);
    }
  } catch (e) {
    console.warn("Could not read coc_campaign from sessionStorage", e);
  }
  return null;
}

/**
 * Persists campaign data to sessionStorage.
 */
export function saveStoredCampaign(data) {
  try {
    sessionStorage.setItem(CAMPAIGN_STORAGE_KEY, JSON.stringify(data));
  } catch (e) {
    console.warn("Could not save coc_campaign to sessionStorage", e);
  }
}

/**
 * Loads stored campaign feedback submissions from sessionStorage.
 */
export function loadStoredFeedbacks() {
  try {
    const stored = sessionStorage.getItem(FEEDBACKS_STORAGE_KEY);
    if (stored) {
      const parsed = JSON.parse(stored);
      return Array.isArray(parsed) ? parsed : [];
    }
  } catch (e) {
    console.warn("Could not read coc_campaign_feedbacks from sessionStorage", e);
  }
  return [];
}

/**
 * Appends a campaign feedback entry to sessionStorage.
 */
export function saveStoredFeedback(entry) {
  const current = loadStoredFeedbacks();
  const next = [...current, entry];
  try {
    sessionStorage.setItem(FEEDBACKS_STORAGE_KEY, JSON.stringify(next));
  } catch (e) {
    console.warn("Could not save coc_campaign_feedbacks to sessionStorage", e);
  }
  return next;
}
