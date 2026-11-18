namespace CoC_API.Models;

public class UserSession
{
    public string TokenHash { get; private set; } = string.Empty;
    public int UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    private UserSession()
    {
    }

    public UserSession(string tokenHash, int userId, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        TokenHash = tokenHash;
        UserId = userId;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public void Revoke(DateTimeOffset revokedAt)
    {
        RevokedAt ??= revokedAt;
    }
}
