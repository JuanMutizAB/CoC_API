using CoC_API.Context;
using CoC_API.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Regista o DbContext (connection string em appsettings.json -> ConnectionStrings:Default).
builder.Services.AddDbContext<CoC_DbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

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

app.MapCampaignEndpoints();
app.MapUserEndpoints();
app.MapAuthEndpoints();

app.Run();
