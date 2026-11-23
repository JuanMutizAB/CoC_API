import PageHeader from "./components/PageHeader";
import { useEditCampaignController } from "../controllers/useEditCampaignController";
import "./EditCampaignView.css";

/**
 * Edit campaign view mirroring the AskBlue account management layout.
 */
export default function EditCampaignView() {
  const {
    name,
    setName,
    description,
    setDescription,
    text,
    setText,
    imageName,
    publishDate,
    setPublishDate,
    publishTime,
    statusMessage,
    statusRole,
    handlePublishTimeChange,
    handleImageChange,
    handleSubmit,
    goToFeedback,
    goToAccounts,
    goToCampaigns,
  } = useEditCampaignController();

  return (
    <main className="app-page edit-campaign-page">
      <PageHeader title="Campaigns" />

      <section
        className="content-card edit-campaign-card"
        aria-labelledby="edit-campaign-title"
      >
        <div className="section-heading">
          <div>
            <h1 id="edit-campaign-title">Edit Campaign</h1>
            <p>Update the campaign details, content, image, and schedule.</p>
          </div>
          <div className="heading-actions">
            <button
              className="back-button"
              type="button"
              onClick={goToCampaigns}
            >
              Back to Campaigns
            </button>
            <button
              className="back-button"
              type="button"
              onClick={goToAccounts}
            >
              Accounts
            </button>
            <button
              className="back-button"
              type="button"
              onClick={goToFeedback}
            >
              Campaign Feedback
            </button>
          </div>
        </div>

        <form
          id="campaign-form"
          className="campaign-form"
          onSubmit={handleSubmit}
        >
          <div className="form-grid">
            <div className="form-field form-field--full">
              <label htmlFor="campaign-name">Campaign Name</label>
              <input
                type="text"
                id="campaign-name"
                name="name"
                placeholder="e.g. Summer Campaign"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
              />
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
              />
            </div>

            <div className="form-field form-field--full">
              <label htmlFor="campaign-text">Campaign Text</label>
              <textarea
                id="campaign-text"
                name="text"
                rows={5}
                placeholder="Write the text to be published"
                value={text}
                onChange={(e) => setText(e.target.value)}
              />
            </div>

            <div className="form-field form-field--full">
              <label htmlFor="campaign-image">Campaign Image</label>
              <input
                className="file-input"
                type="file"
                id="campaign-image"
                name="image"
                accept="image/*"
                required={!imageName}
                onChange={(e) => handleImageChange(e.target.files?.[0])}
              />
              <span className="field-hint">
                {imageName
                  ? `Selected image: ${imageName}`
                  : "Choose an image from your device."}
              </span>
            </div>

            <div className="form-field">
              <label htmlFor="campaign-publish-date">Publish Date</label>
              <input
                type="date"
                id="campaign-publish-date"
                name="publishDate"
                value={publishDate}
                onChange={(e) => setPublishDate(e.target.value)}
                required
              />
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
                required
              />
              <span className="field-hint" id="publish-time-hint">
                Formato de 24 horas, por exemplo 14:30.
              </span>
            </div>
          </div>

          <div className="form-actions">
            <p id="form-status" role={statusRole} aria-live="polite">
              {statusMessage}
            </p>
            <button
              className="primary-button"
              type="submit"
              id="submit-campaign"
            >
              <span>Save Campaign</span>
            </button>
          </div>
        </form>
      </section>
    </main>
  );
}
