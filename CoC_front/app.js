
const loginForm = document.querySelector("#login-form");
const emailInput = document.querySelector("#email");
const passwordInput = document.querySelector("#password");
const emailError = document.querySelector("#email-error");
const passwordError = document.querySelector("#password-error");
const formStatus = document.querySelector("#form-status");
const passwordToggle = document.querySelector(".password-toggle");

passwordToggle.addEventListener("click", () => {
  const isPasswordHidden = passwordInput.type === "password";

  passwordInput.type = isPasswordHidden ? "text" : "password";
  passwordToggle.setAttribute("aria-pressed", String(isPasswordHidden));
  passwordToggle.setAttribute(
    "aria-label",
    isPasswordHidden ? "Ocultar palavra-passe" : "Mostrar palavra-passe"
  );
});

loginForm.addEventListener("submit", (event) => {
  event.preventDefault();

  const email = emailInput.value.trim();
  const password = passwordInput.value;
  let isValid = true;

  emailError.textContent = "";
  passwordError.textContent = "";
  emailInput.classList.remove("invalid");
  passwordInput.classList.remove("invalid");
  formStatus.textContent = "";

  if (!email) {
    emailError.textContent = "Introduza o seu email.";
    emailInput.classList.add("invalid");
    isValid = false;
  } else if (!emailInput.checkValidity()) {
    emailError.textContent = "Introduza um email válido.";
    emailInput.classList.add("invalid");
    isValid = false;
  }

  if (!password) {
    passwordError.textContent = "Introduza a sua palavra-passe.";
    passwordInput.classList.add("invalid");
    isValid = false;
  }

  if (isValid) {
    formStatus.textContent = "Dados válidos. Pronto para ligar à API.";
  }
});
