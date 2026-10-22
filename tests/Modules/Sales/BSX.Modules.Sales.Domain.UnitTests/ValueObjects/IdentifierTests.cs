using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Domain.UnitTests.ValueObjects;

public sealed class IdentifierTests
{
    [Fact]
    public void LeadId_Should_RoundTrip()
    {
        Guid value = Guid.NewGuid();

        LeadId.From(value).Value.Should().Be(value);
        LeadId.New().Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void OpportunityId_Should_RoundTrip()
    {
        Guid value = Guid.NewGuid();

        OpportunityId.From(value).Value.Should().Be(value);
        OpportunityId.New().Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void IntegrationReferences_Should_RoundTrip()
    {
        Guid value = Guid.NewGuid();

        CustomerId.From(value).Value.Should().Be(value);
        OrganizationId.From(value).Value.Should().Be(value);
        TenantId.From(value).Value.Should().Be(value);
        TenantId.New().Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Identifiers_Should_RejectEmptyValues()
    {
        Action lead = () => LeadId.From(Guid.Empty);
        Action opportunity = () => OpportunityId.From(Guid.Empty);
        Action customer = () => CustomerId.From(Guid.Empty);
        Action organization = () => OrganizationId.From(Guid.Empty);
        Action tenant = () => TenantId.From(Guid.Empty);

        lead.Should().Throw<ArgumentException>();
        opportunity.Should().Throw<ArgumentException>();
        customer.Should().Throw<ArgumentException>();
        organization.Should().Throw<ArgumentException>();
        tenant.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Identifiers_Should_BeTypeSensitive()
    {
        Guid value = Guid.NewGuid();

        LeadId.From(value).Should().NotBe(OpportunityId.From(value));
    }
}
