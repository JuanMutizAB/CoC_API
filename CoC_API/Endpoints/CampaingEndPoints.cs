using System.Security.Claims;
using CoC_API.Context;
using CoC_API.DTOs.Request;
using CoC_API.Models;
using CoC_API.Services;
using Microsoft.EntityFrameworkCore;

namespace CoC_API.Endpoints;

public static class CampaingEndPoints
{
    public static void MapCampaignEndpoints(this WebApplication app, CampaignStore campaigns)
    {
        // Endpoint to get all campaigns

        app.MapGet("/campaigns", () => Results.Ok(campaigns.GetAll()));

        // Endpoint to create a new campaign

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
                var status = user.HasCharacteristic(UserCaracteristics.isAprovando)
                    ? CampaignStatus.InReview
                    : CampaignStatus.New;

                var campaign = new Campaign(
                    request.Name,
                    request.Description,
                    request.ModelMsg,
                    request.Type,
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

        // Endpoint to edit a campaign by ID

        app.MapPut("/campaigns/{id:guid}/edit", async (
            Guid id,
            EditCampaignRequest request,
            ClaimsPrincipal principal,
            CoC_DbContext db) =>
        {
            var campaign = campaigns.Get(id);
            if (campaign is null)
                return Results.NotFound();

            var user = await FindAuthenticatedUserAsync(principal, db);
            if (user is null)
                return Results.Unauthorized();
            if (campaign.UserId != user.id)
                return Results.Forbid();

            try
            {
                var status = user.HasCharacteristic(UserCaracteristics.isAprovando)
                    ? CampaignStatus.InReview
                    : campaign.Status;

                campaigns.Update(id, currentCampaign =>
                {
                    currentCampaign.EditCampaign(
                        request.Name,
                        request.Description,
                        request.ModelMsg,
                        request.Type,
                        status,
                        request.PublicationDate,
                        request.ImageUrl,
                        user.id);

                    currentCampaign.CheckStatus();
                });
                return Results.Ok(campaign);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { erro = ex.Message });
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        // Endpoint to get a campaign by ID

        app.MapGet("/campaigns/{id:guid}", (Guid id) =>
        {
            var campaign = campaigns.Get(id);
            return campaign is null ? Results.NotFound() : Results.Ok(campaign);
        });

        // Endpoint to cancel a campaign

        app.MapPut("/campaigns/{id:guid}/cancel", async (
            Guid id,
            ClaimsPrincipal principal,
            CoC_DbContext db) =>
        {
            var campaign = campaigns.Get(id);
            if (campaign is null)
                return Results.NotFound();

            var user = await FindAuthenticatedUserAsync(principal, db);
            if (user is null)
                return Results.Unauthorized();
            if (campaign.UserId != user.id)
                return Results.Forbid();

            try
            {
                campaigns.Update(id, currentCampaign => currentCampaign.CancelCampaign());
                return Results.Ok(campaign);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        // Endpoint to set the motivo and justification text for a campaign

        app.MapPut("/campaigns/{id:guid}/motivo", async (
            Guid id,
            MotivoRequest request,
            ClaimsPrincipal principal,
            CoC_DbContext db) =>
        {
            var campaign = campaigns.Get(id);
            if (campaign is null)
                return Results.NotFound();

            var user = await FindAuthenticatedUserAsync(principal, db);
            if (user is null)
                return Results.Unauthorized();
            if (campaign.UserId != user.id)
                return Results.Forbid();

            try
            {
                campaigns.Update(
                    id,
                    currentCampaign => currentCampaign.SetMotivo(request.JustificationText, request.Motivo));
                return Results.Ok(campaign);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        app.MapGet("/campaigns/{id:guid}/motivo", (Guid id) =>
        {
            var campaign = campaigns.Get(id);
            if (campaign is null)
                return Results.NotFound();

            return Results.Ok(new { JustificationText = campaign.justificationText, Motivo = campaign.motivo });
        });
    }

    private static async Task<User?> FindAuthenticatedUserAsync(ClaimsPrincipal principal, CoC_DbContext db)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idValue, out var id))
            return null;

        var user = await db.Users.FirstOrDefaultAsync(candidate => candidate.id == id);
        return user is not null && user.HasCharacteristic(UserCaracteristics.isActive)
            ? user
            : null;
    }
}
