using System.Collections.Concurrent;

namespace CoC_API.Security;

/// <summary>
/// Tracks failed login attempts per account email and enforces progressive
/// temporary lockouts (5 min after 3 failures, escalating on subsequent failures).
/// </summary>
public sealed class LoginLockoutService(TimeProvider timeProvider)
{
    private const int MaxAttemptsBeforeLockout = 3;
    private static readonly TimeSpan[] LockoutDurations =
    [
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromMinutes(60),
    ];

    private readonly ConcurrentDictionary<string, LockoutEntry> entries = new(StringComparer.Ordinal);

    public LockoutStatus CheckStatus(string normalizedEmail)
    {
        if (!entries.TryGetValue(normalizedEmail, out var entry))
            return LockoutStatus.NotLocked;

        var now = timeProvider.GetUtcNow();
        lock (entry)
        {
            if (entry.LockedUntilUtc is { } lockedUntil && lockedUntil > now)
                return CreateLockedStatus(lockedUntil - now);

            return LockoutStatus.NotLocked;
        }
    }

    public LockoutStatus RecordFailure(string normalizedEmail)
    {
        var now = timeProvider.GetUtcNow();
        var entry = entries.GetOrAdd(normalizedEmail, _ => new LockoutEntry());

        lock (entry)
        {
            if (entry.LockedUntilUtc is { } lockedUntil && lockedUntil > now)
                return CreateLockedStatus(lockedUntil - now);

            // Lockout expired and the user failed again: escalate immediately.
            if (entry.LockoutLevel > 0)
            {
                entry.LockoutLevel++;
                var escalatedDuration = GetDurationForLevel(entry.LockoutLevel);
                entry.LockedUntilUtc = now + escalatedDuration;
                return CreateLockedStatus(escalatedDuration);
            }

            entry.FailedAttempts++;
            if (entry.FailedAttempts >= MaxAttemptsBeforeLockout)
            {
                entry.LockoutLevel = 1;
                var initialDuration = GetDurationForLevel(entry.LockoutLevel);
                entry.LockedUntilUtc = now + initialDuration;
                return CreateLockedStatus(initialDuration);
            }

            return LockoutStatus.NotLocked;
        }
    }

    public void Reset(string normalizedEmail)
    {
        entries.TryRemove(normalizedEmail, out _);
    }

    private static TimeSpan GetDurationForLevel(int level)
    {
        var index = Math.Clamp(level - 1, 0, LockoutDurations.Length - 1);
        return LockoutDurations[index];
    }

    private static LockoutStatus CreateLockedStatus(TimeSpan remaining)
    {
        var totalSeconds = Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds));
        var minutes = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
        var message = minutes == 1
            ? "Try again in 1 minute."
            : $"Try again in {minutes} minutes.";

        return new LockoutStatus(true, totalSeconds, message);
    }

    private sealed class LockoutEntry
    {
        public int FailedAttempts { get; set; }
        public int LockoutLevel { get; set; }
        public DateTimeOffset? LockedUntilUtc { get; set; }
    }
}

public readonly record struct LockoutStatus(
    bool IsLocked,
    int RetryAfterSeconds,
    string Message)
{
    public static LockoutStatus NotLocked => new(false, 0, string.Empty);
}
