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
      return JSON.parse(stored);
    }
  } catch (e) {
    console.warn("Could not read from sessionStorage", e);
  }
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

const accountsList = document.querySelector("#accounts-list");
const searchInput = document.querySelector("#account-search");
const showInactiveInput = document.querySelector("#show-inactive");
const resultsCount = document.querySelector("#results-count");
const actionMessage = document.querySelector("#action-message");
const sortButtons = document.querySelectorAll("[data-sort]");

let sortField = "name";
let sortDirection = "ascending";

function formatBirthDate(value) {
  const [year, month, day] = value.split("-");
  return `${day}-${month}-${year}`;
}

function getVisibleAccounts() {
  const search = searchInput.value.trim().toLocaleLowerCase();
  const showInactive = showInactiveInput.checked;

  return accounts
    .filter((account) => showInactive || account.active)
    .filter((account) =>
      `${account.name} ${account.email}`.toLocaleLowerCase().includes(search)
    )
    .sort((left, right) => {
      const comparison = left[sortField].localeCompare(right[sortField], "pt-PT");
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

function renderAccounts() {
  const visibleAccounts = getVisibleAccounts();
  accountsList.replaceChildren();

  if (visibleAccounts.length === 0) {
    const row = document.createElement("tr");
    const cell = createCell("empty-state", "No accounts match your search.");
    cell.colSpan = 4;
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
      row.append(
        nameCell,
        createCell("", formatBirthDate(account.birthDate)),
        createCell("account-email", account.email)
      );

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
      row.append(actionCell);
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
  actionMessage.textContent = "The account form is not part of this listing page yet.";
});

document.querySelector("#back-to-campaigns").addEventListener("click", () => {
  actionMessage.textContent = "The campaigns page is not part of this demo yet.";
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
    const chars = (account.caracteristics || []).join(",");
    const params = new URLSearchParams({
      id: String(account.id),
      name: account.name,
      email: account.email,
      caracteristics: chars
    });
    window.location.href = `../EditUserPage/edit-user.html?${params.toString()}`;
    return;
  }

  if (actionButton.dataset.accountAction === "deactivate" && account.active) {
    account.active = false;
    saveStoredAccounts(accounts);
    actionMessage.textContent = `${account.name} was deactivated in this demo.`;
    renderAccounts();
  }
});

renderAccounts();
