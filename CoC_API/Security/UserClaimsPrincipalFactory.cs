using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using CoC_API.Models;

namespace CoC_API.Security;

public static class UserClaimsPrincipalFactory
{
    public const string SessionIdClaimType = "session_id";

    public static ClaimsPrincipal Create(User user, string sessionId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.id.ToString()),
            new(ClaimTypes.Name, user.name),
            new(SessionIdClaimType, sessionId)
        };

        if (user.HasCharacteristic(UserCaracteristics.Admin))
            claims.Add(new Claim(ClaimTypes.Role, nameof(UserCaracteristics.Admin)));

        if (user.HasCharacteristic(UserCaracteristics.isAprovador))
            claims.Add(new Claim(ClaimTypes.Role, nameof(UserCaracteristics.isAprovador)));

        return new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
