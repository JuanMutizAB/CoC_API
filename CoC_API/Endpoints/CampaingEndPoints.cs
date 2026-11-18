using System.Security.Claims;
using CoC_API.DTOs.Request;
using CoC_API.Models;

namespace CoC_API.Endpoints;

public static class CampaingEndPoints
{
    public static void MapCampaignEndpoints(this WebApplication app, List<Campaign> campaigns, List<User> users)
    {
        app.MapGet("/campaigns", () => Results.Ok(campaigns));

        app.MapPost("/campaigns", (CreateCampaignRequest request, ClaimsPrincipal principal) =>
        {
            var user = FindAuthenticatedUser(principal, users);
            if (user is null)
                return Results.Unauthorized();

            try
            {
                var status = user.HasCharacteristic(UserCaracteristics.isAprovando)
                    ? CampaignStatus.InReview
                    : request.Status;

                var campaign = new Campaign(
                    request.Name,
                    request.Description,
                    request.Text,
                    request.Model,
                    status,
                    request.PublicationDate,
                    request.ImageUrl,
                    user.id);

                campaign.CheckStatus();
                campaigns.Add(campaign);
                return Results.Created($"/campaigns/{campaign.Id}", campaign);
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

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

            try
            {
                var status = user.HasCharacteristic(UserCaracteristics.isAprovando)
                    ? CampaignStatus.InReview
                    : request.Status;

                campaign.EditCampaign(
                    request.Name,
                    request.Description,
                    request.Text,
                    request.Model,
                    status,
                    request.PublicationDate,
                    request.ImageUrl,
                    user.id);

                campaign.CheckStatus();
                return Results.Ok(campaign);
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        app.MapGet("/campaigns/{id:guid}", (Guid id) =>
        {
            var campaign = campaigns.FirstOrDefault(candidate => candidate.Id == id);
            return campaign is null ? Results.NotFound() : Results.Ok(campaign);
        });

        app.MapPut("/campaigns/{id:guid}/cancel", (Guid id, ClaimsPrincipal principal) =>
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
                campaign.CancelCampaign();
                return Results.Ok(campaign);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        app.MapPut("/campaigns/{id:guid}/motivo", (Guid id, MotivoRequest request, ClaimsPrincipal principal) =>
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
                campaign.SetMotivo(request.JustificationText, request.Motivo);
                return Results.Ok(campaign);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        app.MapGet("/campaigns/{id:guid}/motivo", (Guid id) =>
        {
            var campaign = campaigns.FirstOrDefault(candidate => candidate.Id == id);
            if (campaign is null)
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
}
