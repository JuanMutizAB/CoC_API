export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "/api";

let unauthorizedHandler = null;

/**
 * Custom error carrying HTTP status code and parsed JSON payload from CoC_API.
 */
export class ApiError extends Error {
  constructor(message, status, data = null) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.data = data;
  }
}

/**
 * Registers a callback invoked whenever an API response returns 401 Unauthorized.
 */
export function setUnauthorizedHandler(handler) {
  unauthorizedHandler = handler;
}

/**
 * Sends a cookie-authenticated request to CoC_API and extracts `{ erro }` on failures.
 */
export async function apiRequest(
  path,
  { method = "GET", body, skipUnauthorizedHandler = false } = {}
) {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    credentials: "include",
    headers: body !== undefined ? { "Content-Type": "application/json" } : undefined,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  const text = await response.text();
  let payload = null;
  if (text) {
    try {
      payload = JSON.parse(text);
    } catch {
      payload = { erro: text };
    }
  }

  if (response.status === 401 && !skipUnauthorizedHandler && unauthorizedHandler) {
    unauthorizedHandler();
  }

  if (!response.ok) {
    const message =
      payload?.erro ||
      payload?.message ||
      payload?.title ||
      `Request failed (${response.status})`;
    throw new ApiError(message, response.status, payload);
  }

  if (response.status === 204) {
    return null;
  }

  return payload;
}
