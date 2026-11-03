using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Application.Users.SetUserPlatformRoles;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Users;

public sealed class SetUserPlatformRolesCommandHandlerTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryRoleRepository _roles = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly Guid _actorUserId = Guid.NewGuid();

    private SetUserPlatformRolesCommandHandler CreateHandler(Guid? actorUserId = null)
        => new(_users, _roles, _unitOfWork, new FakeCurrentUser(actorUserId ?? _actorUserId));

    [Fact]
    public async Task Should_AssignMultiplePlatformRoles()
    {
        User user = TestData.CreateActiveUser();
        Role first = TestData.CreateRole(tenantId: null, name: "Administrator", isSystem: true);
        Role second = TestData.CreateRole(tenantId: null, name: "Auditor", isSystem: true);
        _users.Seed(user);
        _roles.Seed(first, second);

        var result = await CreateHandler().HandleAsync(
            new SetUserPlatformRolesCommand(user.Id.Value, [first.Id.Value, second.Id.Value]));

        result.IsSuccess.Should().BeTrue();
        user.PlatformRoleIds.Should().BeEquivalentTo([first.Id, second.Id]);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_ReplaceExistingPlatformRoles()
    {
        User user = TestData.CreateActiveUser();
        Role first = TestData.CreateRole(tenantId: null, name: "Auditor", isSystem: true);
        Role second = TestData.CreateRole(tenantId: null, name: "CRM Reader", isSystem: true);
        user.AssignPlatformRole(first.Id);
        user.ClearDomainEvents();
        _users.Seed(user);
        _roles.Seed(first, second);

        var result = await CreateHandler().HandleAsync(
            new SetUserPlatformRolesCommand(user.Id.Value, [second.Id.Value]));

        result.IsSuccess.Should().BeTrue();
        user.PlatformRoleIds.Should().BeEquivalentTo([second.Id]);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_NotSave_WhenRolesAreUnchanged()
    {
        User user = TestData.CreateActiveUser();
        Role role = TestData.CreateRole(tenantId: null, name: "Auditor", isSystem: true);
        user.AssignPlatformRole(role.Id);
        user.ClearDomainEvents();
        _users.Seed(user);
        _roles.Seed(role);

        var result = await CreateHandler().HandleAsync(
            new SetUserPlatformRolesCommand(user.Id.Value, [role.Id.Value]));

        result.IsSuccess.Should().BeTrue();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_RejectOwnPlatformRoleChange()
    {
        User user = TestData.CreateActiveUser();
        Role role = TestData.CreateRole(tenantId: null, name: "Auditor", isSystem: true);
        _users.Seed(user);
        _roles.Seed(role);

        var result = await CreateHandler(user.Id.Value).HandleAsync(
            new SetUserPlatformRolesCommand(user.Id.Value, [role.Id.Value]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.SelfPlatformRoleChangeForbidden");
        user.PlatformRoleIds.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_RejectWorkspaceRole()
    {
        User user = TestData.CreateActiveUser();
        Role role = TestData.CreateRole(tenantId: TenantId.New(), name: "Workspace Manager");
        _users.Seed(user);
        _roles.Seed(role);

        var result = await CreateHandler().HandleAsync(
            new SetUserPlatformRolesCommand(user.Id.Value, [role.Id.Value]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Role.NotAssignableToPlatform");
        user.PlatformRoleIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_RejectUnknownRole()
    {
        User user = TestData.CreateActiveUser();
        _users.Seed(user);

        var result = await CreateHandler().HandleAsync(
            new SetUserPlatformRolesCommand(user.Id.Value, [Guid.NewGuid()]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Role.NotFound");
    }

    [Fact]
    public async Task Should_RejectLastAdministratorRemoval()
    {
        User user = TestData.CreateActiveUser();
        Role administrator = TestData.CreateRole(tenantId: null, name: "Administrator", isSystem: true);
        user.AssignPlatformRole(administrator.Id);
        user.ClearDomainEvents();
        _users.Seed(user);
        _roles.Seed(administrator);

        var result = await CreateHandler().HandleAsync(
            new SetUserPlatformRolesCommand(user.Id.Value, []));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.LastAdministratorRequired");
        user.PlatformRoleIds.Should().Contain(administrator.Id);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_AllowAdministratorRemoval_WhenAnotherActiveAdministratorExists()
    {
        User user = TestData.CreateActiveUser("first@example.com");
        User otherAdministrator = TestData.CreateActiveUser("second@example.com");
        Role administrator = TestData.CreateRole(tenantId: null, name: "Administrator", isSystem: true);
        user.AssignPlatformRole(administrator.Id);
        otherAdministrator.AssignPlatformRole(administrator.Id);
        user.ClearDomainEvents();
        otherAdministrator.ClearDomainEvents();
        _users.Seed(user, otherAdministrator);
        _roles.Seed(administrator);

        var result = await CreateHandler().HandleAsync(
            new SetUserPlatformRolesCommand(user.Id.Value, []));

        result.IsSuccess.Should().BeTrue();
        user.PlatformRoleIds.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_AllowRemovingAdministrator_FromInactiveUser()
    {
        User user = TestData.CreateActiveUser();
        Role administrator = TestData.CreateRole(tenantId: null, name: "Administrator", isSystem: true);
        user.AssignPlatformRole(administrator.Id);
        user.Deactivate();
        user.ClearDomainEvents();
        _users.Seed(user);
        _roles.Seed(administrator);

        var result = await CreateHandler().HandleAsync(
            new SetUserPlatformRolesCommand(user.Id.Value, []));

        result.IsSuccess.Should().BeTrue();
        user.PlatformRoleIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_WhenUserNotFound()
    {
        var result = await CreateHandler().HandleAsync(
            new SetUserPlatformRolesCommand(Guid.NewGuid(), []));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.NotFound");
    }

    [Fact]
    public async Task Should_Fail_WhenUserIdIsEmpty()
    {
        var result = await CreateHandler().HandleAsync(
            new SetUserPlatformRolesCommand(Guid.Empty, []));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.InvalidId");
    }

    [Fact]
    public async Task Should_Fail_WhenRoleIdIsEmpty()
    {
        User user = TestData.CreateActiveUser();
        _users.Seed(user);

        var result = await CreateHandler().HandleAsync(
            new SetUserPlatformRolesCommand(user.Id.Value, [Guid.Empty]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Role.InvalidId");
    }
}
