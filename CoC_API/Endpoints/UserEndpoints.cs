using CoC_API.Context;
using CoC_API.DTOs.Request;
using CoC_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CoC_API.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        app.MapGet("/users", async (CoC_DbContext db) => Results.Ok(await db.Users.ToListAsync()));

        app.MapGet("/users/{id:int}", async (int id, CoC_DbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        app.MapPut("/users/{id:int}", async (int id, EditUserRequest request, CoC_DbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null)
            {
                return Results.NotFound();
            }

            var normalizedEmail = request.Email.Trim().ToUpperInvariant();
            if (await db.Users.AnyAsync(existing =>
                existing.id != id && existing.email.ToUpper() == normalizedEmail))
                return Results.Conflict(new { erro = "A user with this email already exists." });

            try
            {
                user.UpdateUser(request.Name, request.Email, request.Password, request.caracteristics);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserCaracteristics.Admin)));

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
                return Results.BadRequest(new { erro = ex.Message });
            }
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserCaracteristics.Admin)));
    }
}
