import { useEffect, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "./AuthController";

/**
 * Controller hook for the Login view.
 */
export function useLoginController() {
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [emailError, setEmailError] = useState("");
  const [passwordError, setPasswordError] = useState("");
  const [formStatus, setFormStatus] = useState("");
  const [statusRole, setStatusRole] = useState("status");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const emailInputRef = useRef(null);

  useEffect(() => {
    document.title = "CoC - Login";
  }, []);

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (isSubmitting) {
      return;
    }

    const trimmedEmail = email.trim();
    let isValid = true;

    setEmailError("");
    setPasswordError("");
    setFormStatus("");
    setStatusRole("status");

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

    if (!isValid) {
      return;
    }

    setIsSubmitting(true);
    try {
      await login(trimmedEmail, password);
      const target = location.state?.from || "/campaigns";
      navigate(target, { replace: true });
    } catch (error) {
      setStatusRole("alert");
      setFormStatus(error?.message || "Invalid credentials.");
    } finally {
      setIsSubmitting(false);
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
    statusRole,
    isSubmitting,
    emailInputRef,
    handleSubmit,
  };
}
