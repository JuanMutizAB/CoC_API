using System.Linq.Expressions;
using CoC_API.Integrations;
using CoC_API.Models;

namespace CoC_API.Services;

public sealed class InstagramChannelPublisher(
    InstagramGraphApiClient instagram,
    InstagramPublishingOptions options) : ICampaignChannelPublisher
{
    public CampaignType Channel => CampaignType.Instagram;

    public Expression<Func<Campaign, bool>> PendingChannelFilter =>
        campaign => campaign.InstagramMediaId == null;

    public bool ValidateConfiguration(ILogger logger)
    {
        if (!options.IsConfigured)
        {
            if (options.HasAnyValue)
                logger.LogError("Instagram publishing is disabled: configure the access token, Instagram user ID, and Graph API version.");
            else
                logger.LogWarning("Instagram publishing is disabled because its configuration is missing.");

            return false;
        }

        if (!options.TryGetGraphApiVersion(out _))
        {
            logger.LogError("Instagram publishing is disabled: Graph API version must have the format vX.Y.");
            return false;
        }

        return true;
    }

    public Task<string> PublishAsync(Campaign campaign, CancellationToken cancellationToken) =>
        instagram.PublishImageAsync(campaign, cancellationToken);

    public void MarkPublished(Campaign campaign, string externalId) =>
        campaign.CompleteInstagramPublication(externalId);

    public void MarkFailed(Campaign campaign) =>
        campaign.FailInstagramPublication();
}

public sealed class LinkedInChannelPublisher(
    LinkedInApiClient linkedIn,
    LinkedInPublishingOptions options) : ICampaignChannelPublisher
{
    public CampaignType Channel => CampaignType.LinkedIn;

    public Expression<Func<Campaign, bool>> PendingChannelFilter =>
        campaign => campaign.LinkedInPostId == null;

    public bool ValidateConfiguration(ILogger logger)
    {
        if (!options.IsConfigured)
        {
            if (options.HasAnyValue)
                logger.LogError("LinkedIn publishing is disabled: configure the access token, person URN, and API version.");
            else
                logger.LogWarning("LinkedIn publishing is disabled because its configuration is missing.");

            return false;
        }

        if (!options.IsValidApiVersion || !options.IsValidPersonUrn)
        {
            logger.LogError("LinkedIn publishing is disabled: API version or person URN is invalid.");
            return false;
        }

        return true;
    }

    public Task<string> PublishAsync(Campaign campaign, CancellationToken cancellationToken) =>
        linkedIn.PublishPostAsync(campaign, cancellationToken);

    public void MarkPublished(Campaign campaign, string externalId) =>
        campaign.CompleteLinkedInPublication(externalId);

    public void MarkFailed(Campaign campaign) =>
        campaign.FailLinkedInPublication();
}

public sealed class FacebookChannelPublisher(
    FacebookGraphApiClient facebook,
    FacebookPublishingOptions options) : ICampaignChannelPublisher
{
    public CampaignType Channel => CampaignType.Facebook;

    public Expression<Func<Campaign, bool>> PendingChannelFilter =>
        campaign => campaign.FacebookPostId == null;

    public bool ValidateConfiguration(ILogger logger)
    {
        if (!options.IsConfigured)
        {
            if (options.HasAnyValue)
                logger.LogError("Facebook publishing is disabled: configure the Page access token, Page ID, and a valid Graph API version.");
            else
                logger.LogWarning("Facebook publishing is disabled because its configuration is missing.");

            return false;
        }

        return true;
    }

    public Task<string> PublishAsync(Campaign campaign, CancellationToken cancellationToken) =>
        facebook.PublishPostAsync(campaign, cancellationToken);

    public void MarkPublished(Campaign campaign, string externalId) =>
        campaign.CompleteFacebookPublication(externalId);

    public void MarkFailed(Campaign campaign) =>
        campaign.FailFacebookPublication();
}

public sealed class EmailChannelPublisher(
    EmailClient email,
    EmailPublishingOptions options) : ICampaignChannelPublisher
{
    public CampaignType Channel => CampaignType.Email;

    public Expression<Func<Campaign, bool>> PendingChannelFilter =>
        campaign => campaign.EmailMessageId == null;

    public bool ValidateConfiguration(ILogger logger)
    {
        if (!options.IsConfigured)
        {
            if (options.HasAnyValue)
                logger.LogError("Email publishing is disabled: configure the SMTP host, username, password, and sender.");
            else
                logger.LogWarning("Email publishing is disabled because its configuration is missing.");

            return false;
        }

        return true;
    }

    public Task<string> PublishAsync(Campaign campaign, CancellationToken cancellationToken) =>
        email.SendCampaignAsync(campaign, cancellationToken);

    public void MarkPublished(Campaign campaign, string externalId) =>
        campaign.CompleteEmailPublication(externalId);

    public void MarkFailed(Campaign campaign) =>
        campaign.FailEmailPublication();

    public void LogPublished(ILogger logger, Campaign campaign, string externalId)
    {
        var recipientCount = campaign.GetEmailRecipientList().Count;
        if (recipientCount == 0)
            recipientCount = options.RecipientList.Count;

        logger.LogInformation(
            "Campaign {CampaignId} was emailed to {Count} recipients as message {MessageId}.",
            campaign.Id,
            recipientCount,
            externalId);
    }
}
