import { useEffect, useRef } from "react";

/**
 * Confirmation modal shown before deactivating an active account.
 */
export default function DeactivateModal({ account, onCancel, onConfirm }) {
  const confirmBtnRef = useRef(null);

  useEffect(() => {
    if (!account) {
      return undefined;
    }

    confirmBtnRef.current?.focus();

    const handleKeyDown = (event) => {
      if (event.key === "Escape") {
        onCancel();
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [account, onCancel]);

  if (!account) {
    return null;
  }

  return (
    <div
      className="modal-backdrop"
      id="deactivate-modal"
      onClick={(event) => {
        if (event.target === event.currentTarget) {
          onCancel();
        }
      }}
    >
      <div
        className="modal-card"
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
      >
        <div className="modal-header">
          <div className="modal-icon-wrap" aria-hidden="true">
            <svg viewBox="0 0 24 24">
              <path d="M12 9v4m0 4h.01M12 3c4.97 0 9 4.03 9 9s-4.03 9-9 9-9-4.03-9-9 4.03-9 9-9z" />
            </svg>
          </div>
          <h2 id="modal-title" className="modal-title">
            Deactivate Account
          </h2>
        </div>

        <div className="modal-body">
          <p>
            Are you sure you want to deactivate the account for{" "}
            <strong id="modal-account-name">{account.name}</strong>?
          </p>
          <p className="modal-subtext">
            The user will be marked as inactive and will no longer have active permissions.
          </p>
        </div>

        <div className="modal-actions">
          <button
            type="button"
            className="modal-button modal-button--cancel"
            id="modal-cancel-btn"
            onClick={onCancel}
          >
            Cancel
          </button>
          <button
            type="button"
            className="modal-button modal-button--confirm"
            id="modal-confirm-btn"
            ref={confirmBtnRef}
            onClick={onConfirm}
          >
            Deactivate
          </button>
        </div>
      </div>
    </div>
  );
}
