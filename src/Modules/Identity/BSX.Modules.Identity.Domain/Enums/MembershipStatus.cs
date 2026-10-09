namespace BSX.Modules.Identity.Domain.Enums;

/// <summary>
/// Lifecycle status of a user's membership in a tenant.
/// </summary>
public enum MembershipStatus
{
    /// <summary>Invited but not yet accepted.</summary>
    Invited = 0,

    /// <summary>Active member of the tenant.</summary>
    Active = 1,

    /// <summary>Suspended; grants no permissions.</summary>
    Suspended = 2,

    /// <summary>Removed from the tenant.</summary>
    Removed = 3,
}
