using System.Reflection;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Domain.UnitTests;

/// <summary>
/// Locks the tenant read-isolation decision (Option A): aggregate read contracts take a
/// <see cref="TenantId"/>, so cross-tenant reads cannot be expressed by accident.
/// </summary>
public sealed class RepositoryContractTests
{
    [Fact]
    public void Customer_ReadContract_Should_RequireTenant()
    {
        MethodInfo method = typeof(ICustomerRepository).GetMethod(nameof(ICustomerRepository.GetByIdAsync))!;
        ParameterInfo[] parameters = method.GetParameters();

        parameters[0].ParameterType.Should().Be(typeof(TenantId));
        parameters[1].ParameterType.Should().Be(typeof(CustomerId));
    }

    [Fact]
    public void Organization_ReadContract_Should_RequireTenant()
    {
        MethodInfo method = typeof(IOrganizationRepository).GetMethod(nameof(IOrganizationRepository.GetByIdAsync))!;
        ParameterInfo[] parameters = method.GetParameters();

        parameters[0].ParameterType.Should().Be(typeof(TenantId));
        parameters[1].ParameterType.Should().Be(typeof(OrganizationId));
    }

    [Fact]
    public void Customer_CodeLookup_Should_BeTenantScoped()
    {
        MethodInfo method = typeof(ICustomerRepository).GetMethod(nameof(ICustomerRepository.ExistsByCodeAsync))!;
        ParameterInfo[] parameters = method.GetParameters();

        parameters[0].ParameterType.Should().Be(typeof(TenantId));
        parameters[1].ParameterType.Should().Be(typeof(CustomerCode));
    }
}
