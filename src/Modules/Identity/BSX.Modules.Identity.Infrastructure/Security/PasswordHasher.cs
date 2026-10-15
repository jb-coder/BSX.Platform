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

    /// <inheritdoc />
    public bool Verify(string plainTextPassword, PasswordHash passwordHash)
    {
        ArgumentNullException.ThrowIfNull(passwordHash);

        if (string.IsNullOrEmpty(plainTextPassword))
        {
            return false;
        }

        // Backward-compatible parsing: iterations.salt.key (base64). Unknown formats fail closed.
        string[] parts = passwordHash.Hash.Split('.');
        if (parts.Length != 3
            || !int.TryParse(parts[0], out int iterations)
            || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expectedKey;

        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedKey = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] actualKey = Rfc2898DeriveBytes.Pbkdf2(
            plainTextPassword,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedKey.Length);

        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}
