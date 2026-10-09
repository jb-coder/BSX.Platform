using BSX.Modules.Identity.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>A role name value object, unique per tenant (ADR-013).</summary>
public sealed class RoleName : ValueObject
{
    /// <summary>Maximum supported length.</summary>
    public const int MaxLength = 100;

    private RoleName(string value) => Value = value;

    /// <summary>Gets the normalized (trimmed) value.</summary>
    public string Value { get; }

    /// <summary>Creates a validated role name.</summary>
    /// <param name="value">The raw role name.</param>
    public static Result<RoleName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<RoleName>(IdentityErrors.RoleNameInvalid);
        }

        string normalized = value.Trim();

        if (normalized.Length > MaxLength)
        {
            return Result.Failure<RoleName>(IdentityErrors.RoleNameInvalid);
        }

        return Result.Success(new RoleName(normalized));
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
