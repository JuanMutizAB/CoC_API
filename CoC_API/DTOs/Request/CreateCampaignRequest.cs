namespace CoC_API.DTOs.Request;

using CoC_API.Models;

public record CreateCampaignRequest
(
    string Name,
    string Description,
    string Text,
    CampaignModel Model,
    CampaignStatus Status,
    DateTimeOffset? PublicationDate,
    string? ImageUrl,
    int UserId
);
