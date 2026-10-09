using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Domain.Primitives;

/// <summary>
/// A principal that can authenticate and be authorized. Implemented by users and service
/// accounts (ADR-014).
/// </summary>
public interface IPrincipal
{
    /// <summary>Gets the principal identifier.</summary>
    PrincipalId Principal { get; }
}
