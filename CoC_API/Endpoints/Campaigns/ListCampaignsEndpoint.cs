using System.Security.Claims;
using CoC_API.Context;
using CoC_API.Models;
using Microsoft.EntityFrameworkCore;
using static CoC_API.Endpoints.Campaigns.CampaignAccess;

namespace CoC_API.Endpoints.Campaigns;

internal static class ListCampaignsEndpoint
{
    public static void MapListCampaignsEndpoint(this WebApplication app)
    {
        app.MapGet("/campaigns", async (ClaimsPrincipal principal, CoC_DbContext db) =>
        {
            var user = await FindAuthenticatedUserAsync(principal, db);
            if (user is null)
                return Results.Unauthorized();

            if (user.HasCharacteristic(UserCaracteristics.Admin))
                return Results.Ok(await db.Campaigns.ToListAsync());

            var ownCampaigns = await db.Campaigns
                .Where(campaign => campaign.UserId == user.id)
                .ToListAsync();

            if (!user.HasCharacteristic(UserCaracteristics.isAprovador))
                return Results.Ok(ownCampaigns);

            var reviewCampaigns = await db.Campaigns
                .Where(campaign =>
                    campaign.Status == CampaignStatus.InReview
                    || campaign.Status == CampaignStatus.ForChange)
                .ToListAsync();

            var visibleCampaigns = ownCampaigns
                .Concat(reviewCampaigns)
                .DistinctBy(campaign => campaign.Id)
                .ToList();

            return Results.Ok(visibleCampaigns);
        });
    }
}
