using CoC_API.DTOs.Request;
using CoC_API.Models;

namespace CoC_API.Endpoints;

public static class CampaingEndPoints
{
    public static void MapCampaignEndpoints(this WebApplication app, List<Campaign> campaigns)
    {

        app.MapPost("/campaigns", (CreateCampaignRequest request) =>
        {
            try
            {
                var campaign = new Campaign(
                    request.Name,
                    request.Description,
                    request.Text,
                    request.Model,
                    request.Status,
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
    }

    public static void MapEditCampaignEndpoints(this WebApplication app, List<Campaign> campaigns)
    {
        app.MapPut("/campaigns/{id:guid}", (Guid id, CreateCampaignRequest request) =>
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
    }

    public static void MapGetCampaignByIdEndpoint(this WebApplication app, List<Campaign> campaigns)
    {
        app.MapGet("/campaigns/{id:guid}", (Guid id) =>
        {
            var campaign = campaigns.FirstOrDefault(c => c.Id == id);

            if (campaign == null)
                return Results.NotFound();
    
            return Results.Ok(campaign);
        });
    }

    public static void MapCancelCampaignEndpoint(this WebApplication app, List<Campaign> campaigns)
    {
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
    }
}