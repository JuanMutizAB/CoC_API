using System.Net;
using System.Net.Http.Json;
using CoC_API.Context;
using CoC_API.Integrations;
using CoC_API.Models;
using CoC_API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;

namespace CoC_API.Tests;

public sealed class CampaignWorkflowTests : IAsyncLifetime
{
    private readonly TestApiFactory factory = new();

    public async Task InitializeAsync() => await factory.InitializeDatabaseAsync();

    public async Task DisposeAsync() => await factory.DisposeAsync();

    [Fact]
    public async Task RequestChangesResubmissionAndApprovalPreserveReviewFlow()
    {
        using var owner = await LoginAsync("owner@example.com", "OwnerPass123!");
        using var reviewer = await LoginAsync("reviewer@example.com", "ReviewerPass123!");
        var campaign = await CreateCampaignAsync(owner, CampaignType.Email, UserIds.ApprovalOwner);

        Assert.Equal(CampaignStatus.InReview, campaign.Status);

        var requestedChanges = await reviewer.PutAsJsonAsync(
            $"/campaigns/{campaign.Id}/review/request-changes",
            new { justificationText = "Please correct the target audience." });
        Assert.Equal(HttpStatusCode.OK, requestedChanges.StatusCode);
        Assert.Equal(
            CampaignStatus.ForChange,
            (await ReadCampaignAsync(requestedChanges)).Status);

        var edit = await owner.PutAsJsonAsync(
            $"/campaigns/{campaign.Id}/edit",
            new
            {
                name = "Revised campaign",
                description = "Updated after review",
                modelMsg = "Updated message",
                type = CampaignType.Email,
                publicationDate = DateTimeOffset.UtcNow.AddMinutes(20),
                imageUrl = (string?)null
            });
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        Assert.Equal(CampaignStatus.InReview, (await ReadCampaignAsync(edit)).Status);

        var approval = await reviewer.PutAsync($"/campaigns/{campaign.Id}/review/approve", null);
        Assert.Equal(HttpStatusCode.OK, approval.StatusCode);
        Assert.Equal(CampaignStatus.Scheduled, (await ReadCampaignAsync(approval)).Status);
    }

    [Fact]
    public async Task RejectionPersistsReasonAndJustification()
    {
        using var owner = await LoginAsync("owner@example.com", "OwnerPass123!");
        using var reviewer = await LoginAsync("reviewer@example.com", "ReviewerPass123!");
        var campaign = await CreateCampaignAsync(owner, CampaignType.Email, UserIds.ApprovalOwner);

        var rejection = await reviewer.PutAsJsonAsync(
            $"/campaigns/{campaign.Id}/review/reject",
            new
            {
                motivo = Motivo.InadequateContent,
                justificationText = "The content does not meet the campaign requirements."
            });

        Assert.Equal(HttpStatusCode.OK, rejection.StatusCode);
        Assert.Equal(CampaignStatus.Canceled, (await ReadCampaignAsync(rejection)).Status);

        var reason = await owner.GetFromJsonAsync<ReasonResponse>(
            $"/campaigns/{campaign.Id}/motivo");
        Assert.NotNull(reason);
        Assert.Equal(Motivo.InadequateContent, reason.Motivo);
        Assert.Equal("The content does not meet the campaign requirements.", reason.JustificationText);
    }

    [Fact]
    public async Task OwnerCanCancelWithJustificationAndCannotCancelAgain()
    {
        using var owner = await LoginAsync("standard@example.com", "StandardPass123!");
        var campaign = await CreateCampaignAsync(owner, CampaignType.Email, UserIds.StandardOwner);

        var cancellation = await owner.PutAsJsonAsync(
            $"/campaigns/{campaign.Id}/cancel",
            new
            {
                motivo = Motivo.Other,
                justificationText = "The launch was postponed."
            });

        Assert.Equal(HttpStatusCode.OK, cancellation.StatusCode);
        Assert.Equal(CampaignStatus.Canceled, (await ReadCampaignAsync(cancellation)).Status);

        var secondCancellation = await owner.PutAsJsonAsync(
            $"/campaigns/{campaign.Id}/cancel",
            new
            {
                motivo = Motivo.Other,
                justificationText = "Trying to cancel twice."
            });
        Assert.Equal(HttpStatusCode.BadRequest, secondCancellation.StatusCode);

        var reason = await owner.GetFromJsonAsync<ReasonResponse>(
            $"/campaigns/{campaign.Id}/motivo");
        Assert.NotNull(reason);
        Assert.Equal(Motivo.Other, reason.Motivo);
        Assert.Equal("The launch was postponed.", reason.JustificationText);
    }

    [Fact]
    public async Task CampaignIsNotVisibleToAnotherRegularUser()
    {
        using var owner = await LoginAsync("standard@example.com", "StandardPass123!");
        using var otherUser = await LoginAsync("other@example.com", "OtherPass123!");
        var campaign = await CreateCampaignAsync(owner, CampaignType.Email, UserIds.StandardOwner);

        var response = await otherUser.GetAsync($"/campaigns/{campaign.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task InstagramClientCreatesWaitsAndPublishesWithoutCallingMeta()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"id":"container-123"}"""),
            JsonResponse(HttpStatusCode.OK, """{"status_code":"FINISHED"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":"media-456"}"""));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://graph.facebook.com/")
        };
        var client = new InstagramGraphApiClient(
            httpClient,
            new InstagramPublishingOptions("test-token", "account-789", "v22.0"));
        var campaign = new Campaign(
            "Instagram campaign",
            "Test only",
            "A test caption",
            CampaignType.Instagram,
            CampaignStatus.Scheduled,
            DateTimeOffset.UtcNow.AddMinutes(5),
            "https://images.example.test/campaign.jpg",
            UserIds.StandardOwner,
            DateTimeOffset.UtcNow);

        var mediaId = await client.PublishImageAsync(campaign, CancellationToken.None);

        Assert.Equal("media-456", mediaId);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Contains("account-789/media", handler.Requests[0].Uri);
        Assert.Contains("caption=A+test+caption", handler.Requests[0].Body);
        Assert.Equal("test-token", handler.Requests[0].BearerToken);
        Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
        Assert.Equal(HttpMethod.Post, handler.Requests[2].Method);
        Assert.Contains("creation_id=container-123", handler.Requests[2].Body);
    }

    [Fact]
    public async Task PublishingWorkerClosesCampaignAndStoresInstagramMediaId()
    {
        var campaign = await CreateDueInstagramCampaignAsync();
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"id":"container-123"}"""),
            JsonResponse(HttpStatusCode.OK, """{"status_code":"FINISHED"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":"media-456"}"""));
        await RunPublishingWorkerOnceAsync(handler);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoC_DbContext>();
        var storedCampaign = await db.Campaigns.AsNoTracking().SingleAsync(item => item.Id == campaign.Id);

        Assert.Equal(CampaignStatus.Closed, storedCampaign.Status);
        Assert.Equal("media-456", storedCampaign.InstagramMediaId);
    }

    [Fact]
    public async Task PublishingWorkerMarksCampaignFailedWhenMetaReturnsAnError()
    {
        var campaign = await CreateDueInstagramCampaignAsync();
        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.BadRequest, """{"error":{"message":"Invalid image URL"}}"""));
        await RunPublishingWorkerOnceAsync(handler);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoC_DbContext>();
        var storedCampaign = await db.Campaigns.AsNoTracking().SingleAsync(item => item.Id == campaign.Id);

        Assert.Equal(CampaignStatus.Failed, storedCampaign.Status);
        Assert.Null(storedCampaign.InstagramMediaId);
    }

    private async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });
        var response = await client.PostAsJsonAsync("/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    private async Task<CampaignResponse> CreateCampaignAsync(
        HttpClient client,
        CampaignType type,
        int ownerId)
    {
        var response = await client.PostAsJsonAsync(
            "/campaigns",
            new
            {
                name = "Workflow test campaign",
                description = "Campaign for workflow tests",
                modelMsg = "Test message",
                type,
                publicationDate = DateTimeOffset.UtcNow.AddMinutes(20),
                imageUrl = type == CampaignType.Instagram
                    ? "https://images.example.test/campaign.jpg"
                    : null
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await ReadCampaignAsync(response);
        Assert.Equal(ownerId, result.UserId);
        return result;
    }

    private async Task<CampaignResponse> CreateDueInstagramCampaignAsync()
    {
        using var owner = await LoginAsync("standard@example.com", "StandardPass123!");
        var campaign = await CreateCampaignAsync(owner, CampaignType.Instagram, UserIds.StandardOwner);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoC_DbContext>();
        await db.Campaigns
            .Where(item => item.Id == campaign.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                item => item.PublicationDate,
                DateTimeOffset.UtcNow.AddMinutes(-1)));

        return campaign;
    }

    private async Task RunPublishingWorkerOnceAsync(QueueHttpMessageHandler handler)
    {
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://graph.facebook.com/")
        };
        var instagram = new InstagramGraphApiClient(
            httpClient,
            new InstagramPublishingOptions("test-token", "account-789", "v22.0"));
        var service = new InstagramPublishingService(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            instagram,
            new InstagramPublishingOptions("test-token", "account-789", "v22.0"),
            factory.Services.GetRequiredService<ILogger<InstagramPublishingService>>());

        await service.PublishDueCampaignsAsync(CancellationToken.None);
    }

    private static async Task<CampaignResponse> ReadCampaignAsync(HttpResponseMessage response)
    {
        var result = await response.Content.ReadFromJsonAsync<CampaignResponse>();
        Assert.NotNull(result);
        return result;
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string body) =>
        new(status)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };

    private sealed record CampaignResponse(Guid Id, CampaignStatus Status, int UserId);

    private sealed record ReasonResponse(string? JustificationText, Motivo? Motivo);

    private static class UserIds
    {
        public const int ApprovalOwner = 1;
        public const int Reviewer = 2;
        public const int StandardOwner = 3;
        public const int OtherUser = 4;
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<CoC_DbContext>();
                services.RemoveAll<DbContextOptions<CoC_DbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<CoC_DbContext>>();
                services.AddDbContext<CoC_DbContext>(options =>
                    options.UseSqlite(connection));
                services.RemoveAll<CoC_DbContext>();
                services.AddScoped<CoC_DbContext>(provider =>
                    new TestCoC_DbContext(
                        provider.GetRequiredService<DbContextOptions<CoC_DbContext>>()));
            });
        }

        public async Task InitializeDatabaseAsync()
        {
            await connection.OpenAsync();
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoC_DbContext>();
            await db.Database.EnsureCreatedAsync();
            db.Users.AddRange(
                new User(
                    UserIds.ApprovalOwner,
                    "Approval Owner",
                    "owner@example.com",
                    "OwnerPass123!",
                    new DateOnly(1990, 1, 1),
                    "123456789",
                    [UserCaracteristics.isAprovando]),
                new User(
                    UserIds.Reviewer,
                    "Review User",
                    "reviewer@example.com",
                    "ReviewerPass123!",
                    new DateOnly(1990, 1, 1),
                    "234567890",
                    [UserCaracteristics.isAprovador]),
                new User(
                    UserIds.StandardOwner,
                    "Standard Owner",
                    "standard@example.com",
                    "StandardPass123!",
                    new DateOnly(1990, 1, 1),
                    "345678901",
                    []),
                new User(
                    UserIds.OtherUser,
                    "Other User",
                    "other@example.com",
                    "OtherPass123!",
                    new DateOnly(1990, 1, 1),
                    "456789012",
                    []));
            await db.SaveChangesAsync();
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            await connection.DisposeAsync();
        }

        private sealed class TestCoC_DbContext(DbContextOptions<CoC_DbContext> options)
            : CoC_DbContext(options)
        {
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                var converter = new DateTimeOffsetToBinaryConverter();
                foreach (var property in modelBuilder.Model.GetEntityTypes()
                             .SelectMany(entity => entity.GetProperties())
                             .Where(property => property.ClrType == typeof(DateTimeOffset)))
                {
                    property.SetValueConverter(converter);
                }
            }
        }
    }

    private sealed class QueueHttpMessageHandler(params HttpResponseMessage[] responses)
        : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> responses = new(responses);
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(
                request.Method,
                request.RequestUri!.ToString(),
                body,
                request.Headers.Authorization?.Parameter));

            return responses.Dequeue();
        }
    }

    private sealed record CapturedRequest(
        HttpMethod Method,
        string Uri,
        string Body,
        string? BearerToken);
}
