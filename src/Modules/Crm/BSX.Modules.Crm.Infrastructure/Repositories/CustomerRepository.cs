using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.Modules.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Crm.Infrastructure.Repositories;

internal sealed class CustomerRepository : ICustomerRepository
{
    private readonly CrmDbContext _context;

    public CustomerRepository(CrmDbContext context) => _context = context;

    public Task<Customer?> GetByIdAsync(
        TenantId tenantId,
        CustomerId id,
        CancellationToken cancellationToken = default)
        => _context.Customers
            .Include(customer => customer.Addresses)
            .Include(customer => customer.Contacts)
            .FirstOrDefaultAsync(
                customer => customer.Id == id && customer.TenantId == tenantId,
                cancellationToken);

    public Task<bool> ExistsByCodeAsync(
        TenantId tenantId,
        CustomerCode code,
        CancellationToken cancellationToken = default)
        => _context.Customers.AnyAsync(
            customer => customer.TenantId == tenantId && customer.Code == code,
            cancellationToken);

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.Customers.AddAsync(customer, cancellationToken).ConfigureAwait(false);
    }
}
