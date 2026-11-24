using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoC_API.Models;

namespace CoC_API.Integrations;

public sealed class LinkedInApiClient(
    HttpClient httpClient,
    LinkedInPublishingOptions options)
{
    private const int MaximumImageBytes = 10 * 1024 * 1024;

    public async Task<string> PublishPostAsync(Campaign campaign, CancellationToken cancellationToken)
    {
        if (!options.IsConfigured)
            throw new InvalidOperationException("LinkedIn publishing is not fully configured.");

        if (!options.IsValidApiVersion)
            throw new InvalidOperationException("LinkedIn API version must have the format YYYYMM.");

        if (!options.IsValidPersonUrn)
            throw new InvalidOperationException("LinkedIn person URN must have the format urn:li:person:<id>.");

        var imageUrn = string.IsNullOrWhiteSpace(campaign.ImageUrl)
            ? null
            : await UploadImageAsync(campaign.ImageUrl, cancellationToken);

        var requestBody = new CreatePostRequest(
            options.PersonUrn!,
            $"{campaign.Name}\n{campaign.ModelMsg}",
            imageUrn is null ? null : new PostContent(new PostMedia(imageUrn, campaign.Name)));

        using var request = CreateApiRequest(HttpMethod.Post, "rest/posts");
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using var response = await SendLinkedInRequestAsync(request, cancellationToken);

        if (response.Headers.TryGetValues("x-restli-id", out var postIds))
        {
            var postId = postIds.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(postId))
                return postId;
        }

        throw new InvalidOperationException("LinkedIn API response did not contain an x-restli-id header.");
    }

    private async Task<string> UploadImageAsync(string imageUrl, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var imageUri)
            || imageUri.Scheme != Uri.UriSchemeHttps
            || imageUri.Port != 443
            || !string.IsNullOrEmpty(imageUri.UserInfo))
        {
            throw new InvalidOperationException("LinkedIn campaign images require a public HTTPS URL.");
        }

        IPAddress[] addresses;
        if (IPAddress.TryParse(imageUri.Host, out var imageAddress))
            addresses = [imageAddress];
        else
        {
            try
            {
                addresses = await Dns.GetHostAddressesAsync(imageUri.DnsSafeHost, cancellationToken);
            }
            catch (SocketException exception)
            {
                throw new HttpRequestException("Could not resolve the LinkedIn campaign image host.", exception);
            }
        }

        if (addresses.Length == 0 || addresses.Any(address => !IsPublicAddress(address)))
            throw new InvalidOperationException("LinkedIn campaign image URL must resolve only to public IP addresses.");

        using var imageRequest = new HttpRequestMessage(HttpMethod.Get, imageUri);
        imageRequest.Headers.UserAgent.ParseAdd("CoC_API/1.0");
        using var imageResponse = await httpClient.SendAsync(
            imageRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        var image = await ReadImageAsync(imageResponse, cancellationToken);

        using var initializeRequest = CreateApiRequest(HttpMethod.Post, "rest/images?action=initializeUpload");
        initializeRequest.Content = new StringContent(
            JsonSerializer.Serialize(new InitializeImageUploadRequest(options.PersonUrn!)),
            Encoding.UTF8,
            "application/json");

        using var initializeResponse = await SendLinkedInRequestAsync(initializeRequest, cancellationToken);
        var uploadDetails = await initializeResponse.Content.ReadFromJsonAsync<InitializeImageUploadResponse>(
            cancellationToken: cancellationToken);
        var uploadUrl = uploadDetails?.Value?.UploadUrl;
        var imageUrn = uploadDetails?.Value?.Image;
        if (string.IsNullOrWhiteSpace(uploadUrl)
            || string.IsNullOrWhiteSpace(imageUrn)
            || !Uri.TryCreate(uploadUrl, UriKind.Absolute, out var uploadUri)
            || uploadUri.Scheme != Uri.UriSchemeHttps
            || uploadUri.Port != 443
            || !(uploadUri.Host.Equals("linkedin.com", StringComparison.OrdinalIgnoreCase)
                || uploadUri.Host.EndsWith(".linkedin.com", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("LinkedIn image upload initialization returned invalid upload details.");
        }

        using var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUri);
        uploadRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.NormalizedAccessToken);
        uploadRequest.Content = new ByteArrayContent(image.Bytes);
        uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);

        using var uploadResponse = await httpClient.SendAsync(uploadRequest, cancellationToken);
        await EnsureSuccessAsync(uploadResponse, cancellationToken);
        return imageUrn;
    }

    private static async Task<DownloadedImage> ReadImageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);

        var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
        if (contentType is not ("image/jpeg" or "image/png" or "image/gif"))
            throw new InvalidOperationException("LinkedIn campaign images must be JPEG, PNG, or GIF.");

        if (response.Content.Headers.ContentLength > MaximumImageBytes)
            throw new InvalidOperationException("LinkedIn campaign images cannot exceed 10 MB.");

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        while (true)
        {
            var bytesRead = await source.ReadAsync(chunk.AsMemory(), cancellationToken);
            if (bytesRead == 0)
                break;

            if (buffer.Length + bytesRead > MaximumImageBytes)
                throw new InvalidOperationException("LinkedIn campaign images cannot exceed 10 MB.");

            await buffer.WriteAsync(chunk.AsMemory(0, bytesRead), cancellationToken);
        }

        if (buffer.Length == 0)
            throw new InvalidOperationException("LinkedIn campaign image is empty.");

        return new DownloadedImage(buffer.ToArray(), contentType);
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            return (bytes[0] & 0xE0) == 0x20
                && !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0D && bytes[3] == 0xB8);
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
            return false;

        var ipv4 = address.GetAddressBytes();
        return ipv4[0] != 0
            && ipv4[0] != 10
            && ipv4[0] != 127
            && ipv4[0] < 224
            && !(ipv4[0] == 100 && (ipv4[1] & 0xC0) == 64)
            && !(ipv4[0] == 169 && ipv4[1] == 254)
            && !(ipv4[0] == 172 && (ipv4[1] & 0xF0) == 16)
            && !(ipv4[0] == 192 && ipv4[1] == 168)
            && !(ipv4[0] == 192 && ipv4[1] == 0 && ipv4[2] == 0)
            && !(ipv4[0] == 192 && ipv4[1] == 0 && ipv4[2] == 2)
            && !(ipv4[0] == 192 && ipv4[1] == 88 && ipv4[2] == 99)
            && !(ipv4[0] == 198 && (ipv4[1] == 18 || ipv4[1] == 19))
            && !(ipv4[0] == 198 && ipv4[1] == 51 && ipv4[2] == 100)
            && !(ipv4[0] == 203 && ipv4[1] == 0 && ipv4[2] == 113);
    }

    private HttpRequestMessage CreateApiRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.NormalizedAccessToken);
        request.Headers.Add("Linkedin-Version", options.ApiVersion);
        request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
        return request;
    }

    private async Task<HttpResponseMessage> SendLinkedInRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return response;
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"LinkedIn image or post request returned {(int)response.StatusCode}: {body}",
            null,
            response.StatusCode);
    }

    private sealed record CreatePostRequest(
        [property: JsonPropertyName("author")] string Author,
        [property: JsonPropertyName("commentary")] string Commentary,
        [property: JsonPropertyName("content")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        PostContent? Content)
    {
        [JsonPropertyName("visibility")]
        public string Visibility => "PUBLIC";

        [JsonPropertyName("distribution")]
        public DistributionRequest Distribution => new();

        [JsonPropertyName("lifecycleState")]
        public string LifecycleState => "PUBLISHED";

        [JsonPropertyName("isReshareDisabledByAuthor")]
        public bool IsReshareDisabledByAuthor => false;
    }

    private sealed record PostContent(
        [property: JsonPropertyName("media")] PostMedia Media);

    private sealed record PostMedia(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("altText")] string AltText);

    private sealed record InitializeImageUploadRequest(
        [property: JsonPropertyName("initializeUploadRequest")]
        InitializeImageUploadDetails Request)
    {
        public InitializeImageUploadRequest(string personUrn)
            : this(new InitializeImageUploadDetails(personUrn))
        {
        }
    }

    private sealed record InitializeImageUploadDetails(
        [property: JsonPropertyName("owner")] string Owner);

    private sealed record InitializeImageUploadResponse(
        [property: JsonPropertyName("value")] InitializeImageUploadValue? Value);

    private sealed record InitializeImageUploadValue(
        [property: JsonPropertyName("uploadUrl")] string? UploadUrl,
        [property: JsonPropertyName("image")] string? Image);

    private sealed record DownloadedImage(byte[] Bytes, string ContentType);

    private sealed record DistributionRequest
    {
        [JsonPropertyName("feedDistribution")]
        public string FeedDistribution => "MAIN_FEED";

        [JsonPropertyName("targetEntities")]
        public string[] TargetEntities => [];

        [JsonPropertyName("thirdPartyDistributionChannels")]
        public string[] ThirdPartyDistributionChannels => [];
    }
}
