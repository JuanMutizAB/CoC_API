using CoC_API.DTOs.Request;
using CoC_API.Models;

namespace CoC_API.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app, List<User> users)
    {
        app.MapGet("/users", () => Results.Ok(users));

        app.MapPost("/users", (CreateUserRequest request) =>
        {
            try
            {
                var user = new User(
                    request.Id,
                    request.Name,
                    request.Email,
                    request.Password,
                    request.BirthDate,
                    request.TaxId,
                    request.Caracteristics
                );

                users.Add(user);
                return Results.Created($"/users/{user.Name}", user);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });
    }
}
