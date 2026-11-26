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
  const [lockedUntilMs, setLockedUntilMs] = useState(null);
  const emailInputRef = useRef(null);

  const isLockedOut = Boolean(lockedUntilMs && lockedUntilMs > Date.now());

  useEffect(() => {
    document.title = "CoC - Login";
  }, []);

  useEffect(() => {
    if (!lockedUntilMs) {
      return undefined;
    }

    const remainingMs = lockedUntilMs - Date.now();
    if (remainingMs <= 0) {
      setLockedUntilMs(null);
      return undefined;
    }

    const timerId = window.setTimeout(() => {
      setLockedUntilMs(null);
    }, remainingMs);

    return () => window.clearTimeout(timerId);
  }, [lockedUntilMs]);

  const handleEmailChange = (value) => {
    setEmail(value);
    if (lockedUntilMs) {
      setLockedUntilMs(null);
    }
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (isSubmitting || isLockedOut) {
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
      if (error?.status === 429) {
        const retryAfterSeconds = Number(error?.payload?.retryAfterSeconds);
        const lockoutMs =
          Number.isFinite(retryAfterSeconds) && retryAfterSeconds > 0
            ? retryAfterSeconds * 1000
            : 5 * 60 * 1000;
        setLockedUntilMs(Date.now() + lockoutMs);
      }
      setFormStatus(error?.message || "Invalid credentials.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return {
    email,
    setEmail: handleEmailChange,
    password,
    setPassword,
    emailError,
    passwordError,
    formStatus,
    statusRole,
    isSubmitting,
    isLockedOut,
    emailInputRef,
    handleSubmit,
  };
}
