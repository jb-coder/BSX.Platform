using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Composition.IntegrationTests.TestDoubles;

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public PasswordHash Hash(string plainTextPassword) => PasswordHash.From("hash", "test");

    public bool Verify(string plainTextPassword, PasswordHash passwordHash) => true;
}
