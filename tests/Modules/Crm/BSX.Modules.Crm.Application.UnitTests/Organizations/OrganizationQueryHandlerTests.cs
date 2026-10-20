using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Dtos;
using BSX.Modules.Crm.Application.Errors;
using BSX.Modules.Crm.Application.Organizations.GetOrganizationById;
using BSX.Modules.Crm.Application.Organizations.ListOrganizations;
using BSX.Modules.Crm.Application.UnitTests.Fakes;

namespace BSX.Modules.Crm.Application.UnitTests.Organizations;

public sealed class OrganizationQueryHandlerTests
{
    private readonly FakeOrganizationReadRepository _organizations = new();

    [Fact]
    public async Task Should_GetOrganizationById_UsingTenantScope()
    {
        Guid organizationId = Guid.NewGuid();
        var dto = new OrganizationDto(organizationId, TestData.Tenant, "Holding Group", null, null);
        _organizations.Organization = dto;

        var handler = new GetOrganizationByIdQueryHandler(_organizations, TenantResolver());
        var result = await handler.HandleAsync(new GetOrganizationByIdQuery(organizationId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dto);
        _organizations.LastTenantId.Should().Be(TestData.Tenant);
    }

    [Fact]
    public async Task Should_Fail_WhenOrganizationProjectionNotFound()
    {
        var handler = new GetOrganizationByIdQueryHandler(_organizations, TenantResolver());
        var result = await handler.HandleAsync(new GetOrganizationByIdQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmApplicationErrors.OrganizationNotFound);
    }

    [Fact]
    public async Task Should_ListOrganizations_WithTenantAndNormalizedPaging()
    {
        var handler = new ListOrganizationsQueryHandler(_organizations, TenantResolver());

        var result = await handler.HandleAsync(new ListOrganizationsQuery(-1, 0, "group"));

        result.IsSuccess.Should().BeTrue();
        _organizations.LastTenantId.Should().Be(TestData.Tenant);
        _organizations.LastPage.Should().Be(1);
        _organizations.LastSize.Should().Be(20);
        _organizations.LastSearch.Should().Be("group");
    }

    private CrmTenantResolver TenantResolver() => new(new FakeTenantContext(TestData.Tenant));
}
