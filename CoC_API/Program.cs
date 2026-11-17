using CoC_API.Models;
using CoC_API.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Permitem gerar a descrição dos endpoints e mostrar o Swagger UI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Ativa OpenAPI (https://swagger.io/specification/) para gerar a documentação da API por agora então não é necessário,
// mas futuramente será útil para gerar documentação da API e permitir que outros sistemas consumam a API de forma mais fácil.
// builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // app.MapOpenApi();
}

var campaigns = new List<Campaign>();
var users = new List<User>();

app.MapCampaignEndpoints(campaigns, users);
app.MapUserEndpoints(users);
app.MapAuthEndpoints(users);

app.Run();

