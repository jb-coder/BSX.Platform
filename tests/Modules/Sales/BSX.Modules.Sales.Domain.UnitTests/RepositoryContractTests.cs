using System.Reflection;
using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Domain.UnitTests;

/// <summary>
/// Locks tenant read isolation for Sales: aggregate read contracts take a <see cref="TenantId"/>,
/// so cross-tenant reads cannot be expressed by accident.
/// </summary>
public sealed class RepositoryContractTests
{
    [Fact]
    public void Lead_ReadContract_Should_RequireTenant()
    {
        MethodInfo method = typeof(ILeadRepository).GetMethod(nameof(ILeadRepository.GetByIdAsync))!;
        ParameterInfo[] parameters = method.GetParameters();

        parameters[0].ParameterType.Should().Be(typeof(TenantId));
        parameters[1].ParameterType.Should().Be(typeof(LeadId));
    }

    [Fact]
    public void Opportunity_ReadContract_Should_RequireTenant()
    {
        MethodInfo method = typeof(IOpportunityRepository).GetMethod(nameof(IOpportunityRepository.GetByIdAsync))!;
        ParameterInfo[] parameters = method.GetParameters();

        parameters[0].ParameterType.Should().Be(typeof(TenantId));
        parameters[1].ParameterType.Should().Be(typeof(OpportunityId));
    }

    [Fact]
    public void Opportunity_LeadLookup_Should_BeTenantScoped()
    {
        MethodInfo method = typeof(IOpportunityRepository).GetMethod(nameof(IOpportunityRepository.ExistsByLeadIdAsync))!;
        ParameterInfo[] parameters = method.GetParameters();

        parameters[0].ParameterType.Should().Be(typeof(TenantId));
        parameters[1].ParameterType.Should().Be(typeof(LeadId));
    }

    [Fact]
    public void Repositories_Should_BeAggregateSpecific()
    {
        typeof(ILeadRepository).GetMethods().Should().Contain(method => method.Name == "AddAsync");
        typeof(IOpportunityRepository).GetMethods().Should().Contain(method => method.Name == "AddAsync");
    }
}
