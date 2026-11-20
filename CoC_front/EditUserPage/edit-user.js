// ── Referências do DOM ──
const form = document.querySelector("#edit-user-form");
const subtitle = document.querySelector("#edit-user-subtitle");
const actionMessage = document.querySelector("#action-message");

const nameInput = document.querySelector("#user-name");
const emailInput = document.querySelector("#user-email");
const nameError = document.querySelector("#name-error");
const emailError = document.querySelector("#email-error");

const roleAdminCheckbox = document.querySelector("#role-admin");
const roleAprovandoCheckbox = document.querySelector("#role-aprovando");
const roleAprovadorCheckbox = document.querySelector("#role-aprovador");

// ── Carregar parâmetros da URL ──
const urlParams = new URLSearchParams(window.location.search);
const userId = urlParams.get("id");
const initialName = urlParams.get("name") || "";
const initialEmail = urlParams.get("email") || "";
const rawCaracteristics = urlParams.get("caracteristics") || "";

// Converte a string de características num Set para verificação rápida
const characteristicsSet = new Set(
  rawCaracteristics
    .split(",")
    .map((c) => c.trim())
    .filter(Boolean)
);

function populateForm() {
  if (initialName) {
    nameInput.value = initialName;
    subtitle.textContent = `Editing ${initialName}${userId ? ` (ID ${userId})` : ""}`;
  } else {
    subtitle.textContent = "Fill in the user details and assign roles.";
  }

  if (initialEmail) {
    emailInput.value = initialEmail;
  }

  // Checkboxes de Admin, Aprovando e Aprovador
  // Ficam marcadas (ticked) se o user contiver a respetiva característica
  roleAdminCheckbox.checked =
    characteristicsSet.has("Admin") || characteristicsSet.has("admin");

  roleAprovandoCheckbox.checked =
    characteristicsSet.has("isAprovando") ||
    characteristicsSet.has("Aprovando") ||
    characteristicsSet.has("aprovando");

  roleAprovadorCheckbox.checked =
    characteristicsSet.has("isAprovador") ||
    characteristicsSet.has("Aprovador") ||
    characteristicsSet.has("aprovador");
}

function clearErrors() {
  nameError.textContent = "";
  emailError.textContent = "";
  nameInput.classList.remove("invalid");
  emailInput.classList.remove("invalid");
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

  return valid;
}

// ── Submissão (grava no sessionStorage para persistir entre páginas no demo) ──
form.addEventListener("submit", (event) => {
  event.preventDefault();

  if (!validateForm()) {
    actionMessage.textContent = "Please fix the errors above.";
    actionMessage.className = "action-message error";
    return;
  }

  const updatedName = nameInput.value.trim();
  const updatedEmail = emailInput.value.trim();

  // Constrói lista de características selecionadas
  const updatedChars = [];
  if (roleAdminCheckbox.checked) updatedChars.push("Admin");
  if (roleAprovandoCheckbox.checked) updatedChars.push("isAprovando");
  if (roleAprovadorCheckbox.checked) updatedChars.push("isAprovador");

  // Se o utilizador já tinha isActive ou se foi passado na lista
  if (characteristicsSet.has("isActive")) {
    updatedChars.push("isActive");
  }

  // Atualiza no sessionStorage para o AccountsListingPage sincronizar
  try {
    const stored = sessionStorage.getItem("coc_accounts");
    if (stored && userId) {
      const accountsList = JSON.parse(stored);
      const target = accountsList.find((acc) => String(acc.id) === String(userId));
      if (target) {
        target.name = updatedName;
        target.email = updatedEmail;
        target.caracteristics = updatedChars;
        sessionStorage.setItem("coc_accounts", JSON.stringify(accountsList));
      }
    }
  } catch (e) {
    console.warn("Could not save to sessionStorage", e);
  }

  subtitle.textContent = `Editing ${updatedName}${userId ? ` (ID ${userId})` : ""}`;
  actionMessage.textContent = `User ${updatedName} updated successfully in this demo.`;
  actionMessage.className = "action-message success";
});

// ── Cancelar e repor valores originais ──
document.querySelector("#cancel-edit").addEventListener("click", () => {
  clearErrors();
  populateForm();
  actionMessage.textContent = "Form reset to initial values.";
  actionMessage.className = "action-message";
});

// ── Voltar à listagem de contas ──
document.querySelector("#back-to-accounts").addEventListener("click", () => {
  window.location.href = "../AccountListingPage/accounts.html";
});

// ── Inicialização ──
populateForm();
