using CoC_API.Models;

var builder = WebApplication.CreateBuilder(args);

// Ativa OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var campanhas = new List<Campaign>();
var users = new List<User>();

// --- Campanhas ---
app.MapGet("/campanhas", () => Results.Ok(campanhas));

app.MapPost("/campanhas", (CreateCampaignRequest request) =>
{
    try
    {
        var campanha = new Campaign(
            request.Name,
            request.Description,
            request.Text,
            request.Model,
            request.Status,
            request.PublicationDate,
            request.ImageUrl,
            request.UserId);

        campanhas.Add(campanha);
        return Results.Created($"/campanhas/{campanha.Id}", campanha);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { erro = ex.Message });
    }
});

// --- Utilizadores ---
app.MapGet("/usuarios", () => Results.Ok(users));

app.MapPost("/usuarios", (CreateUserRequest request) =>
{
    try
    {
        var usuario = new User(
            request.id,
            request.name,
            request.email,
            request.password,
            request.birthDate,
            request.taxId,
            request.caracteristics
        );

        users.Add(usuario);
        return Results.Created($"/usuarios/{usuario.name}", usuario);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { erro = ex.Message });
    }
});

app.Run();

// Records DTO
public record CreateCampaignRequest(
    string Name,
    string Description,
    string Text,
    CampaignModel Model,
    CampaignStatus Status,
    DateTimeOffset? PublicationDate,
    string? ImageUrl,
    int UserId
);

public record CreateUserRequest(
    int id,
    string name,
    string email,
    string password,
    DateTime birthDate,
    string taxId,
    List<UserCaracteristics>? caracteristics
);