using System.Security.Claims;
using CoC_API.Context;
using CoC_API.DTOs.Request;
using CoC_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CoC_API.Endpoints;

public static class CampaingEndPoints
{
    public static void MapCampaignEndpoints(this WebApplication app)
    {
        app.MapGet("/campaigns", async (ClaimsPrincipal principal, CoC_DbContext db) =>
        {
            var user = await FindAuthenticatedUserAsync(principal, db);
            if (user is null)
                return Results.Unauthorized();

            if (user.HasCharacteristic(UserCaracteristics.Admin))
                return Results.Ok(await db.Campaigns.ToListAsync());

            var ownCampaigns = await db.Campaigns
                .Where(campaign => campaign.UserId == user.id)
                .ToListAsync();

            if (!user.HasCharacteristic(UserCaracteristics.isAprovador))
                return Results.Ok(ownCampaigns);

            var reviewCampaigns = await db.Campaigns
                .Where(campaign =>
                    campaign.Status == CampaignStatus.InReview
                    || campaign.Status == CampaignStatus.ForChange)
                .ToListAsync();
            var reviewOwnerIds = reviewCampaigns
                .Select(campaign => campaign.UserId)
                .Distinct()
                .ToArray();
            var approvingOwnerIds = (await db.Users
                    .Where(candidate => reviewOwnerIds.Contains(candidate.id))
                    .ToListAsync())
                .Where(candidate => candidate.HasCharacteristic(UserCaracteristics.isAprovando))
                .Select(candidate => candidate.id)
                .ToHashSet();

            var visibleCampaigns = ownCampaigns
                .Concat(reviewCampaigns.Where(campaign => approvingOwnerIds.Contains(campaign.UserId)))
                .DistinctBy(campaign => campaign.Id)
                .ToList();

            return Results.Ok(visibleCampaigns);
        });

        app.MapPost("/campaigns", async (
            CreateCampaignRequest request,
            ClaimsPrincipal principal,
            CoC_DbContext db) =>
        {
            var user = await FindAuthenticatedUserAsync(principal, db);
            if (user is null)
                return Results.Unauthorized();

            try
            {
                var now = DateTimeOffset.UtcNow;
                var publicationStatus = Campaign.ResolvePublicationStatus(request.PublicationDate, now);
                var status = user.HasCharacteristic(UserCaracteristics.isAprovando)
                    ? CampaignStatus.InReview
                    : publicationStatus;
                var types = request.Types ?? new List<CampaignType>();
                if ((types.Contains(CampaignType.Instagram)
                     || types.Contains(CampaignType.LinkedIn)
                     || types.Contains(CampaignType.Facebook))
                    && status == CampaignStatus.Sending)
                    status = CampaignStatus.Scheduled;

                var campaign = new Campaign(
                    request.Name,
                    request.Description,
                    request.ModelMsg,
                    types,
                    status,
                    request.PublicationDate,
                    request.ImageUrl,
                    user.id,
                    now);

                db.Campaigns.Add(campaign);
                await db.SaveChangesAsync();
                return Results.Created($"/campaigns/{campaign.Id}", campaign);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        });

        app.MapPut("/campaigns/{id:guid}/edit", async (
            Guid id,
            EditCampaignRequest request,
            ClaimsPrincipal principal,
            CoC_DbContext db) =>
        {
            var campaign = await db.Campaigns.FindAsync(id);
            if (campaign is null)
                return Results.NotFound();

            var user = await FindAuthenticatedUserAsync(principal, db);
            if (user is null)
                return Results.Unauthorized();
            var isAdmin = user.HasCharacteristic(UserCaracteristics.Admin);
            var isAprovador = user.HasCharacteristic(UserCaracteristics.isAprovador);
            var canAprovadorEditReviewable = isAprovador
                && campaign.Status is CampaignStatus.ForChange or CampaignStatus.InReview
                && await CanViewCampaignAsync(campaign, user, db);
            if (campaign.UserId != user.id && !isAdmin && !canAprovadorEditReviewable)
                return Results.Forbid();
            if (campaign.Status is CampaignStatus.Canceled or CampaignStatus.Closed)
                return Results.Conflict(new { erro = "This campaign can no longer be edited." });
            if (campaign.Status == CampaignStatus.ForChange
                && !isAdmin
                && !isAprovador
                && !user.HasCharacteristic(UserCaracteristics.isAprovando))
                return Results.Forbid();

            try
            {
                var now = DateTimeOffset.UtcNow;

                // Os campos não enviados no pedido mantêm o valor atual da campanha.
                var name = request.Name ?? campaign.Name;
                var description = request.Description ?? campaign.Description;
                var modelMsg = request.ModelMsg ?? campaign.ModelMsg;
                var types = request.Types ?? campaign.Types;
                var publicationDate = request.PublicationDate ?? campaign.PublicationDate;
                var imageUrl = request.ImageUrl is null
                    ? campaign.ImageUrl
                    : string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl;

                var status = campaign.Status;
                if (campaign.Status == CampaignStatus.ForChange)
                {
                    status = isAdmin || isAprovador
                        ? Campaign.ResolvePublicationStatus(publicationDate, now)
                        : CampaignStatus.InReview;
                }
                else if (campaign.Status == CampaignStatus.InReview && (isAdmin || isAprovador))
                {
                    status = Campaign.ResolvePublicationStatus(publicationDate, now);
                }
                else if (status == CampaignStatus.Scheduled)
                {
                    status = Campaign.ResolvePublicationStatus(publicationDate, now);
                }

                if ((types.Contains(CampaignType.Instagram)
                     || types.Contains(CampaignType.LinkedIn)
                     || types.Contains(CampaignType.Facebook))
                    && status == CampaignStatus.Sending)
                    status = CampaignStatus.Scheduled;

                campaign.EditCampaign(
                    name,
                    description,
                    modelMsg,
                    types,
                    status,
                    publicationDate,
                    imageUrl,
                    now);

                await db.SaveChangesAsync();
                return Results.Ok(campaign);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(new { erro = "The campaign changed while it was being edited." });
            }
        });

        app.MapGet("/campaigns/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            CoC_DbContext db) =>
        {
            var campaign = await db.Campaigns.FindAsync(id);
            if (campaign is null)
                return Results.NotFound();

            var user = await FindAuthenticatedUserAsync(principal, db);
            if (user is null)
                return Results.Unauthorized();

            return await CanViewCampaignAsync(campaign, user, db)
                ? Results.Ok(campaign)
                : Results.NotFound();
        });

        app.MapPut("/campaigns/{id:guid}/cancel", async (
            Guid id,
            MotivoRequest request,
            ClaimsPrincipal principal,
            CoC_DbContext db) =>
        {
            var campaign = await db.Campaigns.FindAsync(id);
            if (campaign is null)
                return Results.NotFound();

            var user = await FindAuthenticatedUserAsync(principal, db);
            if (user is null)
                return Results.Unauthorized();
            var isAdmin = user.HasCharacteristic(UserCaracteristics.Admin);
            if (campaign.UserId != user.id && !isAdmin)
                return Results.Forbid();
            if (campaign.Status == CampaignStatus.ForChange && !isAdmin)
                return Results.Forbid();

            try
            {
                campaign.CancelCampaign(request.Motivo, request.JustificationText);
                await db.SaveChangesAsync();
                return Results.Ok(campaign);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(new { erro = "The campaign changed while it was being canceled." });
            }
        });

        app.MapPut("/campaigns/{id:guid}/review/approve", async (
            Guid id,
            ClaimsPrincipal principal,
            CoC_DbContext db) =>
        {
            var campaign = await db.Campaigns.FindAsync(id);
            if (campaign is null)
                return Results.NotFound();

            var user = await FindAuthenticatedUserAsync(principal, db);
            if (user is null)
                return Results.Unauthorized();
            if (!user.HasCharacteristic(UserCaracteristics.isAprovador))
                return Results.Forbid();
            if (!await IsReviewableCampaignAsync(campaign, db))
                return Results.Forbid();

            try
            {
                campaign.Approve();
                await db.SaveChangesAsync();
                return Results.Ok(campaign);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(new { erro = "The campaign changed while it was being reviewed." });
            }
        });

        app.MapPut("/campaigns/{id:guid}/review/request-changes", async (
            Guid id,
            RequestCampaignChangesRequest request,
            ClaimsPrincipal principal,
            CoC_DbContext db) =>
        {
            var campaign = await db.Campaigns.FindAsync(id);
            if (campaign is null)
                return Results.NotFound();

            var user = await FindAuthenticatedUserAsync(principal, db);
            if (user is null)
                return Results.Unauthorized();
            if (!user.HasCharacteristic(UserCaracteristics.isAprovador))
                return Results.Forbid();
            if (!await IsReviewableCampaignAsync(campaign, db))
                return Results.Forbid();

            try
            {
                campaign.RequestChanges(request.JustificationText, request.Motivo);
                await db.SaveChangesAsync();
                return Results.Ok(campaign);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(new { erro = "The campaign changed while it was being reviewed." });
            }
        });

        app.MapPut("/campaigns/{id:guid}/review/reject", async (
            Guid id,
            MotivoRequest request,
            ClaimsPrincipal principal,
            CoC_DbContext db) =>
        {
            var campaign = await db.Campaigns.FindAsync(id);
            if (campaign is null)
                return Results.NotFound();

            var user = await FindAuthenticatedUserAsync(principal, db);
            if (user is null)
                return Results.Unauthorized();
            if (!user.HasCharacteristic(UserCaracteristics.isAprovador))
                return Results.Forbid();
            if (!await IsReviewableCampaignAsync(campaign, db))
                return Results.Forbid();

            try
            {
                campaign.Reject(request.Motivo, request.JustificationText);
                await db.SaveChangesAsync();
                return Results.Ok(campaign);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(new { erro = "The campaign changed while it was being reviewed." });
            }
        });

        app.MapGet("/campaigns/{id:guid}/motivo", async (
            Guid id,
            ClaimsPrincipal principal,
            CoC_DbContext db) =>
        {
            var campaign = await db.Campaigns.FindAsync(id);
            if (campaign is null)
                return Results.NotFound();

            var user = await FindAuthenticatedUserAsync(principal, db);
            if (user is null)
                return Results.Unauthorized();
            if (!await CanViewCampaignAsync(campaign, user, db))
                return Results.NotFound();

            return Results.Ok(new
            {
                JustificationText = campaign.justificationText,
                Motivo = campaign.motivo
            });
        });
    }

    private static async Task<User?> FindAuthenticatedUserAsync(ClaimsPrincipal principal, CoC_DbContext db)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idValue, out var userId))
            return null;

        var user = await db.Users.FindAsync(userId);
        return user is not null && user.HasCharacteristic(UserCaracteristics.isActive)
            ? user
            : null;
    }

    private static async Task<bool> CanViewCampaignAsync(
        Campaign campaign,
        User user,
        CoC_DbContext db)
    {
        if (user.HasCharacteristic(UserCaracteristics.Admin) || campaign.UserId == user.id)
            return true;

        if (!user.HasCharacteristic(UserCaracteristics.isAprovador)
            || campaign.Status is not (CampaignStatus.InReview or CampaignStatus.ForChange))
            return false;

        var owner = await db.Users.FindAsync(campaign.UserId);
        return owner is not null && owner.HasCharacteristic(UserCaracteristics.isAprovando);
    }

    private static async Task<bool> IsReviewableCampaignAsync(Campaign campaign, CoC_DbContext db)
    {
        if (campaign.Status != CampaignStatus.InReview)
            return false;

        var owner = await db.Users.FindAsync(campaign.UserId);
        return owner is not null && owner.HasCharacteristic(UserCaracteristics.isAprovando);
    }
}
