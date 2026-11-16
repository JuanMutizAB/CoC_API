using CoC_API.DTOs.Request;
using CoC_API.Models;

namespace CoC_API.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app, List<User> users)
    {
        app.MapGet("/users", () => Results.Ok(users));

        app.MapGet("/users/{id:int}", (int id) =>
        {
            var user = users.FirstOrDefault(user => user.id == id);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        app.MapPut("/users/{id:int}", (int id, EditUserRequest request) =>
        {
            var user = users.FirstOrDefault(user => user.id == id);
            if (user is null)
            {
                return Results.NotFound();
            }

            try
            {
                user.UpdateUser(request.Name, request.Email, request.Password, request.caracteristics);
                return Results.NoContent();
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        app.MapPost("/users", (CreateUserRequest request) =>
        {
            if (users.Any(user => user.id == request.Id))
            {
                return Results.Conflict(new { erro = "A user with this ID already exists." });
            }

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
                return Results.Created($"/users/{user.id}", user);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });
    }
}
