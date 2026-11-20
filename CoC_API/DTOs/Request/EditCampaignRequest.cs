
namespace CoC_API.DTOs.Request;

using CoC_API.Models;
using System;

/// <summary>
/// Todos os campos são opcionais: o que não for enviado mantém o valor atual da campanha.
/// Para remover a imagem envia-se <c>ImageUrl</c> como string vazia.
/// </summary>
public record EditCampaignRequest
(
       string? Name = null,
       string? Description = null,
       string? ModelMsg = null,
       CampaignType? Type = null,
       DateTimeOffset? PublicationDate = null,
       string? ImageUrl = null
);

