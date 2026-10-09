using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Application.Mappings;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Roles.GetRoleById;

/// <summary>Handles <see cref="GetRoleByIdQuery"/>.</summary>
public sealed class GetRoleByIdQueryHandler : IQueryHandler<GetRoleByIdQuery, RoleDto>
{
    private readonly IRoleRepository _roles;

    /// <summary>Initializes a new instance of the <see cref="GetRoleByIdQueryHandler"/> class.</summary>
    public GetRoleByIdQueryHandler(IRoleRepository roles) => _roles = roles;

    /// <inheritdoc />
    public async Task<Result<RoleDto>> HandleAsync(GetRoleByIdQuery request, CancellationToken cancellationToken = default)
    {
        if (request.RoleId == Guid.Empty)
        {
            return Result.Failure<RoleDto>(IdentityApplicationErrors.InvalidRoleId);
        }

        Role? role = await _roles.GetByIdAsync(RoleId.From(request.RoleId), cancellationToken).ConfigureAwait(false);

        if (role is null)
        {
            return Result.Failure<RoleDto>(IdentityApplicationErrors.RoleNotFound);
        }

        return Result.Success(role.ToDto());
    }
}
