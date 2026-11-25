import { apiRequest } from "./apiClient";

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
 * Formats a date string (ISO / Date) to local DD-MM-YYYY HH:mm format.
 */
export function formatCampaignDateTime(dateValue) {
  if (!dateValue) return "—";
  const date = new Date(dateValue);
  if (isNaN(date.getTime())) {
    return formatCampaignDate(dateValue);
  }
  const day = String(date.getDate()).padStart(2, "0");
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const year = date.getFullYear();
  const hours = String(date.getHours()).padStart(2, "0");
  const minutes = String(date.getMinutes()).padStart(2, "0");
  return `${day}-${month}-${year} ${hours}:${minutes}`;
}

/**
 * Formats the display label for a campaign status, appending "(delayed)" visually
 * when InReview or ForChange is within the 24-hour window past publicationDate.
 */
export function formatCampaignStatusLabel(status, publicationDate) {
  if (
    (status !== CampaignStatus.InReview &&
      status !== CampaignStatus.ForChange) ||
    !publicationDate
  ) {
    return status;
  }

  const pubTime = Date.parse(publicationDate);
  if (Number.isNaN(pubTime)) {
    return status;
  }

  const nowMs = Date.now();
  const isPastMinute = Math.floor(nowMs / 60000) > Math.floor(pubTime / 60000);
  const within24Hours = nowMs - pubTime < 24 * 60 * 60 * 1000;

  return isPastMinute && within24Hours ? `${status} (delayed)` : status;
}

/**
 * Extracts local { publishDate: "YYYY-MM-DD", publishTime: "HH:mm" } from an ISO value.
 */
export function extractDateAndTime(isoValue) {
  if (!isoValue) {
    return { publishDate: "", publishTime: "" };
  }

  const parsed = new Date(isoValue);
  if (Number.isNaN(parsed.getTime())) {
    if (typeof isoValue === "string") {
      const [datePart, timePart = ""] = isoValue.split("T");
      return {
        publishDate: /^\d{4}-\d{2}-\d{2}$/.test(datePart) ? datePart : "",
        publishTime: timePart.length >= 5 ? timePart.slice(0, 5) : "",
      };
    }
    return { publishDate: "", publishTime: "" };
  }

  const year = parsed.getFullYear();
  const month = String(parsed.getMonth() + 1).padStart(2, "0");
  const day = String(parsed.getDate()).padStart(2, "0");
  const hours = String(parsed.getHours()).padStart(2, "0");
  const minutes = String(parsed.getMinutes()).padStart(2, "0");

  return {
    publishDate: `${year}-${month}-${day}`,
    publishTime: `${hours}:${minutes}`,
  };
}

/**
 * Combines local YYYY-MM-DD and HH:mm into a UTC ISO-8601 timestamp string.
 */
export function combineDateAndTime(publishDate, publishTime) {
  if (!publishDate || !isValidPublishTime(publishTime)) {
    return null;
  }

  const [year, month, day] = publishDate.split("-").map(Number);
  const [hours, minutes] = publishTime.split(":").map(Number);
  const localDate = new Date(year, month - 1, day, hours, minutes, 0, 0);
  if (Number.isNaN(localDate.getTime())) {
    return null;
  }
  return localDate.toISOString();
}

/**
 * Normalizes campaign types from an array, single string, or comma-separated string
 * into a deduplicated array preserving order.
 */
export function normalizeCampaignTypes(value) {
  const raw = Array.isArray(value)
    ? value
    : String(value || "").split(",");

  const seen = new Set();
  const result = [];

  for (const item of raw) {
    const trimmed = String(item || "").trim();
    if (trimmed && !seen.has(trimmed)) {
      seen.add(trimmed);
      result.push(trimmed);
    }
  }

  return result;
}

/**
 * Normalizes campaign object fields from PascalCase/camelCase into uniform front-end keys.
 */
export function normalizeCampaign(camp) {
  if (!camp) return null;
  const parsedTypes = normalizeCampaignTypes(
    camp.types ?? camp.Types ?? camp.type ?? camp.Type
  );
  const types = parsedTypes.length > 0 ? parsedTypes : [CampaignType.Email];
  const rawMotivo = camp.motivo ?? camp.Motivo ?? null;
  const normalizedMotivo =
    rawMotivo && String(rawMotivo).toLowerCase() !== "none" ? rawMotivo : null;

  return {
    id: camp.id || camp.Id,
    name: camp.name || camp.Name || "",
    description: camp.description || camp.Description || "",
    modelMsg: camp.modelMsg || camp.ModelMsg || camp.text || camp.Text || "",
    types,
    type: types.join(", "),
    status: camp.status || camp.Status || CampaignStatus.New,
    createdAt: camp.createdAt || camp.CreatedAt || null,
    publicationDate: camp.publicationDate || camp.PublicationDate || null,
    imageUrl: camp.imageUrl ?? camp.ImageUrl ?? camp.image ?? null,
    userId: camp.userId ?? camp.UserId ?? null,
    motivo: normalizedMotivo,
    justificationText:
      camp.justificationText || camp.JustificationText || "",
  };
}

export async function fetchCampaigns() {
  const data = await apiRequest("/campaigns");
  return Array.isArray(data) ? data.map(normalizeCampaign) : [];
}

export async function fetchCampaignById(id) {
  const data = await apiRequest(`/campaigns/${encodeURIComponent(id)}`);
  return normalizeCampaign(data);
}

export async function createCampaignApi(payload) {
  const data = await apiRequest("/campaigns", {
    method: "POST",
    body: payload,
  });
  return normalizeCampaign(data);
}

export async function editCampaignApi(id, payload) {
  const data = await apiRequest(`/campaigns/${encodeURIComponent(id)}/edit`, {
    method: "PUT",
    body: payload,
  });
  return normalizeCampaign(data);
}

export async function cancelCampaignApi(id, payload) {
  const data = await apiRequest(`/campaigns/${encodeURIComponent(id)}/cancel`, {
    method: "PUT",
    body: payload,
  });
  return normalizeCampaign(data);
}

export async function approveCampaignApi(id) {
  const data = await apiRequest(
    `/campaigns/${encodeURIComponent(id)}/review/approve`,
    {
      method: "PUT",
    }
  );
  return normalizeCampaign(data);
}

export async function requestCampaignChangesApi(id, payload) {
  const data = await apiRequest(
    `/campaigns/${encodeURIComponent(id)}/review/request-changes`,
    {
      method: "PUT",
      body: payload,
    }
  );
  return normalizeCampaign(data);
}
