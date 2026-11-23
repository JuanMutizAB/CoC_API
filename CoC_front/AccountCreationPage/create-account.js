// ── Referências do DOM ──
const form = document.querySelector("#create-account-form");
const actionMessage = document.querySelector("#action-message");

const nameInput = document.querySelector("#user-name");
const emailInput = document.querySelector("#user-email");
const birthDateInput = document.querySelector("#user-birthdate");
const taxIdInput = document.querySelector("#user-taxid");
const passwordInput = document.querySelector("#user-password");

const nameError = document.querySelector("#name-error");
const emailError = document.querySelector("#email-error");
const birthDateError = document.querySelector("#birthdate-error");
const taxIdError = document.querySelector("#taxid-error");
const passwordError = document.querySelector("#password-error");

const roleAdminCheckbox = document.querySelector("#role-admin");
const roleAprovandoCheckbox = document.querySelector("#role-aprovando");
const roleAprovadorCheckbox = document.querySelector("#role-aprovador");

// ── Password toggle ──
const passwordToggle = document.querySelector(".password-toggle");
if (passwordToggle) {
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

// ── Limpeza de Erros ──
function clearErrors() {
  nameError.textContent = "";
  emailError.textContent = "";
  birthDateError.textContent = "";
  taxIdError.textContent = "";
  passwordError.textContent = "";

  nameInput.classList.remove("invalid");
  emailInput.classList.remove("invalid");
  birthDateInput.classList.remove("invalid");
  taxIdInput.classList.remove("invalid");
  passwordInput.classList.remove("invalid");

  actionMessage.textContent = "";
  actionMessage.className = "action-message";
}

// ── Validação rigorosa conforme regras do modelo User ──
function validateForm() {
  clearErrors();
  let valid = true;

  // 1. Nome: não pode ser vazio e tem de ter pelo menos 2 partes
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

  // 2. Email: não pode ser vazio e tem de conter '@'
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

  // 3. Data de nascimento: obrigatória e não pode estar no futuro
  const birthVal = birthDateInput.value;
  if (!birthVal) {
    birthDateError.textContent = "Birth date is required.";
    birthDateInput.classList.add("invalid");
    valid = false;
  } else {
    const birthDate = new Date(birthVal);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    if (birthDate > today) {
      birthDateError.textContent = "Birth date cannot be in the future.";
      birthDateInput.classList.add("invalid");
      valid = false;
    }
  }

  // 4. Tax ID: exatamente 9 dígitos numéricos
  const taxIdVal = taxIdInput.value.trim();
  if (!taxIdVal) {
    taxIdError.textContent = "Tax ID is required.";
    taxIdInput.classList.add("invalid");
    valid = false;
  } else if (taxIdVal.length !== 9 || !/^\d{9}$/.test(taxIdVal)) {
    taxIdError.textContent = "Tax ID must have exactly 9 digits.";
    taxIdInput.classList.add("invalid");
    valid = false;
  }

  // 5. Password: >8 chars, maiúscula, minúscula, dígito e símbolo
  const passwordVal = passwordInput.value;
  if (!passwordVal) {
    passwordError.textContent = "Password is required.";
    passwordInput.classList.add("invalid");
    valid = false;
  } else if (passwordVal.length <= 8) {
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

  return valid;
}

// ── Submissão do Formulário de Criação ──
form.addEventListener("submit", (event) => {
  event.preventDefault();

  if (!validateForm()) {
    actionMessage.textContent = "Please fix the errors above.";
    actionMessage.className = "action-message error";
    return;
  }

  const name = nameInput.value.trim();
  const email = emailInput.value.trim();
  const birthDate = birthDateInput.value;
  const taxId = taxIdInput.value.trim();

  // Constrói lista de características
  const caracteristics = ["isActive"];
  if (roleAdminCheckbox.checked) caracteristics.push("Admin");
  if (roleAprovandoCheckbox.checked) caracteristics.push("isAprovando");
  if (roleAprovadorCheckbox.checked) caracteristics.push("isAprovador");

  // Guarda na lista em sessionStorage para persistência demo
  const accountsList = loadStoredAccounts();
  const newId = accountsList.length > 0
    ? Math.max(...accountsList.map((a) => Number(a.id) || 0)) + 1
    : 1;

  const newAccount = {
    id: newId,
    name,
    email,
    birthDate,
    taxId,
    active: true,
    caracteristics
  };

  accountsList.push(newAccount);
  saveStoredAccounts(accountsList);

  try {
    sessionStorage.setItem(
      "coc_flash_message",
      `Account for ${name} (ID ${newId}) was created successfully.`
    );
  } catch (e) {
    // ignore
  }

  // Volta automaticamente à listagem de contas
  window.location.href = "../AccountListingPage/accounts.html";
});

// ── Cancelar ──
document.querySelector("#cancel-create").addEventListener("click", () => {
  form.reset();
  clearErrors();
  actionMessage.textContent = "Form cleared.";
  actionMessage.className = "action-message";
});

// ── Voltar à listagem ──
document.querySelector("#back-to-accounts").addEventListener("click", () => {
  window.location.href = "../AccountListingPage/accounts.html";
});
