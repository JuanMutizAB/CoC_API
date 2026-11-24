using System.Security.Claims;
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
            UserSessionStore sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { erro = "Email and password are required." });

            var normalizedEmail = request.Email.Trim().ToUpperInvariant();
            var user = await db.Users.FirstOrDefaultAsync(
                candidate => candidate.email.ToUpper() == normalizedEmail,
                context.RequestAborted);
            if (user is null
                || !user.CheckPassword(request.Password)
                || !user.HasCharacteristic(UserCaracteristics.isActive))
            {
                return Results.Json(new { erro = "Invalid credentials." }, statusCode: StatusCodes.Status401Unauthorized);
            }

            var sessionId = await sessions.CreateSessionAsync(user.id, context.RequestAborted);
            var principal = UserClaimsPrincipalFactory.Create(user, sessionId);
            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = false });

            return Results.Ok(new
            {
                id = user.id,
                name = user.name,
                email = user.email,
                caracteristics = user.caracteristics
            });
        }).AllowAnonymous();

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

        app.MapPost("/logout", async (HttpContext context, UserSessionStore sessions) =>
        {
            await sessions.RevokeSessionAsync(
                context.User.FindFirst(UserClaimsPrincipalFactory.SessionIdClaimType)?.Value,
                context.RequestAborted);
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        });
    }
}
