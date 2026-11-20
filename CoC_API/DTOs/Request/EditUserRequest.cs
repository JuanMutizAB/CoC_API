
namespace CoC_API.DTOs.Request;

using System.Collections.Generic;
using CoC_API.Models;

/// <summary>
/// Todos os campos são opcionais: o que não for enviado mantém o valor atual do utilizador.
/// </summary>
public record EditUserRequest
(
    string? Name = null,
    string? Email = null,
    string? Password = null,
    List<UserCaracteristics>? caracteristics = null
);
