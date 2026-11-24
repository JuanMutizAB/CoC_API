/**
 * Validates full name (non-empty and at least two parts).
 */
export function validateName(name) {
  const val = String(name || "").trim();
  if (!val) {
    return "Name is required.";
  }
  if (val.split(/\s+/).filter(Boolean).length < 2) {
    return "Name must contain at least a first and last name.";
  }
  return "";
}

/**
 * Validates email (non-empty and contains '@').
 */
export function validateEmail(email) {
  const val = String(email || "").trim();
  if (!val) {
    return "Email is required.";
  }
  if (!val.includes("@")) {
    return "Email must contain '@'.";
  }
  return "";
}

/**
 * Validates birth date (required and not in the future).
 */
export function validateBirthDate(birthVal) {
  if (!birthVal) {
    return "Birth date is required.";
  }
  const birthDate = new Date(birthVal);
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  if (birthDate > today) {
    return "Birth date cannot be in the future.";
  }
  return "";
}

/**
 * Validates Tax ID (required and exactly 9 numeric digits).
 */
export function validateTaxId(taxId) {
  const val = String(taxId || "").trim();
  if (!val) {
    return "Tax ID is required.";
  }
  if (!/^\d{9}$/.test(val)) {
    return "Tax ID must have exactly 9 digits.";
  }
  return "";
}

/**
 * Validates password complexity (>8 chars, uppercase, lowercase, digit, special char).
 */
export function validatePassword(password, { required = false } = {}) {
  const val = String(password || "");
  if (!val) {
    return required ? "Password is required." : "";
  }
  if (val.length <= 8) {
    return "Password must have more than 8 characters.";
  }
  if (!/[A-Z]/.test(val)) {
    return "Password must contain at least one uppercase letter.";
  }
  if (!/[a-z]/.test(val)) {
    return "Password must contain at least one lowercase letter.";
  }
  if (!/\d/.test(val)) {
    return "Password must contain at least one digit.";
  }
  if (!/[^A-Za-z0-9]/.test(val)) {
    return "Password must contain at least one special character.";
  }
  return "";
}

/**
 * Validates an optional http(s) image URL.
 */
export function validateImageUrl(url) {
  const val = String(url || "").trim();
  if (!val) {
    return "";
  }
  if (!/^https?:\/\/\S+$/i.test(val)) {
    return "Image URL must be a valid http(s) URL.";
  }
  return "";
}
