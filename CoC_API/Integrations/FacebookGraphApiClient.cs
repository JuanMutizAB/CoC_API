using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CoC_API.Models;

namespace CoC_API.Integrations;

public sealed class FacebookGraphApiClient(
    HttpClient httpClient,
    FacebookPublishingOptions options)
{
    public async Task<string> PublishPostAsync(Campaign campaign, CancellationToken cancellationToken)
    {
        if (!options.IsConfigured || !options.TryGetGraphApiVersion(out var version))
            throw new InvalidOperationException("Facebook publishing is not fully configured.");

        var pageId = Uri.EscapeDataString(options.PageId!);
        var hasImage = !string.IsNullOrWhiteSpace(campaign.ImageUrl);
        var path = hasImage
            ? $"{version}/{pageId}/photos"
            : $"{version}/{pageId}/feed";

        var message = $"{campaign.Name}\n{campaign.ModelMsg}";
        var form = new Dictionary<string, string>
        {
            ["message"] = message
        };

        if (hasImage)
        {
            if (!Uri.TryCreate(campaign.ImageUrl, UriKind.Absolute, out var imageUri)
                || imageUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException("Facebook campaign images require a publicly accessible HTTPS URL.");
            }

            form["url"] = imageUri.AbsoluteUri;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            options.NormalizedPageAccessToken);
        request.Content = new FormUrlEncodedContent(form);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Facebook Graph API returned {(int)response.StatusCode}: {body}",
                null,
                response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<PagePostResponse>(
            cancellationToken: cancellationToken);
        var postId = hasImage ? result?.PostId : result?.Id;
        if (string.IsNullOrWhiteSpace(postId))
            throw new InvalidOperationException("Facebook Graph API response did not contain a post ID.");

        return postId;
    }

    private sealed record PagePostResponse(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("post_id")] string? PostId);
}
