
namespace CoC_API.DTOs.Request;

using System.Collections.Generic;
using CoC_API.Models;

public record EditUserRequest
(
    string Name,
    string Email,
    string Password,
    List<UserCaracteristics> caracteristics
);
