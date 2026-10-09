using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Common;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Roles.CreateRole;

/// <summary>Handles <see cref="CreateRoleCommand"/>.</summary>
public sealed class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, CreateRoleResponse>
{
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance of the <see cref="CreateRoleCommandHandler"/> class.</summary>
    public CreateRoleCommandHandler(IRoleRepository roles, IUnitOfWork unitOfWork)
    {
        _roles = roles;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<CreateRoleResponse>> HandleAsync(CreateRoleCommand request, CancellationToken cancellationToken = default)
    {
        TenantId? tenantId = null;

        if (request.TenantId.HasValue)
        {
            if (request.TenantId.Value == Guid.Empty)
            {
                return Result.Failure<CreateRoleResponse>(IdentityApplicationErrors.InvalidTenantId);
            }

            tenantId = TenantId.From(request.TenantId.Value);
        }

        var nameResult = RoleName.Create(request.Name);

        if (nameResult.IsFailure)
        {
            return Result.Failure<CreateRoleResponse>(nameResult.Error);
        }

        var permissionsResult = PermissionCodes.Parse(request.Permissions);

        if (permissionsResult.IsFailure)
        {
            return Result.Failure<CreateRoleResponse>(permissionsResult.Error);
        }

        if (await _roles.ExistsByNameAsync(tenantId, nameResult.Value, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<CreateRoleResponse>(IdentityApplicationErrors.RoleNameAlreadyExists);
        }

        var roleResult = Role.Create(RoleId.New(), tenantId, nameResult.Value, permissionsResult.Value, request.Description);

        if (roleResult.IsFailure)
        {
            return Result.Failure<CreateRoleResponse>(roleResult.Error);
        }

        await _roles.AddAsync(roleResult.Value, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new CreateRoleResponse(roleResult.Value.Id.Value));
    }
}
