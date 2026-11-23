// ── Referências do DOM ──
const form = document.querySelector("#edit-user-form");
const subtitle = document.querySelector("#edit-user-subtitle");
const actionMessage = document.querySelector("#action-message");

const nameInput = document.querySelector("#user-name");
const emailInput = document.querySelector("#user-email");
const passwordInput = document.querySelector("#user-password");

const nameError = document.querySelector("#name-error");
const emailError = document.querySelector("#email-error");
const passwordError = document.querySelector("#password-error");

const roleAdminCheckbox = document.querySelector("#role-admin");
const roleAprovandoCheckbox = document.querySelector("#role-aprovando");
const roleAprovadorCheckbox = document.querySelector("#role-aprovador");

// ── Password toggle ──
const passwordToggle = document.querySelector(".password-toggle");
if (passwordToggle && passwordInput) {
  passwordToggle.addEventListener("click", () => {
    const isPressed = passwordToggle.getAttribute("aria-pressed") === "true";
    passwordToggle.setAttribute("aria-pressed", String(!isPressed));
    passwordInput.type = isPressed ? "password" : "text";
    passwordToggle.setAttribute(
      "aria-label",
      isPressed ? "Show password" : "Hide password"
    );
  });
}

// ── Estado do utilizador ──
let accountsList = [];
let currentUser = null;
let initialUserState = null;
let isOriginalAdmin = false;

// ── Carregar e guardar no sessionStorage ──
function loadStoredAccounts() {
  try {
    const stored = sessionStorage.getItem("coc_accounts");
    if (stored) {
      return JSON.parse(stored);
    }
  } catch (e) {
    console.warn("Could not read coc_accounts from sessionStorage", e);
  }
  return [];
}

function saveStoredAccounts(list) {
  try {
    sessionStorage.setItem("coc_accounts", JSON.stringify(list));
  } catch (e) {
    console.warn("Could not save coc_accounts to sessionStorage", e);
  }
}

// ── Inicialização dos dados do utilizador ──
function initUser() {
  accountsList = loadStoredAccounts();

  const urlParams = new URLSearchParams(window.location.search);
  const paramId = urlParams.get("id");
  const paramName = urlParams.get("name") || "";
  const paramEmail = urlParams.get("email") || "";
  const paramBirthDate = urlParams.get("birthDate") || "1990-01-01";
  const paramChars = (urlParams.get("caracteristics") || "")
    .split(",")
    .map((c) => c.trim())
    .filter(Boolean);

  // 1. Procura o utilizador por ID na lista de contas guardadas
  if (paramId && accountsList.length > 0) {
    currentUser = accountsList.find((acc) => String(acc.id) === String(paramId));
  }

  // Determina características (do utilizador guardado ou dos parâmetros da URL)
  let resolvedChars = [];
  if (currentUser && Array.isArray(currentUser.caracteristics) && currentUser.caracteristics.length > 0) {
    resolvedChars = currentUser.caracteristics;
  } else if (currentUser && typeof currentUser.caracteristics === "string" && currentUser.caracteristics.trim()) {
    resolvedChars = currentUser.caracteristics.split(",").map((c) => c.trim()).filter(Boolean);
  } else if (paramChars.length > 0) {
    resolvedChars = paramChars;
  }

  // 2. Se não existir na lista guardada, constrói a partir dos parâmetros da URL
  if (!currentUser) {
    currentUser = {
      id: paramId ? Number(paramId) : accountsList.length + 1,
      name: paramName || "User",
      email: paramEmail || "",
      birthDate: paramBirthDate,
      active: true,
      caracteristics: resolvedChars.length > 0 ? resolvedChars : ["isActive"]
    };
    if (paramId) {
      accountsList.push(currentUser);
      saveStoredAccounts(accountsList);
    }
  } else {
    currentUser.caracteristics = resolvedChars.length > 0 ? resolvedChars : (currentUser.caracteristics || ["isActive"]);
    if (!currentUser.birthDate) {
      currentUser.birthDate = paramBirthDate;
    }
  }

  // 3. Guarda estado inicial para a função de cancelar
  initialUserState = {
    name: currentUser.name || "",
    email: currentUser.email || "",
    caracteristics: Array.isArray(currentUser.caracteristics)
      ? [...currentUser.caracteristics]
      : []
  };

  const initialChars = (Array.isArray(initialUserState.caracteristics)
    ? initialUserState.caracteristics
    : String(initialUserState.caracteristics || "").split(",")
  ).map((c) => String(c).trim().toLowerCase());
  isOriginalAdmin = initialChars.includes("admin");

  populateForm(initialUserState);
}

function populateForm(state) {
  nameInput.value = state.name;
  emailInput.value = state.email;
  if (passwordInput) {
    passwordInput.value = "";
  }
  subtitle.textContent = `Editing ${state.name}${currentUser && currentUser.id ? ` (ID ${currentUser.id})` : ""}`;

  // Características / Checkboxes
  const rawChars = Array.isArray(state.caracteristics)
    ? state.caracteristics
    : String(state.caracteristics || "").split(",");

  const chars = rawChars
    .map((c) => String(c).trim().toLowerCase())
    .filter(Boolean);

  const hasAdmin = chars.some((c) => c === "admin");
  roleAdminCheckbox.checked = hasAdmin;
  roleAprovandoCheckbox.checked = chars.some(
    (c) => c === "isaprovando" || c === "aprovando"
  );
  roleAprovadorCheckbox.checked = chars.some(
    (c) => c === "isaprovador" || c === "aprovador"
  );

  const adminToggle = roleAdminCheckbox.closest(".characteristic-toggle");
  const adminLockedBadge = document.querySelector("#admin-locked-badge");

  if (isOriginalAdmin) {
    roleAdminCheckbox.disabled = true;
    roleAdminCheckbox.checked = true;
    if (adminToggle) {
      adminToggle.classList.add("is-locked");
      adminToggle.title = "Admin status cannot be removed once granted";
    }
    if (adminLockedBadge) {
      adminLockedBadge.hidden = false;
    }
  } else {
    roleAdminCheckbox.disabled = false;
    if (adminToggle) {
      adminToggle.classList.remove("is-locked");
      adminToggle.removeAttribute("title");
    }
    if (adminLockedBadge) {
      adminLockedBadge.hidden = true;
    }
  }

  clearErrors();
}

// ── Validações ──
function clearErrors() {
  nameError.textContent = "";
  emailError.textContent = "";
  passwordError.textContent = "";

  nameInput.classList.remove("invalid");
  emailInput.classList.remove("invalid");
  if (passwordInput) {
    passwordInput.classList.remove("invalid");
  }

  actionMessage.textContent = "";
  actionMessage.className = "action-message";
}

function validateForm() {
  clearErrors();
  let valid = true;

  const nameVal = nameInput.value.trim();
  if (!nameVal) {
    nameError.textContent = "Name is required.";
    nameInput.classList.add("invalid");
    valid = false;
  } else if (nameVal.split(/\s+/).filter(Boolean).length < 2) {
    nameError.textContent = "Name must contain at least a first and last name.";
    nameInput.classList.add("invalid");
    valid = false;
  }

  const emailVal = emailInput.value.trim();
  if (!emailVal) {
    emailError.textContent = "Email is required.";
    emailInput.classList.add("invalid");
    valid = false;
  } else if (!emailVal.includes("@")) {
    emailError.textContent = "Email must contain '@'.";
    emailInput.classList.add("invalid");
    valid = false;
  }

  // Password é opcional na edição; se for preenchida, valida os requisitos
  const passwordVal = passwordInput ? passwordInput.value : "";
  if (passwordVal) {
    if (passwordVal.length <= 8) {
      passwordError.textContent = "Password must have more than 8 characters.";
      passwordInput.classList.add("invalid");
      valid = false;
    } else if (!/[A-Z]/.test(passwordVal)) {
      passwordError.textContent = "Password must contain at least one uppercase letter.";
      passwordInput.classList.add("invalid");
      valid = false;
    } else if (!/[a-z]/.test(passwordVal)) {
      passwordError.textContent = "Password must contain at least one lowercase letter.";
      passwordInput.classList.add("invalid");
      valid = false;
    } else if (!/\d/.test(passwordVal)) {
      passwordError.textContent = "Password must contain at least one digit.";
      passwordInput.classList.add("invalid");
      valid = false;
    } else if (!/[^A-Za-z0-9]/.test(passwordVal)) {
      passwordError.textContent = "Password must contain at least one special character.";
      passwordInput.classList.add("invalid");
      valid = false;
    }
  }

  return valid;
}

// ── Submissão do Formulário ──
form.addEventListener("submit", (event) => {
  event.preventDefault();

  if (!validateForm()) {
    actionMessage.textContent = "Please fix the errors above.";
    actionMessage.className = "action-message error";
    return;
  }

  const updatedName = nameInput.value.trim();
  const updatedEmail = emailInput.value.trim();
  const newPassword = passwordInput ? passwordInput.value : "";

  // Constrói lista atualizada de características
  const updatedChars = [];
  if (isOriginalAdmin || roleAdminCheckbox.checked) updatedChars.push("Admin");
  if (roleAprovandoCheckbox.checked) updatedChars.push("isAprovando");
  if (roleAprovadorCheckbox.checked) updatedChars.push("isAprovador");

  // Preserva isActive se existia
  const hadIsActive = (currentUser.caracteristics || []).some(
    (c) => c.toLowerCase() === "isactive"
  );
  if (hadIsActive || currentUser.active) {
    updatedChars.push("isActive");
  }

  // Atualiza utilizador
  currentUser.name = updatedName;
  currentUser.email = updatedEmail;
  currentUser.caracteristics = updatedChars;
  if (newPassword) {
    currentUser.password = newPassword;
  }

  // Persiste em sessionStorage
  const targetIndex = accountsList.findIndex(
    (acc) => String(acc.id) === String(currentUser.id)
  );
  if (targetIndex !== -1) {
    accountsList[targetIndex] = currentUser;
  } else {
    accountsList.push(currentUser);
  }
  saveStoredAccounts(accountsList);

  try {
    sessionStorage.setItem(
      "coc_flash_message",
      `User ${updatedName} updated successfully.${newPassword ? " Password was changed." : ""}`
    );
  } catch (e) {
    // ignore
  }

  // Volta automaticamente à listagem de contas
  window.location.href = "../AccountListingPage/accounts.html";
});

// ── Voltar à listagem ──
document.querySelector("#back-to-accounts").addEventListener("click", () => {
  window.location.href = "../AccountListingPage/accounts.html";
});

// ── Arranque ──
initUser();
