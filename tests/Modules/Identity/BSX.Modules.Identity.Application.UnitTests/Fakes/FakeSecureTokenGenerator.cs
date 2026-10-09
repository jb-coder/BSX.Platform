using BSX.Modules.Identity.Application.Abstractions;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

/// <summary>Deterministic <see cref="ISecureTokenGenerator"/> for handler tests.</summary>
internal sealed class FakeSecureTokenGenerator : ISecureTokenGenerator
{
    public string GenerateToken(int byteLength = 32) => "secret_" + Guid.NewGuid().ToString("N");
}
