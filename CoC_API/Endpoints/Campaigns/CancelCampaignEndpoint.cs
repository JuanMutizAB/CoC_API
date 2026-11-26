using System.Security.Claims;
using CoC_API.Context;
using CoC_API.DTOs.Request;
using CoC_API.Models;
using Microsoft.EntityFrameworkCore;
using static CoC_API.Endpoints.Campaigns.CampaignAccess;

namespace CoC_API.Endpoints.Campaigns;

internal static class CancelCampaignEndpoint
{
    public static void MapCancelCampaignEndpoint(this WebApplication app)
    {
        app.MapPut("/campaigns/{id:guid}/cancel", async (
            Guid id,
            MotivoRequest request,
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
            if (campaign.UserId != user.id && !isAdmin)
                return Results.Forbid();
            if (campaign.Status == CampaignStatus.ForChange && !isAdmin)
                return Results.Forbid();

            try
            {
                campaign.CancelCampaign(request.Motivo, request.JustificationText);
                await db.SaveChangesAsync();
                return Results.Ok(campaign);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return Results.BadRequest(new { erro = EndpointErrors.FormatErrorMessage(ex) });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(new { erro = "The campaign changed while it was being canceled." });
            }
        });
    }
}
