namespace CoC_API.DTOs.Request;

using CoC_API.Models;

public record CreateCampaignRequest
(
    string Name,
    string Description,
    string ModelMsg,
    CampaignType Type,
    DateTimeOffset PublicationDate,
    string? ImageUrl
);
