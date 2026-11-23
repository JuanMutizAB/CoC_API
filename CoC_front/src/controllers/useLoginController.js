import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAccounts } from "./AccountsController";

/**
 * Controller hook for the Login view.
 */
export function useLoginController() {
  const navigate = useNavigate();
  const { setFlashMessage } = useAccounts();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [emailError, setEmailError] = useState("");
  const [passwordError, setPasswordError] = useState("");
  const [formStatus, setFormStatus] = useState("");
  const emailInputRef = useRef(null);

  useEffect(() => {
    document.title = "CoC - Login";
  }, []);

  const handleSubmit = (event) => {
    event.preventDefault();

    const trimmedEmail = email.trim();
    let isValid = true;

    setEmailError("");
    setPasswordError("");
    setFormStatus("");

    if (!trimmedEmail) {
      setEmailError("Introduza o seu email.");
      isValid = false;
    } else if (emailInputRef.current && !emailInputRef.current.checkValidity()) {
      setEmailError("Introduza um email válido.");
      isValid = false;
    }

    if (!password) {
      setPasswordError("Introduza a sua palavra-passe.");
      isValid = false;
    }

    if (isValid) {
      setFlashMessage(`Logged in as ${trimmedEmail} (demo mode).`);
      navigate("/accounts");
    }
  };

  return {
    email,
    setEmail,
    password,
    setPassword,
    emailError,
    passwordError,
    formStatus,
    emailInputRef,
    handleSubmit,
  };
}
