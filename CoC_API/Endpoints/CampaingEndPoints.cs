using CoC_API.Context;
using CoC_API.DTOs.Request;
using CoC_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CoC_API.Endpoints;

public static class CampaingEndPoints
{
    public static void MapCampaignEndpoints(this WebApplication app)
    {
        // Endpoint to get all campaigns

        app.MapGet("/campaigns", async (CoC_DbContext db) => Results.Ok(await db.Campaigns.ToListAsync()));

        // Endpoint to create a new campaign

        app.MapPost("/campaigns", async (CreateCampaignRequest request, CoC_DbContext db) =>
        {
            var user = await db.Users.FindAsync(request.UserId);
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
                db.Campaigns.Add(campaign);
                await db.SaveChangesAsync();
                return Results.Created($"/campaigns/{campaign.Id}", campaign);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        // Endpoint to edit a campaign by ID

        app.MapPut("/campaigns/{id:guid}/edit", async (Guid id, CreateCampaignRequest request, CoC_DbContext db) =>
        {
            var campaign = await db.Campaigns.FindAsync(id);

            if (campaign == null)
            {
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
                await db.SaveChangesAsync();
                return Results.Ok(campaign);
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        // Endpoint to get a campaign by ID

        app.MapGet("/campaigns/{id:guid}", async (Guid id, CoC_DbContext db) =>
        {
            var campaign = await db.Campaigns.FindAsync(id);

            if (campaign == null)
                return Results.NotFound();

            return Results.Ok(campaign);
        });

        // Endpoint to cancel a campaign

        app.MapPut("/campaigns/{id:guid}/cancel", async (Guid id, CoC_DbContext db) =>
        {
            var campaign = await db.Campaigns.FindAsync(id);

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
                await db.SaveChangesAsync();
                return Results.Ok(campaign);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        // Endpoint to set the motivo and justification text for a campaign

        app.MapPut("/campaigns/{id:guid}/motivo", async (Guid id, MotivoRequest request, CoC_DbContext db) =>
        {
            var campaign = await db.Campaigns.FindAsync(id);

            if (campaign == null)
                return Results.NotFound();

            var user = FindAuthenticatedUser(principal, users);
            if (user is null)
                return Results.Unauthorized();
            if (campaign.UserId != user.id)
                return Results.Forbid();

            try
            {
                campaign.SetMotivo(request.JustificationText, request.Motivo);
                await db.SaveChangesAsync();
                return Results.Ok(campaign);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        app.MapGet("/campaigns/{id:guid}/motivo", async (Guid id, CoC_DbContext db) =>
        {
            var campaign = await db.Campaigns.FindAsync(id);

            if (campaign == null)
                return Results.NotFound();

            return Results.Ok(new { JustificationText = campaign.justificationText, Motivo = campaign.motivo });
        });
    }
}
