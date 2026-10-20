using BSX.Modules.Crm.Domain.DomainEvents;
using BSX.Modules.Crm.Domain.Enums;
using BSX.Modules.Crm.Domain.Errors;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Domain.Customers;

/// <summary>
/// The customer aggregate: the consistency boundary for a customer's addresses and contact persons.
/// Addresses and contact persons are owned entities; other aggregates are referenced by identifier
/// only (ADR-003, ADR-013).
/// </summary>
public sealed class Customer : AggregateRoot<CustomerId>
{
    /// <summary>Maximum number of addresses a customer may have.</summary>
    public const int MaxAddresses = 20;

    /// <summary>Maximum number of contact persons a customer may have.</summary>
    public const int MaxContacts = 50;

    private readonly List<CustomerAddress> _addresses = [];
    private readonly List<ContactPerson> _contacts = [];

    private Customer(
        CustomerId id,
        TenantId tenantId,
        CustomerCode code,
        CustomerName name,
        TaxIdentifier? taxIdentifier)
        : base(id)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
        TaxIdentifier = taxIdentifier;
        Status = CustomerStatus.Active;
    }

    private Customer()
    {
    }

    /// <summary>Gets the reserved tenant identifier (ADR-013).</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Gets the unique customer code.</summary>
    public CustomerCode Code { get; private set; } = null!;

    /// <summary>Gets the customer name.</summary>
    public CustomerName Name { get; private set; } = null!;

    /// <summary>Gets the optional tax identifier.</summary>
    public TaxIdentifier? TaxIdentifier { get; private set; }

    /// <summary>Gets the assigned organization, if any (referenced by identifier).</summary>
    public OrganizationId? OrganizationId { get; private set; }

    /// <summary>Gets the lifecycle status.</summary>
    public CustomerStatus Status { get; private set; }

    /// <summary>Gets the owned addresses.</summary>
    public IReadOnlyCollection<CustomerAddress> Addresses => _addresses.AsReadOnly();

    /// <summary>Gets the owned contact persons.</summary>
    public IReadOnlyCollection<ContactPerson> Contacts => _contacts.AsReadOnly();

    /// <summary>Creates an active customer.</summary>
    /// <param name="id">The customer identifier.</param>
    /// <param name="tenantId">The reserved tenant identifier.</param>
    /// <param name="code">The unique customer code.</param>
    /// <param name="name">The customer name.</param>
    /// <param name="taxIdentifier">The optional tax identifier.</param>
    public static Result<Customer> Create(
        CustomerId id,
        TenantId tenantId,
        CustomerCode code,
        CustomerName name,
        TaxIdentifier? taxIdentifier = null)
    {
        var customer = new Customer(id, tenantId, code, name, taxIdentifier);
        customer.RaiseDomainEvent(new CustomerCreated(id, code));
        return Result.Success(customer);
    }

    /// <summary>Reactivates a deactivated customer.</summary>
    public Result Activate()
    {
        if (Status == CustomerStatus.Active)
        {
            return Result.Failure(CrmErrors.CustomerAlreadyActive);
        }

        Status = CustomerStatus.Active;
        RaiseDomainEvent(new CustomerActivated(Id));
        return Result.Success();
    }

    /// <summary>Deactivates an active customer.</summary>
    public Result Deactivate()
    {
        if (Status == CustomerStatus.Deactivated)
        {
            return Result.Failure(CrmErrors.CustomerAlreadyDeactivated);
        }

        Status = CustomerStatus.Deactivated;
        RaiseDomainEvent(new CustomerDeactivated(Id));
        return Result.Success();
    }

    /// <summary>Changes the customer name.</summary>
    /// <param name="name">The new name.</param>
    public Result ChangeName(CustomerName name)
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

    /// <summary>Assigns the customer to an organization (referenced by identifier).</summary>
    /// <param name="organizationId">The organization identifier.</param>
    public Result AssignOrganization(OrganizationId organizationId)
    {
        if (Status != CustomerStatus.Active)
        {
            return Result.Failure(CrmErrors.CustomerNotActive);
        }

        if (OrganizationId is not null && OrganizationId == organizationId)
        {
            return Result.Failure(CrmErrors.OrganizationAlreadyAssigned);
        }

        OrganizationId = organizationId;
        RaiseDomainEvent(new OrganizationAssigned(Id, organizationId));
        return Result.Success();
    }

    /// <summary>Detaches the customer from its organization. Allowed in any status.</summary>
    public Result ClearOrganization()
    {
        if (OrganizationId is null)
        {
            return Result.Failure(CrmErrors.OrganizationNotAssigned);
        }

        OrganizationId = null;
        RaiseDomainEvent(new OrganizationUnassigned(Id));
        return Result.Success();
    }

    /// <summary>
    /// Adds an address. Uniqueness is enforced per <c>(address, type)</c> so the same physical
    /// address may be used for multiple purposes. The first address becomes the default.
    /// </summary>
    /// <param name="id">The address identifier.</param>
    /// <param name="address">The postal address.</param>
    /// <param name="type">The address purpose.</param>
    /// <param name="isDefault">Whether the address should become the default.</param>
    public Result AddAddress(CustomerAddressId id, Address address, AddressType type, bool isDefault = false)
    {
        if (_addresses.Count >= MaxAddresses)
        {
            return Result.Failure(CrmErrors.AddressLimitReached);
        }

        if (_addresses.Any(existing => existing.Address.Equals(address) && existing.Type == type))
        {
            return Result.Failure(CrmErrors.AddressDuplicate);
        }

        bool becomesDefault = isDefault || _addresses.Count == 0;

        if (becomesDefault)
        {
            foreach (CustomerAddress existing in _addresses)
            {
                existing.ClearDefault();
            }
        }

        _addresses.Add(new CustomerAddress(id, address, type, becomesDefault));
        return Result.Success();
    }

    /// <summary>Removes an address, promoting another address to default when necessary.</summary>
    /// <param name="addressId">The address identifier.</param>
    public Result RemoveAddress(CustomerAddressId addressId)
    {
        CustomerAddress? address = _addresses.Find(existing => existing.Id == addressId);

        if (address is null)
        {
            return Result.Failure(CrmErrors.AddressNotFound);
        }

        _addresses.Remove(address);

        if (address.IsDefault && _addresses.Count > 0)
        {
            _addresses[0].MarkAsDefault();
        }

        return Result.Success();
    }

    /// <summary>Marks an address as the customer's default address.</summary>
    /// <param name="addressId">The address identifier.</param>
    public Result SetDefaultAddress(CustomerAddressId addressId)
    {
        CustomerAddress? address = _addresses.Find(existing => existing.Id == addressId);

        if (address is null)
        {
            return Result.Failure(CrmErrors.AddressNotFound);
        }

        foreach (CustomerAddress existing in _addresses)
        {
            existing.ClearDefault();
        }

        address.MarkAsDefault();
        return Result.Success();
    }

    /// <summary>Changes the purpose of an address, enforcing <c>(address, type)</c> uniqueness.</summary>
    /// <param name="addressId">The address identifier.</param>
    /// <param name="type">The new purpose.</param>
    public Result ChangeAddressType(CustomerAddressId addressId, AddressType type)
    {
        CustomerAddress? address = _addresses.Find(existing => existing.Id == addressId);

        if (address is null)
        {
            return Result.Failure(CrmErrors.AddressNotFound);
        }

        bool conflict = _addresses.Any(existing => existing.Id != addressId
            && existing.Address.Equals(address.Address)
            && existing.Type == type);

        if (conflict)
        {
            return Result.Failure(CrmErrors.AddressDuplicate);
        }

        address.ChangeType(type);
        return Result.Success();
    }

    /// <summary>
    /// Adds a contact person. A contact must have at least one reachability channel (email or
    /// phone). The first contact becomes the primary contact automatically.
    /// </summary>
    /// <param name="id">The contact person identifier.</param>
    /// <param name="name">The person's name.</param>
    /// <param name="email">The optional email address.</param>
    /// <param name="phoneNumber">The optional phone number.</param>
    /// <param name="isPrimary">Whether the contact should become the primary contact.</param>
    public Result AddContact(
        ContactPersonId id,
        CustomerName name,
        Email? email = null,
        PhoneNumber? phoneNumber = null,
        bool isPrimary = false)
    {
        if (Status != CustomerStatus.Active)
        {
            return Result.Failure(CrmErrors.CustomerNotActive);
        }

        if (_contacts.Count >= MaxContacts)
        {
            return Result.Failure(CrmErrors.ContactLimitReached);
        }

        if (email is null && phoneNumber is null)
        {
            return Result.Failure(CrmErrors.ContactUnreachable);
        }

        if (email is not null && _contacts.Any(existing => existing.Email is not null && existing.Email.Equals(email)))
        {
            return Result.Failure(CrmErrors.ContactEmailDuplicate);
        }

        bool becomesPrimary = isPrimary || _contacts.Count == 0;

        if (becomesPrimary)
        {
            foreach (ContactPerson existing in _contacts)
            {
                existing.ClearPrimary();
            }
        }

        _contacts.Add(new ContactPerson(id, name, email, phoneNumber, becomesPrimary));
        RaiseDomainEvent(new ContactAdded(Id, id));
        return Result.Success();
    }

    /// <summary>Removes a contact person, promoting another contact to primary when necessary.</summary>
    /// <param name="contactId">The contact person identifier.</param>
    public Result RemoveContact(ContactPersonId contactId)
    {
        ContactPerson? contact = _contacts.Find(existing => existing.Id == contactId);

        if (contact is null)
        {
            return Result.Failure(CrmErrors.ContactNotFound);
        }

        _contacts.Remove(contact);

        if (contact.IsPrimary && _contacts.Count > 0)
        {
            _contacts[0].MarkPrimary();
        }

        RaiseDomainEvent(new ContactRemoved(Id, contactId));
        return Result.Success();
    }

    /// <summary>Marks a contact person as the customer's primary contact.</summary>
    /// <param name="contactId">The contact person identifier.</param>
    public Result SetPrimaryContact(ContactPersonId contactId)
    {
        ContactPerson? contact = _contacts.Find(existing => existing.Id == contactId);

        if (contact is null)
        {
            return Result.Failure(CrmErrors.ContactNotFound);
        }

        foreach (ContactPerson existing in _contacts)
        {
            existing.ClearPrimary();
        }

        contact.MarkPrimary();
        return Result.Success();
    }

    /// <summary>Changes a contact person's details, requiring at least one reachability channel.</summary>
    /// <param name="contactId">The contact person identifier.</param>
    /// <param name="name">The new name.</param>
    /// <param name="email">The new email, if any.</param>
    /// <param name="phoneNumber">The new phone number, if any.</param>
    public Result ChangeContactDetails(
        ContactPersonId contactId,
        CustomerName name,
        Email? email = null,
        PhoneNumber? phoneNumber = null)
    {
        ContactPerson? contact = _contacts.Find(existing => existing.Id == contactId);

        if (contact is null)
        {
            return Result.Failure(CrmErrors.ContactNotFound);
        }

        if (email is null && phoneNumber is null)
        {
            return Result.Failure(CrmErrors.ContactUnreachable);
        }

        bool duplicateEmail = email is not null
            && _contacts.Any(existing => existing.Id != contactId
                && existing.Email is not null
                && existing.Email.Equals(email));

        if (duplicateEmail)
        {
            return Result.Failure(CrmErrors.ContactEmailDuplicate);
        }

        contact.ChangeDetails(name, email, phoneNumber);
        return Result.Success();
    }
}
