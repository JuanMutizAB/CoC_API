namespace CoC_API.DTOs.Request;

using System;
using System.Collections.Generic;
using CoC_API.Models;

public record CreateUserRequest(
    int Id,
    string Name,
    string Email,
    string Password,
    DateOnly BirthDate,
    string TaxId,
    List<UserCaracteristics>? Caracteristics
);