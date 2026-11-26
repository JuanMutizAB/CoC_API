using CoC_API.Context;
using CoC_API.DTOs.Request;
using CoC_API.Models;
using CoC_API.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace CoC_API.Endpoints.Auth;

internal static class LoginEndpoint
{
    public static void MapLoginEndpoint(this WebApplication app)
    {
        app.MapPost("/login", async (
            LoginRequest request,
            HttpContext context,
            CoC_DbContext db,
            UserSessionStore sessions,
            LoginLockoutService lockouts) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { erro = "Email and password are required." });

            var normalizedEmail = request.Email.Trim().ToUpperInvariant();
            var currentLock = lockouts.CheckStatus(normalizedEmail);
            if (currentLock.IsLocked)
            {
                context.Response.Headers.RetryAfter = currentLock.RetryAfterSeconds.ToString();
                return Results.Json(
                    new
                    {
                        erro = currentLock.Message,
                        retryAfterSeconds = currentLock.RetryAfterSeconds
                    },
                    statusCode: StatusCodes.Status429TooManyRequests);
            }

            var user = await db.Users.FirstOrDefaultAsync(
                candidate => candidate.email.ToUpper() == normalizedEmail,
                context.RequestAborted);
            if (user is null
                || !user.CheckPassword(request.Password)
                || !user.HasCharacteristic(UserCaracteristics.isActive))
            {
                var failureLock = lockouts.RecordFailure(normalizedEmail);
                if (failureLock.IsLocked)
                {
                    context.Response.Headers.RetryAfter = failureLock.RetryAfterSeconds.ToString();
                    return Results.Json(
                        new
                        {
                            erro = failureLock.Message,
                            retryAfterSeconds = failureLock.RetryAfterSeconds
                        },
                        statusCode: StatusCodes.Status429TooManyRequests);
                }

                return Results.Json(new { erro = "Invalid credentials." }, statusCode: StatusCodes.Status401Unauthorized);
            }

            lockouts.Reset(normalizedEmail);

            if (db.ChangeTracker.HasChanges())
                await db.SaveChangesAsync(context.RequestAborted);

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
        }).AllowAnonymous().RequireRateLimiting("login-ip");
    }
}
