using CoC_API.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CoC_API.Endpoints.Auth;

internal static class LogoutEndpoint
{
    public static void MapLogoutEndpoint(this WebApplication app)
    {
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
