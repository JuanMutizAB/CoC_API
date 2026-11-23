const initialAccounts = [
  {
    id: 1,
    name: "Eduardo",
    birthDate: "1990-11-12",
    email: "eduardo@example.com",
    active: true,
    caracteristics: ["Admin", "isActive"]
  },
  {
    id: 2,
    name: "Juan",
    birthDate: "1988-07-07",
    email: "juan@example.com",
    active: false,
    caracteristics: ["isAprovador"]
  },
  {
    id: 3,
    name: "Ana Martins",
    birthDate: "1992-04-23",
    email: "ana.martins@example.com",
    active: true,
    caracteristics: ["isAprovando", "isActive"]
  },
  {
    id: 4,
    name: "Luis Costa",
    birthDate: "1986-02-15",
    email: "luis.costa@example.com",
    active: true,
    caracteristics: ["isActive"]
  },
  {
    id: 5,
    name: "Marta Silva",
    birthDate: "1995-08-29",
    email: "marta.silva@example.com",
    active: true,
    caracteristics: ["isAprovador", "isActive"]
  },
  {
    id: 6,
    name: "Rui Ferreira",
    birthDate: "1989-10-10",
    email: "rui.ferreira@example.com",
    active: true,
    caracteristics: ["Admin", "isAprovador", "isActive"]
  },
  {
    id: 7,
    name: "Sofia Almeida",
    birthDate: "1994-06-01",
    email: "sofia.almeida@example.com",
    active: true,
    caracteristics: ["isActive"]
  }
];

const demoFirstNames = [
  "Bruno",
  "Carla",
  "Diogo",
  "Eva",
  "Filipe",
  "Ines",
  "Joao",
  "Leonor",
  "Miguel",
  "Teresa"
];
const demoLastNames = [
  "Alves",
  "Barros",
  "Carvalho",
  "Dias",
  "Esteves",
  "Fonseca"
];

for (let firstNameIndex = 0; firstNameIndex < demoFirstNames.length; firstNameIndex += 1) {
  for (let lastNameIndex = 0; lastNameIndex < demoLastNames.length; lastNameIndex += 1) {
    const id = initialAccounts.length + 1;
    const firstName = demoFirstNames[firstNameIndex];
    const lastName = demoLastNames[lastNameIndex];
    const year = 1980 + (id * 3) % 25;
    const month = String((id * 7) % 12 + 1).padStart(2, "0");
    const day = String((id * 11) % 28 + 1).padStart(2, "0");

    const caracteristics = ["isActive"];
    if (id % 5 === 0) caracteristics.push("Admin");
    if (id % 3 === 0) caracteristics.push("isAprovador");
    if (id % 4 === 0) caracteristics.push("isAprovando");

    initialAccounts.push({
      id,
      name: `${firstName} ${lastName}`,
      birthDate: `${year}-${month}-${day}`,
      email: `${firstName.toLowerCase()}.${lastName.toLowerCase()}${id}@example.com`,
      active: id % 9 !== 0,
      caracteristics
    });
  }
}

// Persistência em sessionStorage para sincronizar alterações com a página de edição no demo
function loadStoredAccounts() {
  try {
    const stored = sessionStorage.getItem("coc_accounts");
    if (stored) {
      const parsed = JSON.parse(stored);
      if (Array.isArray(parsed) && parsed.length >= initialAccounts.length) {
        return parsed;
      }
      if (Array.isArray(parsed) && parsed.length > 0) {
        // Se a lista guardada tiver menos registos (ex: por edição isolada anterior),
        // funde com as contas iniciais para garantir que os utilizadores não desaparecem
        const merged = [...initialAccounts];
        for (const item of parsed) {
          const idx = merged.findIndex((m) => String(m.id) === String(item.id));
          if (idx !== -1) {
            merged[idx] = { ...merged[idx], ...item };
          } else {
            merged.push(item);
          }
        }
        saveStoredAccounts(merged);
        return merged;
      }
    }
  } catch (e) {
    console.warn("Could not read from sessionStorage", e);
  }
  saveStoredAccounts(initialAccounts);
  return initialAccounts;
}

function saveStoredAccounts(list) {
  try {
    sessionStorage.setItem("coc_accounts", JSON.stringify(list));
  } catch (e) {
    console.warn("Could not save to sessionStorage", e);
  }
}

const accounts = loadStoredAccounts();
saveStoredAccounts(accounts);

const accountsList = document.querySelector("#accounts-list");
const searchInput = document.querySelector("#account-search");
const showInactiveInput = document.querySelector("#show-inactive");
const resultsCount = document.querySelector("#results-count");
const actionMessage = document.querySelector("#action-message");
const sortButtons = document.querySelectorAll("[data-sort]");

let sortField = "name";
let sortDirection = "ascending";

function formatBirthDate(value) {
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

function getVisibleAccounts() {
  const search = searchInput.value.trim().toLocaleLowerCase();
  const showInactive = showInactiveInput.checked;

  return accounts
    .filter((account) => showInactive || account.active)
    .filter((account) => {
      const rolesStr = (account.caracteristics || []).join(" ");
      return `${account.name} ${account.email} ${rolesStr}`.toLocaleLowerCase().includes(search);
    })
    .sort((left, right) => {
      const leftVal = String(left[sortField] ?? "");
      const rightVal = String(right[sortField] ?? "");
      const comparison = leftVal.localeCompare(rightVal, "pt-PT");
      return sortDirection === "ascending" ? comparison : -comparison;
    });
}

function createCell(className, text) {
  const cell = document.createElement("td");
  if (className) {
    cell.className = className;
  }
  cell.textContent = text;
  return cell;
}

function createRolesCell(caracteristics = []) {
  const cell = document.createElement("td");
  cell.className = "roles-cell";

  const wrap = document.createElement("div");
  wrap.className = "roles-badges";

  const chars = (Array.isArray(caracteristics)
    ? caracteristics
    : String(caracteristics || "").split(",")
  )
    .map((c) => c.trim().toLowerCase())
    .filter(Boolean);

  const hasAdmin = chars.some((c) => c === "admin");
  const hasAprovando = chars.some((c) => c === "isaprovando" || c === "aprovando");
  const hasAprovador = chars.some((c) => c === "isaprovador" || c === "aprovador");

  if (!hasAdmin && !hasAprovando && !hasAprovador) {
    const defaultBadge = document.createElement("span");
    defaultBadge.className = "role-badge role-badge--none";
    defaultBadge.textContent = "—";
    wrap.append(defaultBadge);
  } else {
    if (hasAdmin) {
      const badge = document.createElement("span");
      badge.className = "role-badge role-badge--admin";
      badge.textContent = "Admin";
      wrap.append(badge);
    }
    if (hasAprovador) {
      const badge = document.createElement("span");
      badge.className = "role-badge role-badge--aprovador";
      badge.textContent = "Aprovador";
      wrap.append(badge);
    }
    if (hasAprovando) {
      const badge = document.createElement("span");
      badge.className = "role-badge role-badge--aprovando";
      badge.textContent = "Aprovando";
      wrap.append(badge);
    }
  }

  cell.append(wrap);
  return cell;
}

function renderAccounts() {
  const visibleAccounts = getVisibleAccounts();
  accountsList.replaceChildren();

  if (visibleAccounts.length === 0) {
    const row = document.createElement("tr");
    const cell = createCell("empty-state", "No accounts match your search.");
    cell.colSpan = 5;
    row.append(cell);
    accountsList.append(row);
  } else {
    for (const account of visibleAccounts) {
      const row = document.createElement("tr");
      const nameCell = document.createElement("td");
      const nameContent = document.createElement("div");
      const name = document.createElement("span");
      nameContent.className = "account-name-cell";
      name.className = "account-name";
      name.textContent = account.name;
      nameContent.append(name);

      if (!account.active) {
        const state = document.createElement("span");
        state.className = "account-state";
        state.textContent = "Inactive";
        nameContent.append(state);
      }

      nameCell.append(nameContent);

      const actionCell = document.createElement("td");
      const editButton = document.createElement("button");
      const deactivateButton = document.createElement("button");
      const rowActions = document.createElement("div");
      rowActions.className = "row-actions";
      editButton.className = "edit-button";
      editButton.type = "button";
      editButton.dataset.accountAction = "edit";
      editButton.dataset.userId = String(account.id);
      editButton.textContent = "Edit";
      editButton.setAttribute("aria-label", `Edit ${account.name}`);
      deactivateButton.className = "deactivate-button";
      deactivateButton.type = "button";
      deactivateButton.dataset.accountAction = "deactivate";
      deactivateButton.dataset.userId = String(account.id);
      deactivateButton.textContent = account.active ? "Deactivate" : "Deactivated";
      deactivateButton.disabled = !account.active;
      deactivateButton.setAttribute("aria-label", `Deactivate ${account.name}`);
      rowActions.append(editButton, deactivateButton);
      actionCell.append(rowActions);

      row.append(
        nameCell,
        createRolesCell(account.caracteristics),
        createCell("", formatBirthDate(account.birthDate)),
        createCell("account-email", account.email),
        actionCell
      );
      accountsList.append(row);
    }
  }

  resultsCount.textContent = `Showing ${visibleAccounts.length} of ${accounts.length} accounts`;
  updateSortIndicators();
}

function updateSortIndicators() {
  for (const button of sortButtons) {
    const heading = button.closest("th");
    const isSorted = button.dataset.sort === sortField;
    const indicator = button.querySelector(".sort-indicator");
    heading.setAttribute(
      "aria-sort",
      isSorted ? sortDirection : "none"
    );
    indicator.textContent = isSorted
      ? sortDirection === "ascending" ? "↑" : "↓"
      : "↕";
  }
}

searchInput.addEventListener("input", renderAccounts);
showInactiveInput.addEventListener("change", renderAccounts);

for (const button of sortButtons) {
  button.addEventListener("click", () => {
    if (sortField === button.dataset.sort) {
      sortDirection = sortDirection === "ascending" ? "descending" : "ascending";
    } else {
      sortField = button.dataset.sort;
      sortDirection = "ascending";
    }
    renderAccounts();
  });
}

document.querySelector("#create-account").addEventListener("click", () => {
  window.location.href = "../AccountCreationPage/create-account.html";
});

document.querySelector("#back-to-campaigns").addEventListener("click", () => {
  actionMessage.textContent = "The campaigns page is not part of this demo yet.";
});

// ── Modal de confirmação para desativação ──
const deactivateModal = document.querySelector("#deactivate-modal");
const modalAccountName = document.querySelector("#modal-account-name");
const modalCancelBtn = document.querySelector("#modal-cancel-btn");
const modalConfirmBtn = document.querySelector("#modal-confirm-btn");
let pendingDeactivateAccount = null;

function openDeactivateModal(account) {
  pendingDeactivateAccount = account;
  modalAccountName.textContent = account.name;
  deactivateModal.hidden = false;
  modalConfirmBtn.focus();
}

function closeDeactivateModal() {
  deactivateModal.hidden = true;
  pendingDeactivateAccount = null;
}

modalCancelBtn.addEventListener("click", closeDeactivateModal);

deactivateModal.addEventListener("click", (event) => {
  if (event.target === deactivateModal) {
    closeDeactivateModal();
  }
});

document.addEventListener("keydown", (event) => {
  if (event.key === "Escape" && !deactivateModal.hidden) {
    closeDeactivateModal();
  }
});

modalConfirmBtn.addEventListener("click", () => {
  if (pendingDeactivateAccount && pendingDeactivateAccount.active) {
    pendingDeactivateAccount.active = false;
    saveStoredAccounts(accounts);
    actionMessage.textContent = `${pendingDeactivateAccount.name} was deactivated in this demo.`;
    renderAccounts();
  }
  closeDeactivateModal();
});

accountsList.addEventListener("click", (event) => {
  const actionButton = event.target.closest("[data-account-action]");
  if (!actionButton) {
    return;
  }

  const account = accounts.find(({ id }) => id === Number(actionButton.dataset.userId));
  if (!account) {
    return;
  }

  if (actionButton.dataset.accountAction === "edit") {
    saveStoredAccounts(accounts);
    const chars = (account.caracteristics || []).join(",");
    const params = new URLSearchParams({
      id: String(account.id),
      name: account.name,
      email: account.email,
      birthDate: account.birthDate || "",
      caracteristics: chars
    });
    window.location.href = `../EditUserPage/edit-user.html?${params.toString()}`;
    return;
  }

  if (actionButton.dataset.accountAction === "deactivate" && account.active) {
    openDeactivateModal(account);
  }
});

renderAccounts();

// ── Exibir mensagem de confirmação vinda de Edit ou Create ──
try {
  const flashMessage = sessionStorage.getItem("coc_flash_message");
  if (flashMessage) {
    sessionStorage.removeItem("coc_flash_message");
    actionMessage.textContent = flashMessage;
  }
} catch (e) {
  console.warn("Could not read flash message from sessionStorage", e);
}
