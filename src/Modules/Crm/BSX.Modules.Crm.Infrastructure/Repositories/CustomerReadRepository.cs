using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Dtos;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.Modules.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Crm.Infrastructure.Repositories;

/// <summary>Projection-based customer reads; list queries never load aggregates.</summary>
internal sealed class CustomerReadRepository : ICustomerReadRepository
{
    private const int DefaultSize = 20;
    private const int MaxSize = 100;

    private readonly CrmDbContext _context;

    public CustomerReadRepository(CrmDbContext context) => _context = context;

    public async Task<CustomerDto?> GetByIdAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        TenantId tenant = TenantId.From(tenantId);
        CustomerId id = CustomerId.From(customerId);

        var row = await _context.Customers
            .AsNoTracking()
            .AsSplitQuery()
            .Where(customer => customer.TenantId == tenant && customer.Id == id)
            .Select(customer => new
            {
                customer.Id,
                customer.TenantId,
                customer.Code,
                customer.Name,
                customer.TaxIdentifier,
                customer.OrganizationId,
                customer.Status,
                Addresses = customer.Addresses
                    .Select(address => new
                    {
                        address.Id,
                        address.Address,
                        address.Type,
                        address.IsDefault,
                    })
                    .ToList(),
                Contacts = customer.Contacts
                    .Select(contact => new
                    {
                        contact.Id,
                        contact.Name,
                        contact.Email,
                        contact.PhoneNumber,
                        contact.IsPrimary,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
        {
            return null;
        }

        List<AddressDto> addresses = row.Addresses
            .OrderByDescending(address => address.IsDefault)
            .Select(address => new AddressDto(
                address.Id.Value,
                address.Address.Line1,
                address.Address.Line2,
                address.Address.City,
                address.Address.Region,
                address.Address.PostalCode,
                address.Address.Country,
                address.Type.ToString(),
                address.IsDefault))
            .ToList();

        List<ContactDto> contacts = row.Contacts
            .OrderByDescending(contact => contact.IsPrimary)
            .Select(contact => new ContactDto(
                contact.Id.Value,
                contact.Name.Value,
                contact.Email?.Value,
                contact.PhoneNumber?.Value,
                contact.IsPrimary))
            .ToList();

        return new CustomerDto(
            row.Id.Value,
            row.TenantId.Value,
            row.Code.Value,
            row.Name.Value,
            row.TaxIdentifier?.Value,
            row.OrganizationId?.Value,
            row.Status.ToString(),
            addresses,
            contacts);
    }

    public async Task<PagedResult<CustomerSummaryDto>> ListAsync(
        Guid tenantId,
        int page,
        int size,
        string? search,
        CancellationToken cancellationToken = default)
    {
        int normalizedPage = page < 1 ? 1 : page;
        int normalizedSize = size is < 1 or > MaxSize ? DefaultSize : size;
        TenantId tenant = TenantId.From(tenantId);

        IQueryable<Customer> query = _context.Customers
            .AsNoTracking()
            .Where(customer => customer.TenantId == tenant);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string pattern = $"%{search.Trim().ToLowerInvariant()}%";
            query = query.Where(customer =>
                EF.Functions.Like(EF.Property<string>(customer, "SearchCode"), pattern)
                || EF.Functions.Like(EF.Property<string>(customer, "SearchName"), pattern));
        }

        int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var rows = await query
            .OrderBy(customer => EF.Property<string>(customer, "SearchCode"))
            .ThenBy(customer => customer.Id)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .Select(customer => new
            {
                customer.Id,
                customer.Code,
                customer.Name,
                customer.Status,
                customer.OrganizationId,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<CustomerSummaryDto> items = rows
            .Select(row => new CustomerSummaryDto(
                row.Id.Value,
                row.Code.Value,
                row.Name.Value,
                row.Status.ToString(),
                row.OrganizationId?.Value))
            .ToList();

        return new PagedResult<CustomerSummaryDto>(items, normalizedPage, normalizedSize, total);
    }
}
