using System.Security.Claims;
using CoC_API.Context;
using CoC_API.Models;

namespace CoC_API.Endpoints.Auth;

internal static class MeEndpoint
{
    public static void MapMeEndpoint(this WebApplication app)
    {
        app.MapGet("/me", async (ClaimsPrincipal principal, CoC_DbContext db, CancellationToken cancellationToken) =>
        {
            var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idValue, out var userId))
                return Results.Unauthorized();

            var user = await db.Users.FindAsync([userId], cancellationToken);
            if (user is null || !user.HasCharacteristic(UserCaracteristics.isActive))
                return Results.Unauthorized();

            return Results.Ok(new
            {
                id = user.id,
                name = user.name,
                email = user.email,
                caracteristics = user.caracteristics
            });
        });
    }
}
