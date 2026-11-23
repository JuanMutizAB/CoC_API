import PageHeader from "./components/PageHeader";
import { useCampaignFeedbackController } from "../controllers/useCampaignFeedbackController";
import "./CampaignFeedbackView.css";

/**
 * Campaign feedback view using Reason (Motivo) enum options from campaignModel.
 */
export default function CampaignFeedbackView() {
  const {
    reasons,
    reason,
    setReason,
    feedbackText,
    setFeedbackText,
    statusMessage,
    statusRole,
    handleSubmit,
    goBackToCampaign,
  } = useCampaignFeedbackController();

  return (
    <main className="app-page feedback-page">
      <PageHeader title="Campaign Feedback" />

      <section
        className="content-card feedback-card"
        aria-labelledby="feedback-title"
      >
        <div className="section-heading">
          <div>
            <h1 id="feedback-title">Campaign Feedback</h1>
            <p>Share feedback about this campaign.</p>
          </div>
          <button
            className="back-button"
            type="button"
            onClick={goBackToCampaign}
          >
            Back to Campaign
          </button>
        </div>

        <form
          id="feedback-form"
          className="feedback-form"
          onSubmit={handleSubmit}
        >
          <div className="feedback-layout">
            <div className="form-field">
              <label htmlFor="Motivo">Reason</label>
              <select
                id="Motivo"
                name="Motivo"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                required
              >
                <option value="" disabled>
                  Select a reason
                </option>
                {reasons.map((valor) => (
                  <option key={valor} value={valor}>
                    {valor}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-field feedback-message">
              <label htmlFor="Feedback-description">Feedback</label>
              <textarea
                id="Feedback-description"
                name="Feedback"
                rows={4}
                placeholder="Describe the issue or suggestion"
                value={feedbackText}
                onChange={(e) => setFeedbackText(e.target.value)}
              />
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
              <span>Submit Feedback</span>
            </button>
          </div>
        </form>
      </section>
    </main>
  );
}
