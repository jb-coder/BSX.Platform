using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Errors;
using BSX.Modules.Crm.Application.Organizations.CreateOrganization;
using BSX.Modules.Crm.Application.Organizations.UpdateOrganization;
using BSX.Modules.Crm.Application.UnitTests.Fakes;
using BSX.Modules.Crm.Domain.Organizations;

namespace BSX.Modules.Crm.Application.UnitTests.Organizations;

public sealed class OrganizationCommandHandlerTests
{
    private readonly InMemoryOrganizationRepository _organizations = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Should_CreateOrganization_AndPersist()
    {
        var handler = new CreateOrganizationCommandHandler(_organizations, _unitOfWork, TenantResolver());

        var result = await handler.HandleAsync(
            new CreateOrganizationCommand("Holding Group", "TAX-1", TestData.AddressInput()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _organizations.Organizations.Should().ContainSingle();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenTenantIsMissing()
    {
        var handler = new CreateOrganizationCommandHandler(
            _organizations,
            _unitOfWork,
            new CrmTenantResolver(new FakeTenantContext(null)));

        var result = await handler.HandleAsync(new CreateOrganizationCommand("Holding Group", null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmApplicationErrors.TenantRequired);
    }

    [Fact]
    public async Task Should_UpdateOrganization()
    {
        Guid organizationId = Guid.NewGuid();
        Organization organization = TestData.CreateOrganization(id: organizationId);
        _organizations.Seed(organization);

        var handler = new UpdateOrganizationCommandHandler(_organizations, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(
            new UpdateOrganizationCommand(
                organizationId,
                "Renamed Group",
                "TAX-9",
                TestData.AddressInput("5 Corporate Way")));

        result.IsSuccess.Should().BeTrue();
        organization.Name.Value.Should().Be("Renamed Group");
        organization.TaxIdentifier!.Value.Should().Be("TAX-9");
        organization.Address!.Line1.Should().Be("5 Corporate Way");
        _organizations.LastRequestedTenant.Should().Be(TestData.TenantValue());
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenUpdatingUnknownOrganization()
    {
        var handler = new UpdateOrganizationCommandHandler(_organizations, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(
            new UpdateOrganizationCommand(Guid.NewGuid(), "Renamed", null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmApplicationErrors.OrganizationNotFound);
    }

    [Fact]
    public async Task Should_Fail_WhenOrganizationIdIsEmpty()
    {
        var handler = new UpdateOrganizationCommandHandler(_organizations, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(new UpdateOrganizationCommand(Guid.Empty, "Renamed", null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmApplicationErrors.InvalidOrganizationId);
    }

    private CrmTenantResolver TenantResolver() => new(new FakeTenantContext(TestData.Tenant));
}
