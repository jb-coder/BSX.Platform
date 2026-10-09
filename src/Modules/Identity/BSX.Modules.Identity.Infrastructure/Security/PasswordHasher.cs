using System.Security.Cryptography;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Infrastructure.Security;

/// <summary>PBKDF2-SHA256 password hasher (ADR-014).</summary>
internal sealed class PasswordHasher : IPasswordHasher
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const string Algorithm = "pbkdf2-sha256";

    /// <inheritdoc />
    public PasswordHash Hash(string plainTextPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainTextPassword);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(
            plainTextPassword,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        string encoded = $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        return PasswordHash.From(encoded, Algorithm);
    }
}
