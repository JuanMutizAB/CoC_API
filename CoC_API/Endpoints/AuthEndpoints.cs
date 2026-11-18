using CoC_API.Context;
using CoC_API.DTOs.Request;
using CoC_API.Models;
using CoC_API.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace CoC_API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/login", async (
            LoginRequest request,
            HttpContext context,
            CoC_DbContext db,
            InMemoryUserSessionStore sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { erro = "Email and password are required." });

            var user = (await db.Users.ToListAsync())
                .FirstOrDefault(candidate => candidate.MatchesEmail(request.Email));
            if (user is null
                || !user.CheckPassword(request.Password)
                || !user.HasCharacteristic(UserCaracteristics.isActive))
            {
                return Results.Json(new { erro = "Invalid credentials." }, statusCode: StatusCodes.Status401Unauthorized);
            }

            var sessionId = sessions.CreateSession(user.id);
            var principal = UserClaimsPrincipalFactory.Create(user, sessionId);
            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = false });

            return Results.Ok(new { id = user.id, name = user.name });
        }).AllowAnonymous();

        app.MapPost("/logout", async (HttpContext context, InMemoryUserSessionStore sessions) =>
        {
            sessions.RevokeSession(context.User.FindFirst(UserClaimsPrincipalFactory.SessionIdClaimType)?.Value);
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        });
    }
}
