using System.Data.Common;
using System.Text.Json;
using CoC_API.Context;
using CoC_API.Integrations;
using CoC_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoC_API.Services;

public sealed class InstagramPublishingService(
    IServiceScopeFactory scopeFactory,
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
            try
            {
                await PublishDueCampaignsAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is DbException or DbUpdateException)
            {
                logger.LogError(exception, "Could not check or update scheduled Instagram campaigns.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    internal async Task PublishDueCampaignsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoC_DbContext>();
        var now = DateTimeOffset.UtcNow;

        var dueCandidates = await db.Campaigns
            .AsNoTracking()
            .Where(campaign =>
                (campaign.Status == CampaignStatus.Scheduled
                 || campaign.Status == CampaignStatus.Approved
                 || campaign.Status == CampaignStatus.Sending)
                && campaign.PublicationDate <= now
                && campaign.InstagramMediaId == null)
            .OrderBy(campaign => campaign.PublicationDate)
            .Select(campaign => new { campaign.Id, campaign.Types })
            .ToListAsync(cancellationToken);

        var dueCampaignIds = dueCandidates
            .Where(campaign => campaign.Types.Contains(CampaignType.Instagram))
            .Select(campaign => campaign.Id)
            .ToList();

        foreach (var campaignId in dueCampaignIds)
            await PublishCampaignAsync(campaignId, now, cancellationToken);
    }

    private async Task PublishCampaignAsync(
        Guid campaignId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoC_DbContext>();

        var claimed = await db.Campaigns
            .Where(campaign =>
                campaign.Id == campaignId
                && (campaign.Status == CampaignStatus.Scheduled
                    || campaign.Status == CampaignStatus.Approved
                    || campaign.Status == CampaignStatus.Sending)
                && campaign.PublicationDate <= now
                && campaign.InstagramMediaId == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(campaign => campaign.Status, CampaignStatus.Sending),
                cancellationToken);

        if (claimed == 0)
            return;

        var campaignToPublish = await db.Campaigns.SingleAsync(
            campaign => campaign.Id == campaignId,
            cancellationToken);
        if (!campaignToPublish.Types.Contains(CampaignType.Instagram))
            return;

        try
        {
            var mediaId = await instagram.PublishImageAsync(campaignToPublish, cancellationToken);
            campaignToPublish.CompleteInstagramPublication(mediaId);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Campaign {CampaignId} was published to Instagram as media {MediaId}.",
                campaignId,
                mediaId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            campaignToPublish.FailInstagramPublication();
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException
            or InvalidOperationException
            or JsonException
            or TimeoutException
            or OperationCanceledException
            or ArgumentException)
        {
            campaignToPublish.FailInstagramPublication();
            await db.SaveChangesAsync(CancellationToken.None);
            logger.LogError(exception, "Campaign {CampaignId} failed to publish to Instagram.", campaignId);
        }
    }
}
