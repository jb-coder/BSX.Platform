using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Common;

/// <summary>Parses permission codes into domain value objects.</summary>
internal static class PermissionCodes
{
    /// <summary>Parses a collection of permission codes.</summary>
    /// <param name="codes">The permission codes.</param>
    public static Result<IReadOnlyCollection<Permission>> Parse(IEnumerable<string> codes)
    {
        var permissions = new List<Permission>();

        foreach (string code in codes)
        {
            var result = Permission.Create(code);

            if (result.IsFailure)
            {
                return Result.Failure<IReadOnlyCollection<Permission>>(result.Error);
            }

            permissions.Add(result.Value);
        }

        return Result.Success<IReadOnlyCollection<Permission>>(permissions);
    }
}
