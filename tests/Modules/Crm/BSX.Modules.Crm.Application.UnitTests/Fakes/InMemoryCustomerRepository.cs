using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Application.UnitTests.Fakes;

internal sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly List<Customer> _customers = [];

    public IReadOnlyCollection<Customer> Customers => _customers;

    public TenantId? LastRequestedTenant { get; private set; }

    public void Seed(params Customer[] customers) => _customers.AddRange(customers);

    public Task<Customer?> GetByIdAsync(TenantId tenantId, CustomerId id, CancellationToken cancellationToken = default)
    {
        LastRequestedTenant = tenantId;
        return Task.FromResult(_customers.Find(customer => customer.Id == id && customer.TenantId == tenantId));
    }

    public Task<bool> ExistsByCodeAsync(TenantId tenantId, CustomerCode code, CancellationToken cancellationToken = default)
    {
        LastRequestedTenant = tenantId;
        return Task.FromResult(_customers.Exists(customer => customer.TenantId == tenantId && customer.Code == code));
    }

    public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _customers.Add(customer);
        return Task.CompletedTask;
    }
}
