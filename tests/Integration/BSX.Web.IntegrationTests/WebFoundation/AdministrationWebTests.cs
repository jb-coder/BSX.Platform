using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class AdministrationWebTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public AdministrationWebTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task AdministrationScreens_ShouldRenderUsersRolesMembershipsAndSessions()
    {
        HttpClient api = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);

        Guid userId = await CreateUserAsync(api);
        (Guid roleId, string roleName) = await CreateRoleAsync(api);
        Guid membershipId = await InviteAsync(api, userId);

        HttpClient client = await SignInAsync(IdentityApiFactory.AdminEmail);

        HttpResponseMessage users = await client.GetAsync("/app/admin/users");
        users.StatusCode.Should().Be(HttpStatusCode.OK);
        string usersHtml = await users.Content.ReadAsStringAsync();
        usersHtml.Should().Contain("New user");
        usersHtml.Should().Contain("Search users");

        HttpResponseMessage userDetail = await client.GetAsync($"/app/admin/users/{userId}");
        userDetail.StatusCode.Should().Be(HttpStatusCode.OK);
        string userHtml = await userDetail.Content.ReadAsStringAsync();
        userHtml.Should().Contain("Managed UI User");
        userHtml.Should().Contain("Pending");
        userHtml.Should().Contain("Edit user");
        userHtml.Should().Contain("Activate user");
        userHtml.Should().Contain("Invite to workspace");

        HttpResponseMessage userCreate = await client.GetAsync("/app/admin/users/new");
        userCreate.StatusCode.Should().Be(HttpStatusCode.OK);
        string userCreateHtml = await userCreate.Content.ReadAsStringAsync();
        userCreateHtml.Should().Contain("for=\"user-name\"");
        userCreateHtml.Should().Contain("for=\"user-email\"");
        userCreateHtml.Should().Contain("for=\"user-password\"");

        HttpResponseMessage userEdit = await client.GetAsync($"/app/admin/users/{userId}/edit");
        userEdit.StatusCode.Should().Be(HttpStatusCode.OK);
        (await userEdit.Content.ReadAsStringAsync()).Should().Contain("value=\"Managed UI User\"");

        HttpResponseMessage roles = await client.GetAsync("/app/admin/roles");
        roles.StatusCode.Should().Be(HttpStatusCode.OK);
        string rolesHtml = await roles.Content.ReadAsStringAsync();
        rolesHtml.Should().Contain("New role");
        rolesHtml.Should().Contain("Administrator");
        rolesHtml.Should().Contain(roleName);

        HttpResponseMessage roleDetail = await client.GetAsync($"/app/admin/roles/{roleId}");
        roleDetail.StatusCode.Should().Be(HttpStatusCode.OK);
        string roleHtml = await roleDetail.Content.ReadAsStringAsync();
        roleHtml.Should().Contain(roleName);
        roleHtml.Should().Contain("Edit role");
        roleHtml.Should().Contain("Manage permissions");
        roleHtml.Should().Contain("Read customers.");

        HttpResponseMessage rolePermissions = await client.GetAsync($"/app/admin/roles/{roleId}/permissions");
        rolePermissions.StatusCode.Should().Be(HttpStatusCode.OK);
        string permissionHtml = await rolePermissions.Content.ReadAsStringAsync();
        permissionHtml.Should().Contain("Search permissions");
        permissionHtml.Should().Contain("Identity");
        permissionHtml.Should().Contain("Crm");

        HttpResponseMessage memberships = await client.GetAsync("/app/admin/memberships");
        memberships.StatusCode.Should().Be(HttpStatusCode.OK);
        string membershipHtml = await memberships.Content.ReadAsStringAsync();
        membershipHtml.Should().Contain("Invite member");
        membershipHtml.Should().Contain("Managed UI User");
        membershipHtml.Should().Contain("Accept");
        membershipHtml.Should().Contain("Change role");

        HttpResponseMessage invite = await client.GetAsync("/app/admin/memberships/invite");
        invite.StatusCode.Should().Be(HttpStatusCode.OK);
        (await invite.Content.ReadAsStringAsync()).Should().Contain("Choose user");

        HttpResponseMessage invitePrefill = await client.GetAsync(
            $"/app/admin/memberships/invite?userId={userId}");
        invitePrefill.StatusCode.Should().Be(HttpStatusCode.OK);
        (await invitePrefill.Content.ReadAsStringAsync()).Should().Contain("Managed UI User");

        HttpResponseMessage changeRole = await client.GetAsync($"/app/admin/memberships/{membershipId}/role");
        changeRole.StatusCode.Should().Be(HttpStatusCode.OK);
        (await changeRole.Content.ReadAsStringAsync()).Should().Contain("Save role");

        HttpResponseMessage sessions = await client.GetAsync("/app/admin/sessions");
        sessions.StatusCode.Should().Be(HttpStatusCode.OK);
        string sessionsHtml = await sessions.Content.ReadAsStringAsync();
        sessionsHtml.Should().Contain("Current");
        sessionsHtml.Should().Contain("Revoke all sessions");
    }

    [Fact]
    public async Task AdministrationReader_ShouldSeeReadOnlyScreensWithoutMutationActions()
    {
        HttpClient api = await _factory.CreateAuthenticatedClientAsync(
            IdentityApiFactory.AdminEmail,
            IdentityApiFactory.AdminPassword);
        (Guid roleId, string roleName) = await CreateRoleAsync(api);

        HttpClient client = await SignInAsync(IdentityApiFactory.AdministrationReaderEmail);

        HttpResponseMessage users = await client.GetAsync("/app/admin/users");
        users.StatusCode.Should().Be(HttpStatusCode.OK);
        string usersHtml = await users.Content.ReadAsStringAsync();
        usersHtml.Should().Contain("Search users");
        usersHtml.Should().NotContain("New user");

        HttpResponseMessage userDetail = await client.GetAsync($"/app/admin/users/{_factory.AdminUserId}");
        userDetail.StatusCode.Should().Be(HttpStatusCode.OK);
        string userHtml = await userDetail.Content.ReadAsStringAsync();
        userHtml.Should().NotContain("Edit user");
        userHtml.Should().NotContain("Deactivate user");

        HttpResponseMessage roles = await client.GetAsync("/app/admin/roles");
        roles.StatusCode.Should().Be(HttpStatusCode.OK);
        string rolesHtml = await roles.Content.ReadAsStringAsync();
        rolesHtml.Should().Contain(roleName);
        rolesHtml.Should().NotContain("New role");

        HttpResponseMessage roleDetail = await client.GetAsync($"/app/admin/roles/{roleId}");
        roleDetail.StatusCode.Should().Be(HttpStatusCode.OK);
        string roleHtml = await roleDetail.Content.ReadAsStringAsync();
        roleHtml.Should().NotContain("Edit role");
        roleHtml.Should().NotContain("Manage permissions");

        HttpResponseMessage memberships = await client.GetAsync("/app/admin/memberships");
        memberships.StatusCode.Should().Be(HttpStatusCode.OK);
        string membershipHtml = await memberships.Content.ReadAsStringAsync();
        membershipHtml.Should().NotContain("Invite member");
        membershipHtml.Should().NotContain("Change role");
        membershipHtml.Should().NotContain("Remove");
    }

    [Fact]
    public async Task AdministrationPages_ShouldRequireAuthenticationAndPermission()
    {
        HttpClient anonymous = WebFoundationTestHelper.CreateClient(_factory);

        HttpResponseMessage users = await anonymous.GetAsync("/app/admin/users");
        users.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(users).Should().StartWith("/login?returnUrl=");

        HttpResponseMessage roles = await anonymous.GetAsync("/app/admin/roles");
        roles.StatusCode.Should().Be(HttpStatusCode.Redirect);

        HttpClient restricted = await WebFoundationTestHelper.SignInAsync(
            _factory,
            IdentityApiFactory.RestrictedEmail,
            IdentityApiFactory.AdminPassword);

        HttpResponseMessage denied = await restricted.GetAsync("/app/admin/users");
        denied.StatusCode.Should().Be(HttpStatusCode.Redirect);
        WebFoundationTestHelper.LocationPath(denied).Should().StartWith("/access-denied");
    }

    private Task<HttpClient> SignInAsync(string email)
        => WebFoundationTestHelper.SignInAsync(_factory, email, IdentityApiFactory.AdminPassword);

    private static async Task<Guid> CreateUserAsync(HttpClient api)
    {
        HttpResponseMessage response = await api.PostAsJsonAsync(
            "/identity/users",
            new
            {
                email = $"managed-ui-{Guid.NewGuid():N}@example.com",
                name = "Managed UI User",
                password = IdentityApiFactory.AdminPassword,
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("userId").GetGuid();
    }

    private static async Task<(Guid Id, string Name)> CreateRoleAsync(HttpClient api)
    {
        string name = $"UI Role {Guid.NewGuid():N}";

        HttpResponseMessage response = await api.PostAsJsonAsync(
            "/identity/roles",
            new
            {
                name,
                description = "Administration UI test role",
                permissions = new[] { "crm.customers.read" },
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (json.RootElement.GetProperty("roleId").GetGuid(), name);
    }

    private static async Task<Guid> InviteAsync(HttpClient api, Guid userId)
    {
        HttpResponseMessage response = await api.PostAsJsonAsync(
            "/identity/memberships",
            new { userId, invitedByUserId = (Guid?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("membershipId").GetGuid();
    }
}
