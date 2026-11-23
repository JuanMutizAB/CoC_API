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
  const { getAccountById, updateAccount, setFlashMessage } = useAccounts();

  const user = getAccountById(id);

  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isAdmin, setIsAdmin] = useState(false);
  const [isAprovando, setIsAprovando] = useState(false);
  const [isAprovador, setIsAprovador] = useState(false);
  const [isOriginalAdmin, setIsOriginalAdmin] = useState(false);

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
    if (!user) {
      return;
    }

    const adminFlag = hasRole(user.caracteristics, "admin");
    setName(user.name || "");
    setEmail(user.email || "");
    setPassword("");
    setIsOriginalAdmin(adminFlag);
    setIsAdmin(adminFlag);
    setIsAprovando(hasRole(user.caracteristics, "aprovando"));
    setIsAprovador(hasRole(user.caracteristics, "aprovador"));
    setErrors({ name: "", email: "", password: "" });
    setActionMessage("");
    setActionType("");
  }, [user]);

  const handleSubmit = (event) => {
    event.preventDefault();
    if (!user) {
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

    const updatedChars = [];
    if (isOriginalAdmin || isAdmin) updatedChars.push("Admin");
    if (isAprovando) updatedChars.push("isAprovando");
    if (isAprovador) updatedChars.push("isAprovador");

    const updatedName = name.trim();
    updateAccount(user.id, {
      name: updatedName,
      email: email.trim(),
      password,
      caracteristics: updatedChars,
    });

    setFlashMessage(
      `User ${updatedName} updated successfully.${password ? " Password was changed." : ""}`
    );
    navigate("/accounts");
  };

  const goBackToAccounts = () => navigate("/accounts");

  return {
    id,
    user,
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
    handleSubmit,
    goBackToAccounts,
  };
}
