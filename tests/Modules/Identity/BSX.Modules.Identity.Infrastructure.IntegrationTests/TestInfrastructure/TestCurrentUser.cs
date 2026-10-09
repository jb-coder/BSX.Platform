using BSX.BuildingBlocks.Authorization;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;

internal sealed class TestCurrentUser : ICurrentUser
{
    public TestCurrentUser(Guid? userId = null) => UserId = userId;

    public Guid? UserId { get; }

    public string? Email => null;

    public bool IsAuthenticated => UserId.HasValue;

    public IReadOnlyCollection<string> Permissions => [];
}
