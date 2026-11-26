using System.Security.Claims;
using CoC_API.Context;
using CoC_API.Models;

namespace CoC_API.Endpoints.Campaigns;

internal static class CampaignAccess
{
    public static async Task<User?> FindAuthenticatedUserAsync(ClaimsPrincipal principal, CoC_DbContext db)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idValue, out var userId))
            return null;

        var user = await db.Users.FindAsync(userId);
        return user is not null && user.HasCharacteristic(UserCaracteristics.isActive)
            ? user
            : null;
    }

    public static Task<bool> CanViewCampaignAsync(
        Campaign campaign,
        User user,
        CoC_DbContext db)
    {
        if (user.HasCharacteristic(UserCaracteristics.Admin) || campaign.UserId == user.id)
            return Task.FromResult(true);

        return Task.FromResult(
            user.HasCharacteristic(UserCaracteristics.isAprovador)
            && campaign.Status is CampaignStatus.InReview or CampaignStatus.ForChange);
    }

    public static Task<bool> IsReviewableCampaignAsync(Campaign campaign, CoC_DbContext db)
    {
        return Task.FromResult(campaign.Status == CampaignStatus.InReview);
    }
}
