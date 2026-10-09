namespace BSX.Modules.Identity.Domain.Enums;

/// <summary>
/// The kind of principal that can authenticate and be authorized.
/// </summary>
public enum PrincipalType
{
    /// <summary>A human user.</summary>
    User = 0,

    /// <summary>A machine service account.</summary>
    ServiceAccount = 1,
}
