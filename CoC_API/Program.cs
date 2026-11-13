var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Lista temporária de campanhas
var campanhas = new List<Campanha>();

// GET /campanhas: consulta as campanhas
app.MapGet("/campanhas", () =>
{
    return Results.Ok(campanhas);
});

// POST /campanhas: recebe e cadastra uma campanha
app.MapPost("/campanhas", (Campanha campanha) =>
{
    if (string.IsNullOrWhiteSpace(campanha.Nome))
    {
        return Results.BadRequest("O nome é obrigatório.");
    }

    campanhas.Add(campanha);
    return Results.Ok(campanha);
});

app.Run();

public record Campanha(string Nome, string Mensagem);