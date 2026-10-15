using BSX.Modules.Identity.Application.Abstractions;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

internal sealed class FakeAccessTokenIssuer : IAccessTokenIssuer
{
    public AccessToken Issue(AccessTokenDescriptor descriptor)
        => new($"access::{descriptor.UserId}", DateTimeOffset.UtcNow.AddMinutes(15));
}
