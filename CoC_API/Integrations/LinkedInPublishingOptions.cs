namespace CoC_API.Integrations;

public sealed record LinkedInPublishingOptions(
    string? AccessToken,
    string? PersonUrn,
    string? ApiVersion)
{
    public string? NormalizedAccessToken => AccessToken?.Trim();

    public bool IsValidAccessToken
    {
        get
        {
            var token = NormalizedAccessToken;
            return !string.IsNullOrWhiteSpace(token)
                && !token.Any(character => char.IsWhiteSpace(character) || char.IsControl(character));
        }
    }

    public bool IsConfigured =>
        IsValidAccessToken
        && !string.IsNullOrWhiteSpace(PersonUrn)
        && !string.IsNullOrWhiteSpace(ApiVersion);

    public bool HasAnyValue =>
        !string.IsNullOrWhiteSpace(AccessToken)
        || !string.IsNullOrWhiteSpace(PersonUrn)
        || !string.IsNullOrWhiteSpace(ApiVersion);

    public bool IsValidApiVersion =>
        ApiVersion is { Length: 6 } && ApiVersion.All(char.IsAsciiDigit);

    public bool IsValidPersonUrn =>
        PersonUrn is not null
        && PersonUrn.StartsWith("urn:li:person:", StringComparison.Ordinal)
        && PersonUrn.Length > "urn:li:person:".Length;
}
