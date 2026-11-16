using CoC_API.DTOs.Request;
using CoC_API.Models;

namespace CoC_API.Endpoints;

public static class CampaingEndPoints
{
    public static void MapCampaignEndpoints(this WebApplication app, List<Campaign> campaigns)
    {
        app.MapGet("/campaigns", () => Results.Ok(campaigns));

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

                campaigns.Add(campaign);
                return Results.Created($"/campaigns/{campaign.Id}", campaign);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });
    }
}