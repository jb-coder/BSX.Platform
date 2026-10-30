using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.DependencyInjection;
using BSX.BuildingBlocks.Persistence;
using BSX.BuildingBlocks.UnitTests.Fixtures;
using BSX.SharedKernel.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSX.BuildingBlocks.UnitTests.Behaviors;

public sealed class AuthorizationBehaviorTests
{
    [Fact]
    public async Task Should_InvokeHandler_WhenPermissionGranted()
    {
        RequirePermissionCommandHandler.Reset();
        ISender sender = CreateSender(new FakeCurrentUser(Guid.NewGuid(), null, true, "crm.customers.read"));

        Result result = await sender.SendAsync(new RequirePermissionCommand());

        result.IsSuccess.Should().BeTrue();
        RequirePermissionCommandHandler.InvocationCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_ReturnForbidden_WhenPermissionMissing()
    {
        RequirePermissionCommandHandler.Reset();
        ISender sender = CreateSender(new FakeCurrentUser(Guid.NewGuid(), null, true));

        Result result = await sender.SendAsync(new RequirePermissionCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authorization.Forbidden");
        RequirePermissionCommandHandler.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_FailClosed_WhenNotAuthenticated()
    {
        RequirePermissionCommandHandler.Reset();
        ISender sender = CreateSender(new FakeCurrentUser());

        Result result = await sender.SendAsync(new RequirePermissionCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authorization.Unauthenticated");
        RequirePermissionCommandHandler.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_RequireAuthentication_ForCommandsWithoutAllowAnonymous()
    {
        RequirePermissionCommandHandler.Reset();
        ISender sender = CreateSender(new FakeCurrentUser());

        Result result = await sender.SendAsync(new RequirePermissionCommand());

        result.IsFailure.Should().BeTrue();
        RequirePermissionCommandHandler.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_AllowAnonymous_WhenMarked()
    {
        RequirePermissionCommandHandler.Reset();
        ISender sender = CreateSender(new FakeCurrentUser());

        Result result = await sender.SendAsync(new CreateThingCommand("thing"));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Deny_WhenRevalidationFails()
    {
        RequirePermissionCommandHandler.Reset();
        ISender sender = CreateSender(
            new FakeCurrentUser(Guid.NewGuid(), null, true, "crm.customers.read"),
            new FakeRevalidator(AuthorizationRevalidationResult.Denied));

        Result result = await sender.SendAsync(new RequirePermissionCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authorization.SessionExpired");
        RequirePermissionCommandHandler.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_UseFreshPermissions_InsteadOfStaleClaims()
    {
        RequirePermissionCommandHandler.Reset();
        ISender sender = CreateSender(
            new FakeCurrentUser(Guid.NewGuid(), null, true, "crm.customers.read"),
            new FakeRevalidator(AuthorizationRevalidationResult.Authoritative([])));

        Result result = await sender.SendAsync(new RequirePermissionCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authorization.Forbidden");
        RequirePermissionCommandHandler.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Grant_WhenFreshPermissionsContainThePermission()
    {
        RequirePermissionCommandHandler.Reset();
        ISender sender = CreateSender(
            new FakeCurrentUser(Guid.NewGuid(), null, true),
            new FakeRevalidator(AuthorizationRevalidationResult.Authoritative(["crm.customers.read"])));

        Result result = await sender.SendAsync(new RequirePermissionCommand());

        result.IsSuccess.Should().BeTrue();
        RequirePermissionCommandHandler.InvocationCount.Should().Be(1);
    }

    private static ISender CreateSender(ICurrentUser currentUser, IAuthorizationRevalidator? revalidator = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(currentUser);
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddBuildingBlocks(typeof(AuthorizationBehaviorTests).Assembly);

        if (revalidator is not null)
        {
            services.RemoveAll<IAuthorizationRevalidator>();
            services.AddSingleton(revalidator);
        }

        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private sealed class FakeRevalidator(AuthorizationRevalidationResult result) : IAuthorizationRevalidator
    {
        public Task<AuthorizationRevalidationResult> RevalidateAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }
}
