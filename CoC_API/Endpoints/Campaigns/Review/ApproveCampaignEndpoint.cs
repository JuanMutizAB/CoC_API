using System.Security.Claims;
using CoC_API.Context;
using CoC_API.Models;
using Microsoft.EntityFrameworkCore;
using static CoC_API.Endpoints.Campaigns.CampaignAccess;

namespace CoC_API.Endpoints.Campaigns.Review;

internal static class ApproveCampaignEndpoint
{
    public static void MapApproveCampaignEndpoint(this WebApplication app)
    {
        app.MapPut("/campaigns/{id:guid}/review/approve", async (
            Guid id,
            ClaimsPrincipal principal,
            CoC_DbContext db) =>
        {
            var campaign = await db.Campaigns.FindAsync(id);
            if (campaign is null)
                return Results.NotFound();

            var user = await FindAuthenticatedUserAsync(principal, db);
            if (user is null)
                return Results.Unauthorized();
            if (!user.HasCharacteristic(UserCaracteristics.isAprovador))
                return Results.Forbid();
            if (!await IsReviewableCampaignAsync(campaign, db))
                return Results.Forbid();

            try
            {
                campaign.Approve();
                await db.SaveChangesAsync();
                return Results.Ok(campaign);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return Results.BadRequest(new { erro = EndpointErrors.FormatErrorMessage(ex) });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(new { erro = "The campaign changed while it was being reviewed." });
            }
        });
    }
}
