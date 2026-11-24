using System.ComponentModel.DataAnnotations.Schema;

namespace CoC_API.Models
{
    public class Campaign
    {
        private string name = string.Empty;
        private string description = string.Empty;
        private string modelMsg = string.Empty;
        public string? justificationText { get; private set; }
        public Motivo? motivo { get; private set; }
        private CampaignType type;
        private CampaignStatus status;
        private DateTimeOffset createdAt;
        private DateTimeOffset publicationDate;
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

        [Column("Text")]
        public string ModelMsg
        {
            get => modelMsg;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Message cannot be empty.", nameof(ModelMsg));

                modelMsg = value.Trim();
            }
        }

        [Column("Model")]
        public CampaignType Type
        {
            get => type;
            private set
            {
                if (!Enum.IsDefined(typeof(CampaignType), value))
                    throw new ArgumentOutOfRangeException(nameof(Type), "Campaign type is invalid.");

                type = value;
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

        public DateTimeOffset PublicationDate
        {
            get => publicationDate;
            private set
            {
                if (value == default)
                    throw new ArgumentOutOfRangeException(nameof(PublicationDate), "A publication date is required.");

                publicationDate = value;
            }
        }

        public string? ImageUrl
        {
            get => imageUrl;
            private set => imageUrl = value;
        }

        public string? InstagramMediaId { get; private set; }

        public string? LinkedInPostId { get; private set; }

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
        private Campaign()
        {
            name = description = modelMsg = string.Empty;
        }

        public Campaign(string name, string description, string modelMsg, CampaignType type,
            CampaignStatus status, DateTimeOffset publicationDate, string? imageUrl, int userId,
            DateTimeOffset validationTime)
        {
            ResolvePublicationStatus(publicationDate, validationTime);
            Name = name;
            Description = description;
            ModelMsg = modelMsg;
            Type = type;
            Status = status;
            CreatedAt = DateTimeOffset.UtcNow;
            PublicationDate = publicationDate;
            ImageUrl = imageUrl;
            UserId = userId;
            justificationText = null;
            motivo = null;
        }

        public void EditCampaign(string name, string description, string modelMsg, CampaignType type,
            CampaignStatus status, DateTimeOffset publicationDate, string? imageUrl, DateTimeOffset validationTime)
        {
            var editedCampaign = new Campaign(
                name,
                description,
                modelMsg,
                type,
                status,
                publicationDate,
                imageUrl,
                userId,
                validationTime);

            Name = editedCampaign.Name;
            Description = editedCampaign.Description;
            ModelMsg = editedCampaign.ModelMsg;
            Type = editedCampaign.Type;
            Status = editedCampaign.Status;
            PublicationDate = editedCampaign.PublicationDate;
            ImageUrl = editedCampaign.ImageUrl;
        }

        public void Approve()
        {
            if (Status != CampaignStatus.InReview)
                throw new InvalidOperationException("Only campaigns in review can be approved.");

            var publicationStatus = ResolvePublicationStatus(PublicationDate, DateTimeOffset.UtcNow);
            Status = Type is CampaignType.Instagram or CampaignType.LinkedIn
                ? CampaignStatus.Approved
                : publicationStatus;
        }

        public void RequestChanges(string justificationText)
        {
            if (Status != CampaignStatus.InReview)
                throw new InvalidOperationException("Only campaigns in review can be sent for changes.");

            this.justificationText = ValidateJustification(justificationText);
            Status = CampaignStatus.ForChange;
        }

        public void Reject(Motivo motivo, string justificationText)
        {
            if (Status != CampaignStatus.InReview)
                throw new InvalidOperationException("Only campaigns in review can be rejected.");

            CompleteCancellation(motivo, justificationText);
        }

        public void CancelCampaign(Motivo motivo, string justificationText)
        {
            if (Status is CampaignStatus.Sending or CampaignStatus.Closed or CampaignStatus.Canceled)
                throw new InvalidOperationException("This campaign can no longer be canceled.");

            CompleteCancellation(motivo, justificationText);
        }

        public void CheckStatus(DateTimeOffset now)
        {
            if (Status is CampaignStatus.New or CampaignStatus.Scheduled)
                Status = ResolvePublicationStatus(PublicationDate, now);
        }

        public static CampaignStatus ResolvePublicationStatus(DateTimeOffset publicationDate, DateTimeOffset now)
        {
            if (publicationDate == default)
                throw new ArgumentOutOfRangeException(nameof(publicationDate), "A publication date is required.");

            var publicationMinute = ToUtcMinuteTicks(publicationDate);
            var currentMinute = ToUtcMinuteTicks(now);
            if (publicationMinute < currentMinute)
                throw new ArgumentOutOfRangeException(nameof(publicationDate), "Publication date cannot be in the past.");

            return publicationMinute == currentMinute
                ? CampaignStatus.Sending
                : CampaignStatus.Scheduled;
        }

        private void CompleteCancellation(Motivo motivo, string justificationText)
        {
            if (!Enum.IsDefined(motivo))
                throw new ArgumentOutOfRangeException(nameof(motivo), "Campaign reason is invalid.");

            this.justificationText = ValidateJustification(justificationText);
            this.motivo = motivo;
            Status = CampaignStatus.Canceled;
        }

        public void CompleteInstagramPublication(string mediaId)
        {
            if (Status != CampaignStatus.Sending)
                throw new InvalidOperationException("The campaign is not being published.");
            if (string.IsNullOrWhiteSpace(mediaId))
                throw new ArgumentException("Instagram media ID cannot be empty.", nameof(mediaId));

            InstagramMediaId = mediaId;
            Status = CampaignStatus.Closed;
        }

        public void FailInstagramPublication()
        {
            if (Status == CampaignStatus.Sending)
                Status = CampaignStatus.Failed;
        }

        public void CompleteLinkedInPublication(string postId)
        {
            if (Status != CampaignStatus.Sending)
                throw new InvalidOperationException("The campaign is not being published.");
            if (string.IsNullOrWhiteSpace(postId))
                throw new ArgumentException("LinkedIn post ID cannot be empty.", nameof(postId));

            LinkedInPostId = postId;
            Status = CampaignStatus.Closed;
        }

        public void FailLinkedInPublication()
        {
            if (Status == CampaignStatus.Sending)
                Status = CampaignStatus.Failed;
        }

        private static string ValidateJustification(string justificationText)
        {
            if (string.IsNullOrWhiteSpace(justificationText))
                throw new ArgumentException("A justification is required.", nameof(justificationText));

            return justificationText.Trim();
        }

        private static long ToUtcMinuteTicks(DateTimeOffset value)
        {
            var ticks = value.UtcDateTime.Ticks;
            return ticks - ticks % TimeSpan.TicksPerMinute;
        }
    }
}