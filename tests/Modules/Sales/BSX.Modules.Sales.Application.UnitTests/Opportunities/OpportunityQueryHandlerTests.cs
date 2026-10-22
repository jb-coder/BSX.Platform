using BSX.Modules.Sales.Application.Abstractions;
using BSX.Modules.Sales.Application.Dtos;
using BSX.Modules.Sales.Application.Errors;
using BSX.Modules.Sales.Application.Opportunities.GetOpportunityById;
using BSX.Modules.Sales.Application.Opportunities.ListOpportunities;
using BSX.Modules.Sales.Application.UnitTests.Fakes;

namespace BSX.Modules.Sales.Application.UnitTests.Opportunities;

public sealed class OpportunityQueryHandlerTests
{
    private readonly FakeOpportunityReadRepository _opportunities = new();

    [Fact]
    public async Task Should_GetOpportunityById_UsingTenantScope()
    {
        Guid opportunityId = Guid.NewGuid();
        OpportunityDto dto = TestData.OpportunityDto(id: opportunityId);
        _opportunities.Opportunity = dto;

        var handler = new GetOpportunityByIdQueryHandler(_opportunities, TenantResolver());
        var result = await handler.HandleAsync(new GetOpportunityByIdQuery(opportunityId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dto);
        _opportunities.LastTenantId.Should().Be(TestData.Tenant);
    }

    [Fact]
    public async Task Should_Fail_WhenOpportunityProjectionNotFound()
    {
        var handler = new GetOpportunityByIdQueryHandler(_opportunities, TenantResolver());
        var result = await handler.HandleAsync(new GetOpportunityByIdQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.OpportunityNotFound);
    }

    [Fact]
    public async Task Should_Fail_WhenOpportunityIdIsEmpty()
    {
        var handler = new GetOpportunityByIdQueryHandler(_opportunities, TenantResolver());
        var result = await handler.HandleAsync(new GetOpportunityByIdQuery(Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.InvalidOpportunityId);
    }

    [Fact]
    public async Task Should_Fail_WhenTenantIsMissing()
    {
        var handler = new GetOpportunityByIdQueryHandler(
            _opportunities,
            new SalesTenantResolver(new FakeTenantContext(null)));

        var result = await handler.HandleAsync(new GetOpportunityByIdQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.TenantRequired);
    }

    [Fact]
    public async Task Should_ListOpportunities_WithTenantAndNormalizedPaging()
    {
        var handler = new ListOpportunitiesQueryHandler(_opportunities, TenantResolver());

        var result = await handler.HandleAsync(new ListOpportunitiesQuery(0, 500, "enterprise"));

        result.IsSuccess.Should().BeTrue();
        _opportunities.LastTenantId.Should().Be(TestData.Tenant);
        _opportunities.LastPage.Should().Be(1);
        _opportunities.LastSize.Should().Be(20);
        _opportunities.LastSearch.Should().Be("enterprise");
    }

    [Fact]
    public async Task Should_ListOpportunities_WithRequestedPaging()
    {
        var handler = new ListOpportunitiesQueryHandler(_opportunities, TenantResolver());

        await handler.HandleAsync(new ListOpportunitiesQuery(3, 50, null));

        _opportunities.LastPage.Should().Be(3);
        _opportunities.LastSize.Should().Be(50);
    }

    [Fact]
    public async Task Should_Fail_To_ListOpportunities_WhenTenantIsMissing()
    {
        var handler = new ListOpportunitiesQueryHandler(
            _opportunities,
            new SalesTenantResolver(new FakeTenantContext(null)));

        var result = await handler.HandleAsync(new ListOpportunitiesQuery(1, 20, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesApplicationErrors.TenantRequired);
    }

    private SalesTenantResolver TenantResolver() => new(new FakeTenantContext(TestData.Tenant));
}
