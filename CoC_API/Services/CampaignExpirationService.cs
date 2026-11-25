using System.Data.Common;
using CoC_API.Context;
using CoC_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoC_API.Services;

public sealed class CampaignExpirationService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    ILogger<CampaignExpirationService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan GracePeriod = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ReviewGracePeriod = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (environment.IsEnvironment("Testing"))
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessCampaignLifecycleAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is DbException or DbUpdateException)
            {
                logger.LogError(exception, "Could not process campaign lifecycle transitions.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    internal async Task ProcessCampaignLifecycleAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoC_DbContext>();
        var now = DateTimeOffset.UtcNow;
        var cutoff = now - GracePeriod;

        var failedCount = await db.Campaigns
            .Where(campaign =>
                (campaign.Status == CampaignStatus.Approved
                 || campaign.Status == CampaignStatus.Scheduled
                 || campaign.Status == CampaignStatus.Sending)
                && campaign.PublicationDate <= cutoff)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(campaign => campaign.Status, CampaignStatus.Failed),
                cancellationToken);

        if (failedCount > 0)
        {
            logger.LogWarning(
                "Marked {FailedCount} overdue campaign(s) as Failed.",
                failedCount);
        }

        var sendingCount = await db.Campaigns
            .Where(campaign =>
                (campaign.Status == CampaignStatus.Approved
                 || campaign.Status == CampaignStatus.Scheduled)
                && campaign.PublicationDate <= now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(campaign => campaign.Status, CampaignStatus.Sending),
                cancellationToken);

        if (sendingCount > 0)
        {
            logger.LogInformation(
                "Moved {SendingCount} due campaign(s) to Sending.",
                sendingCount);
        }

        var reviewCutoff = now - ReviewGracePeriod;
        var canceledCount = await db.Campaigns
            .Where(campaign =>
                (campaign.Status == CampaignStatus.InReview
                 || campaign.Status == CampaignStatus.ForChange)
                && campaign.PublicationDate <= reviewCutoff)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(campaign => campaign.Status, CampaignStatus.Canceled)
                    .SetProperty(campaign => campaign.motivo, Motivo.Other)
                    .SetProperty(
                        campaign => campaign.justificationText,
                        "Automatically canceled 24 hours after publication date without approval or changes."),
                cancellationToken);

        if (canceledCount > 0)
        {
            logger.LogWarning(
                "Automatically canceled {CanceledCount} unreviewed campaign(s) past the 24-hour window.",
                canceledCount);
        }
    }
}
