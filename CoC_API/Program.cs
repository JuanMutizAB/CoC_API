using CoC_API.Context;
using CoC_API.Endpoints;
using CoC_API.Models;
using CoC_API.Security;
using CoC_API.Services;
using CoC_API.Integrations;
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
var instagramOptions = new InstagramPublishingOptions(
    builder.Configuration["COC_API_INSTAGRAM_ACCESS_TOKEN"],
    builder.Configuration["COC_API_INSTAGRAM_USER_ID"],
    builder.Configuration["COC_API_INSTAGRAM_GRAPH_API_VERSION"]);

builder.Services.AddSingleton(instagramOptions);
builder.Services.AddHttpClient<InstagramGraphApiClient>(client =>
{
    client.BaseAddress = new Uri("https://graph.facebook.com/");
});
builder.Services.AddHostedService<InstagramPublishingService>();

var linkedInOptions = new LinkedInPublishingOptions(
    builder.Configuration["LinkedIn:AccessToken"],
    builder.Configuration["LinkedIn:PersonUrn"],
    builder.Configuration["LinkedIn:ApiVersion"] ?? "202611");
builder.Services.AddSingleton(linkedInOptions);
builder.Services.AddHttpClient<LinkedInApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.linkedin.com/");
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    AllowAutoRedirect = false
});
builder.Services.AddHostedService<LinkedInPublishingService>();

var facebookOptions = new FacebookPublishingOptions(
    builder.Configuration["COC_API_FACEBOOK_PAGE_ACCESS_TOKEN"],
    builder.Configuration["COC_API_FACEBOOK_PAGE_ID"],
    builder.Configuration["COC_API_FACEBOOK_GRAPH_API_VERSION"]);
builder.Services.AddSingleton(facebookOptions);
builder.Services.AddHttpClient<FacebookGraphApiClient>(client =>
{
    client.BaseAddress = new Uri("https://graph.facebook.com/");
});
builder.Services.AddHostedService<FacebookPublishingService>();

var emailOptions = new EmailPublishingOptions(
    builder.Configuration["COC_API_EMAIL_SMTP_HOST"],
    builder.Configuration["COC_API_EMAIL_SMTP_PORT"],
    builder.Configuration["COC_API_EMAIL_USERNAME"],
    builder.Configuration["COC_API_EMAIL_PASSWORD"],
    builder.Configuration["COC_API_EMAIL_FROM"],
    builder.Configuration["COC_API_EMAIL_RECIPIENTS"]);
builder.Services.AddSingleton(emailOptions);
builder.Services.AddSingleton<EmailClient>();
builder.Services.AddHostedService<EmailPublishingService>();

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
            if (sessionId is null)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var sessions = context.HttpContext.RequestServices.GetRequiredService<UserSessionStore>();
            var user = await sessions.TryGetActiveUserAsync(sessionId, context.HttpContext.RequestAborted);

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
    app.MapSwaggerUI().AllowAnonymous();
    // app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapCampaignEndpoints();
app.MapUserEndpoints();
app.MapAuthEndpoints();

app.Run();

public partial class Program { }
