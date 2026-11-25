using System.Linq.Expressions;
using CoC_API.Models;

namespace CoC_API.Services;

/// <summary>
/// Strategy contract for publishing due campaigns to an external channel.
/// </summary>
public interface ICampaignChannelPublisher
{
    CampaignType Channel { get; }

    Expression<Func<Campaign, bool>> PendingChannelFilter { get; }

    bool ValidateConfiguration(ILogger logger);

    Task<string> PublishAsync(Campaign campaign, CancellationToken cancellationToken);

    void MarkPublished(Campaign campaign, string externalId);

    void MarkFailed(Campaign campaign);

    void LogPublished(ILogger logger, Campaign campaign, string externalId)
    {
        logger.LogInformation(
            "Campaign {CampaignId} was published to {Channel} as {ExternalId}.",
            campaign.Id,
            Channel,
            externalId);
    }

    bool IsHandledFailure(Exception exception) =>
        exception is HttpRequestException
            or InvalidOperationException
            or System.Text.Json.JsonException
            or FormatException
            or TimeoutException
            or OperationCanceledException
            or ArgumentException;
}
