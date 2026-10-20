using BSX.BuildingBlocks.Authorization;

namespace BSX.Composition.IntegrationTests.TestDoubles;

internal sealed class TestCurrentUser : ICurrentUser
{
    public TestCurrentUser(Guid? userId = null) => UserId = userId;

    public Guid? UserId { get; }

    public Guid? TenantId => null;

    public string? Email => null;

    public bool IsAuthenticated => UserId.HasValue;

    public IReadOnlyCollection<string> Permissions => [];
}
