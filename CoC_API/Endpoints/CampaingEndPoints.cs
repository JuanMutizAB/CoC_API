using CoC_API.DTOs.Request;
using CoC_API.Models;

namespace CoC_API.Endpoints;

public static class CampaingEndPoints
{
    public static void MapCampaignEndpoints(this WebApplication app, List<Campaign> campaigns, List<User> users)
    {
        // Endpoint to get all campaigns

        app.MapGet("/campaigns", () => Results.Ok(campaigns));

        // Endpoint to create a new campaign

        app.MapPost("/campaigns", (CreateCampaignRequest request) =>
        {
            var user = users.FirstOrDefault(user => user.id == request.UserId);
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
                campaigns.Add(campaign);
                return Results.Created($"/campaigns/{campaign.Id}", campaign);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        // Endpoint to edit a campaign by ID

        app.MapPut("/campaigns/{id:guid}/edit", (Guid id, CreateCampaignRequest request) =>
        {
            var campaign = campaigns.FirstOrDefault(c => c.Id == id);

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
                return Results.Ok(campaign);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        // Endpoint to get a campaign by ID

        app.MapGet("/campaigns/{id:guid}", (Guid id) =>
        {
            var campaign = campaigns.FirstOrDefault(c => c.Id == id);

            if (campaign == null)
                return Results.NotFound();
    
            return Results.Ok(campaign);
        });

        // Endpoint to cancel a campaign
    
        app.MapPut("/campaigns/{id:guid}/cancel", (Guid id) =>
        {
            var campaign = campaigns.FirstOrDefault(c => c.Id == id);

            if (campaign == null)
                return Results.NotFound();

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

        // Endpoint to set the motivo and justification text for a campaign

        app.MapPut("/campaigns/{id:guid}/motivo", (Guid id, MotivoRequest request) =>
        {
            var campaign = campaigns.FirstOrDefault(c => c.Id == id);

            if (campaign == null)
                return Results.NotFound();

            try
            {
                campaign.SetMotivo(request.JustificationText, request.Motivo);
                return Results.Ok(campaign);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        app.MapGet("/campaigns/{id:guid}/motivo", (Guid id) =>
        {
            var campaign = campaigns.FirstOrDefault(c => c.Id == id);

            if (campaign == null)
                return Results.NotFound();

            return Results.Ok(new { JustificationText = campaign.justificationText, Motivo = campaign.motivo });
        });
    }
}