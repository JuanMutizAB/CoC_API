using System.Security.Claims;
using CoC_API.Context;
using CoC_API.DTOs.Request;
using CoC_API.Models;
using static CoC_API.Endpoints.Campaigns.CampaignAccess;

namespace CoC_API.Endpoints.Campaigns;

internal static class CreateCampaignEndpoint
{
    public static void MapCreateCampaignEndpoint(this WebApplication app)
    {
        app.MapPost("/campaigns", async (
            CreateCampaignRequest request,
            ClaimsPrincipal principal,
            CoC_DbContext db) =>
        {
            var user = await FindAuthenticatedUserAsync(principal, db);
            if (user is null)
                return Results.Unauthorized();

            try
            {
                var now = DateTimeOffset.UtcNow;
                var publicationStatus = Campaign.ResolvePublicationStatus(request.PublicationDate, now);
                var status = user.HasCharacteristic(UserCaracteristics.isAprovando)
                    ? CampaignStatus.InReview
                    : publicationStatus;
                var types = request.Types ?? new List<CampaignType>();
                if ((types.Contains(CampaignType.Instagram)
                     || types.Contains(CampaignType.LinkedIn)
                     || types.Contains(CampaignType.Facebook)
                     || types.Contains(CampaignType.Email))
                    && status == CampaignStatus.Sending)
                    status = CampaignStatus.Scheduled;

                var campaign = new Campaign(
                    request.Name,
                    request.Description,
                    request.ModelMsg,
                    types,
                    status,
                    request.PublicationDate,
                    request.ImageUrl,
                    user.id,
                    now,
                    request.EmailRecipients);

                db.Campaigns.Add(campaign);
                await db.SaveChangesAsync();
                return Results.Created($"/campaigns/{campaign.Id}", campaign);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { erro = EndpointErrors.FormatErrorMessage(ex) });
            }
        });
    }
}
