namespace BSX.Modules.Identity.Domain.Sessions;

/// <summary>Lifecycle status of a session.</summary>
public enum SessionStatus
{
    /// <summary>Active and usable for refresh.</summary>
    Active = 0,

    /// <summary>Revoked; no token in the chain is usable.</summary>
    Revoked = 1,
}
