using System.Collections.Concurrent;
using System.Security.Cryptography;
using CoC_API.Models;

namespace CoC_API.Security;

public sealed class InMemoryUserSessionStore
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
    private readonly ConcurrentDictionary<string, UserSession> sessions = new(StringComparer.Ordinal);
    private readonly List<User> users;

    public InMemoryUserSessionStore(List<User> users)
    {
        this.users = users;
    }

    public string CreateSession(int userId)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var session in sessions)
        {
            if (session.Value.ExpiresAt <= now)
                sessions.TryRemove(session.Key, out _);
        }

        while (true)
        {
            var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            if (sessions.TryAdd(sessionId, new UserSession(userId, now.Add(SessionLifetime))))
                return sessionId;
        }
    }

    public bool TryGetActiveUser(string sessionId, out User? user)
    {
        user = null;
        if (!sessions.TryGetValue(sessionId, out var session))
            return false;

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            sessions.TryRemove(sessionId, out _);
            return false;
        }

        user = users.FirstOrDefault(candidate => candidate.id == session.UserId);
        if (user is null || !user.HasCharacteristic(UserCaracteristics.isActive))
        {
            sessions.TryRemove(sessionId, out _);
            user = null;
            return false;
        }

        return true;
    }

    public void RevokeSession(string? sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
            sessions.TryRemove(sessionId, out _);
    }

    private sealed record UserSession(int UserId, DateTimeOffset ExpiresAt);
}
