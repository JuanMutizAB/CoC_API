namespace CoC_API.DTOs.Request;

using CoC_API.Models;

public record MotivoRequest
(
    Guid Id, 
    string JustificationText,
    Motivo Motivo,
    int UserId
);
