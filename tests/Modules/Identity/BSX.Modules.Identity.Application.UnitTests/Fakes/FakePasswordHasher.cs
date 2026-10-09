using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

/// <summary>Deterministic <see cref="IPasswordHasher"/> for handler tests.</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public PasswordHash Hash(string plainTextPassword) => PasswordHash.From($"hashed::{plainTextPassword}", "fake");
}
