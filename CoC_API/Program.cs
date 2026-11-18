using CoC_API.Context;
using CoC_API.Endpoints;
using CoC_API.Models;
using CoC_API.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Regista o DbContext (connection string em appsettings.json -> ConnectionStrings:Default).
builder.Services.AddDbContext<CoC_DbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Permitem gerar a descrição dos endpoints e mostrar o Swagger UI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<UserSessionStore>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "CoC_API.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
        options.Events.OnValidatePrincipal = async context =>
        {
            var sessionId = context.Principal?.FindFirst(UserClaimsPrincipalFactory.SessionIdClaimType)?.Value;
            var sessions = context.HttpContext.RequestServices.GetRequiredService<UserSessionStore>();
            var user = sessionId is null
                ? null
                : await sessions.TryGetActiveUserAsync(sessionId, context.HttpContext.RequestAborted);

            if (user is null)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            context.ReplacePrincipal(UserClaimsPrincipalFactory.Create(user, sessionId));
        };
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var adminEmail = builder.Configuration["COC_API_DEV_ADMIN_EMAIL"];
    var adminPassword = builder.Configuration["COC_API_DEV_ADMIN_PASSWORD"];

    if (adminEmail is not null || adminPassword is not null)
    {
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            throw new InvalidOperationException("Both development admin email and password must be configured.");

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoC_DbContext>();
        var normalizedEmail = adminEmail.Trim().ToUpperInvariant();
        var admin = await db.Users.FirstOrDefaultAsync(
            user => user.email.ToUpper() == normalizedEmail);

        if (admin is null)
        {
            db.Users.Add(new User(
                0,
                "Development Admin",
                adminEmail,
                adminPassword,
                new DateOnly(2000, 1, 1),
                "000000000",
                new List<UserCaracteristics> { UserCaracteristics.Admin }));
            await db.SaveChangesAsync();
        }
        else if (!admin.HasCharacteristic(UserCaracteristics.Admin))
        {
            throw new InvalidOperationException("The configured development admin email belongs to a non-admin user.");
        }
    }
}

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
