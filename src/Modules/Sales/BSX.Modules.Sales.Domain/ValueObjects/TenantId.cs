namespace BSX.Modules.Sales.Domain.ValueObjects;

/// <summary>
/// Strongly typed tenant identifier. Sales aggregates are tenant-owned (ADR-013); the identifier is
/// carried by every aggregate so isolation can be enforced by the Infrastructure layer.
/// </summary>
public sealed class TenantId : SalesId
{
    private TenantId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static TenantId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    /// <param name="value">The identifier value.</param>
    public static TenantId From(Guid value) => new(value);
}
