using CoC_API.Context;
using CoC_API.DTOs.Request;
using CoC_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CoC_API.Endpoints.Users;

internal static class EditUserEndpoint
{
    public static void MapEditUserEndpoint(this WebApplication app)
    {
        app.MapPut("/users/{id:int}", async (int id, EditUserRequest request, CoC_DbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null)
            {
                return Results.NotFound();
            }

            // Os campos não enviados no pedido mantêm o valor atual do utilizador.
            if (request.Email is not null)
            {
                var normalizedEmail = request.Email.Trim().ToUpperInvariant();
                if (await db.Users.AnyAsync(existing =>
                    existing.id != id && existing.email.ToUpper() == normalizedEmail))
                    return Results.Conflict(new { erro = "A user with this email already exists." });
            }

            try
            {
                user.UpdateUser(request.Name, request.Email, request.Password, request.caracteristics);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException)
            {
                return Results.BadRequest(new { erro = EndpointErrors.FormatErrorMessage(ex) });
            }
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserCaracteristics.Admin)));
    }
}
