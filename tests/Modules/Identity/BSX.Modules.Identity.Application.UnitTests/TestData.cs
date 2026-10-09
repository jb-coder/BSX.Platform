using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ServiceAccounts;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests;

/// <summary>Shared builders for application-layer tests.</summary>
internal static class TestData
{
    internal static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    internal static Email CreateEmail(string value = "user@example.com") => Email.Create(value).Value;

    internal static PersonName CreateName(string value = "Test User") => PersonName.Create(value).Value;

    internal static PasswordHash CreatePasswordHash(string value = "hash") => PasswordHash.From(value, "test");

    internal static Permission CreatePermission(string code = "crm.customers.read") => Permission.Create(code).Value;

    internal static User CreateActiveUser(string email = "user@example.com")
    {
        User user = User.Register(UserId.New(), CreateEmail(email), CreateName(), Now).Value;
        user.SetPassword(CreatePasswordHash(), Now);
        user.Activate(Now);
        user.ClearDomainEvents();
        return user;
    }

    internal static Role CreateRole(
        TenantId? tenantId = null,
        string name = "Manager",
        string permission = "crm.customers.read",
        bool isSystem = false)
        => Role.Create(RoleId.New(), tenantId, RoleName.Create(name).Value, [CreatePermission(permission)], isSystem: isSystem).Value;

    internal static TenantMembership CreateActiveMembership(TenantId tenantId, UserId userId)
    {
        TenantMembership membership = TenantMembership.Invite(MembershipId.New(), tenantId, userId, null, Now).Value;
        membership.Accept(Now);
        membership.ClearDomainEvents();
        return membership;
    }

    internal static ServiceAccount CreateServiceAccount(TenantId tenantId, string name = "bot")
        => ServiceAccount.Create(ServiceAccountId.New(), tenantId, name).Value;
}
