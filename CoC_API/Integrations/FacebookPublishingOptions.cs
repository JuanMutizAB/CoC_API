namespace CoC_API.Integrations;

public sealed record FacebookPublishingOptions(
    string? PageAccessToken,
    string? PageId,
    string? GraphApiVersion)
{
    public string? NormalizedPageAccessToken => PageAccessToken?.Trim();

    public bool IsValidPageAccessToken
    {
        get
        {
            var token = NormalizedPageAccessToken;
            return !string.IsNullOrWhiteSpace(token)
                && !token.Any(character => char.IsWhiteSpace(character) || char.IsControl(character));
        }
    }

    public bool IsConfigured =>
        IsValidPageAccessToken
        && !string.IsNullOrWhiteSpace(PageId)
        && TryGetGraphApiVersion(out _);

    public bool HasAnyValue =>
        !string.IsNullOrWhiteSpace(PageAccessToken)
        || !string.IsNullOrWhiteSpace(PageId)
        || !string.IsNullOrWhiteSpace(GraphApiVersion);

    public bool TryGetGraphApiVersion(out string version)
    {
        var configuredVersion = GraphApiVersion?.Trim() ?? string.Empty;
        if (configuredVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            configuredVersion = configuredVersion[1..];

        var parts = configuredVersion.Split('.');
        if (parts.Length == 2
            && int.TryParse(parts[0], out var major)
            && int.TryParse(parts[1], out var minor))
        {
            version = $"v{major}.{minor}";
            return true;
        }

        version = string.Empty;
        return false;
    }
}
