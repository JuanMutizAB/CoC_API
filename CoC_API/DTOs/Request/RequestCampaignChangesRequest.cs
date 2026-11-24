namespace CoC_API.DTOs.Request;

using CoC_API.Models;

public record RequestCampaignChangesRequest(string JustificationText, Motivo? Motivo = null);
