using BSX.Modules.Identity.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>An email address value object, globally unique at the identity level (ADR-013).</summary>
public sealed class Email : ValueObject
{
    /// <summary>Maximum supported length.</summary>
    public const int MaxLength = 320;

    private Email(string value) => Value = value;

    /// <summary>Gets the normalized (trimmed, lower-case invariant) value.</summary>
    public string Value { get; }

    /// <summary>Creates a validated email address.</summary>
    /// <param name="value">The raw email value.</param>
    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Email>(IdentityErrors.EmailInvalid);
        }

        string normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength || !IsValid(normalized))
        {
            return Result.Failure<Email>(IdentityErrors.EmailInvalid);
        }

        return Result.Success(new Email(normalized));
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static bool IsValid(string email)
    {
        int at = email.IndexOf('@');
        return at > 0
            && at == email.LastIndexOf('@')
            && at < email.Length - 1
            && !email.Contains(' ');
    }
}
