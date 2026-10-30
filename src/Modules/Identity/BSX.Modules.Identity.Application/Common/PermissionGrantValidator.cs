using BSX.Contracts.Permissions;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Common;

/// <summary>
/// Validates permission grants against the platform permission catalog and the permission-scope
/// boundary (ADR-020). Every write path that accepts permission codes must use this validator so an
/// unknown or platform-scoped permission can never be persisted on a tenant role or a tenant-scoped
/// API key.
/// </summary>
internal static class PermissionGrantValidator
{
    /// <summary>
    /// Parses and validates permission codes that may be granted to a tenant-scoped principal.
    /// Codes must be syntactically valid, declared by the catalog and tenant-scoped. Duplicate codes
    /// are collapsed; the order of first occurrence is preserved.
    /// </summary>
    /// <param name="catalog">The authoritative platform permission catalog.</param>
    /// <param name="codes">The requested permission codes.</param>
    public static Result<IReadOnlyCollection<Permission>> ParseTenantGrantable(
        PermissionCatalog catalog,
        IEnumerable<string> codes)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(codes);

        var permissions = new List<Permission>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (string code in codes)
        {
            var parsed = Permission.Create(code);

            if (parsed.IsFailure)
            {
                return Result.Failure<IReadOnlyCollection<Permission>>(parsed.Error);
            }

            Permission permission = parsed.Value;

            if (!catalog.TryGet(permission.Code, out PermissionDefinition? definition))
            {
                return Result.Failure<IReadOnlyCollection<Permission>>(
                    IdentityApplicationErrors.PermissionUnknown(permission.Code));
            }

            if (definition!.Scope != PermissionScope.Tenant)
            {
                return Result.Failure<IReadOnlyCollection<Permission>>(
                    IdentityApplicationErrors.PermissionScopeForbidden(permission.Code));
            }

            if (seen.Add(permission.Code))
            {
                permissions.Add(permission);
            }
        }

        return Result.Success<IReadOnlyCollection<Permission>>(permissions);
    }
}
