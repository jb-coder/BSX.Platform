using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>
/// An encrypted TOTP shared secret. The plaintext secret is never represented in the domain.
/// </summary>
public sealed class TotpSecret : ValueObject
{
    private TotpSecret(string encryptedValue) => EncryptedValue = encryptedValue;

    /// <summary>Gets the encrypted secret.</summary>
    public string EncryptedValue { get; }

    /// <summary>Creates a TOTP secret from its encrypted representation.</summary>
    /// <param name="encryptedValue">The encrypted secret.</param>
    public static TotpSecret From(string encryptedValue)
    {
        if (string.IsNullOrWhiteSpace(encryptedValue))
        {
            throw new ArgumentException("The TOTP secret cannot be empty.", nameof(encryptedValue));
        }

        return new TotpSecret(encryptedValue);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return EncryptedValue;
    }
}
