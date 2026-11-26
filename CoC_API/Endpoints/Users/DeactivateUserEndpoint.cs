using System.Security.Claims;
using CoC_API.Context;
using CoC_API.Models;

namespace CoC_API.Endpoints.Users;

internal static class DeactivateUserEndpoint
{
    public static void MapDeactivateUserEndpoint(this WebApplication app)
    {
        app.MapPut("/users/{id:int}/deactivate", async (int id, ClaimsPrincipal principal, CoC_DbContext db) =>
        {
            var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(idValue, out var currentUserId) && currentUserId == id)
                return Results.BadRequest(new { erro = "You cannot deactivate your own account." });

            var user = await db.Users.FindAsync(id);
            if (user is null)
                return Results.NotFound();

            try
            {
                user.DeactivateUser();
                await db.SaveChangesAsync();
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { erro = EndpointErrors.FormatErrorMessage(ex) });
            }
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserCaracteristics.Admin)));
    }
}
