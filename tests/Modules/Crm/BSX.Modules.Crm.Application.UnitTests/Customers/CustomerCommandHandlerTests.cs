using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Customers.ActivateCustomer;
using BSX.Modules.Crm.Application.Customers.AssignOrganization;
using BSX.Modules.Crm.Application.Customers.ClearOrganization;
using BSX.Modules.Crm.Application.Customers.CreateCustomer;
using BSX.Modules.Crm.Application.Customers.DeactivateCustomer;
using BSX.Modules.Crm.Application.Customers.UpdateCustomer;
using BSX.Modules.Crm.Application.Errors;
using BSX.Modules.Crm.Application.UnitTests.Fakes;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Enums;
using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Application.UnitTests.Customers;

public sealed class CustomerCommandHandlerTests
{
    private readonly InMemoryCustomerRepository _customers = new();
    private readonly InMemoryOrganizationRepository _organizations = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Should_CreateCustomer_AndPersist()
    {
        CreateCustomerCommandHandler handler = CreateHandler();

        var result = await handler.HandleAsync(new CreateCustomerCommand("cust-001", "Acme Corporation", "TAX-1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _customers.Customers.Should().ContainSingle();
        _customers.LastRequestedTenant.Should().Be(TestData.TenantValue());
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenCustomerCodeAlreadyExists()
    {
        _customers.Seed(TestData.CreateCustomer(code: "CUST-001"));

        var result = await CreateHandler().HandleAsync(new CreateCustomerCommand("CUST-001", "Other", null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmApplicationErrors.CustomerCodeAlreadyExists);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenTenantIsMissing()
    {
        var handler = new CreateCustomerCommandHandler(
            _customers,
            _unitOfWork,
            new CrmTenantResolver(new FakeTenantContext(null)));

        var result = await handler.HandleAsync(new CreateCustomerCommand("CUST-001", "Acme", null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmApplicationErrors.TenantRequired);
    }

    [Fact]
    public async Task Should_Fail_WhenCustomerCodeIsInvalid()
    {
        var result = await CreateHandler().HandleAsync(new CreateCustomerCommand("bad#code", "Acme", null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CRM.CustomerCode.Invalid");
    }

    [Fact]
    public async Task Should_UpdateCustomer()
    {
        Guid customerId = Guid.NewGuid();
        Customer customer = TestData.CreateCustomer(id: customerId);
        _customers.Seed(customer);

        var result = await UpdateHandler().HandleAsync(new UpdateCustomerCommand(customerId, "Renamed", "TAX-9"));

        result.IsSuccess.Should().BeTrue();
        customer.Name.Value.Should().Be("Renamed");
        customer.TaxIdentifier!.Value.Should().Be("TAX-9");
        _customers.LastRequestedTenant.Should().Be(TestData.TenantValue());
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenUpdatingUnknownCustomer()
    {
        var result = await UpdateHandler().HandleAsync(new UpdateCustomerCommand(Guid.NewGuid(), "Renamed", null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmApplicationErrors.CustomerNotFound);
    }

    [Fact]
    public async Task Should_Fail_WhenCustomerIdIsEmpty()
    {
        var result = await UpdateHandler().HandleAsync(new UpdateCustomerCommand(Guid.Empty, "Renamed", null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmApplicationErrors.InvalidCustomerId);
    }

    [Fact]
    public async Task Should_ActivateCustomer()
    {
        Guid customerId = Guid.NewGuid();
        Customer customer = TestData.CreateCustomer(id: customerId);
        customer.Deactivate().IsSuccess.Should().BeTrue();
        _customers.Seed(customer);

        var handler = new ActivateCustomerCommandHandler(_customers, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(new ActivateCustomerCommand(customerId));

        result.IsSuccess.Should().BeTrue();
        customer.Status.Should().Be(CustomerStatus.Active);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_DeactivateCustomer()
    {
        Guid customerId = Guid.NewGuid();
        Customer customer = TestData.CreateCustomer(id: customerId);
        _customers.Seed(customer);

        var handler = new DeactivateCustomerCommandHandler(_customers, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(new DeactivateCustomerCommand(customerId));

        result.IsSuccess.Should().BeTrue();
        customer.Status.Should().Be(CustomerStatus.Deactivated);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_AssignOrganization_WithinTenant()
    {
        Guid customerId = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();
        Customer customer = TestData.CreateCustomer(id: customerId);
        Organization organization = TestData.CreateOrganization(id: organizationId);
        _customers.Seed(customer);
        _organizations.Seed(organization);

        var handler = new AssignOrganizationCommandHandler(
            _customers,
            _organizations,
            _unitOfWork,
            TenantResolver());

        var result = await handler.HandleAsync(new AssignOrganizationCommand(customerId, organizationId));

        result.IsSuccess.Should().BeTrue();
        customer.OrganizationId.Should().Be(organization.Id);
        _customers.LastRequestedTenant.Should().Be(TestData.TenantValue());
        _organizations.LastRequestedTenant.Should().Be(TestData.TenantValue());
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenOrganizationNotFound()
    {
        Guid customerId = Guid.NewGuid();
        _customers.Seed(TestData.CreateCustomer(id: customerId));

        var handler = new AssignOrganizationCommandHandler(
            _customers,
            _organizations,
            _unitOfWork,
            TenantResolver());

        var result = await handler.HandleAsync(new AssignOrganizationCommand(customerId, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmApplicationErrors.OrganizationNotFound);
    }

    [Fact]
    public async Task Should_ClearOrganization()
    {
        Guid customerId = Guid.NewGuid();
        Customer customer = TestData.CreateCustomer(id: customerId);
        customer.AssignOrganization(OrganizationId.New()).IsSuccess.Should().BeTrue();
        customer.ClearDomainEvents();
        _customers.Seed(customer);

        var handler = new ClearOrganizationCommandHandler(_customers, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(new ClearOrganizationCommand(customerId));

        result.IsSuccess.Should().BeTrue();
        customer.OrganizationId.Should().BeNull();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenClearingOrganization_ButNotAssigned()
    {
        Guid customerId = Guid.NewGuid();
        _customers.Seed(TestData.CreateCustomer(id: customerId));

        var handler = new ClearOrganizationCommandHandler(_customers, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(new ClearOrganizationCommand(customerId));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CRM.Customer.OrganizationNotAssigned");
    }

    private CrmTenantResolver TenantResolver() => new(new FakeTenantContext(TestData.Tenant));

    private CreateCustomerCommandHandler CreateHandler()
        => new(_customers, _unitOfWork, TenantResolver());

    private UpdateCustomerCommandHandler UpdateHandler()
        => new(_customers, _unitOfWork, TenantResolver());
}
