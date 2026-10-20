using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Domain.Contacts;

/// <summary>Persistence contract for the <see cref="Contact"/> aggregate (aggregate-specific).</summary>
public interface IContactRepository
{
    /// <summary>Gets a contact by identifier.</summary>
    /// <param name="id">The contact identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<Contact?> GetByIdAsync(ContactId id, CancellationToken cancellationToken = default);

    /// <summary>Adds a new contact.</summary>
    /// <param name="contact">The contact.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(Contact contact, CancellationToken cancellationToken = default);
}
