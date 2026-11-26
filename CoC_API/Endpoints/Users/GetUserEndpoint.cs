using CoC_API.Context;
using CoC_API.Models;

namespace CoC_API.Endpoints.Users;

internal static class GetUserEndpoint
{
    public static void MapGetUserEndpoint(this WebApplication app)
    {
        app.MapGet("/users/{id:int}", async (int id, CoC_DbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            return user is null ? Results.NotFound() : Results.Ok(user);
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserCaracteristics.Admin)));
    }
}
