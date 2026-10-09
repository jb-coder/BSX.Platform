using System.Text.RegularExpressions;
using BSX.Modules.Identity.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>
/// A platform permission code in the form <c>module.resource.action</c> (ADR-012).
/// Equality is based on the code only.
/// </summary>
public sealed class Permission : ValueObject
{
    private static readonly Regex CodePattern = new(
        "^[a-z][a-z0-9]*(\\.[a-z][a-z0-9-]*)+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private Permission(string code, string? description)
    {
        Code = code;
        Description = description;
    }

    /// <summary>Gets the normalized permission code.</summary>
    public string Code { get; }

    /// <summary>Gets the optional human-readable description.</summary>
    public string? Description { get; }

    /// <summary>Creates a validated permission.</summary>
    /// <param name="code">The permission code.</param>
    /// <param name="description">An optional description.</param>
    public static Result<Permission> Create(string? code, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Permission>(IdentityErrors.PermissionInvalid);
        }

        string normalized = code.Trim();

        if (!CodePattern.IsMatch(normalized))
        {
            return Result.Failure<Permission>(IdentityErrors.PermissionInvalid);
        }

        string? normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        return Result.Success(new Permission(normalized, normalizedDescription));
    }

    /// <inheritdoc />
    public override string ToString() => Code;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }
}
