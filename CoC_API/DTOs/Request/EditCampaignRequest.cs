
namespace CoC_API.DTOs.Request;

using CoC_API.Models;
using System;

public record EditCampaignRequest
(
       string Name,
       string Description,
       string Text,
       CampaignModel Model,
       CampaignStatus Status,
       DateTimeOffset? PublicationDate,
       string? ImageUrl
);

