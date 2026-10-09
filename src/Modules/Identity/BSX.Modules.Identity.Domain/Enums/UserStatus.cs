namespace BSX.Modules.Identity.Domain.Enums;

/// <summary>
/// Lifecycle status of a user identity.
/// </summary>
public enum UserStatus
{
    /// <summary>Registered but not yet activated.</summary>
    Pending = 0,

    /// <summary>Active and allowed to authenticate.</summary>
    Active = 1,

    /// <summary>Disabled; cannot authenticate.</summary>
    Disabled = 2,

    /// <summary>Temporarily locked; cannot authenticate until unlocked.</summary>
    Locked = 3,
}
