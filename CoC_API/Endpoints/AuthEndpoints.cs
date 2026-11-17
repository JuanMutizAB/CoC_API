using CoC_API.DTOs.Request;
using CoC_API.Models;

namespace CoC_API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app, List<User> users)
    {
        app.MapPost("/login", (LoginRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { erro = "Email and password are required." });
            }

            var user = users.FirstOrDefault(user => user.MatchesEmail(request.Email));

            // Same message for unknown user, wrong password and inactive user,
            // so the response does not reveal which accounts exist.
            if (user is null
                || !user.CheckPassword(request.Password)
                || !user.HasCharacteristic(UserCaracteristics.isActive))
            {
                return Results.Json(new { erro = "Invalid credentials." }, statusCode: StatusCodes.Status401Unauthorized);
            }

            return Results.Ok(new { id = user.id, name = user.name });
        });
    }
}
