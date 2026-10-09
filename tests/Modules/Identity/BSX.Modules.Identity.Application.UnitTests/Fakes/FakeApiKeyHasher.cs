using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

/// <summary>Deterministic <see cref="IApiKeyHasher"/> for handler tests.</summary>
internal sealed class FakeApiKeyHasher : IApiKeyHasher
{
    public ApiKeyHash Hash(string secret) => ApiKeyHash.From("hashed::" + secret);
}
