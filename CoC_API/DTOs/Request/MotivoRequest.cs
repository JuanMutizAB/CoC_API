namespace CoC_API.DTOs.Request;

using CoC_API.Models;

public record MotivoRequest
(
    string JustificationText,
    Motivo Motivo
);
