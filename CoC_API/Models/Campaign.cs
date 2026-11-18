namespace CoC_API.Models
{
    public class Campaign
    {
        private string name;
        private string description;
        private string text;
        public string justificationText { get; private set; }
        public Motivo motivo { get; private set; }
        private CampaignModel model;
        private CampaignStatus status;
        private DateTimeOffset createdAt;
        private DateTimeOffset? publicationDate;
        private string? imageUrl;
        private int userId;

        public Guid Id { get; private set; } = Guid.NewGuid();

        public string Name
        {
            get => name;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Name cannot be empty.", nameof(Name));

                name = value.Trim();
            }
        }

        public string Description
        {
            get => description;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Description cannot be empty.", nameof(Description));

                description = value.Trim();
            }
        }

        public string Text
        {
            get => text;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Text cannot be empty.", nameof(Text));

                text = value.Trim();
            }
        }

        public CampaignModel Model
        {
            get => model;
            private set
            {
                if (!Enum.IsDefined(typeof(CampaignModel), value))
                    throw new ArgumentOutOfRangeException(nameof(Model), "Campaign model is invalid.");

                model = value;
            }
        }

        public CampaignStatus Status
        {
            get =>status;
            private set
            {
                if (!Enum.IsDefined(typeof(CampaignStatus), value))
                    throw new ArgumentOutOfRangeException(nameof(Status), "Campaign status is invalid.");

                status = value;
            }
        }

        public DateTimeOffset CreatedAt
        {
            get => createdAt;
            private set => createdAt = value;
        }

        public DateTimeOffset? PublicationDate
        {
            get => publicationDate;
            private set
            {
                if (Status == CampaignStatus.Scheduled && !value.HasValue)
                    throw new ArgumentException("A publication date is required for scheduled campaigns.", nameof(PublicationDate));

                if (value.HasValue && value.Value <= DateTimeOffset.UtcNow)
                    throw new ArgumentOutOfRangeException(nameof(PublicationDate), "Publication date must be in the future.");

                publicationDate = value;
            }
        }

        public string? ImageUrl
        {
            get => imageUrl;
            private set => imageUrl = value;
        }

        public int UserId
        {
            get => userId;
            private set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(UserId), "User ID must be greater than zero.");

                userId = value;
            }
        }

        // Usado apenas pelo EF Core para materializar campanhas lidas da BD.
        // Sem ele o EF chamaria o construtor abaixo, que revalida tudo (ex.: a data de
        // publicação tem de ser futura, o que falharia para campanhas já publicadas).
        private Campaign()
        {
            name = description = text = justificationText = string.Empty;
        }

        public Campaign(string name, string description, string text, CampaignModel model,
            CampaignStatus status, DateTimeOffset? publicationDate, string? imageUrl, int userId)
        {
            Name = name;
            Description = description;
            Text = text;
            Model = model;
            Status = status;
            CreatedAt = DateTimeOffset.UtcNow;
            PublicationDate = publicationDate;
            ImageUrl = imageUrl;
            UserId = userId;
            justificationText = string.Empty;
            motivo = Motivo.Other;
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

        public void CancelCampaign()
        {
            if (Status == CampaignStatus.Canceled)
                throw new InvalidOperationException("Campaign is already canceled.");

            Status = CampaignStatus.Canceled;
        }

        public void CheckStatus()
        {
            var now = DateTimeOffset.UtcNow;

            if (Status == CampaignStatus.New && PublicationDate.HasValue)
            {
                Status = CampaignStatus.Scheduled;
                return;
            }
            if (Status == CampaignStatus.Scheduled && PublicationDate.HasValue && PublicationDate.Value <= now)
            {
                Status = CampaignStatus.Sending;
            }
        }

        public void SetMotivo(string justificationText, Motivo motivo)
        {
            this.justificationText = justificationText;
            this.motivo = motivo;
        }
    }
}