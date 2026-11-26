using System.Security.Claims;
using CoC_API.Context;
using static CoC_API.Endpoints.Campaigns.CampaignAccess;

namespace CoC_API.Endpoints.Campaigns;

internal static class GetCampaignMotivoEndpoint
{
    public static void MapGetCampaignMotivoEndpoint(this WebApplication app)
    {
        app.MapGet("/campaigns/{id:guid}/motivo", async (
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
            if (!await CanViewCampaignAsync(campaign, user, db))
                return Results.NotFound();

            return Results.Ok(new
            {
                JustificationText = campaign.justificationText,
                Motivo = campaign.motivo
            });
        });
    }
}
