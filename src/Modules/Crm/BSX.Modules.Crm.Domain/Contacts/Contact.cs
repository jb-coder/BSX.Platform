using BSX.Modules.Crm.Domain.Enums;
using BSX.Modules.Crm.Domain.Errors;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Domain.Contacts;

/// <summary>
/// A standalone contact directory entry. It may optionally be associated with a customer, which is
/// referenced by identifier only (ADR-003).
/// </summary>
public sealed class Contact : AggregateRoot<ContactId>
{
    private Contact(
        ContactId id,
        TenantId tenantId,
        CustomerName name,
        Email? email,
        PhoneNumber? phoneNumber,
        CustomerId? customerId)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        CustomerId = customerId;
        Status = ContactStatus.Active;
    }

    private Contact()
    {
    }

    /// <summary>Gets the reserved tenant identifier (ADR-013).</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Gets the contact name.</summary>
    public CustomerName Name { get; private set; } = null!;

    /// <summary>Gets the optional email address.</summary>
    public Email? Email { get; private set; }

    /// <summary>Gets the optional phone number.</summary>
    public PhoneNumber? PhoneNumber { get; private set; }

    /// <summary>Gets the associated customer, if any (referenced by identifier).</summary>
    public CustomerId? CustomerId { get; private set; }

    /// <summary>Gets the lifecycle status.</summary>
    public ContactStatus Status { get; private set; }

    /// <summary>Creates an active contact.</summary>
    /// <param name="id">The contact identifier.</param>
    /// <param name="tenantId">The reserved tenant identifier.</param>
    /// <param name="name">The contact name.</param>
    /// <param name="email">The optional email address.</param>
    /// <param name="phoneNumber">The optional phone number.</param>
    /// <param name="customerId">The optional associated customer.</param>
    public static Result<Contact> Create(
        ContactId id,
        TenantId tenantId,
        CustomerName name,
        Email? email = null,
        PhoneNumber? phoneNumber = null,
        CustomerId? customerId = null)
        => Result.Success(new Contact(id, tenantId, name, email, phoneNumber, customerId));

    /// <summary>Changes the contact name.</summary>
    /// <param name="name">The new name.</param>
    public Result ChangeName(CustomerName name)
    {
        Name = name;
        return Result.Success();
    }

    /// <summary>Changes or clears the email address.</summary>
    /// <param name="email">The new email, or <see langword="null"/> to clear it.</param>
    public Result ChangeEmail(Email? email)
    {
        Email = email;
        return Result.Success();
    }

    /// <summary>Changes or clears the phone number.</summary>
    /// <param name="phoneNumber">The new phone number, or <see langword="null"/> to clear it.</param>
    public Result ChangePhoneNumber(PhoneNumber? phoneNumber)
    {
        PhoneNumber = phoneNumber;
        return Result.Success();
    }

    /// <summary>Associates the contact with a customer.</summary>
    /// <param name="customerId">The customer identifier.</param>
    public Result AssignToCustomer(CustomerId customerId)
    {
        if (CustomerId is not null && CustomerId == customerId)
        {
            return Result.Failure(CrmErrors.ContactAlreadyAssigned);
        }

        CustomerId = customerId;
        return Result.Success();
    }

    /// <summary>Removes the association with the customer.</summary>
    public Result RemoveFromCustomer()
    {
        if (CustomerId is null)
        {
            return Result.Failure(CrmErrors.ContactNotAssigned);
        }

        CustomerId = null;
        return Result.Success();
    }

    /// <summary>Activates the contact.</summary>
    public Result Activate()
    {
        if (Status == ContactStatus.Active)
        {
            return Result.Failure(CrmErrors.ContactAlreadyActive);
        }

        Status = ContactStatus.Active;
        return Result.Success();
    }

    /// <summary>Deactivates the contact.</summary>
    public Result Deactivate()
    {
        if (Status == ContactStatus.Inactive)
        {
            return Result.Failure(CrmErrors.ContactAlreadyInactive);
        }

        Status = ContactStatus.Inactive;
        return Result.Success();
    }
}
