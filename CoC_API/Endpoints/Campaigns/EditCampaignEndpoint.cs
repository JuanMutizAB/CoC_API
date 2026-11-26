using System.Security.Claims;
using CoC_API.Context;
using CoC_API.DTOs.Request;
using CoC_API.Models;
using Microsoft.EntityFrameworkCore;
using static CoC_API.Endpoints.Campaigns.CampaignAccess;

namespace CoC_API.Endpoints.Campaigns;

internal static class EditCampaignEndpoint
{
    public static void MapEditCampaignEndpoint(this WebApplication app)
    {
        app.MapPut("/campaigns/{id:guid}/edit", async (
            Guid id,
            EditCampaignRequest request,
            ClaimsPrincipal principal,
            CoC_DbContext db) =>
        {
            var campaign = await db.Campaigns.FindAsync(id);
            if (campaign is null)
                return Results.NotFound();

            var user = await FindAuthenticatedUserAsync(principal, db);
            if (user is null)
                return Results.Unauthorized();
            var isAdmin = user.HasCharacteristic(UserCaracteristics.Admin);
            var isAprovador = user.HasCharacteristic(UserCaracteristics.isAprovador);
            var canAprovadorEditReviewable = isAprovador
                && campaign.Status is CampaignStatus.ForChange or CampaignStatus.InReview
                && await CanViewCampaignAsync(campaign, user, db);
            if (campaign.UserId != user.id && !isAdmin && !canAprovadorEditReviewable)
                return Results.Forbid();
            if (campaign.Status is CampaignStatus.Canceled or CampaignStatus.Closed or CampaignStatus.Sending or CampaignStatus.Failed)
                return Results.Conflict(new { erro = "This campaign can no longer be edited." });
            if (campaign.Status == CampaignStatus.ForChange
                && !isAdmin
                && !isAprovador
                && !user.HasCharacteristic(UserCaracteristics.isAprovando))
                return Results.Forbid();

            try
            {
                var now = DateTimeOffset.UtcNow;

                // Os campos não enviados no pedido mantêm o valor atual da campanha.
                var name = request.Name ?? campaign.Name;
                var description = request.Description ?? campaign.Description;
                var modelMsg = request.ModelMsg ?? campaign.ModelMsg;
                var types = request.Types ?? campaign.Types;
                var publicationDate = request.PublicationDate ?? campaign.PublicationDate;
                var imageUrl = request.ImageUrl is null
                    ? campaign.ImageUrl
                    : string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl;
                var emailRecipients = request.EmailRecipients is null
                    ? campaign.EmailRecipients
                    : string.IsNullOrWhiteSpace(request.EmailRecipients) ? null : request.EmailRecipients;

                if (campaign.Status is CampaignStatus.InReview or CampaignStatus.ForChange
                    && Campaign.IsPastMinute(publicationDate, now))
                {
                    if (campaign.PublicationDate <= now - TimeSpan.FromHours(24))
                        return Results.Conflict(new { erro = "Campaign review period has expired." });

                    publicationDate = now;
                }

                var status = campaign.Status;
                if (campaign.Status == CampaignStatus.ForChange)
                {
                    status = isAdmin || isAprovador
                        ? Campaign.ResolvePublicationStatus(publicationDate, now)
                        : CampaignStatus.InReview;
                }
                else if (campaign.Status == CampaignStatus.InReview && (isAdmin || isAprovador))
                {
                    status = Campaign.ResolvePublicationStatus(publicationDate, now);
                }
                else if (status == CampaignStatus.Scheduled)
                {
                    status = Campaign.ResolvePublicationStatus(publicationDate, now);
                }

                if ((types.Contains(CampaignType.Instagram)
                     || types.Contains(CampaignType.LinkedIn)
                     || types.Contains(CampaignType.Facebook)
                     || types.Contains(CampaignType.Email))
                    && status == CampaignStatus.Sending)
                    status = CampaignStatus.Scheduled;

                campaign.EditCampaign(
                    name,
                    description,
                    modelMsg,
                    types,
                    status,
                    publicationDate,
                    imageUrl,
                    now,
                    emailRecipients);

                await db.SaveChangesAsync();
                return Results.Ok(campaign);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { erro = EndpointErrors.FormatErrorMessage(ex) });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(new { erro = "The campaign changed while it was being edited." });
            }
        });
    }
}
