using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BSX.BuildingBlocks.Cqrs;
using BSX.Contracts.Permissions;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.Permissions.ListPermissions;
using BSX.Modules.Identity.Application.Roles.CreateRole;
using BSX.Modules.Identity.Application.Roles.GetRoleById;
using BSX.Modules.Identity.Application.Roles.SetRolePermissions;
using BSX.SharedKernel.Results;
using BSX.Web.Administration;
using BSX.Web.Authentication;
using BSX.Web.Components.Pages.App.Admin;
using BSX.Web.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
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

    [Fact]
    public async Task RolePermissions_WhenCatalogFails_ShouldShowLoadErrorAndNotDispatchCommand()
    {
        var role = new RoleDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Sales",
            null,
            false,
            ["crm.customers.read"]);
        var sender = new CatalogFailureSender(role);
        using ServiceProvider services = CreateAdministrationServices(sender);

        string html = await RenderWithServicesAsync<RolePermissions>(
            services,
            new Dictionary<string, object?> { [nameof(RolePermissions.Id)] = role.Id });

        html.Should().Contain("Permissions could not be loaded");
        html.Should().Contain("The permission catalog is unavailable.");
        html.Should().NotContain("Save permissions");
        html.Should().NotContain("permission-selector");
        sender.Requests.OfType<SetRolePermissionsCommand>().Should().BeEmpty();
    }

    [Fact]
    public async Task RoleCreate_WhenCatalogFails_ShouldShowLoadErrorAndNotDispatchCommand()
    {
        var sender = new CatalogFailureSender(role: null);
        using ServiceProvider services = CreateAdministrationServices(sender);

        string html = await RenderWithServicesAsync<RoleCreate>(services);

        html.Should().Contain("Permissions could not be loaded");
        html.Should().Contain("The permission catalog is unavailable.");
        html.Should().NotContain("Role name");
        html.Should().NotContain("Create role");
        sender.Requests.OfType<CreateRoleCommand>().Should().BeEmpty();
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

    private static ServiceProvider CreateAdministrationServices(ISender sender)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(sender);
        services.AddSingleton<AuthenticationStateProvider, TestAuthenticationStateProvider>();
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<WebCurrentUser>();
        services.AddSingleton<AdministrationDispatcher>();
        return services.BuildServiceProvider();
    }

    private static async Task<string> RenderWithServicesAsync<TComponent>(
        ServiceProvider services,
        Dictionary<string, object?>? parameters = null)
        where TComponent : IComponent
    {
        ILoggerFactory loggerFactory = services.GetRequiredService<ILoggerFactory>();
        await using var renderer = new HtmlRenderer(services, loggerFactory);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = parameters is null
                ? await renderer.RenderComponentAsync<TComponent>()
                : await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters));

            await root.QuiescenceTask;
            return root.ToHtmlString();
        });
    }

    private sealed class CatalogFailureSender(RoleDto? role) : ISender
    {
        public List<object> Requests { get; } = [];

        public Task<TResponse> SendAsync<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            object response = request switch
            {
                GetRoleByIdQuery when role is not null => Result.Success(role),
                GetRoleByIdQuery => Result.Failure<RoleDto>(
                    Error.NotFound("Identity.Role.NotFound", "The role no longer exists.")),
                ListPermissionsQuery => Result.Failure<IReadOnlyList<PermissionDto>>(
                    Error.Unexpected("Test.PermissionCatalog.Unavailable", "The permission catalog is unavailable.")),
                SetRolePermissionsCommand => Result.Success(),
                CreateRoleCommand => Result.Success(new CreateRoleResponse(Guid.NewGuid())),
                _ => throw new InvalidOperationException($"Unexpected request type {request.GetType().Name}."),
            };

            return Task.FromResult((TResponse)response);
        }
    }

    private sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(Anonymous);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }
}
