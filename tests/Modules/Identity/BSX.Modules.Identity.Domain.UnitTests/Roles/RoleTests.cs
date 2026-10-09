using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.UnitTests.Roles;

public sealed class RoleTests
{
    private static Role CreateRole(bool isSystem = false)
        => Role.Create(
            RoleId.New(),
            TenantId.New(),
            RoleName.Create("Manager").Value,
            [TestData.CreatePermission("crm.customers.read")],
            isSystem: isSystem).Value;

    [Fact]
    public void Should_Create_WithPermissions_AndRaiseEvent()
    {
        var role = CreateRole();

        role.Permissions.Should().ContainSingle();
        role.IsDeleted.Should().BeFalse();
        role.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is RoleCreated);
    }

    [Fact]
    public void Should_FailRename_WhenSystemRole()
    {
        var role = CreateRole(isSystem: true);

        Result result = role.Rename(RoleName.Create("Other").Value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Role.SystemImmutable");
    }

    [Fact]
    public void Should_FailGrant_WhenPermissionAlreadyPresent()
    {
        var role = CreateRole();
        var permission = TestData.CreatePermission("crm.customers.read");

        Result result = role.Grant(permission);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Role.PermissionAlreadyGranted");
    }

    [Fact]
    public void Should_Grant_NewPermission_AndRaiseEvent()
    {
        var role = CreateRole();

        Result result = role.Grant(TestData.CreatePermission("crm.customers.create"));

        result.IsSuccess.Should().BeTrue();
        role.DomainEvents.Should().Contain(domainEvent => domainEvent is RolePermissionsChanged);
    }

    [Fact]
    public void Should_FailRevoke_WhenPermissionNotGranted()
    {
        var role = CreateRole();

        Result result = role.Revoke(TestData.CreatePermission("crm.customers.delete"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Role.PermissionNotGranted");
    }

    [Fact]
    public void Should_SoftDelete_WhenNotSystem()
    {
        var role = CreateRole();

        Result result = role.Delete();

        result.IsSuccess.Should().BeTrue();
        role.IsDeleted.Should().BeTrue();
        role.DomainEvents.Should().Contain(domainEvent => domainEvent is RoleDeleted);
    }

    [Fact]
    public void Should_FailDelete_WhenSystemRole()
    {
        var role = CreateRole(isSystem: true);

        Result result = role.Delete();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Role.SystemImmutable");
    }
}
