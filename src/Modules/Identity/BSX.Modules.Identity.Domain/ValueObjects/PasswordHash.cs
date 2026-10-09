using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>
/// A hashed password with its algorithm. The plaintext is never represented in the domain.
/// </summary>
public sealed class PasswordHash : ValueObject
{
    private PasswordHash(string hash, string algorithm)
    {
        Hash = hash;
        Algorithm = algorithm;
    }

    /// <summary>Gets the encoded hash.</summary>
    public string Hash { get; }

    /// <summary>Gets the hashing algorithm identifier.</summary>
    public string Algorithm { get; }

    /// <summary>Creates a password hash from its encoded parts.</summary>
    /// <param name="hash">The encoded hash.</param>
    /// <param name="algorithm">The algorithm identifier.</param>
    public static PasswordHash From(string hash, string algorithm)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new ArgumentException("The hash cannot be empty.", nameof(hash));
        }

        if (string.IsNullOrWhiteSpace(algorithm))
        {
            throw new ArgumentException("The algorithm cannot be empty.", nameof(algorithm));
        }

        return new PasswordHash(hash, algorithm);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hash;
        yield return Algorithm;
    }
}
