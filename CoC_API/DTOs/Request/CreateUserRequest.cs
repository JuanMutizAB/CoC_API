namespace CoC_API.DTOs.Request;

using System;
using System.Collections.Generic;
using CoC_API.Models;

public record CreateUserRequest(
    int id,
    string name,
    string email,
    string password,
    DateTime birthDate,
    string taxId,
    List<UserCaracteristics>? caracteristics
);