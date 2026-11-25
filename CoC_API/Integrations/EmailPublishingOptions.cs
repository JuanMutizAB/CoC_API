namespace CoC_API.Integrations;

public sealed record EmailPublishingOptions(
    string? Host,
    string? Port,
    string? Username,
    string? Password,
    string? From,
    string? Recipients)
{
    public int PortNumber => int.TryParse(Port, out var port) ? port : 587;

    public IReadOnlyList<string> RecipientList =>
        (Recipients ?? string.Empty)
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password)
        && !string.IsNullOrWhiteSpace(From);

    public bool HasAnyValue =>
        !string.IsNullOrWhiteSpace(Host)
        || !string.IsNullOrWhiteSpace(Username)
        || !string.IsNullOrWhiteSpace(Password)
        || !string.IsNullOrWhiteSpace(From)
        || !string.IsNullOrWhiteSpace(Recipients);
}
