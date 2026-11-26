using CoC_API.Context;
using CoC_API.DTOs.Request;
using CoC_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CoC_API.Endpoints.Users;

internal static class CreateUserEndpoint
{
    public static void MapCreateUserEndpoint(this WebApplication app)
    {
        app.MapPost("/users", async (CreateUserRequest request, CoC_DbContext db) =>
        {
            try
            {
                // O id é gerado pela BD (coluna IDENTITY), por isso passamos 0.
                var user = new User(
                    0,
                    request.Name,
                    request.Email,
                    request.Password,
                    request.BirthDate,
                    request.TaxId,
                    request.Caracteristics
                );

                var normalizedEmail = user.email.ToUpperInvariant();
                if (await db.Users.AnyAsync(existing => existing.email.ToUpper() == normalizedEmail))
                {
                    return Results.Conflict(new { erro = "A user with this email already exists." });
                }

                db.Users.Add(user);
                await db.SaveChangesAsync();
                return Results.Created($"/users/{user.id}", user);
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException)
            {
                return Results.BadRequest(new { erro = EndpointErrors.FormatErrorMessage(ex) });
            }
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserCaracteristics.Admin)));
    }
}
