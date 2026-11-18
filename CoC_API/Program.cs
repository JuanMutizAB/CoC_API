using CoC_API.Models;
using CoC_API.Endpoints;
using CoC_API.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var users = new List<User>();
var campaigns = new List<Campaign>();
var sessions = new InMemoryUserSessionStore(users);
var builder = WebApplication.CreateBuilder(args);

// Permitem gerar a descrição dos endpoints e mostrar o Swagger UI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(sessions);

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
        options.Events.OnValidatePrincipal = context =>
        {
            var sessionId = context.Principal?.FindFirst(UserClaimsPrincipalFactory.SessionIdClaimType)?.Value;
            if (sessionId is null || !sessions.TryGetActiveUser(sessionId, out var user) || user is null)
            {
                context.RejectPrincipal();
                return context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            context.ReplacePrincipal(UserClaimsPrincipalFactory.Create(user, sessionId));
            return Task.CompletedTask;
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

        users.Add(new User(
            1,
            "Development Admin",
            adminEmail,
            adminPassword,
            new DateOnly(2000, 1, 1),
            "000000000",
            new List<UserCaracteristics> { UserCaracteristics.Admin }));
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapCampaignEndpoints(campaigns, users);
app.MapUserEndpoints(users);
app.MapAuthEndpoints(users, sessions);

app.Run();

