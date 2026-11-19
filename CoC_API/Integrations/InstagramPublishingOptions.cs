namespace CoC_API.Integrations;

public sealed record InstagramPublishingOptions(
    string? AccessToken,
    string? InstagramUserId,
    string? GraphApiVersion)
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccessToken)
        && !string.IsNullOrWhiteSpace(InstagramUserId)
        && !string.IsNullOrWhiteSpace(GraphApiVersion);

    public bool HasAnyValue =>
        !string.IsNullOrWhiteSpace(AccessToken)
        || !string.IsNullOrWhiteSpace(InstagramUserId)
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
