const STORAGE_KEY = "coc_accounts";

const NAMED_ACCOUNTS = [
  {
    id: 1,
    name: "Eduardo",
    birthDate: "1990-11-12",
    email: "eduardo@example.com",
    active: true,
    caracteristics: ["Admin", "isActive"],
  },
  {
    id: 2,
    name: "Juan",
    birthDate: "1988-07-07",
    email: "juan@example.com",
    active: false,
    caracteristics: ["isAprovador"],
  },
  {
    id: 3,
    name: "Ana Martins",
    birthDate: "1992-04-23",
    email: "ana.martins@example.com",
    active: true,
    caracteristics: ["isAprovando", "isActive"],
  },
  {
    id: 4,
    name: "Luis Costa",
    birthDate: "1986-02-15",
    email: "luis.costa@example.com",
    active: true,
    caracteristics: ["isActive"],
  },
  {
    id: 5,
    name: "Marta Silva",
    birthDate: "1995-08-29",
    email: "marta.silva@example.com",
    active: true,
    caracteristics: ["isAprovador", "isActive"],
  },
  {
    id: 6,
    name: "Rui Ferreira",
    birthDate: "1989-10-10",
    email: "rui.ferreira@example.com",
    active: true,
    caracteristics: ["Admin", "isAprovador", "isActive"],
  },
  {
    id: 7,
    name: "Sofia Almeida",
    birthDate: "1994-06-01",
    email: "sofia.almeida@example.com",
    active: true,
    caracteristics: ["isActive"],
  },
];

const DEMO_FIRST_NAMES = [
  "Bruno",
  "Carla",
  "Diogo",
  "Eva",
  "Filipe",
  "Ines",
  "Joao",
  "Leonor",
  "Miguel",
  "Teresa",
];

const DEMO_LAST_NAMES = [
  "Alves",
  "Barros",
  "Carvalho",
  "Dias",
  "Esteves",
  "Fonseca",
];

/**
 * Generates the initial 67 demo accounts (7 named + 60 generated combinations).
 */
export function createInitialAccounts() {
  const accounts = NAMED_ACCOUNTS.map((acc) => ({
    ...acc,
    caracteristics: [...acc.caracteristics],
  }));

  for (let firstNameIndex = 0; firstNameIndex < DEMO_FIRST_NAMES.length; firstNameIndex += 1) {
    for (let lastNameIndex = 0; lastNameIndex < DEMO_LAST_NAMES.length; lastNameIndex += 1) {
      const id = accounts.length + 1;
      const firstName = DEMO_FIRST_NAMES[firstNameIndex];
      const lastName = DEMO_LAST_NAMES[lastNameIndex];
      const year = 1980 + ((id * 3) % 25);
      const month = String(((id * 7) % 12) + 1).padStart(2, "0");
      const day = String(((id * 11) % 28) + 1).padStart(2, "0");

      const caracteristics = ["isActive"];
      if (id % 5 === 0) caracteristics.push("Admin");
      if (id % 3 === 0) caracteristics.push("isAprovador");
      if (id % 4 === 0) caracteristics.push("isAprovando");

      accounts.push({
        id,
        name: `${firstName} ${lastName}`,
        birthDate: `${year}-${month}-${day}`,
        email: `${firstName.toLowerCase()}.${lastName.toLowerCase()}${id}@example.com`,
        active: id % 9 !== 0,
        caracteristics,
      });
    }
  }

  return accounts;
}

/**
 * Persists accounts array to sessionStorage.
 */
export function saveStoredAccounts(list) {
  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(list));
  } catch (e) {
    console.warn("Could not save to sessionStorage", e);
  }
}

/**
 * Loads accounts from sessionStorage, merging with demo accounts if truncated.
 */
export function loadStoredAccounts() {
  const initialAccounts = createInitialAccounts();

  try {
    const stored = sessionStorage.getItem(STORAGE_KEY);
    if (stored) {
      const parsed = JSON.parse(stored);
      if (Array.isArray(parsed) && parsed.length >= initialAccounts.length) {
        return parsed;
      }
      if (Array.isArray(parsed) && parsed.length > 0) {
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
