using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Dtos;
using BSX.Modules.Sales.Application.Errors;
using BSX.Modules.Sales.Application.Leads.GetLeadById;
using BSX.Modules.Sales.Application.Leads.ListLeads;
using BSX.Modules.Sales.Application.UnitTests.Fakes;

namespace BSX.Modules.Sales.Application.UnitTests.Leads;

public sealed class LeadQueryHandlerTests
{
    private readonly FakeLeadReadRepository _leads = new();

    [Fact]
    public async Task Should_GetLeadById_UsingTenantScope()
    {
        Guid leadId = Guid.NewGuid();
        LeadDto dto = TestData.LeadDto(id: leadId);
        _leads.Lead = dto;

        var handler = new GetLeadByIdQueryHandler(_leads, TenantResolver());
        var result = await handler.HandleAsync(new GetLeadByIdQuery(leadId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dto);
        _leads.LastTenantId.Should().Be(TestData.Tenant);
    }

    [Fact]
    public async Task Should_Fail_WhenLeadProjectionNotFound()
    {
        var handler = new GetLeadByIdQueryHandler(_leads, TenantResolver());
        var result = await handler.HandleAsync(new GetLeadByIdQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.LeadNotFound);
    }

    [Fact]
    public async Task Should_Fail_WhenLeadIdIsEmpty()
    {
        var handler = new GetLeadByIdQueryHandler(_leads, TenantResolver());
        var result = await handler.HandleAsync(new GetLeadByIdQuery(Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.InvalidLeadId);
    }

    [Fact]
    public async Task Should_Fail_WhenTenantIsMissing()
    {
        var handler = new GetLeadByIdQueryHandler(
            _leads,
            new SalesTenantResolver(new FakeTenantContext(null)));

        var result = await handler.HandleAsync(new GetLeadByIdQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.TenantRequired);
    }

    [Fact]
    public async Task Should_ListLeads_WithTenantAndNormalizedPaging()
    {
        var handler = new ListLeadsQueryHandler(_leads, TenantResolver());

        var result = await handler.HandleAsync(new ListLeadsQuery(0, 500, "acme"));

        result.IsSuccess.Should().BeTrue();
        _leads.LastTenantId.Should().Be(TestData.Tenant);
        _leads.LastPage.Should().Be(1);
        _leads.LastSize.Should().Be(20);
        _leads.LastSearch.Should().Be("acme");
    }

    [Fact]
    public async Task Should_ListLeads_WithRequestedPaging()
    {
        var handler = new ListLeadsQueryHandler(_leads, TenantResolver());

        await handler.HandleAsync(new ListLeadsQuery(3, 50, null));

        _leads.LastPage.Should().Be(3);
        _leads.LastSize.Should().Be(50);
    }

    [Fact]
    public async Task Should_Fail_To_ListLeads_WhenTenantIsMissing()
    {
        var handler = new ListLeadsQueryHandler(
            _leads,
            new SalesTenantResolver(new FakeTenantContext(null)));

        var result = await handler.HandleAsync(new ListLeadsQuery(1, 20, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.TenantRequired);
    }

    private SalesTenantResolver TenantResolver() => new(new FakeTenantContext(TestData.Tenant));
}
