using System.Net;
using System.Net.Http.Json;
using System.Text;
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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

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
                types = new[] { CampaignType.Email },
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
            new[] { CampaignType.Instagram },
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
        Assert.Contains("caption=Instagram+campaign%0AA+test+caption", handler.Requests[0].Body);
        Assert.Equal("test-token", handler.Requests[0].BearerToken);
        Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
        Assert.Equal(HttpMethod.Post, handler.Requests[2].Method);
        Assert.Contains("creation_id=container-123", handler.Requests[2].Body);
    }

    [Fact]
    public async Task LinkedInClientPublishesMemberTextPostAndReturnsPostId()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Created);
        response.Headers.Add("x-restli-id", "urn:li:share:post-123");
        var handler = new QueueHttpMessageHandler(response);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.linkedin.com/")
        };
        var client = new LinkedInApiClient(
            httpClient,
            new LinkedInPublishingOptions(
                "\r\ntest-token\r\n",
                "urn:li:person:member-123",
                "202611"));
        var campaign = new Campaign(
            "LinkedIn campaign",
            "Test only",
            "A test post",
            new[] { CampaignType.LinkedIn },
            CampaignStatus.Scheduled,
            DateTimeOffset.UtcNow.AddMinutes(5),
            null,
            UserIds.StandardOwner,
            DateTimeOffset.UtcNow);

        var postId = await client.PublishPostAsync(campaign, CancellationToken.None);

        Assert.Equal("urn:li:share:post-123", postId);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.linkedin.com/rest/posts", request.Uri);
        Assert.Equal("test-token", request.BearerToken);
        Assert.Equal("202611", request.LinkedInVersion);
        Assert.Equal("2.0.0", request.RestliProtocolVersion);
        Assert.Contains("\"author\":\"urn:li:person:member-123\"", request.Body);
        Assert.Contains("\"commentary\":\"LinkedIn campaign\\nA test post\"", request.Body);
    }

    [Fact]
    public async Task LinkedInClientRejectsTokensWithEmbeddedNewLines()
    {
        var handler = new QueueHttpMessageHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.linkedin.com/")
        };
        var client = new LinkedInApiClient(
            httpClient,
            new LinkedInPublishingOptions(
                "test-\ntoken",
                "urn:li:person:member-123",
                "202611"));
        var campaign = new Campaign(
            "LinkedIn campaign",
            "Test only",
            "A test post",
            new[] { CampaignType.LinkedIn },
            CampaignStatus.Scheduled,
            DateTimeOffset.UtcNow.AddMinutes(5),
            null,
            UserIds.StandardOwner,
            DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.PublishPostAsync(campaign, CancellationToken.None));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task LinkedInClientUploadsCampaignImageBeforePublishingPost()
    {
        var imageResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("image-bytes"))
        };
        imageResponse.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        var initializeResponse = JsonResponse(
            HttpStatusCode.OK,
            """{"value":{"uploadUrl":"https://www.linkedin.com/dms-uploads/upload-123","image":"urn:li:image:image-123"}}""");
        var uploadResponse = new HttpResponseMessage(HttpStatusCode.Created);
        var postResponse = new HttpResponseMessage(HttpStatusCode.Created);
        postResponse.Headers.Add("x-restli-id", "urn:li:share:post-image-123");
        var handler = new QueueHttpMessageHandler(
            imageResponse,
            initializeResponse,
            uploadResponse,
            postResponse);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.linkedin.com/")
        };
        var client = new LinkedInApiClient(
            httpClient,
            new LinkedInPublishingOptions(
                "test-token",
                "urn:li:person:member-123",
                "202611"));
        var campaign = new Campaign(
            "LinkedIn campaign",
            "Test only",
            "A test post",
            new[] { CampaignType.LinkedIn },
            CampaignStatus.Scheduled,
            DateTimeOffset.UtcNow.AddMinutes(5),
            "https://8.8.8.8/campaign.png",
            UserIds.StandardOwner,
            DateTimeOffset.UtcNow);

        var postId = await client.PublishPostAsync(campaign, CancellationToken.None);

        Assert.Equal("urn:li:share:post-image-123", postId);
        Assert.Equal(4, handler.Requests.Count);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Null(handler.Requests[0].BearerToken);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Contains("initializeUpload", handler.Requests[1].Uri);
        Assert.Equal(HttpMethod.Put, handler.Requests[2].Method);
        Assert.Equal("https://www.linkedin.com/dms-uploads/upload-123", handler.Requests[2].Uri);
        Assert.Equal("test-token", handler.Requests[2].BearerToken);
        Assert.Contains("\"id\":\"urn:li:image:image-123\"", handler.Requests[3].Body);
    }

    [Fact]
    public async Task LinkedInClientRejectsPrivateCampaignImageUrls()
    {
        using var httpClient = new HttpClient(new QueueHttpMessageHandler())
        {
            BaseAddress = new Uri("https://api.linkedin.com/")
        };
        var client = new LinkedInApiClient(
            httpClient,
            new LinkedInPublishingOptions(
                "test-token",
                "urn:li:person:member-123",
                "202611"));
        var campaign = new Campaign(
            "LinkedIn campaign",
            "Test only",
            "A test post",
            new[] { CampaignType.LinkedIn },
            CampaignStatus.Scheduled,
            DateTimeOffset.UtcNow.AddMinutes(5),
            "https://127.0.0.1/campaign.png",
            UserIds.StandardOwner,
            DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.PublishPostAsync(campaign, CancellationToken.None));
    }

    [Fact]
    public async Task LinkedInPublishingWorkerClosesCampaignAndStoresPostId()
    {
        var campaign = await CreateDueLinkedInCampaignAsync();
        var imageResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("image-bytes"))
        };
        imageResponse.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        var postResponse = new HttpResponseMessage(HttpStatusCode.Created);
        postResponse.Headers.Add("x-restli-id", "urn:li:share:post-456");
        await RunLinkedInPublishingWorkerOnceAsync(new QueueHttpMessageHandler(
            imageResponse,
            JsonResponse(
                HttpStatusCode.OK,
                """{"value":{"uploadUrl":"https://www.linkedin.com/dms-uploads/upload-456","image":"urn:li:image:image-456"}}"""),
            new HttpResponseMessage(HttpStatusCode.Created),
            postResponse));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoC_DbContext>();
        var storedCampaign = await db.Campaigns.AsNoTracking().SingleAsync(item => item.Id == campaign.Id);

        Assert.Equal(CampaignStatus.Closed, storedCampaign.Status);
        Assert.Equal("urn:li:share:post-456", storedCampaign.LinkedInPostId);
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

    [Fact]
    public async Task MeEndpointReturnsAuthenticatedUserAndRejectsAnonymous()
    {
        using var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        var anonResponse = await anonymous.GetAsync("/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonResponse.StatusCode);

        using var reviewer = await LoginAsync("reviewer@example.com", "ReviewerPass123!");
        var meResponse = await reviewer.GetAsync("/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var me = await meResponse.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(me);
        Assert.Equal(UserIds.Reviewer, me.Id);
        Assert.Equal("reviewer@example.com", me.Email);
        Assert.Contains(UserCaracteristics.isAprovador, me.Caracteristics);
        Assert.Contains(UserCaracteristics.isActive, me.Caracteristics);
    }

    [Fact]
    public async Task AdminCanDeactivateUserAndDeactivatedUserCannotAuthenticate()
    {
        using var admin = await LoginAsync("admin@example.com", "AdminPass123!");
        using var other = await LoginAsync("other@example.com", "OtherPass123!");

        var selfDeactivate = await admin.PutAsync($"/users/{UserIds.Admin}/deactivate", null);
        Assert.Equal(HttpStatusCode.BadRequest, selfDeactivate.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/me")).StatusCode);

        var deactivate = await admin.PutAsync($"/users/{UserIds.OtherUser}/deactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);

        var revokedSessionResponse = await other.GetAsync("/me");
        Assert.Equal(HttpStatusCode.Unauthorized, revokedSessionResponse.StatusCode);

        using var freshClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        var relogin = await freshClient.PostAsJsonAsync(
            "/login",
            new { email = "other@example.com", password = "OtherPass123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, relogin.StatusCode);
    }

    [Fact]
    public async Task AdminCanCreateAprovandoUserWhoCanLoginAndAdminCanEditAndDeactivate()
    {
        using var admin = await LoginAsync("admin@example.com", "AdminPass123!");

        var createResponse = await admin.PostAsJsonAsync(
            "/users",
            new
            {
                name = "Maria Silva",
                email = "aprovando.novo@example.com",
                password = "NewUser123!",
                birthDate = "1995-05-10",
                taxId = "123456789",
                caracteristics = new[] { "isActive", "isAprovando" }
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Contains(UserCaracteristics.isActive, created.Caracteristics);
        Assert.Contains(UserCaracteristics.isAprovando, created.Caracteristics);

        using var newUserClient = await LoginAsync("aprovando.novo@example.com", "NewUser123!");
        var meResponse = await newUserClient.GetAsync("/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var campaignsResponse = await newUserClient.GetAsync("/campaigns");
        Assert.Equal(HttpStatusCode.OK, campaignsResponse.StatusCode);

        var editResponse = await admin.PutAsJsonAsync(
            $"/users/{created.Id}",
            new
            {
                name = "Maria Silva Atualizada",
                email = "aprovando.novo@example.com",
                password = "UpdatedPass123!",
                caracteristics = new[] { "isAprovando" }
            });
        Assert.Equal(HttpStatusCode.NoContent, editResponse.StatusCode);

        using var oldPasswordClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        var oldPassLogin = await oldPasswordClient.PostAsJsonAsync(
            "/login",
            new { email = "aprovando.novo@example.com", password = "NewUser123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassLogin.StatusCode);

        using var updatedClient = await LoginAsync("aprovando.novo@example.com", "UpdatedPass123!");

        var deactivateResponse = await admin.PutAsync($"/users/{created.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        using var deactivatedClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        var deactivatedLogin = await deactivatedClient.PostAsJsonAsync(
            "/login",
            new { email = "aprovando.novo@example.com", password = "UpdatedPass123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, deactivatedLogin.StatusCode);
    }

    [Fact]
    public async Task AdminAddingAprovadorRoleToSelfUpdatesMeAndEnablesApprovalFlow()
    {
        using var owner = await LoginAsync("owner@example.com", "OwnerPass123!");
        using var admin = await LoginAsync("admin@example.com", "AdminPass123!");
        var campaign = await CreateCampaignAsync(owner, CampaignType.Email, UserIds.ApprovalOwner);

        var preUpdateApprove = await admin.PutAsync($"/campaigns/{campaign.Id}/review/approve", null);
        Assert.Equal(HttpStatusCode.Forbidden, preUpdateApprove.StatusCode);

        var editSelf = await admin.PutAsJsonAsync(
            $"/users/{UserIds.Admin}",
            new { caracteristics = new[] { "Admin", "isAprovador" } });
        Assert.Equal(HttpStatusCode.NoContent, editSelf.StatusCode);

        var meResponse = await admin.GetAsync("/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var me = await meResponse.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(me);
        Assert.Contains(UserCaracteristics.Admin, me.Caracteristics);
        Assert.Contains(UserCaracteristics.isAprovador, me.Caracteristics);
        Assert.Contains(UserCaracteristics.isActive, me.Caracteristics);

        var postUpdateApprove = await admin.PutAsync($"/campaigns/{campaign.Id}/review/approve", null);
        Assert.Equal(HttpStatusCode.OK, postUpdateApprove.StatusCode);
        Assert.Equal(CampaignStatus.Scheduled, (await ReadCampaignAsync(postUpdateApprove)).Status);
    }

    [Fact]
    public async Task MultiTypeCampaignPersistsAllTypesAndPublishesToInstagram()
    {
        using var owner = await LoginAsync("standard@example.com", "StandardPass123!");
        var campaign = await CreateCampaignAsync(
            owner,
            UserIds.StandardOwner,
            CampaignType.Email,
            CampaignType.Instagram);

        Assert.Equal([CampaignType.Email, CampaignType.Instagram], campaign.Types);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoC_DbContext>();
            await db.Campaigns
                .Where(item => item.Id == campaign.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    item => item.PublicationDate,
                    DateTimeOffset.UtcNow.AddMinutes(-1)));
        }

        var handler = new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"id":"container-123"}"""),
            JsonResponse(HttpStatusCode.OK, """{"status_code":"FINISHED"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":"media-456"}"""));
        await RunPublishingWorkerOnceAsync(handler);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CoC_DbContext>();
        var stored = await verifyDb.Campaigns.AsNoTracking().SingleAsync(item => item.Id == campaign.Id);
        Assert.Equal(CampaignStatus.Closed, stored.Status);
        Assert.Equal("media-456", stored.InstagramMediaId);
    }

    [Fact]
    public async Task RequestChangesPersistsMotivoAndResubmissionClearsIt()
    {
        using var owner = await LoginAsync("owner@example.com", "OwnerPass123!");
        using var reviewer = await LoginAsync("reviewer@example.com", "ReviewerPass123!");
        var campaign = await CreateCampaignAsync(owner, CampaignType.Email, UserIds.ApprovalOwner);

        var requestedChanges = await reviewer.PutAsJsonAsync(
            $"/campaigns/{campaign.Id}/review/request-changes",
            new
            {
                motivo = Motivo.InadequateLanguage,
                justificationText = "Tone is too informal."
            });
        Assert.Equal(HttpStatusCode.OK, requestedChanges.StatusCode);

        var updated = await ReadCampaignAsync(requestedChanges);
        Assert.Equal(CampaignStatus.ForChange, updated.Status);
        Assert.Equal(Motivo.InadequateLanguage, updated.Motivo);
        Assert.Equal("Tone is too informal.", updated.JustificationText);

        var resubmit = await owner.PutAsJsonAsync(
            $"/campaigns/{campaign.Id}/edit",
            new
            {
                name = "Formal campaign",
                description = "Updated after feedback",
                modelMsg = "Formal message",
                types = new[] { CampaignType.Email },
                publicationDate = DateTimeOffset.UtcNow.AddMinutes(20)
            });
        Assert.Equal(HttpStatusCode.OK, resubmit.StatusCode);

        var resubmitted = await ReadCampaignAsync(resubmit);
        Assert.Equal(CampaignStatus.InReview, resubmitted.Status);
        Assert.Equal(Motivo.None, resubmitted.Motivo);
        Assert.Equal(string.Empty, resubmitted.JustificationText);
    }

    [Fact]
    public async Task ForChangeEditByAdminOrAprovadorMovesToScheduledOrSendingAndOnlyAdminCanCancel()
    {
        using var owner = await LoginAsync("owner@example.com", "OwnerPass123!");
        using var reviewer = await LoginAsync("reviewer@example.com", "ReviewerPass123!");
        using var admin = await LoginAsync("admin@example.com", "AdminPass123!");

        // 1. Admin editing a ForChange campaign with a future date moves it directly to Scheduled.
        var scheduledCampaign = await CreateCampaignAsync(owner, CampaignType.Email, UserIds.ApprovalOwner);
        var req1 = await reviewer.PutAsJsonAsync(
            $"/campaigns/{scheduledCampaign.Id}/review/request-changes",
            new { motivo = Motivo.InadequateContent, justificationText = "Missing compliance text." });
        Assert.Equal(HttpStatusCode.OK, req1.StatusCode);

        var ownerCancel = await owner.PutAsJsonAsync(
            $"/campaigns/{scheduledCampaign.Id}/cancel",
            new { motivo = Motivo.Other, justificationText = "Owner trying to cancel ForChange." });
        Assert.Equal(HttpStatusCode.Forbidden, ownerCancel.StatusCode);

        var adminEdit = await admin.PutAsJsonAsync(
            $"/campaigns/{scheduledCampaign.Id}/edit",
            new { name = "Admin adjusted", publicationDate = DateTimeOffset.UtcNow.AddMinutes(30) });
        Assert.Equal(HttpStatusCode.OK, adminEdit.StatusCode);
        var adminEdited = await ReadCampaignAsync(adminEdit);
        Assert.Equal(CampaignStatus.Scheduled, adminEdited.Status);
        Assert.Equal(Motivo.None, adminEdited.Motivo);
        Assert.Equal(string.Empty, adminEdited.JustificationText);

        // 2. Aprovador editing a ForChange campaign with current minute moves it directly to Sending.
        var sendingCampaign = await CreateCampaignAsync(owner, CampaignType.Email, UserIds.ApprovalOwner);
        var req2 = await reviewer.PutAsJsonAsync(
            $"/campaigns/{sendingCampaign.Id}/review/request-changes",
            new { motivo = Motivo.InadequateLanguage, justificationText = "Adjust phrasing." });
        Assert.Equal(HttpStatusCode.OK, req2.StatusCode);

        var current = DateTimeOffset.UtcNow;
        if (current.Second >= 55)
            await Task.Delay((60 - current.Second) * 1000 + 200);

        var reviewerEdit = await reviewer.PutAsJsonAsync(
            $"/campaigns/{sendingCampaign.Id}/edit",
            new { name = "Reviewer immediate publish", publicationDate = DateTimeOffset.UtcNow });
        Assert.Equal(HttpStatusCode.OK, reviewerEdit.StatusCode);
        var reviewerEdited = await ReadCampaignAsync(reviewerEdit);
        Assert.Equal(CampaignStatus.Sending, reviewerEdited.Status);
        Assert.Equal(Motivo.None, reviewerEdited.Motivo);
        Assert.Equal(string.Empty, reviewerEdited.JustificationText);

        // 3. Admin can cancel a campaign in ForChange.
        var cancelCampaign = await CreateCampaignAsync(owner, CampaignType.Email, UserIds.ApprovalOwner);
        var req3 = await reviewer.PutAsJsonAsync(
            $"/campaigns/{cancelCampaign.Id}/review/request-changes",
            new { motivo = Motivo.InadequateImage, justificationText = "Replace image." });
        Assert.Equal(HttpStatusCode.OK, req3.StatusCode);

        var adminCancel = await admin.PutAsJsonAsync(
            $"/campaigns/{cancelCampaign.Id}/cancel",
            new { motivo = Motivo.Other, justificationText = "Canceled by Admin." });
        Assert.Equal(HttpStatusCode.OK, adminCancel.StatusCode);
        Assert.Equal(CampaignStatus.Canceled, (await ReadCampaignAsync(adminCancel)).Status);
    }

    [Fact]
    public async Task InReviewEditByAdminOrAprovadorMovesDirectlyToScheduledOrSending()
    {
        using var owner = await LoginAsync("owner@example.com", "OwnerPass123!");
        using var reviewer = await LoginAsync("reviewer@example.com", "ReviewerPass123!");
        using var admin = await LoginAsync("admin@example.com", "AdminPass123!");

        var campaignForOwnerAndAdmin = await CreateCampaignAsync(owner, CampaignType.Email, UserIds.ApprovalOwner);

        var ownerEdit = await owner.PutAsJsonAsync(
            $"/campaigns/{campaignForOwnerAndAdmin.Id}/edit",
            new { name = "Owner edit stays in review" });
        Assert.Equal(HttpStatusCode.OK, ownerEdit.StatusCode);
        Assert.Equal(CampaignStatus.InReview, (await ReadCampaignAsync(ownerEdit)).Status);

        var adminEdit = await admin.PutAsJsonAsync(
            $"/campaigns/{campaignForOwnerAndAdmin.Id}/edit",
            new { name = "Admin edit scheduled", publicationDate = DateTimeOffset.UtcNow.AddMinutes(30) });
        Assert.Equal(HttpStatusCode.OK, adminEdit.StatusCode);
        Assert.Equal(CampaignStatus.Scheduled, (await ReadCampaignAsync(adminEdit)).Status);

        var campaignForReviewer = await CreateCampaignAsync(owner, CampaignType.Email, UserIds.ApprovalOwner);
        var current = DateTimeOffset.UtcNow;
        if (current.Second >= 55)
            await Task.Delay((60 - current.Second) * 1000 + 200);

        var reviewerEdit = await reviewer.PutAsJsonAsync(
            $"/campaigns/{campaignForReviewer.Id}/edit",
            new { name = "Reviewer edit sending", publicationDate = DateTimeOffset.UtcNow });
        Assert.Equal(HttpStatusCode.OK, reviewerEdit.StatusCode);
        Assert.Equal(CampaignStatus.Sending, (await ReadCampaignAsync(reviewerEdit)).Status);
    }

    [Fact]
    public async Task MultiPlatformInstagramAndLinkedInCampaignPublishesBothBeforeClosing()
    {
        using var owner = await LoginAsync("standard@example.com", "StandardPass123!");
        var createResponse = await owner.PostAsJsonAsync(
            "/campaigns",
            new
            {
                name = "Multi-platform campaign",
                description = "Publish to Instagram and LinkedIn",
                modelMsg = "Cross-platform post",
                types = new[] { CampaignType.Instagram, CampaignType.LinkedIn },
                publicationDate = DateTimeOffset.UtcNow.AddMinutes(20),
                imageUrl = "https://8.8.8.8/campaign.png"
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var campaign = await ReadCampaignAsync(createResponse);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoC_DbContext>();
            await db.Campaigns
                .Where(item => item.Id == campaign.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    item => item.PublicationDate,
                    DateTimeOffset.UtcNow.AddMinutes(-1)));
        }

        await RunPublishingWorkerOnceAsync(new QueueHttpMessageHandler(
            JsonResponse(HttpStatusCode.OK, """{"id":"container-123"}"""),
            JsonResponse(HttpStatusCode.OK, """{"status_code":"FINISHED"}"""),
            JsonResponse(HttpStatusCode.OK, """{"id":"media-456"}""")));

        using (var midScope = factory.Services.CreateScope())
        {
            var midDb = midScope.ServiceProvider.GetRequiredService<CoC_DbContext>();
            var afterInsta = await midDb.Campaigns.AsNoTracking().SingleAsync(item => item.Id == campaign.Id);
            Assert.Equal("media-456", afterInsta.InstagramMediaId);
            Assert.Null(afterInsta.LinkedInPostId);
            Assert.Equal(CampaignStatus.Scheduled, afterInsta.Status);
        }

        var imageResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("image-bytes"))
        };
        imageResponse.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        var postResponse = new HttpResponseMessage(HttpStatusCode.Created);
        postResponse.Headers.Add("x-restli-id", "urn:li:share:post-456");

        await RunLinkedInPublishingWorkerOnceAsync(new QueueHttpMessageHandler(
            imageResponse,
            JsonResponse(
                HttpStatusCode.OK,
                """{"value":{"uploadUrl":"https://www.linkedin.com/dms-uploads/upload-456","image":"urn:li:image:image-456"}}"""),
            new HttpResponseMessage(HttpStatusCode.Created),
            postResponse));

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CoC_DbContext>();
        var finalCampaign = await verifyDb.Campaigns.AsNoTracking().SingleAsync(item => item.Id == campaign.Id);
        Assert.Equal("media-456", finalCampaign.InstagramMediaId);
        Assert.Equal("urn:li:share:post-456", finalCampaign.LinkedInPostId);
        Assert.Equal(CampaignStatus.Closed, finalCampaign.Status);
    }

    [Fact]
    public async Task OverdueApprovedScheduledOrSendingCampaignsAreMarkedFailedAfterFiveMinutes()
    {
        using var owner = await LoginAsync("standard@example.com", "StandardPass123!");
        using var approvalOwner = await LoginAsync("owner@example.com", "OwnerPass123!");
        using var reviewer = await LoginAsync("reviewer@example.com", "ReviewerPass123!");

        var approvedCampaign = await CreateCampaignAsync(approvalOwner, CampaignType.Instagram, UserIds.ApprovalOwner);
        var approveResponse = await reviewer.PutAsync($"/campaigns/{approvedCampaign.Id}/review/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var scheduledOverdue = await CreateCampaignAsync(owner, CampaignType.Email, UserIds.StandardOwner);
        var sendingOverdue = await CreateCampaignAsync(owner, CampaignType.Email, UserIds.StandardOwner);
        var scheduledWithinGrace = await CreateCampaignAsync(owner, CampaignType.Email, UserIds.StandardOwner);
        var closedOverdue = await CreateCampaignAsync(owner, CampaignType.Email, UserIds.StandardOwner);

        var overdueTime = DateTimeOffset.UtcNow.AddMinutes(-6);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoC_DbContext>();
            await db.Campaigns
                .Where(item => item.Id == approvedCampaign.Id || item.Id == scheduledOverdue.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.PublicationDate, overdueTime));
            await db.Campaigns
                .Where(item => item.Id == sendingOverdue.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.PublicationDate, overdueTime)
                    .SetProperty(item => item.Status, CampaignStatus.Sending));
            await db.Campaigns
                .Where(item => item.Id == scheduledWithinGrace.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    item => item.PublicationDate,
                    DateTimeOffset.UtcNow.AddMinutes(-3)));
            await db.Campaigns
                .Where(item => item.Id == closedOverdue.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.PublicationDate, overdueTime)
                    .SetProperty(item => item.Status, CampaignStatus.Closed));
        }

        var service = new CampaignExpirationService(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            factory.Services.GetRequiredService<IHostEnvironment>(),
            factory.Services.GetRequiredService<ILogger<CampaignExpirationService>>());

        await service.FailOverdueCampaignsAsync(CancellationToken.None);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CoC_DbContext>();
        Assert.Equal(CampaignStatus.Failed, (await verifyDb.Campaigns.AsNoTracking().SingleAsync(item => item.Id == approvedCampaign.Id)).Status);
        Assert.Equal(CampaignStatus.Failed, (await verifyDb.Campaigns.AsNoTracking().SingleAsync(item => item.Id == scheduledOverdue.Id)).Status);
        Assert.Equal(CampaignStatus.Failed, (await verifyDb.Campaigns.AsNoTracking().SingleAsync(item => item.Id == sendingOverdue.Id)).Status);
        Assert.Equal(CampaignStatus.Scheduled, (await verifyDb.Campaigns.AsNoTracking().SingleAsync(item => item.Id == scheduledWithinGrace.Id)).Status);
        Assert.Equal(CampaignStatus.Closed, (await verifyDb.Campaigns.AsNoTracking().SingleAsync(item => item.Id == closedOverdue.Id)).Status);
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

    private Task<CampaignResponse> CreateCampaignAsync(
        HttpClient client,
        CampaignType type,
        int ownerId) =>
        CreateCampaignAsync(client, ownerId, type);

    private async Task<CampaignResponse> CreateCampaignAsync(
        HttpClient client,
        int ownerId,
        params CampaignType[] types)
    {
        var response = await client.PostAsJsonAsync(
            "/campaigns",
            new
            {
                name = "Workflow test campaign",
                description = "Campaign for workflow tests",
                modelMsg = "Test message",
                types,
                publicationDate = DateTimeOffset.UtcNow.AddMinutes(20),
                imageUrl = types.Contains(CampaignType.Instagram)
                    ? "https://images.example.test/campaign.jpg"
                    : types.Contains(CampaignType.LinkedIn)
                        ? "https://8.8.8.8/campaign.png"
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

    private async Task<CampaignResponse> CreateDueLinkedInCampaignAsync()
    {
        using var owner = await LoginAsync("standard@example.com", "StandardPass123!");
        var campaign = await CreateCampaignAsync(owner, CampaignType.LinkedIn, UserIds.StandardOwner);

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

    private async Task RunLinkedInPublishingWorkerOnceAsync(QueueHttpMessageHandler handler)
    {
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.linkedin.com/")
        };
        var options = new LinkedInPublishingOptions(
            "test-token",
            "urn:li:person:member-123",
            "202611");
        var linkedIn = new LinkedInApiClient(httpClient, options);
        var service = new LinkedInPublishingService(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            linkedIn,
            options,
            factory.Services.GetRequiredService<ILogger<LinkedInPublishingService>>());

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

    private sealed record CampaignResponse(
        Guid Id,
        CampaignStatus Status,
        int UserId,
        List<CampaignType> Types,
        Motivo? Motivo,
        string? JustificationText);

    private sealed record MeResponse(
        int Id,
        string Name,
        string Email,
        List<UserCaracteristics> Caracteristics);

    private sealed record ReasonResponse(string? JustificationText, Motivo? Motivo);

    private sealed record ErrorResponse(string? Erro);

    private static class UserIds
    {
        public const int ApprovalOwner = 1;
        public const int Reviewer = 2;
        public const int StandardOwner = 3;
        public const int OtherUser = 4;
        public const int Admin = 5;
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
                    []),
                new User(
                    UserIds.Admin,
                    "Admin User",
                    "admin@example.com",
                    "AdminPass123!",
                    new DateOnly(1990, 1, 1),
                    "567890123",
                    [UserCaracteristics.Admin]));
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
                             .Where(property =>
                                 property.ClrType == typeof(DateTimeOffset)
                                 || property.ClrType == typeof(DateTimeOffset?)))
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
                request.Headers.Authorization?.Parameter,
                request.Headers.TryGetValues("Linkedin-Version", out var versions)
                    ? versions.Single()
                    : null,
                request.Headers.TryGetValues("X-Restli-Protocol-Version", out var protocolVersions)
                    ? protocolVersions.Single()
                    : null));

            return responses.Dequeue();
        }
    }

    private sealed record CapturedRequest(
        HttpMethod Method,
        string Uri,
        string Body,
        string? BearerToken,
        string? LinkedInVersion,
        string? RestliProtocolVersion);
}
