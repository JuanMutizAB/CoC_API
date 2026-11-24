import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import App from "./App";
import { AccountsProvider } from "./controllers/AccountsController";
import { AuthProvider } from "./controllers/AuthController";
import "./views/global.css";

ReactDOM.createRoot(document.getElementById("root")).render(
  <React.StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <AccountsProvider>
          <App />
        </AccountsProvider>
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>
);
