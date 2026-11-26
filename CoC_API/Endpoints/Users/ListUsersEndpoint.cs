using CoC_API.Context;
using CoC_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CoC_API.Endpoints.Users;

internal static class ListUsersEndpoint
{
    public static void MapListUsersEndpoint(this WebApplication app)
    {
        app.MapGet("/users", async (CoC_DbContext db) => Results.Ok(await db.Users.ToListAsync()))
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserCaracteristics.Admin)));
    }
}
