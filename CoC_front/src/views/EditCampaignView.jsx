import MultiSelectDropdown from "./components/MultiSelectDropdown";
import PageHeader from "./components/PageHeader";
import { useEditCampaignController } from "../controllers/useEditCampaignController";
import "./EditCampaignView.css";

/**
 * Edit campaign / Campaign details view with URL-based image field,
 * multi-platform type selector, ForChange/Canceled feedback display, and conditional workflow actions.
 */
export default function EditCampaignView({ readOnly = false }) {
  const {
    id,
    campaign,
    isLoading,
    loadError,
    name,
    setName,
    types,
    setTypes,
    description,
    setDescription,
    modelMsg,
    setModelMsg,
    imageUrl,
    setImageUrl,
    publishDate,
    setPublishDate,
    publishTime,
    status,
    motivo,
    justificationText,
    isDetailsMode,
    isCanceled,
    canEditFields,
    canReview,
    canCancel,
    showFeedbackBox,
    errors,
    statusMessage,
    statusRole,
    isSubmitting,
    handlePublishTimeChange,
    handleSubmit,
    handleCancelCampaign,
    handleApproveCampaign,
    handleSendForChange,
    goToCampaigns,
    typeOptions,
  } = useEditCampaignController({ readOnly });

  if (isLoading) {
    return (
      <main className="app-page edit-campaign-page">
        <PageHeader title="Campaigns" />
        <section
          className="content-card edit-campaign-card"
          aria-labelledby="edit-campaign-title"
        >
          <div className="section-heading">
            <div>
              <h1 id="edit-campaign-title">
                {readOnly ? "Campaign Details" : "Edit Campaign"}
              </h1>
              <p>Loading campaign details...</p>
            </div>
          </div>
        </section>
      </main>
    );
  }

  if (!campaign) {
    return (
      <main className="app-page edit-campaign-page">
        <PageHeader title="Campaigns" />
        <section
          className="content-card edit-campaign-card"
          aria-labelledby="edit-campaign-title"
        >
          <div className="section-heading">
            <div>
              <h1 id="edit-campaign-title">Campaign not found</h1>
              <p>
                {loadError ||
                  (id
                    ? `The requested campaign (${id}) could not be found.`
                    : "Select a campaign from the listing to view or edit.")}
              </p>
            </div>
            <div className="heading-actions">
              <button
                className="back-button"
                type="button"
                onClick={goToCampaigns}
              >
                Back to Campaigns
              </button>
            </div>
          </div>
        </section>
      </main>
    );
  }

  const fieldsDisabled = !canEditFields;
  const showActionsBar = canEditFields || canReview || canCancel;
  const statusClass = `status-pill status-pill--${String(status || "").toLowerCase()}`;

  const subtitleText = !canEditFields
    ? isCanceled
      ? "View campaign details, content, schedule, and cancellation feedback."
      : "View campaign details, content, and schedule."
    : "Update the campaign details, content, image URL, and schedule.";

  return (
    <main className="app-page edit-campaign-page">
      <PageHeader title="Campaigns" />

      <section
        className="content-card edit-campaign-card"
        aria-labelledby="edit-campaign-title"
      >
        <div className="section-heading">
          <div>
            <div className="heading-title-row">
              <h1 id="edit-campaign-title">
                {isDetailsMode || !canEditFields ? "Campaign Details" : "Edit Campaign"}
              </h1>
              <span className={statusClass}>{status}</span>
            </div>
            <p>{subtitleText}</p>
          </div>
          <div className="heading-actions">
            <button
              className="back-button"
              type="button"
              onClick={goToCampaigns}
            >
              Back to Campaigns
            </button>
          </div>
        </div>

        {showFeedbackBox && (
          <div
            className={`for-change-feedback${isCanceled ? " for-change-feedback--canceled" : ""}`}
            role="region"
            aria-label={
              isCanceled ? "Cancellation Feedback" : "Requested Changes Feedback"
            }
          >
            <h2 className="for-change-feedback__title">
              {isCanceled ? "Cancellation Feedback" : "Requested Changes Feedback"}
            </h2>
            <div className="for-change-feedback__grid">
              <div className="for-change-feedback__field">
                <label htmlFor="received-reason">Reason</label>
                <input
                  id="received-reason"
                  type="text"
                  value={motivo || "—"}
                  readOnly
                />
              </div>
              <div className="for-change-feedback__field">
                <label htmlFor="received-feedback">Feedback</label>
                <textarea
                  id="received-feedback"
                  rows={3}
                  value={justificationText || "—"}
                  readOnly
                />
              </div>
            </div>
          </div>
        )}

        <form
          id="campaign-form"
          className="campaign-form"
          onSubmit={handleSubmit}
          noValidate
        >
          <div className="form-grid">
            <div className="form-field">
              <label htmlFor="campaign-name">Campaign Name</label>
              <input
                type="text"
                id="campaign-name"
                name="name"
                placeholder="e.g. Summer Campaign"
                value={name}
                onChange={(e) => setName(e.target.value)}
                className={errors.name ? "invalid" : undefined}
                disabled={fieldsDisabled}
                required
              />
              {!fieldsDisabled && (
                <span className="error-message">{errors.name}</span>
              )}
            </div>

            <div className="form-field">
              <label htmlFor="campaign-type">Type</label>
              <MultiSelectDropdown
                id="campaign-type"
                options={typeOptions}
                selected={types}
                onChange={setTypes}
                placeholder="Select one or more types"
                invalid={Boolean(errors.type)}
                disabled={fieldsDisabled}
              />
              {!fieldsDisabled && (
                <span className="error-message">{errors.type}</span>
              )}
            </div>

            <div className="form-field form-field--full">
              <label htmlFor="campaign-description">Description</label>
              <textarea
                id="campaign-description"
                name="description"
                rows={3}
                placeholder="Briefly describe the campaign goal"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                className={errors.description ? "invalid" : undefined}
                disabled={fieldsDisabled}
                required
              />
              {!fieldsDisabled && (
                <span className="error-message">{errors.description}</span>
              )}
            </div>

            <div className="form-field form-field--full">
              <label htmlFor="campaign-modelmsg">Message to send</label>
              <textarea
                id="campaign-modelmsg"
                name="modelMsg"
                rows={5}
                placeholder="Write the message to be published"
                value={modelMsg}
                onChange={(e) => setModelMsg(e.target.value)}
                className={errors.modelMsg ? "invalid" : undefined}
                disabled={fieldsDisabled}
                required
              />
              {!fieldsDisabled && (
                <span className="error-message">{errors.modelMsg}</span>
              )}
            </div>

            <div className="form-field form-field--full">
              <label htmlFor="campaign-image-url">Campaign Image URL</label>
              <input
                type="url"
                id="campaign-image-url"
                name="imageUrl"
                placeholder={fieldsDisabled ? "—" : "https://example.com/campaign-image.jpg"}
                value={imageUrl}
                onChange={(e) => setImageUrl(e.target.value)}
                className={errors.imageUrl ? "invalid" : undefined}
                disabled={fieldsDisabled}
              />
              {!fieldsDisabled && (
                <>
                  <span className="field-hint">
                    Optional. Provide the public URL of the campaign image.
                  </span>
                  <span className="error-message">{errors.imageUrl}</span>
                </>
              )}
            </div>

            <div className="form-field">
              <label htmlFor="campaign-publish-date">Publish Date</label>
              <input
                type="date"
                id="campaign-publish-date"
                name="publishDate"
                value={publishDate}
                onChange={(e) => setPublishDate(e.target.value)}
                className={errors.publishDate ? "invalid" : undefined}
                disabled={fieldsDisabled}
                required
              />
              {!fieldsDisabled && (
                <span className="error-message">{errors.publishDate}</span>
              )}
            </div>

            <div className="form-field">
              <label htmlFor="campaign-publish-time">Publish Time</label>
              <input
                type="text"
                id="campaign-publish-time"
                name="publisTime"
                inputMode="numeric"
                maxLength={5}
                pattern="([01][0-9]|2[0-3]):[0-5][0-9]"
                placeholder="HH:mm"
                aria-describedby="publish-time-hint"
                value={publishTime}
                onChange={(e) => handlePublishTimeChange(e.target.value)}
                className={errors.publishTime ? "invalid" : undefined}
                disabled={fieldsDisabled}
                required
              />
              {!fieldsDisabled && (
                <>
                  <span className="field-hint" id="publish-time-hint">
                    Formato de 24 horas, por exemplo 14:30.
                  </span>
                  <span className="error-message">{errors.publishTime}</span>
                </>
              )}
            </div>
          </div>

          {showActionsBar && (
            <div className="form-actions">
              <div className="workflow-actions">
                {canReview && (
                  <>
                    <button
                      className="workflow-button approve-button"
                      type="button"
                      id="approve-campaign"
                      disabled={isSubmitting}
                      onClick={handleApproveCampaign}
                    >
                      Approve
                    </button>
                    <button
                      className="workflow-button send-change-button"
                      type="button"
                      id="send-for-change"
                      disabled={isSubmitting}
                      onClick={handleSendForChange}
                    >
                      Send for change
                    </button>
                  </>
                )}

                {canCancel && (
                  <button
                    className="workflow-button cancel-campaign-button"
                    type="button"
                    id="cancel-campaign"
                    disabled={isSubmitting}
                    onClick={handleCancelCampaign}
                  >
                    Cancel Campaign
                  </button>
                )}
              </div>

              <div className="submit-actions">
                <p id="form-status" role={statusRole} aria-live="polite">
                  {statusMessage}
                </p>
                {canEditFields && (
                  <button
                    className="primary-button"
                    type="submit"
                    id="submit-campaign"
                    disabled={isSubmitting}
                  >
                    <span>{isSubmitting ? "Saving..." : "Save Campaign"}</span>
                  </button>
                )}
              </div>
            </div>
          )}
        </form>
      </section>
    </main>
  );
}
