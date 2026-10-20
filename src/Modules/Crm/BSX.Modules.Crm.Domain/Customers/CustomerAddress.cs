using BSX.Modules.Crm.Domain.Enums;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Crm.Domain.Customers;

/// <summary>An address owned by a <see cref="Customer"/> aggregate.</summary>
public sealed class CustomerAddress : Entity<CustomerAddressId>
{
    internal CustomerAddress(CustomerAddressId id, Address address, AddressType type, bool isDefault)
        : base(id)
    {
        Address = address;
        Type = type;
        IsDefault = isDefault;
    }

    private CustomerAddress()
    {
    }

    /// <summary>Gets the postal address.</summary>
    public Address Address { get; private set; } = null!;

    /// <summary>Gets the address purpose.</summary>
    public AddressType Type { get; private set; }

    /// <summary>Gets a value indicating whether this is the customer's default address.</summary>
    public bool IsDefault { get; private set; }

    internal void ChangeType(AddressType type) => Type = type;

    internal void ChangeAddress(Address address) => Address = address;

    internal void MarkAsDefault() => IsDefault = true;

    internal void ClearDefault() => IsDefault = false;
}
