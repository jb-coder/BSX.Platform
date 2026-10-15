using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

internal sealed class FakeRefreshTokenHasher : IRefreshTokenHasher
{
    public RefreshTokenHash Hash(string token) => RefreshTokenHash.From("hash::" + token);
}
