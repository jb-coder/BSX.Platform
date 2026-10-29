using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Identity.Infrastructure.Persistence;
using BSX.Modules.Identity.Infrastructure.Repositories;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests;

public sealed class RepositoryTests
{
    [Fact]
    public async Task Should_RoundTripUserWithCredential()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync();
        var repository = new UserRepository(database.Context);
        var unitOfWork = new IdentityUnitOfWork(database.Context, new RecordingDomainEventDispatcher());

        User user = TestData.CreateUser("roundtrip@example.com");
        await repository.AddAsync(user);
        await unitOfWork.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        User? loaded = await repository.GetByIdAsync(user.Id);

        loaded.Should().NotBeNull();
        loaded!.Email.Value.Should().Be("roundtrip@example.com");
        loaded.HasCredential(CredentialType.Password).Should().BeTrue();
        (await repository.ExistsByEmailAsync(TestData.CreateEmail("roundtrip@example.com"))).Should().BeTrue();
    }

    [Fact]
    public async Task Should_RoundTripRolePermissions()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync();
        var repository = new RoleRepository(database.Context);
        var unitOfWork = new IdentityUnitOfWork(database.Context, new RecordingDomainEventDispatcher());

        Role role = TestData.CreateRole(TenantId.New(), "Manager", "crm.customers.read", "crm.customers.create");
        await repository.AddAsync(role);
        await unitOfWork.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        Role? loaded = await repository.GetByIdAsync(role.Id);

        loaded.Should().NotBeNull();
        loaded!.Permissions.Select(permission => permission.Code)
            .Should().BeEquivalentTo(["crm.customers.read", "crm.customers.create"]);
    }

    [Fact]
    public async Task Should_RoundTripMembershipRoleIds()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync();
        var userRepository = new UserRepository(database.Context);
        var membershipRepository = new TenantMembershipRepository(database.Context);
        var unitOfWork = new IdentityUnitOfWork(database.Context, new RecordingDomainEventDispatcher());

        User user = TestData.CreateUser();
        await userRepository.AddAsync(user);
        var tenantId = TenantId.New();
        var membership = BSX.Modules.Identity.Domain.Memberships.TenantMembership
            .Invite(MembershipId.New(), tenantId, user.Id, null, TestData.Now).Value;
        membership.Accept(TestData.Now);
        var roleId = RoleId.New();
        membership.AssignRole(roleId);
        await membershipRepository.AddAsync(membership);
        await unitOfWork.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var loaded = await membershipRepository.GetByIdAsync(membership.Id);

        loaded.Should().NotBeNull();
        loaded!.RoleIds.Should().Contain(roleId);
    }

    [Fact]
    public async Task Should_PersistRoleAssignedAfterMembershipIsLoaded()
    {
        await using SqliteIdentityDatabase database = await SqliteIdentityDatabase.CreateAsync();
        var userRepository = new UserRepository(database.Context);
        var membershipRepository = new TenantMembershipRepository(database.Context);
        var unitOfWork = new IdentityUnitOfWork(database.Context, new RecordingDomainEventDispatcher());

        User user = TestData.CreateUser();
        await userRepository.AddAsync(user);
        var tenantId = TenantId.New();
        var membership = BSX.Modules.Identity.Domain.Memberships.TenantMembership
            .Invite(MembershipId.New(), tenantId, user.Id, null, TestData.Now).Value;
        membership.Accept(TestData.Now);
        await membershipRepository.AddAsync(membership);
        await unitOfWork.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var loaded = await membershipRepository.GetByIdAsync(membership.Id);
        loaded.Should().NotBeNull();
        var roleId = RoleId.New();
        loaded!.AssignRole(roleId);
        await membershipRepository.TrackRoleChangesAsync(loaded);
        await unitOfWork.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var reloaded = await membershipRepository.GetByIdAsync(membership.Id);

        reloaded.Should().NotBeNull();
        reloaded!.RoleIds.Should().Contain(roleId);
    }
}
