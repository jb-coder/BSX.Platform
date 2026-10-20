using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Dtos;
using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.Modules.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Crm.Infrastructure.Repositories;

/// <summary>Projection-based organization reads; list queries never load aggregates.</summary>
internal sealed class OrganizationReadRepository : IOrganizationReadRepository
{
    private const int DefaultSize = 20;
    private const int MaxSize = 100;

    private readonly CrmDbContext _context;

    public OrganizationReadRepository(CrmDbContext context) => _context = context;

    public async Task<OrganizationDto?> GetByIdAsync(
        Guid tenantId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        TenantId tenant = TenantId.From(tenantId);
        OrganizationId id = OrganizationId.From(organizationId);

        var row = await _context.Organizations
            .AsNoTracking()
            .Where(organization => organization.TenantId == tenant && organization.Id == id)
            .Select(organization => new
            {
                organization.Id,
                organization.TenantId,
                organization.Name,
                organization.TaxIdentifier,
                organization.Address,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return row is null
            ? null
            : new OrganizationDto(
                row.Id.Value,
                row.TenantId.Value,
                row.Name.Value,
                row.TaxIdentifier?.Value,
                ToAddressDto(row.Address));
    }

    public async Task<PagedResult<OrganizationDto>> ListAsync(
        Guid tenantId,
        int page,
        int size,
        string? search,
        CancellationToken cancellationToken = default)
    {
        int normalizedPage = page < 1 ? 1 : page;
        int normalizedSize = size is < 1 or > MaxSize ? DefaultSize : size;
        TenantId tenant = TenantId.From(tenantId);

        IQueryable<Organization> query = _context.Organizations
            .AsNoTracking()
            .Where(organization => organization.TenantId == tenant);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string pattern = $"%{search.Trim().ToLowerInvariant()}%";
            query = query.Where(organization =>
                EF.Functions.Like(EF.Property<string>(organization, "SearchName"), pattern)
                || EF.Functions.Like(EF.Property<string>(organization, "SearchTaxIdentifier"), pattern));
        }

        int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var rows = await query
            .OrderBy(organization => EF.Property<string>(organization, "SearchName"))
            .ThenBy(organization => organization.Id)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .Select(organization => new
            {
                organization.Id,
                organization.TenantId,
                organization.Name,
                organization.TaxIdentifier,
                organization.Address,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<OrganizationDto> items = rows
            .Select(row => new OrganizationDto(
                row.Id.Value,
                row.TenantId.Value,
                row.Name.Value,
                row.TaxIdentifier?.Value,
                ToAddressDto(row.Address)))
            .ToList();

        return new PagedResult<OrganizationDto>(items, normalizedPage, normalizedSize, total);
    }

    private static AddressDto? ToAddressDto(Address? address)
        => address is null
            ? null
            : new AddressDto(
                Guid.Empty,
                address.Line1,
                address.Line2,
                address.City,
                address.Region,
                address.PostalCode,
                address.Country,
                "Other",
                false);
}
