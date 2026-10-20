using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Customers.GetCustomerById;
using BSX.Modules.Crm.Application.Customers.ListCustomers;
using BSX.Modules.Crm.Application.Dtos;
using BSX.Modules.Crm.Application.Errors;
using BSX.Modules.Crm.Application.UnitTests.Fakes;

namespace BSX.Modules.Crm.Application.UnitTests.Customers;

public sealed class CustomerQueryHandlerTests
{
    private readonly FakeCustomerReadRepository _customers = new();

    [Fact]
    public async Task Should_GetCustomerById_UsingTenantScope()
    {
        Guid customerId = Guid.NewGuid();
        var dto = new CustomerDto(
            customerId,
            TestData.Tenant,
            "CUST-001",
            "Acme",
            null,
            null,
            "Active",
            [],
            []);
        _customers.Customer = dto;

        var handler = new GetCustomerByIdQueryHandler(_customers, TenantResolver());
        var result = await handler.HandleAsync(new GetCustomerByIdQuery(customerId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dto);
        _customers.LastTenantId.Should().Be(TestData.Tenant);
    }

    [Fact]
    public async Task Should_Fail_WhenCustomerProjectionNotFound()
    {
        var handler = new GetCustomerByIdQueryHandler(_customers, TenantResolver());
        var result = await handler.HandleAsync(new GetCustomerByIdQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmApplicationErrors.CustomerNotFound);
    }

    [Fact]
    public async Task Should_Fail_WhenCustomerIdIsEmpty()
    {
        var handler = new GetCustomerByIdQueryHandler(_customers, TenantResolver());
        var result = await handler.HandleAsync(new GetCustomerByIdQuery(Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmApplicationErrors.InvalidCustomerId);
    }

    [Fact]
    public async Task Should_Fail_WhenTenantIsMissing()
    {
        var handler = new GetCustomerByIdQueryHandler(
            _customers,
            new CrmTenantResolver(new FakeTenantContext(null)));

        var result = await handler.HandleAsync(new GetCustomerByIdQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmApplicationErrors.TenantRequired);
    }

    [Fact]
    public async Task Should_ListCustomers_WithTenantAndNormalizedPaging()
    {
        var handler = new ListCustomersQueryHandler(_customers, TenantResolver());

        var result = await handler.HandleAsync(new ListCustomersQuery(0, 500, "acme"));

        result.IsSuccess.Should().BeTrue();
        _customers.LastTenantId.Should().Be(TestData.Tenant);
        _customers.LastPage.Should().Be(1);
        _customers.LastSize.Should().Be(20);
        _customers.LastSearch.Should().Be("acme");
    }

    [Fact]
    public async Task Should_ListCustomers_WithRequestedPaging()
    {
        var handler = new ListCustomersQueryHandler(_customers, TenantResolver());

        await handler.HandleAsync(new ListCustomersQuery(3, 50, null));

        _customers.LastPage.Should().Be(3);
        _customers.LastSize.Should().Be(50);
    }

    private CrmTenantResolver TenantResolver() => new(new FakeTenantContext(TestData.Tenant));
}
