import { apiRequest } from "./apiClient";

/**
 * Normalizes characteristics from an array or comma-separated string.
 */
export function normalizeCharacteristics(value) {
  const raw = Array.isArray(value)
    ? value
    : String(value || "").split(",");

  return raw
    .map((item) => String(item).trim())
    .filter(Boolean);
}

/**
 * Case-insensitive role check handling "admin", "isaprovando"/"aprovando",
 * "isaprovador"/"aprovador", and "isactive".
 */
export function hasRole(caracteristics, roleName) {
  const normalized = normalizeCharacteristics(caracteristics).map((c) =>
    c.toLowerCase()
  );
  const target = String(roleName || "").trim().toLowerCase();

  if (target === "admin") {
    return normalized.includes("admin");
  }
  if (target === "aprovando" || target === "isaprovando") {
    return normalized.includes("isaprovando") || normalized.includes("aprovando");
  }
  if (target === "aprovador" || target === "isaprovador") {
    return normalized.includes("isaprovador") || normalized.includes("aprovador");
  }
  if (target === "isactive" || target === "active") {
    return normalized.includes("isactive");
  }
  return normalized.includes(target);
}

/**
 * Normalizes a User object returned by CoC_API into consistent front-end properties.
 */
export function normalizeAccount(raw) {
  if (!raw) return null;
  const caracteristics = normalizeCharacteristics(
    raw.caracteristics ?? raw.Caracteristics
  );

  return {
    id: raw.id ?? raw.Id,
    name: raw.name ?? raw.Name ?? "",
    email: raw.email ?? raw.Email ?? "",
    birthDate: raw.birthDate ?? raw.BirthDate ?? "",
    taxId: raw.taxId ?? raw.TaxId ?? "",
    caracteristics,
    active: hasRole(caracteristics, "isActive"),
  };
}

/**
 * Formats YYYY-MM-DD to DD-MM-YYYY safely.
 */
export function formatBirthDate(value) {
  if (!value || typeof value !== "string") {
    return "—";
  }
  const parts = value.split("-");
  if (parts.length === 3) {
    const [year, month, day] = parts;
    return `${day}-${month}-${year}`;
  }
  return value;
}

export async function fetchAccounts() {
  const data = await apiRequest("/users");
  return Array.isArray(data) ? data.map(normalizeAccount) : [];
}

export async function fetchAccountById(id) {
  const data = await apiRequest(`/users/${encodeURIComponent(id)}`);
  return normalizeAccount(data);
}

export async function createAccountApi(payload) {
  const data = await apiRequest("/users", {
    method: "POST",
    body: payload,
  });
  return normalizeAccount(data);
}

export async function updateAccountApi(id, payload) {
  return apiRequest(`/users/${encodeURIComponent(id)}`, {
    method: "PUT",
    body: payload,
  });
}

export async function deactivateAccountApi(id) {
  return apiRequest(`/users/${encodeURIComponent(id)}/deactivate`, {
    method: "PUT",
  });
}
