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
                return Results.NotFound(new { erro = "User not found." });

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
                    request.UserId);

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
            }

            try
            {
                campaign.EditCampaign(
                    request.Name,
                    request.Description,
                    request.Text,
                    request.Model,
                    request.Status,
                    request.PublicationDate,
                    request.ImageUrl,
                    request.UserId);

                campaign.CheckStatus();
                await db.SaveChangesAsync();
                return Results.Ok(campaign);
            }
            catch (ArgumentException ex)
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

            if (campaign == null)
                return Results.NotFound();

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
