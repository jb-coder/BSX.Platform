using System.Reflection;
using BSX.BuildingBlocks.Authorization;
using BSX.Contracts.Permissions;
using BSX.Modules.Identity.Application.ApiKeys.CreateApiKey;
using BSX.Modules.Identity.Application.Common;
using BSX.Modules.Identity.Application.ServiceAccounts.CreateServiceAccount;

namespace BSX.Modules.Identity.Application.UnitTests.Authorization;

/// <summary>
/// Regression coverage for write paths that previously had no permission declaration or tenant
/// guard. Every command that mints a tenant-scoped credential must declare a tenant permission.
/// </summary>
public sealed class IdentityCommandAuthorizationTests
{
    [Theory]
    [InlineData(typeof(CreateServiceAccountCommand), IdentityPermissionCodes.ServiceAccountsCreate)]
    [InlineData(typeof(CreateApiKeyCommand), IdentityPermissionCodes.ApiKeysCreate)]
    public void CredentialCommand_ShouldDeclareATenantPermission(Type commandType, string expectedPermission)
    {
        RequirePermissionAttribute[] attributes = commandType
            .GetCustomAttributes<RequirePermissionAttribute>(inherit: true)
            .ToArray();

        attributes.Should().ContainSingle();
        attributes[0].Permission.Should().Be(expectedPermission);
        TestPermissionCatalog.Platform.ScopeOf(expectedPermission).Should().Be(PermissionScope.Tenant);
    }
}
