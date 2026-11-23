import { useState } from "react";

/**
 * Password input with an accessible eye-icon visibility toggle.
 */
export default function PasswordField({
  id,
  name,
  value,
  onChange,
  placeholder,
  autoComplete,
  required = false,
  invalid = false,
  showLabel = "Show password",
  hideLabel = "Hide password",
}) {
  const [visible, setVisible] = useState(false);

  return (
    <div className="password-field">
      <input
        id={id}
        name={name}
        type={visible ? "text" : "password"}
        value={value}
        onChange={onChange}
        placeholder={placeholder}
        autoComplete={autoComplete}
        required={required}
        className={invalid ? "invalid" : undefined}
      />
      <button
        className="password-toggle"
        type="button"
        aria-label={visible ? hideLabel : showLabel}
        aria-pressed={String(visible)}
        onClick={() => setVisible((prev) => !prev)}
      >
        <svg className="eye-icon eye-icon-hidden" viewBox="0 0 24 24" aria-hidden="true">
          <path d="M3 3l18 18M10.6 10.6a2 2 0 002.8 2.8M9.9 4.24A10.8 10.8 0 0112 4c5.5 0 9.5 5 9.5 8a7.9 7.9 0 01-2.1 3.6M6.2 6.2C3.9 7.7 2.5 10 2.5 12c0 3 4 8 9.5 8a9.7 9.7 0 004.1-.9" />
        </svg>
        <svg className="eye-icon eye-icon-visible" viewBox="0 0 24 24" aria-hidden="true">
          <path d="M2.5 12c0-3 4-8 9.5-8s9.5 5 9.5 8-4 8-9.5 8-9.5-5-9.5-8z" />
          <circle cx="12" cy="12" r="3" />
        </svg>
      </button>
    </div>
  );
}
