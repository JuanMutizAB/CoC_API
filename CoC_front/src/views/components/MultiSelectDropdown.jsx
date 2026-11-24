import { useEffect, useRef, useState } from "react";
import "./MultiSelectDropdown.css";

/**
 * Accessible AskBlue-styled multi-select dropdown with tag chips and checkboxes.
 */
export default function MultiSelectDropdown({
  id,
  options = [],
  selected = [],
  onChange,
  placeholder = "Select options",
  invalid = false,
  disabled = false,
}) {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef(null);

  useEffect(() => {
    if (!isOpen) {
      return undefined;
    }

    const handleMouseDown = (event) => {
      if (containerRef.current && !containerRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    };

    const handleKeyDown = (event) => {
      if (event.key === "Escape") {
        setIsOpen(false);
      }
    };

    document.addEventListener("mousedown", handleMouseDown);
    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("mousedown", handleMouseDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [isOpen]);

  useEffect(() => {
    if (disabled && isOpen) {
      setIsOpen(false);
    }
  }, [disabled, isOpen]);

  const toggleOption = (option) => {
    if (disabled) {
      return;
    }

    const exists = selected.includes(option);
    const nextRaw = exists
      ? selected.filter((item) => item !== option)
      : [...selected, option];

    const nextOrdered = options.filter((opt) => nextRaw.includes(opt));
    const extras = nextRaw.filter((item) => !options.includes(item));
    onChange([...nextOrdered, ...extras]);
  };

  const removeOption = (event, option) => {
    event.stopPropagation();
    if (disabled) {
      return;
    }
    onChange(selected.filter((item) => item !== option));
  };

  const triggerClasses = [
    "multiselect-trigger",
    isOpen && !disabled ? "is-open" : "",
    invalid ? "invalid" : "",
    disabled ? "is-disabled" : "",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div className="multiselect" ref={containerRef}>
      <button
        id={id}
        type="button"
        className={triggerClasses}
        aria-haspopup="listbox"
        aria-expanded={Boolean(isOpen && !disabled)}
        disabled={disabled}
        onClick={() => {
          if (!disabled) {
            setIsOpen((prev) => !prev);
          }
        }}
      >
        <span className="multiselect-value">
          {selected.length === 0 ? (
            <span className="multiselect-placeholder">{placeholder}</span>
          ) : (
            <span className="multiselect-tags">
              {selected.map((item) => (
                <span key={item} className="multiselect-tag">
                  <span>{item}</span>
                  {!disabled && (
                    <span
                      role="button"
                      tabIndex={0}
                      className="multiselect-tag-remove"
                      aria-label={`Remove ${item}`}
                      onClick={(e) => removeOption(e, item)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter" || e.key === " ") {
                          e.preventDefault();
                          removeOption(e, item);
                        }
                      }}
                    >
                      ×
                    </span>
                  )}
                </span>
              ))}
            </span>
          )}
        </span>

        {!disabled && (
          <svg
            className="multiselect-chevron"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <polyline points="6 9 12 15 18 9" />
          </svg>
        )}
      </button>

      {isOpen && !disabled && (
        <div
          className="multiselect-menu"
          role="listbox"
          aria-multiselectable="true"
          aria-labelledby={id}
        >
          {options.map((option) => {
            const isChecked = selected.includes(option);
            return (
              <label key={option} className="multiselect-option">
                <input
                  type="checkbox"
                  checked={isChecked}
                  onChange={() => toggleOption(option)}
                />
                <span className="checkbox-mark" aria-hidden="true" />
                <span className="multiselect-option-label">{option}</span>
              </label>
            );
          })}
        </div>
      )}
    </div>
  );
}
