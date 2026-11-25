using System.Data.Common;
using CoC_API.Context;
using CoC_API.Integrations;
using CoC_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CoC_API.Services;

public sealed class EmailPublishingService(
    IServiceScopeFactory scopeFactory,
    EmailClient email,
    EmailPublishingOptions options,
    ILogger<EmailPublishingService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.IsConfigured)
        {
            if (options.HasAnyValue)
                logger.LogError("Email publishing is disabled: configure the SMTP host, username, password, sender, and recipients.");
            else
                logger.LogWarning("Email publishing is disabled because its configuration is missing.");

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
                logger.LogError(exception, "Could not check or update scheduled email campaigns.");
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
                && campaign.EmailMessageId == null)
            .OrderBy(campaign => campaign.PublicationDate)
            .Select(campaign => new { campaign.Id, campaign.Types })
            .ToListAsync(cancellationToken);

        var dueCampaignIds = dueCandidates
            .Where(campaign => campaign.Types.Contains(CampaignType.Email))
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
                && campaign.EmailMessageId == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(campaign => campaign.Status, CampaignStatus.Sending),
                cancellationToken);

        if (claimed == 0)
            return;

        var campaignToPublish = await db.Campaigns.SingleAsync(
            campaign => campaign.Id == campaignId,
            cancellationToken);
        if (!campaignToPublish.Types.Contains(CampaignType.Email))
            return;

        try
        {
            var messageId = await email.SendCampaignAsync(campaignToPublish, cancellationToken);
            campaignToPublish.CompleteEmailPublication(messageId);
            await db.SaveChangesAsync(cancellationToken);
            var recipientCount = campaignToPublish.GetEmailRecipientList().Count;
            if (recipientCount == 0)
                recipientCount = options.RecipientList.Count;
            logger.LogInformation(
                "Campaign {CampaignId} was emailed to {Count} recipients as message {MessageId}.",
                campaignId,
                recipientCount,
                messageId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            campaignToPublish.FailEmailPublication();
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException
            or FormatException
            or TimeoutException
            or OperationCanceledException
            or ArgumentException)
        {
            campaignToPublish.FailEmailPublication();
            await db.SaveChangesAsync(CancellationToken.None);
            logger.LogError(exception, "Campaign {CampaignId} failed to send by email.", campaignId);
        }
    }
}
