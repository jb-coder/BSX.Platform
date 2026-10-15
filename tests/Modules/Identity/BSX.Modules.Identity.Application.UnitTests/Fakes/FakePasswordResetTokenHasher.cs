using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

internal sealed class FakePasswordResetTokenHasher : IPasswordResetTokenHasher
{
    public PasswordResetTokenHash Hash(string token) => PasswordResetTokenHash.From("resethash::" + token);
}
