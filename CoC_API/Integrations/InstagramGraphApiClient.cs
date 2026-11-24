using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoC_API.Models;

namespace CoC_API.Integrations;

public sealed class InstagramGraphApiClient(
    HttpClient httpClient,
    InstagramPublishingOptions options)
{
    private static readonly TimeSpan ContainerPollInterval = TimeSpan.FromSeconds(2);
    private const int ContainerPollAttempts = 30;

    public async Task<string> PublishImageAsync(Campaign campaign, CancellationToken cancellationToken)
    {
        if (!options.IsConfigured)
            throw new InvalidOperationException("Instagram publishing is not fully configured.");

        if (!options.TryGetGraphApiVersion(out var version))
            throw new InvalidOperationException("Instagram Graph API version must have the format vX.Y.");

        if (!Uri.TryCreate(campaign.ImageUrl, UriKind.Absolute, out var imageUri)
            || imageUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Instagram campaigns require a publicly accessible HTTPS image URL.");
        }

        var userId = Uri.EscapeDataString(options.InstagramUserId!);
        var containerId = await SendForIdAsync(
            HttpMethod.Post,
            $"{version}/{userId}/media",
            new Dictionary<string, string>
            {
                ["image_url"] = imageUri.AbsoluteUri,
                ["caption"] = $"{campaign.Name}\n{campaign.ModelMsg}"
            },
            cancellationToken);

        await WaitForContainerAsync(version, containerId, cancellationToken);

        return await SendForIdAsync(
            HttpMethod.Post,
            $"{version}/{userId}/media_publish",
            new Dictionary<string, string> { ["creation_id"] = containerId },
            cancellationToken);
    }

    private async Task<string> SendForIdAsync(
        HttpMethod method,
        string path,
        Dictionary<string, string>? form,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        if (form is not null)
            request.Content = new FormUrlEncodedContent(form);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Instagram Graph API returned {(int)response.StatusCode}: {body}",
                null,
                response.StatusCode);
        }

        var result = JsonSerializer.Deserialize<IdResponse>(body);
        return !string.IsNullOrWhiteSpace(result?.Id)
            ? result.Id
            : throw new InvalidOperationException("Instagram Graph API response did not contain an ID.");
    }

    private async Task WaitForContainerAsync(
        string version,
        string containerId,
        CancellationToken cancellationToken)
    {
        var path = $"{version}/{Uri.EscapeDataString(containerId)}?fields=status_code";

        for (var attempt = 0; attempt < ContainerPollAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Instagram Graph API returned {(int)response.StatusCode}: {body}",
                    null,
                    response.StatusCode);
            }

            var result = JsonSerializer.Deserialize<ContainerStatusResponse>(body);
            switch (result?.StatusCode)
            {
                case "FINISHED":
                case "PUBLISHED":
                    return;
                case "ERROR":
                case "EXPIRED":
                    throw new InvalidOperationException(
                        $"Instagram media container '{containerId}' ended with status '{result.StatusCode}'.");
                case "IN_PROGRESS":
                    await Task.Delay(ContainerPollInterval, cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Instagram Graph API returned an unexpected media container status: '{result?.StatusCode}'.");
            }
        }

        throw new TimeoutException($"Instagram media container '{containerId}' was not ready in time.");
    }

    private sealed record IdResponse([property: JsonPropertyName("id")] string? Id);

    private sealed record ContainerStatusResponse(
        [property: JsonPropertyName("status_code")] string? StatusCode);
}
