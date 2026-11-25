using System.Data.Common;
using System.Text.Json;
using CoC_API.Context;
using CoC_API.Integrations;
using CoC_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoC_API.Services;

public sealed class FacebookPublishingService(
    IServiceScopeFactory scopeFactory,
    FacebookGraphApiClient facebook,
    FacebookPublishingOptions options,
    ILogger<FacebookPublishingService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.IsConfigured)
        {
            if (options.HasAnyValue)
                logger.LogError("Facebook publishing is disabled: configure the Page access token, Page ID, and a valid Graph API version.");
            else
                logger.LogWarning("Facebook publishing is disabled because its configuration is missing.");

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
                logger.LogError(exception, "Could not check or update scheduled Facebook campaigns.");
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
                && campaign.FacebookPostId == null)
            .OrderBy(campaign => campaign.PublicationDate)
            .Select(campaign => new { campaign.Id, campaign.Types })
            .ToListAsync(cancellationToken);

        var dueCampaignIds = dueCandidates
            .Where(campaign => campaign.Types.Contains(CampaignType.Facebook))
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
                && campaign.FacebookPostId == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(campaign => campaign.Status, CampaignStatus.Sending),
                cancellationToken);

        if (claimed == 0)
            return;

        var campaignToPublish = await db.Campaigns.SingleAsync(
            campaign => campaign.Id == campaignId,
            cancellationToken);
        if (!campaignToPublish.Types.Contains(CampaignType.Facebook))
            return;

        try
        {
            var postId = await facebook.PublishPostAsync(campaignToPublish, cancellationToken);
            campaignToPublish.CompleteFacebookPublication(postId);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Campaign {CampaignId} was published to Facebook as post {PostId}.",
                campaignId,
                postId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            campaignToPublish.FailFacebookPublication();
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
            campaignToPublish.FailFacebookPublication();
            await db.SaveChangesAsync(CancellationToken.None);
            logger.LogError(exception, "Campaign {CampaignId} failed to publish to Facebook.", campaignId);
        }
    }
}
