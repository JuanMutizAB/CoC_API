import PageHeader from "./components/PageHeader";
import { useCreateCampaignController } from "../controllers/useCreateCampaignController";
import "./CreateCampaignView.css";

/**
 * Campaign creation view following the AccountCreationView layout and AskBlue styling.
 */
export default function CreateCampaignView() {
  const {
    name,
    setName,
    type,
    setType,
    description,
    setDescription,
    modelMsg,
    setModelMsg,
    publishDate,
    setPublishDate,
    publishTime,
    handlePublishTimeChange,
    imageUrl,
    setImageUrl,
    errors,
    actionMessage,
    actionType,
    handleSubmit,
    handleClear,
    goBackToCampaigns,
    typeOptions,
  } = useCreateCampaignController();

  return (
    <main className="app-page create-campaign-page">
      <PageHeader title="Create Campaign" />

      <section
        className="content-card create-campaign-card"
        aria-labelledby="create-campaign-title"
      >
        <div className="toolbar">
          <button
            className="back-button"
            id="back-to-campaigns"
            type="button"
            onClick={goBackToCampaigns}
          >
            <svg viewBox="0 0 24 24" aria-hidden="true">
              <path d="M19 12H5m7 7-7-7 7-7" />
            </svg>
            <span>Back to campaigns</span>
          </button>
        </div>

        <div className="section-heading">
          <div>
            <h1 id="create-campaign-title">Create Campaign</h1>
            <p id="create-campaign-subtitle">
              Enter details to create a new campaign
            </p>
          </div>
          <span className="demo-badge">Demo data</span>
        </div>

        <form
          id="create-campaign-form"
          className="management-form create-campaign-form"
          onSubmit={handleSubmit}
          noValidate
        >
          <div className="form-grid">
            <div className="form-group">
              <label htmlFor="campaign-name">Name</label>
              <input
                id="campaign-name"
                type="text"
                placeholder="e.g. Summer Campaign"
                value={name}
                onChange={(e) => setName(e.target.value)}
                className={errors.name ? "invalid" : undefined}
                required
              />
              <span className="error-message" id="name-error">
                {errors.name}
              </span>
            </div>

            <div className="form-group">
              <label htmlFor="campaign-type">Type</label>
              <select
                id="campaign-type"
                value={type}
                onChange={(e) => setType(e.target.value)}
                className={errors.type ? "invalid" : undefined}
                required
              >
                <option value="" disabled>
                  Select a type
                </option>
                {typeOptions.map((tp) => (
                  <option key={tp} value={tp}>
                    {tp}
                  </option>
                ))}
              </select>
              <span className="error-message" id="type-error">
                {errors.type}
              </span>
            </div>

            <div className="form-group form-group--full">
              <label htmlFor="campaign-description">Description</label>
              <textarea
                id="campaign-description"
                rows={3}
                placeholder="Briefly describe the campaign goal"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                className={errors.description ? "invalid" : undefined}
                required
              />
              <span className="error-message" id="description-error">
                {errors.description}
              </span>
            </div>

            <div className="form-group form-group--full">
              <label htmlFor="campaign-modelmsg">Model Message</label>
              <textarea
                id="campaign-modelmsg"
                rows={5}
                placeholder="Write the message to be published"
                value={modelMsg}
                onChange={(e) => setModelMsg(e.target.value)}
                className={errors.modelMsg ? "invalid" : undefined}
                required
              />
              <span className="error-message" id="modelmsg-error">
                {errors.modelMsg}
              </span>
            </div>

            <div className="form-group">
              <label htmlFor="campaign-publish-date">Publication Date</label>
              <input
                id="campaign-publish-date"
                type="date"
                title="Click the calendar icon to select the publication date"
                value={publishDate}
                onChange={(e) => setPublishDate(e.target.value)}
                className={errors.publishDate ? "invalid" : undefined}
                required
              />
              <span className="error-message" id="publish-date-error">
                {errors.publishDate}
              </span>
            </div>

            <div className="form-group">
              <label htmlFor="campaign-publish-time">Publication Time</label>
              <input
                id="campaign-publish-time"
                type="text"
                inputMode="numeric"
                maxLength={5}
                placeholder="HH:mm"
                value={publishTime}
                onChange={(e) => handlePublishTimeChange(e.target.value)}
                className={errors.publishTime ? "invalid" : undefined}
                required
              />
              <span className="field-hint">
                24-hour format, e.g. 14:30.
              </span>
              <span className="error-message" id="publish-time-error">
                {errors.publishTime}
              </span>
            </div>

            <div className="form-group form-group--full">
              <label htmlFor="campaign-image-url">Image URL</label>
              <input
                id="campaign-image-url"
                type="url"
                placeholder="https://example.com/image.jpg"
                value={imageUrl}
                onChange={(e) => setImageUrl(e.target.value)}
                className={errors.imageUrl ? "invalid" : undefined}
              />
              <span className="field-hint">
                Optional. Provide an image URL.
              </span>
              <span className="error-message" id="image-url-error">
                {errors.imageUrl}
              </span>
            </div>
          </div>

          <div className="form-actions">
            <button className="primary-button" type="submit">
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path d="M12 5v14M5 12h14" />
              </svg>
              <span>Create Campaign</span>
            </button>
            <button
              className="cancel-button"
              id="cancel-create-campaign"
              type="button"
              onClick={handleClear}
            >
              Clear
            </button>
          </div>

          <p
            className={`action-message${actionType ? ` ${actionType}` : ""}`}
            id="action-message"
            role="status"
            aria-live="polite"
          >
            {actionMessage}
          </p>
        </form>
      </section>
    </main>
  );
}
