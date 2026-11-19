using System.Security.Cryptography;
using System.Text;
using CoC_API.Context;
using CoC_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CoC_API.Security;

public sealed class UserSessionStore
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
    private readonly CoC_DbContext db;

    public UserSessionStore(CoC_DbContext db)
    {
        this.db = db;
    }

    public async Task<string> CreateSessionAsync(int userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var expiredSessions = await db.UserSessions
            .Where(session => session.ExpiresAt <= now || session.RevokedAt != null)
            .ToListAsync(cancellationToken);
        db.UserSessions.RemoveRange(expiredSessions);

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        db.UserSessions.Add(new UserSession(
            HashToken(token),
            userId,
            now,
            now.Add(SessionLifetime)));
        await db.SaveChangesAsync(cancellationToken);

        return token;
    }

    public async Task<User?> TryGetActiveUserAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(token);
        var session = await db.UserSessions
            .SingleOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, cancellationToken);
        if (session is null)
            return null;

        var now = DateTimeOffset.UtcNow;
        if (session.RevokedAt is not null || session.ExpiresAt <= now)
        {
            session.Revoke(now);
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        var user = await db.Users.FindAsync([session.UserId], cancellationToken);
        if (user is null || !user.HasCharacteristic(UserCaracteristics.isActive))
        {
            session.Revoke(now);
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        return user;
    }

    public async Task RevokeSessionAsync(
        string? token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return;

        var tokenHash = HashToken(token);
        var session = await db.UserSessions
            .SingleOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, cancellationToken);
        if (session is null)
            return;

        session.Revoke(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
