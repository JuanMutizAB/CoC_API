using System.Security.Claims;
using CoC_API.DTOs.Request;
using CoC_API.Models;

namespace CoC_API.Endpoints;

public static class CampaingEndPoints
{
    public static void MapCampaignEndpoints(this WebApplication app, List<Campaign> campaigns, List<User> users)
    {
        // Endpoint to get all campaigns

        app.MapGet("/campaigns", (ClaimsPrincipal principal) =>
        {
            var user = FindAuthenticatedUser(principal, users);
            if (user is null)
                return Results.Unauthorized();

            var visibleCampaigns = campaigns
                .Where(campaign => CanViewCampaign(campaign, user, users))
                .ToList();
            return Results.Ok(visibleCampaigns);
        });

        // Endpoint to create a new campaign

        app.MapPost("/campaigns", (CreateCampaignRequest request, ClaimsPrincipal principal) =>
        {
            var user = FindAuthenticatedUser(principal, users);
            if (user is null)
                return Results.Unauthorized();

            try
            {
                var now = DateTimeOffset.UtcNow;
                var publicationStatus = Campaign.ResolvePublicationStatus(request.PublicationDate, now);
                var status = user.HasCharacteristic(UserCaracteristics.isAprovando)
                    ? CampaignStatus.InReview
                    : publicationStatus;

                var campaign = new Campaign(
                    request.Name,
                    request.Description,
                    request.ModelMsg,
                    request.Type,
                    status,
                    request.PublicationDate,
                    request.ImageUrl,
                    user.id,
                    now);

                campaign.CheckStatus(now);
                campaigns.Add(campaign);
                return Results.Created($"/campaigns/{campaign.Id}", campaign);
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        // Endpoint to edit a campaign by ID

        app.MapPut("/campaigns/{id:guid}/edit", (Guid id, EditCampaignRequest request, ClaimsPrincipal principal) =>
        {
            var campaign = campaigns.FirstOrDefault(candidate => candidate.Id == id);
            if (campaign is null)
                return Results.NotFound();

            var user = FindAuthenticatedUser(principal, users);
            if (user is null)
                return Results.Unauthorized();
            if (campaign.UserId != user.id)
                return Results.Forbid();
            if (campaign.Status is CampaignStatus.Canceled or CampaignStatus.Closed)
                return Results.Conflict(new { erro = "This campaign can no longer be edited." });
            if (campaign.Status == CampaignStatus.ForChange
                && !user.HasCharacteristic(UserCaracteristics.isAprovando))
                return Results.Forbid();

            try
            {
                var now = DateTimeOffset.UtcNow;
                var status = campaign.Status == CampaignStatus.ForChange
                    && user.HasCharacteristic(UserCaracteristics.isAprovando)
                    ? CampaignStatus.InReview
                    : campaign.Status;
                if (status == CampaignStatus.Scheduled)
                    status = Campaign.ResolvePublicationStatus(request.PublicationDate, now);

                campaign.EditCampaign(
                    request.Name,
                    request.Description,
                    request.ModelMsg,
                    request.Type,
                    status,
                    request.PublicationDate,
                    request.ImageUrl,
                    now);

                campaign.CheckStatus(now);
                return Results.Ok(campaign);
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        // Endpoint to get a campaign by ID

        app.MapGet("/campaigns/{id:guid}", (Guid id, ClaimsPrincipal principal) =>
        {
            var campaign = campaigns.FirstOrDefault(candidate => candidate.Id == id);
            if (campaign is null)
                return Results.NotFound();

            var user = FindAuthenticatedUser(principal, users);
            if (user is null)
                return Results.Unauthorized();
            return CanViewCampaign(campaign, user, users)
                ? Results.Ok(campaign)
                : Results.NotFound();
        });

        // Endpoint to cancel a campaign

        app.MapPut("/campaigns/{id:guid}/cancel", (Guid id, MotivoRequest request, ClaimsPrincipal principal) =>
        {
            var campaign = campaigns.FirstOrDefault(candidate => candidate.Id == id);
            if (campaign is null)
                return Results.NotFound();

            var user = FindAuthenticatedUser(principal, users);
            if (user is null)
                return Results.Unauthorized();
            if (campaign.UserId != user.id)
                return Results.Forbid();

            try
            {
                campaign.CancelCampaign(request.Motivo, request.JustificationText);
                return Results.Ok(campaign);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        app.MapPut("/campaigns/{id:guid}/review/approve", (Guid id, ClaimsPrincipal principal) =>
        {
            var campaign = campaigns.FirstOrDefault(candidate => candidate.Id == id);
            if (campaign is null)
                return Results.NotFound();

            var user = FindAuthenticatedUser(principal, users);
            if (user is null)
                return Results.Unauthorized();
            if (!user.HasCharacteristic(UserCaracteristics.isAprovador))
                return Results.Forbid();

            try
            {
                campaign.Approve();
                return Results.Ok(campaign);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        app.MapPut("/campaigns/{id:guid}/review/request-changes", (
            Guid id,
            RequestCampaignChangesRequest request,
            ClaimsPrincipal principal) =>
        {
            var campaign = campaigns.FirstOrDefault(candidate => candidate.Id == id);
            if (campaign is null)
                return Results.NotFound();

            var user = FindAuthenticatedUser(principal, users);
            if (user is null)
                return Results.Unauthorized();
            if (!user.HasCharacteristic(UserCaracteristics.isAprovador))
                return Results.Forbid();

            try
            {
                campaign.RequestChanges(request.JustificationText);
                return Results.Ok(campaign);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        app.MapPut("/campaigns/{id:guid}/review/reject", (
            Guid id,
            MotivoRequest request,
            ClaimsPrincipal principal) =>
        {
            var campaign = campaigns.FirstOrDefault(candidate => candidate.Id == id);
            if (campaign is null)
                return Results.NotFound();

            var user = FindAuthenticatedUser(principal, users);
            if (user is null)
                return Results.Unauthorized();
            if (!user.HasCharacteristic(UserCaracteristics.isAprovador))
                return Results.Forbid();

            try
            {
                campaign.Reject(request.Motivo, request.JustificationText);
                return Results.Ok(campaign);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        app.MapGet("/campaigns/{id:guid}/motivo", (Guid id, ClaimsPrincipal principal) =>
        {
            var campaign = campaigns.FirstOrDefault(candidate => candidate.Id == id);
            if (campaign is null)
                return Results.NotFound();

            var user = FindAuthenticatedUser(principal, users);
            if (user is null)
                return Results.Unauthorized();
            if (!CanViewCampaign(campaign, user, users))
                return Results.NotFound();

            return Results.Ok(new { JustificationText = campaign.justificationText, Motivo = campaign.motivo });
        });
    }

    private static User? FindAuthenticatedUser(ClaimsPrincipal principal, List<User> users)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idValue, out var id)
            ? users.FirstOrDefault(user =>
                user.id == id && user.HasCharacteristic(UserCaracteristics.isActive))
            : null;
    }

    private static bool CanViewCampaign(Campaign campaign, User user, List<User> users)
    {
        if (campaign.UserId == user.id)
            return true;

        return user.HasCharacteristic(UserCaracteristics.isAprovador)
            && campaign.Status == CampaignStatus.InReview
            && users.Any(candidate =>
                candidate.id == campaign.UserId
                && candidate.HasCharacteristic(UserCaracteristics.isAprovando));
    }
}
