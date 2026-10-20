using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Domain.Organizations;

/// <summary>
/// An organization groups customers (for example a holding company or corporate group).
/// Customers reference an organization by identifier only (ADR-003, ADR-013).
/// </summary>
public sealed class Organization : AggregateRoot<OrganizationId>
{
    private Organization(
        OrganizationId id,
        TenantId tenantId,
        CustomerName name,
        TaxIdentifier? taxIdentifier,
        Address? address)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        TaxIdentifier = taxIdentifier;
        Address = address;
    }

    private Organization()
    {
    }

    /// <summary>Gets the reserved tenant identifier (ADR-013).</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Gets the organization name.</summary>
    public CustomerName Name { get; private set; } = null!;

    /// <summary>Gets the optional tax identifier.</summary>
    public TaxIdentifier? TaxIdentifier { get; private set; }

    /// <summary>Gets the optional primary address.</summary>
    public Address? Address { get; private set; }

    /// <summary>Creates an organization.</summary>
    /// <param name="id">The organization identifier.</param>
    /// <param name="tenantId">The reserved tenant identifier.</param>
    /// <param name="name">The organization name.</param>
    /// <param name="taxIdentifier">The optional tax identifier.</param>
    /// <param name="address">The optional primary address.</param>
    public static Result<Organization> Create(
        OrganizationId id,
        TenantId tenantId,
        CustomerName name,
        TaxIdentifier? taxIdentifier = null,
        Address? address = null)
        => Result.Success(new Organization(id, tenantId, name, taxIdentifier, address));

    /// <summary>Renames the organization.</summary>
    /// <param name="name">The new name.</param>
    public Result Rename(CustomerName name)
    {
        Name = name;
        return Result.Success();
    }

    /// <summary>Changes or clears the tax identifier.</summary>
    /// <param name="taxIdentifier">The new tax identifier, or <see langword="null"/> to clear it.</param>
    public Result ChangeTaxIdentifier(TaxIdentifier? taxIdentifier)
    {
        TaxIdentifier = taxIdentifier;
        return Result.Success();
    }

    /// <summary>Changes or clears the primary address.</summary>
    /// <param name="address">The new address, or <see langword="null"/> to clear it.</param>
    public Result ChangeAddress(Address? address)
    {
        Address = address;
        return Result.Success();
    }
}
