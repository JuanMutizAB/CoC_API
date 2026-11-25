using System.Data.Common;
using CoC_API.Context;
using CoC_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CoC_API.Services;

/// <summary>
/// Unified background orchestrator that polls due campaigns and delegates
/// channel-specific publishing to registered <see cref="ICampaignChannelPublisher"/> strategies.
/// </summary>
public sealed class CampaignPublishingService(
    IServiceScopeFactory scopeFactory,
    IEnumerable<ICampaignChannelPublisher> publishers,
    ILogger<CampaignPublishingService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var activePublishers = publishers
            .Where(publisher => publisher.ValidateConfiguration(logger))
            .ToList();

        if (activePublishers.Count == 0)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var publisher in activePublishers)
            {
                try
                {
                    await PublishDueCampaignsAsync(publisher, stoppingToken);
                }
                catch (Exception exception) when (exception is DbException or DbUpdateException)
                {
                    logger.LogError(
                        exception,
                        "Could not check or update scheduled {Channel} campaigns.",
                        publisher.Channel);
                }
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    internal async Task PublishDueCampaignsAsync(CancellationToken cancellationToken)
    {
        foreach (var publisher in publishers)
            await PublishDueCampaignsAsync(publisher, cancellationToken);
    }

    internal async Task PublishDueCampaignsAsync(
        ICampaignChannelPublisher publisher,
        CancellationToken cancellationToken)
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
                && campaign.PublicationDate <= now)
            .Where(publisher.PendingChannelFilter)
            .OrderBy(campaign => campaign.PublicationDate)
            .Select(campaign => new { campaign.Id, campaign.Types })
            .ToListAsync(cancellationToken);

        var dueCampaignIds = dueCandidates
            .Where(campaign => campaign.Types.Contains(publisher.Channel))
            .Select(campaign => campaign.Id)
            .ToList();

        foreach (var campaignId in dueCampaignIds)
            await PublishCampaignAsync(publisher, campaignId, now, cancellationToken);
    }

    private async Task PublishCampaignAsync(
        ICampaignChannelPublisher publisher,
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
                && campaign.PublicationDate <= now)
            .Where(publisher.PendingChannelFilter)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(campaign => campaign.Status, CampaignStatus.Sending),
                cancellationToken);

        if (claimed == 0)
            return;

        var campaignToPublish = await db.Campaigns.SingleAsync(
            campaign => campaign.Id == campaignId,
            cancellationToken);
        if (!campaignToPublish.Types.Contains(publisher.Channel))
            return;

        try
        {
            var externalId = await publisher.PublishAsync(campaignToPublish, cancellationToken);
            publisher.MarkPublished(campaignToPublish, externalId);
            await db.SaveChangesAsync(cancellationToken);
            publisher.LogPublished(logger, campaignToPublish, externalId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            publisher.MarkFailed(campaignToPublish);
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception exception) when (publisher.IsHandledFailure(exception))
        {
            publisher.MarkFailed(campaignToPublish);
            await db.SaveChangesAsync(CancellationToken.None);
            logger.LogError(
                exception,
                "Campaign {CampaignId} failed to publish to {Channel}.",
                campaignId,
                publisher.Channel);
        }
    }
}
