namespace CoC_API.Models
{
    public class Campaign
    {
        private string _name;
        private string _description;
        private string _text;
        private CampaignModel _model;
        private CampaignStatus _status;
        private DateTimeOffset _createdAt;
        private DateTimeOffset? _publicationDate;
        private string? _imageUrl;
        private int _userId;

        public Guid Id { get; private set; } = Guid.NewGuid();

        public string Name
        {
            get => _name;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Name cannot be empty.", nameof(Name));

                _name = value.Trim();
            }
        }

        public string Description
        {
            get => _description;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Description cannot be empty.", nameof(Description));

                _description = value.Trim();
            }
        }

        public string Text
        {
            get => _text;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Text cannot be empty.", nameof(Text));

                _text = value.Trim();
            }
        }

        public CampaignModel Model
        {
            get => _model;
            private set
            {
                if (!Enum.IsDefined(typeof(CampaignModel), value))
                    throw new ArgumentOutOfRangeException(nameof(Model), "Campaign model is invalid.");

                _model = value;
            }
        }

        public CampaignStatus Status
        {
            get => _status;
            private set
            {
                if (!Enum.IsDefined(typeof(CampaignStatus), value))
                    throw new ArgumentOutOfRangeException(nameof(Status), "Campaign status is invalid.");

                _status = value;
            }
        }

        public DateTimeOffset CreatedAt
        {
            get => _createdAt;
            private set => _createdAt = value;
        }

        public DateTimeOffset? PublicationDate
        {
            get => _publicationDate;
            private set
            {
                if (Status == CampaignStatus.Scheduled && !value.HasValue)
                    throw new ArgumentException("A publication date is required for scheduled campaigns.", nameof(PublicationDate));

                if (value.HasValue && value.Value <= DateTimeOffset.UtcNow)
                    throw new ArgumentOutOfRangeException(nameof(PublicationDate), "Publication date must be in the future.");

                _publicationDate = value;
            }
        }

        public string? ImageUrl
        {
            get => _imageUrl;
            private set;
        }

        public int UserId
        {
            get => _userId;
            private set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(UserId), "User ID must be greater than zero.");

                _userId = value;
            }
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
            }

            if (Status == CampaignStatus.Scheduled && PublicationDate.HasValue && PublicationDate.Value <= now)
            {
                Status = CampaignStatus.Sending;
            }
        }
    }
}