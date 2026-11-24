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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (environment.IsEnvironment("Testing"))
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FailOverdueCampaignsAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is DbException or DbUpdateException)
            {
                logger.LogError(exception, "Could not mark overdue campaigns as failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    internal async Task FailOverdueCampaignsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoC_DbContext>();
        var cutoff = DateTimeOffset.UtcNow - GracePeriod;

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
    }
}
