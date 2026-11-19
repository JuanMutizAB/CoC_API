
namespace CoC_API.DTOs.Request;

using CoC_API.Models;
using System;

public record EditCampaignRequest
(
       string Name,
       string Description,
       string ModelMsg,
       CampaignType Type,
       DateTimeOffset PublicationDate,
       string? ImageUrl
);

