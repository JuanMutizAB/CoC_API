using System.Data.Common;
using System.Text.Json;
using CoC_API.Context;
using CoC_API.Integrations;
using CoC_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoC_API.Services;

public sealed class LinkedInPublishingService(
    IServiceScopeFactory scopeFactory,
    LinkedInApiClient linkedIn,
    LinkedInPublishingOptions options,
    ILogger<LinkedInPublishingService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.IsConfigured)
        {
            if (options.HasAnyValue)
                logger.LogError("LinkedIn publishing is disabled: configure the access token, person URN, and API version.");
            else
                logger.LogWarning("LinkedIn publishing is disabled because its configuration is missing.");

            return;
        }

        if (!options.IsValidApiVersion || !options.IsValidPersonUrn)
        {
            logger.LogError("LinkedIn publishing is disabled: API version or person URN is invalid.");
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
                logger.LogError(exception, "Could not check or update scheduled LinkedIn campaigns.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    internal async Task PublishDueCampaignsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoC_DbContext>();
        var now = DateTimeOffset.UtcNow;

        var dueCampaignIds = await db.Campaigns
            .AsNoTracking()
            .Where(campaign =>
                campaign.Type == CampaignType.LinkedIn
                && (campaign.Status == CampaignStatus.Scheduled || campaign.Status == CampaignStatus.Approved)
                && campaign.PublicationDate <= now
                && campaign.LinkedInPostId == null)
            .OrderBy(campaign => campaign.PublicationDate)
            .Select(campaign => campaign.Id)
            .ToListAsync(cancellationToken);

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
                && campaign.Type == CampaignType.LinkedIn
                && (campaign.Status == CampaignStatus.Scheduled || campaign.Status == CampaignStatus.Approved)
                && campaign.PublicationDate <= now
                && campaign.LinkedInPostId == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(campaign => campaign.Status, CampaignStatus.Sending),
                cancellationToken);

        if (claimed == 0)
            return;

        var campaignToPublish = await db.Campaigns.SingleAsync(
            campaign => campaign.Id == campaignId,
            cancellationToken);

        try
        {
            var postId = await linkedIn.PublishPostAsync(campaignToPublish, cancellationToken);
            campaignToPublish.CompleteLinkedInPublication(postId);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Campaign {CampaignId} was published to LinkedIn as post {PostId}.",
                campaignId,
                postId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            campaignToPublish.FailLinkedInPublication();
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException
            or InvalidOperationException
            or JsonException
            or FormatException
            or TimeoutException
            or OperationCanceledException
            or ArgumentException)
        {
            campaignToPublish.FailLinkedInPublication();
            await db.SaveChangesAsync(CancellationToken.None);
            logger.LogError(exception, "Campaign {CampaignId} failed to publish to LinkedIn.", campaignId);
        }
    }
}
