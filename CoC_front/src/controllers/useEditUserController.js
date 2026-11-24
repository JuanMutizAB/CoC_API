import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { hasRole } from "../models/accountModel";
import {
  validateEmail,
  validateName,
  validatePassword,
} from "../models/validationModel";
import { useAccounts } from "./AccountsController";

/**
 * Controller hook for the Edit User view.
 */
export function useEditUserController() {
  const { id } = useParams();
  const navigate = useNavigate();
  const {
    getAccountById,
    fetchAccount,
    updateAccount,
    setFlashMessage,
  } = useAccounts();

  const user = getAccountById(id);

  const [isLoading, setIsLoading] = useState(!user);
  const [loadError, setLoadError] = useState("");
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isAdmin, setIsAdmin] = useState(false);
  const [isAprovando, setIsAprovandoState] = useState(false);
  const [isAprovador, setIsAprovadorState] = useState(false);
  const [isOriginalAdmin, setIsOriginalAdmin] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const setIsAprovando = (checked) => {
    setIsAprovandoState(checked);
    if (checked) {
      setIsAprovadorState(false);
    }
  };

  const setIsAprovador = (checked) => {
    setIsAprovadorState(checked);
    if (checked) {
      setIsAprovandoState(false);
    }
  };

  const [errors, setErrors] = useState({
    name: "",
    email: "",
    password: "",
  });
  const [actionMessage, setActionMessage] = useState("");
  const [actionType, setActionType] = useState("");

  useEffect(() => {
    document.title = "Edit User - CoC";
  }, []);

  useEffect(() => {
    if (!id) {
      setIsLoading(false);
      return;
    }
    if (getAccountById(id)) {
      setIsLoading(false);
      setLoadError("");
      return;
    }

    let cancelled = false;
    setIsLoading(true);
    setLoadError("");
    fetchAccount(id)
      .catch((error) => {
        if (!cancelled) {
          setLoadError(error?.message || "Could not load account.");
        }
      })
      .finally(() => {
        if (!cancelled) {
          setIsLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [id, getAccountById, fetchAccount]);

  useEffect(() => {
    if (!user) {
      return;
    }

    const adminFlag = hasRole(user.caracteristics, "admin");
    const aprovadorFlag = hasRole(user.caracteristics, "aprovador");
    const aprovandoFlag =
      !aprovadorFlag && hasRole(user.caracteristics, "aprovando");
    setName(user.name || "");
    setEmail(user.email || "");
    setPassword("");
    setIsOriginalAdmin(adminFlag);
    setIsAdmin(adminFlag);
    setIsAprovandoState(aprovandoFlag);
    setIsAprovadorState(aprovadorFlag);
    setErrors({ name: "", email: "", password: "" });
    setActionMessage("");
    setActionType("");
  }, [user]);

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!user || isSubmitting) {
      return;
    }

    const nextErrors = {
      name: validateName(name),
      email: validateEmail(email),
      password: validatePassword(password, { required: false }),
    };

    const hasErrors = Object.values(nextErrors).some(Boolean);
    if (hasErrors) {
      setErrors(nextErrors);
      setActionMessage("Please fix the errors above.");
      setActionType("error");
      return;
    }

    setErrors({ name: "", email: "", password: "" });
    setActionMessage("");
    setActionType("");

    const updatedChars = [];
    if (isOriginalAdmin || isAdmin) updatedChars.push("Admin");
    if (isAprovando) updatedChars.push("isAprovando");
    else if (isAprovador) updatedChars.push("isAprovador");

    const updatedName = name.trim();
    setIsSubmitting(true);
    try {
      await updateAccount(user.id, {
        name: updatedName,
        email: email.trim(),
        password: password || null,
        caracteristics: updatedChars,
      });

      setFlashMessage(
        `User ${updatedName} updated successfully.${password ? " Password was changed." : ""}`
      );
      navigate("/accounts");
    } catch (error) {
      setActionMessage(error?.message || "Could not update user.");
      setActionType("error");
    } finally {
      setIsSubmitting(false);
    }
  };

  const goBackToAccounts = () => navigate("/accounts");

  return {
    id,
    user,
    isLoading,
    loadError,
    name,
    setName,
    email,
    setEmail,
    password,
    setPassword,
    isAdmin,
    setIsAdmin,
    isAprovando,
    setIsAprovando,
    isAprovador,
    setIsAprovador,
    isOriginalAdmin,
    errors,
    actionMessage,
    actionType,
    isSubmitting,
    handleSubmit,
    goBackToAccounts,
  };
}
