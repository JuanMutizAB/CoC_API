namespace CoC_API.DTOs.Request;

using CoC_API.Models;

public record CreateCampaignRequest
(
    string Name,
    string Description,
    string ModelMsg,
    List<CampaignType> Types,
    DateTimeOffset PublicationDate,
    string? ImageUrl
);
