import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  validateBirthDate,
  validateEmail,
  validateName,
  validatePassword,
  validateTaxId,
} from "../models/validationModel";
import { useAccounts } from "./AccountsController";

/**
 * Controller hook for the Account Creation view.
 */
export function useAccountCreationController() {
  const navigate = useNavigate();
  const { createAccount, setFlashMessage } = useAccounts();

  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [birthDate, setBirthDate] = useState("");
  const [taxId, setTaxId] = useState("");
  const [password, setPassword] = useState("");

  const [isAdmin, setIsAdmin] = useState(false);
  const [isAprovando, setIsAprovando] = useState(false);
  const [isAprovador, setIsAprovador] = useState(false);

  const [errors, setErrors] = useState({
    name: "",
    email: "",
    birthDate: "",
    taxId: "",
    password: "",
  });
  const [actionMessage, setActionMessage] = useState("");
  const [actionType, setActionType] = useState("");

  useEffect(() => {
    document.title = "Create Account - CoC";
  }, []);

  const clearErrors = () => {
    setErrors({
      name: "",
      email: "",
      birthDate: "",
      taxId: "",
      password: "",
    });
    setActionMessage("");
    setActionType("");
  };

  const handleSubmit = (event) => {
    event.preventDefault();

    const nextErrors = {
      name: validateName(name),
      email: validateEmail(email),
      birthDate: validateBirthDate(birthDate),
      taxId: validateTaxId(taxId),
      password: validatePassword(password, { required: true }),
    };

    const hasErrors = Object.values(nextErrors).some(Boolean);
    if (hasErrors) {
      setErrors(nextErrors);
      setActionMessage("Please fix the errors above.");
      setActionType("error");
      return;
    }

    const caracteristics = ["isActive"];
    if (isAdmin) caracteristics.push("Admin");
    if (isAprovando) caracteristics.push("isAprovando");
    if (isAprovador) caracteristics.push("isAprovador");

    const trimmedName = name.trim();
    const created = createAccount({
      name: trimmedName,
      email: email.trim(),
      birthDate,
      taxId: taxId.trim(),
      password,
      caracteristics,
    });

    setFlashMessage(
      `Account for ${trimmedName} (ID ${created.id}) was created successfully.`
    );
    navigate("/accounts");
  };

  const handleClear = () => {
    setName("");
    setEmail("");
    setBirthDate("");
    setTaxId("");
    setPassword("");
    setIsAdmin(false);
    setIsAprovando(false);
    setIsAprovador(false);
    clearErrors();
    setActionMessage("Form cleared.");
    setActionType("");
  };

  const goBackToAccounts = () => navigate("/accounts");

  return {
    name,
    setName,
    email,
    setEmail,
    birthDate,
    setBirthDate,
    taxId,
    setTaxId,
    password,
    setPassword,
    isAdmin,
    setIsAdmin,
    isAprovando,
    setIsAprovando,
    isAprovador,
    setIsAprovador,
    errors,
    actionMessage,
    actionType,
    handleSubmit,
    handleClear,
    goBackToAccounts,
  };
}
