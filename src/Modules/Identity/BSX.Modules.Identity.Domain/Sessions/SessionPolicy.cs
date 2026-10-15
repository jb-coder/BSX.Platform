namespace BSX.Modules.Identity.Domain.Sessions;

/// <summary>Refresh/session lifetime policy (ADR-017).</summary>
public sealed record SessionPolicy(
    TimeSpan TokenLifetime,
    TimeSpan IdleTimeout,
    TimeSpan AbsoluteLifetime,
    TimeSpan GraceWindow)
{
    /// <summary>Default policy: 7-day token, 7-day idle, 30-day absolute, 30-second grace.</summary>
    public static SessionPolicy Default { get; } = new(
        TimeSpan.FromDays(7),
        TimeSpan.FromDays(7),
        TimeSpan.FromDays(30),
        TimeSpan.FromSeconds(30));
}
