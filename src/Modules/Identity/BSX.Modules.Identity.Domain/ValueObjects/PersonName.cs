using BSX.Modules.Identity.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>A person's display name value object.</summary>
public sealed class PersonName : ValueObject
{
    /// <summary>Maximum supported length.</summary>
    public const int MaxLength = 200;

    private PersonName(string value) => Value = value;

    /// <summary>Gets the normalized (trimmed) value.</summary>
    public string Value { get; }

    /// <summary>Creates a validated display name.</summary>
    /// <param name="value">The raw name value.</param>
    public static Result<PersonName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<PersonName>(IdentityErrors.NameInvalid);
        }

        string normalized = value.Trim();

        if (normalized.Length > MaxLength)
        {
            return Result.Failure<PersonName>(IdentityErrors.NameInvalid);
        }

        return Result.Success(new PersonName(normalized));
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
