namespace CoC_API.Models
{
    public class Campaign
    {
        
        public string Name { get; private set; }
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Description { get; private set; }
        public string Text { get; private set; }
        public CampaignModel Model { get; private set; }
        public CampaignStatus Status { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? PublicationDate { get; private set; }
        public string? ImageUrl { get; private set; }
        public int UserId { get; private set; }

        public Campaign(string name, string description, string text, CampaignModel model,
            CampaignStatus status, DateTimeOffset? publicationDate, string? imageUrl, int userId)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty.", nameof(name));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description cannot be empty.", nameof(description));

            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text cannot be empty.", nameof(text));

            if (!Enum.IsDefined(typeof(CampaignModel), model))
                throw new ArgumentOutOfRangeException(nameof(model), "Campaign model is invalid.");

            if (!Enum.IsDefined(typeof(CampaignStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status), "Campaign status is invalid.");

            if (userId <= 0)
                throw new ArgumentOutOfRangeException(nameof(userId), "User ID must be greater than zero.");

            if (status == CampaignStatus.Scheduled)
            {
                if (!publicationDate.HasValue)
                    throw new ArgumentException("A publication date is required for scheduled campaigns.", nameof(publicationDate));

                if (publicationDate.Value <= DateTimeOffset.UtcNow)
                    throw new ArgumentOutOfRangeException(nameof(publicationDate), "Publication date must be in the future.");
            }

            Name = name.Trim();
            Description = description.Trim();
            Text = text.Trim();
            Model = model;
            Status = status;
            CreatedAt = DateTimeOffset.UtcNow;
            PublicationDate = publicationDate;
            ImageUrl = imageUrl;
            UserId = userId;
        }

        public void EditCampaign(string name, string description, string text, CampaignModel model,
            CampaignStatus status, DateTimeOffset? publicationDate, string? imageUrl, int userId)
        {
            var editedCampaign = new Campaign(
                name,
                description,
                text,
                model,
                status,
                publicationDate,
                imageUrl,
                userId);

            Name = editedCampaign.Name;
            Description = editedCampaign.Description;
            Text = editedCampaign.Text;
            Model = editedCampaign.Model;
            Status = editedCampaign.Status;
            PublicationDate = editedCampaign.PublicationDate;
            ImageUrl = editedCampaign.ImageUrl;
            UserId = editedCampaign.UserId;
        }

        public void DeleteCampaign()
        {

        }
    }
}