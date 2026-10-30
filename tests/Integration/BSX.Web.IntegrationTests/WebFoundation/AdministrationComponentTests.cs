using System.ComponentModel.DataAnnotations;
using BSX.Contracts.Permissions;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Web.Administration;
using BSX.Web.Components.Pages.App.Admin;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class AdministrationComponentTests
{
    private readonly ServiceProvider _services;

    public AdministrationComponentTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        _services = services.BuildServiceProvider();
    }

    [Fact]
    public async Task PermissionSelector_ShouldGroupAndDescribePermissions()
    {
        string html = await RenderAsync<PermissionSelector>(new Dictionary<string, object?>
        {
            [nameof(PermissionSelector.Permissions)] = new[]
            {
                new PermissionDto("identity.users.read", "Read users.", PermissionScope.Platform),
                new PermissionDto("identity.roles.read", "Read roles.", PermissionScope.Tenant),
                new PermissionDto("crm.customers.read", "Read customers.", PermissionScope.Tenant),
            },
            [nameof(PermissionSelector.Selected)] = new HashSet<string>(["identity.roles.read"], StringComparer.Ordinal),
        });

        html.Should().Contain("Identity");
        html.Should().Contain("Crm");
        html.Should().Contain("Read roles.");
        html.Should().Contain("Read customers.");
        html.Should().Contain("1 selected");
        html.Should().Contain("identity.roles.read");
        html.Should().NotContain("Read users.");
        html.Should().NotContain("identity.users.read");
    }

    [Fact]
    public async Task PermissionSelector_ShouldHidePlatformPermissions_AndExplainWhy()
    {
        string html = await RenderAsync<PermissionSelector>(new Dictionary<string, object?>
        {
            [nameof(PermissionSelector.Permissions)] = new[]
            {
                new PermissionDto("identity.users.read", "Read users.", PermissionScope.Platform),
                new PermissionDto("identity.users.update", "Update users.", PermissionScope.Platform),
                new PermissionDto("crm.customers.read", "Read customers.", PermissionScope.Tenant),
            },
            [nameof(PermissionSelector.Selected)] = new HashSet<string>(StringComparer.Ordinal),
        });

        html.Should().Contain("Platform permissions are not shown.");
        html.Should().Contain("2 platform-scoped permission(s)");
        html.Should().NotContain("Read users.");
        html.Should().Contain("Read customers.");
    }

    [Fact]
    public async Task PermissionSelector_ShouldRenderReadOnlyForSystemRoles()
    {
        string html = await RenderAsync<PermissionSelector>(new Dictionary<string, object?>
        {
            [nameof(PermissionSelector.Permissions)] = new[]
            {
                new PermissionDto("identity.roles.read", "Read roles.", PermissionScope.Tenant),
            },
            [nameof(PermissionSelector.Selected)] = new HashSet<string>(["identity.roles.read"], StringComparer.Ordinal),
            [nameof(PermissionSelector.Disabled)] = true,
        });

        html.Should().Contain("disabled");
        html.Should().Contain("Read roles.");
    }

    [Fact]
    public void UserCreateValidation_ShouldRequireEmailNameAndPassword()
    {
        var model = new UserCreateFormModel();

        IReadOnlyList<ValidationResult> errors = Validate(model);

        errors.Should().Contain(result => result.MemberNames.Contains(nameof(UserCreateFormModel.Email)));
        errors.Should().Contain(result => result.MemberNames.Contains(nameof(UserCreateFormModel.Name)));
        errors.Should().Contain(result => result.MemberNames.Contains(nameof(UserCreateFormModel.Password)));
    }

    [Fact]
    public void RoleFormValidation_ShouldRequireNameAndBoundDescription()
    {
        var missingName = new RoleFormModel { Description = new string('x', 501) };
        IReadOnlyList<ValidationResult> errors = Validate(missingName);

        errors.Should().Contain(result => result.MemberNames.Contains(nameof(RoleFormModel.Name)));
        errors.Should().Contain(result => result.MemberNames.Contains(nameof(RoleFormModel.Description)));
    }

    [Fact]
    public void AdministrationDisplay_ShouldMapStatuses()
    {
        AdministrationDisplay.UserStatusClass("Active").Should().Contain("status-active");
        AdministrationDisplay.UserStatusClass("Pending").Should().Contain("status-lead-contacted");
        AdministrationDisplay.UserStatusClass("Locked").Should().Contain("status-lead-disqualified");
        AdministrationDisplay.MembershipStatusClass("Invited").Should().Contain("status-lead-contacted");
        AdministrationDisplay.SessionStatusClass("Active").Should().Contain("status-active");
    }

    private static IReadOnlyList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private async Task<string> RenderAsync<TComponent>(Dictionary<string, object?> parameters)
        where TComponent : IComponent
    {
        ILoggerFactory loggerFactory = _services.GetRequiredService<ILoggerFactory>();
        await using var renderer = new HtmlRenderer(_services, loggerFactory);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<TComponent>(
                ParameterView.FromDictionary(parameters));

            return root.ToHtmlString();
        });
    }
}
