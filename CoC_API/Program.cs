using CoC_API.Models;

var builder = WebApplication.CreateBuilder(args);

// Permitem gerar a descrição dos endpoints e mostrar o Swagger UI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Ativa OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var campaigns = new List<Campaign>();
var users = new List<User>();

// --- Campanhas ---
app.MapGet("/campaigns", () => Results.Ok(campaigns));

app.MapPost("/campaigns", (CreateCampaignRequest request) =>
{
    try
    {
        var campaign = new Campaign(
            request.Name,
            request.Description,
            request.Text,
            request.Model,
            request.Status,
            request.PublicationDate,
            request.ImageUrl,
            request.UserId);

        campaigns.Add(campaign);
        return Results.Created($"/campaigns/{campaign.Id}", campaign);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { erro = ex.Message });
    }
});

// --- Utilizadores ---
app.MapGet("/users", () => Results.Ok(users));

app.MapPost("/users", (CreateUserRequest request) =>
{
    try
    {
        var user = new User(
            request.id,
            request.name,
            request.email,
            request.password,
            request.birthDate,
            request.taxId,
            request.caracteristics
        );

        users.Add(user);
        return Results.Created($"/users/{user.name}", user);
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
