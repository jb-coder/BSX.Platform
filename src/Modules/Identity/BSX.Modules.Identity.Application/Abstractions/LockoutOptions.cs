using BSX.Modules.Identity.Domain.Users;

namespace BSX.Modules.Identity.Application.Abstractions;

/// <summary>
/// Durable account-lockout policy (ADR-022). The defaults match the domain defaults so an
/// unconfigured deployment still locks accounts after repeated failures.
/// </summary>
public sealed class LockoutOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Identity:Lockout";

    /// <summary>Gets or sets the number of consecutive failures before the account is locked.</summary>
    public int MaxFailedAttempts { get; set; } = User.DefaultMaxAccessFailedCount;

    /// <summary>Gets or sets the lockout duration in minutes.</summary>
    public int LockoutDurationMinutes { get; set; } = (int)User.DefaultLockoutDuration.TotalMinutes;

    /// <summary>Gets the effective failure threshold (at least one).</summary>
    public int EffectiveMaxFailedAttempts => Math.Max(1, MaxFailedAttempts);

    /// <summary>Gets the effective lockout duration (at least one minute).</summary>
    public TimeSpan EffectiveLockoutDuration => TimeSpan.FromMinutes(Math.Max(1, LockoutDurationMinutes));
}
