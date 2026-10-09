using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Domain.UnitTests.ValueObjects;

public sealed class PermissionTests
{
    [Theory]
    [InlineData("identity.users.read")]
    [InlineData("crm.customers.create")]
    [InlineData("identity.roles.set-permissions")]
    public void Should_Create_WhenValid(string code)
    {
        Permission.Create(code).IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("UPPER.case")]
    [InlineData("noundot")]
    [InlineData("trailing.")]
    [InlineData(".leading")]
    [InlineData("")]
    [InlineData(null)]
    public void Should_Fail_WhenInvalid(string? code)
    {
        Permission.Create(code).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_BeEqual_ByCode()
    {
        Permission.Create("crm.customers.read", "one").Value
            .Should().Be(Permission.Create("crm.customers.read", "two").Value);
    }
}
