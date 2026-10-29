using BSX.BuildingBlocks.Cqrs;
using BSX.Contracts.Permissions;
using BSX.Modules.Identity.Application.Dtos;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Permissions.ListPermissions;

/// <summary>Handles <see cref="ListPermissionsQuery"/>.</summary>
public sealed class ListPermissionsQueryHandler
    : IQueryHandler<ListPermissionsQuery, IReadOnlyList<PermissionDto>>
{
    private readonly PermissionCatalog _catalog;

    /// <summary>Initializes a new instance of the <see cref="ListPermissionsQueryHandler"/> class.</summary>
    public ListPermissionsQueryHandler(PermissionCatalog catalog) => _catalog = catalog;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<PermissionDto>>> HandleAsync(
        ListPermissionsQuery request,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PermissionDto> permissions = _catalog.Permissions
            .OrderBy(permission => permission.Code, StringComparer.Ordinal)
            .Select(permission => new PermissionDto(permission.Code, permission.Description))
            .ToArray();

        return Task.FromResult(Result.Success(permissions));
    }
}
