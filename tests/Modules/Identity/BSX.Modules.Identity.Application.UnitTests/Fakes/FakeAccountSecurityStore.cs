using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

/// <summary>Records durable security writes for handler tests.</summary>
internal sealed class FakeAccountSecurityStore : IAccountSecurityStore
{
    public List<UserId> Failures { get; } = [];

    public List<(SessionId SessionId, string Reason)> Revocations { get; } = [];

    public Task RecordAccessFailureAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        Failures.Add(userId);
        return Task.CompletedTask;
    }

    public Task RevokeSessionAsync(SessionId sessionId, string reason, CancellationToken cancellationToken = default)
    {
        Revocations.Add((sessionId, reason));
        return Task.CompletedTask;
    }
}
