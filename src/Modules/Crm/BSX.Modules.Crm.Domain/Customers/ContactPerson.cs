using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Crm.Domain.Customers;

/// <summary>A contact person owned by a <see cref="Customer"/> aggregate.</summary>
public sealed class ContactPerson : Entity<ContactPersonId>
{
    internal ContactPerson(
        ContactPersonId id,
        CustomerName name,
        Email? email,
        PhoneNumber? phoneNumber,
        bool isPrimary)
        : base(id)
    {
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        IsPrimary = isPrimary;
    }

    private ContactPerson()
    {
    }

    /// <summary>Gets the person's name.</summary>
    public CustomerName Name { get; private set; } = null!;

    /// <summary>Gets the optional email address.</summary>
    public Email? Email { get; private set; }

    /// <summary>Gets the optional phone number.</summary>
    public PhoneNumber? PhoneNumber { get; private set; }

    /// <summary>Gets a value indicating whether this is the customer's primary contact.</summary>
    public bool IsPrimary { get; private set; }

    internal void ChangeDetails(CustomerName name, Email? email, PhoneNumber? phoneNumber)
    {
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
    }

    internal void MarkPrimary() => IsPrimary = true;

    internal void ClearPrimary() => IsPrimary = false;
}
