using System.Text.Json;
using CoC_API.Integrations;
using Microsoft.Extensions.Hosting;

namespace CoC_API.Services;

public sealed class InstagramPublishingService(
    CampaignStore campaigns,
    InstagramGraphApiClient instagram,
    InstagramPublishingOptions options,
    ILogger<InstagramPublishingService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.IsConfigured)
        {
            if (options.HasAnyValue)
                logger.LogError("Instagram publishing is disabled: configure the access token, Instagram user ID, and Graph API version.");
            else
                logger.LogWarning("Instagram publishing is disabled because its configuration is missing.");

            return;
        }

        if (!options.TryGetGraphApiVersion(out _))
        {
            logger.LogError("Instagram publishing is disabled: Graph API version must have the format vX.Y.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var dueCampaigns = campaigns.ClaimDueInstagramCampaigns(DateTimeOffset.UtcNow);
            foreach (var campaign in dueCampaigns)
            {
                try
                {
                    var mediaId = await instagram.PublishImageAsync(campaign, stoppingToken);
                    campaigns.CompleteInstagramPublication(campaign, mediaId);
                    logger.LogInformation(
                        "Campaign {CampaignId} was published to Instagram as media {MediaId}.",
                        campaign.Id,
                        mediaId);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception) when (
                    exception is HttpRequestException
                    or InvalidOperationException
                    or JsonException
                    or TimeoutException
                    or OperationCanceledException)
                {
                    campaigns.FailInstagramPublication(campaign);
                    logger.LogError(
                        exception,
                        "Campaign {CampaignId} failed to publish to Instagram.",
                        campaign.Id);
                }
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
