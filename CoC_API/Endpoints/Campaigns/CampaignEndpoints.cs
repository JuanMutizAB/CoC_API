using CoC_API.Endpoints.Campaigns.Review;

namespace CoC_API.Endpoints.Campaigns;

public static class CampaignEndpoints
{
    public static void MapCampaignEndpoints(this WebApplication app)
    {
        app.MapListCampaignsEndpoint();
        app.MapCreateCampaignEndpoint();
        app.MapEditCampaignEndpoint();
        app.MapGetCampaignEndpoint();
        app.MapCancelCampaignEndpoint();
        app.MapApproveCampaignEndpoint();
        app.MapRequestCampaignChangesEndpoint();
        app.MapRejectCampaignEndpoint();
        app.MapGetCampaignMotivoEndpoint();
    }
}
