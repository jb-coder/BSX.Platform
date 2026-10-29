using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Errors;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Roles.UpdateRole;

/// <summary>Handles <see cref="UpdateRoleCommand"/>.</summary>
public sealed class UpdateRoleCommandHandler : ICommandHandler<UpdateRoleCommand>
{
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TenantGuard _tenantGuard;

    /// <summary>Initializes a new instance of the <see cref="UpdateRoleCommandHandler"/> class.</summary>
    public UpdateRoleCommandHandler(
        IRoleRepository roles,
        IUnitOfWork unitOfWork,
        TenantGuard tenantGuard)
    {
        _roles = roles;
        _unitOfWork = unitOfWork;
        _tenantGuard = tenantGuard;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(UpdateRoleCommand request, CancellationToken cancellationToken = default)
    {
        if (request.RoleId == Guid.Empty)
        {
            return Result.Failure(IdentityApplicationErrors.InvalidRoleId);
        }

        Role? role = await _roles
            .GetByIdAsync(RoleId.From(request.RoleId), cancellationToken)
            .ConfigureAwait(false);

        if (role is null)
        {
            return Result.Failure(IdentityApplicationErrors.RoleNotFound);
        }

        if (role.IsSystem)
        {
            return Result.Failure(IdentityErrors.RoleSystemImmutable);
        }

        Result guard = _tenantGuard.Ensure(role.TenantId?.Value);
        if (guard.IsFailure)
        {
            return guard;
        }

        Result<RoleName> name = RoleName.Create(request.Name);
        if (name.IsFailure)
        {
            return Result.Failure(name.Error);
        }

        if (role.Name != name.Value
            && await _roles.ExistsByNameAsync(role.TenantId, name.Value, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(IdentityApplicationErrors.RoleNameAlreadyExists);
        }

        Result rename = role.Rename(name.Value);
        if (rename.IsFailure)
        {
            return rename;
        }

        Result description = role.ChangeDescription(request.Description);
        if (description.IsFailure)
        {
            return description;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
