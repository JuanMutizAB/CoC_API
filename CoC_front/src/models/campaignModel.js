const CAMPAIGN_STORAGE_KEY = "coc_campaign";
const FEEDBACKS_STORAGE_KEY = "coc_campaign_feedbacks";

export const Motivo = Object.freeze({
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
