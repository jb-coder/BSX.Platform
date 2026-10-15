using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Domain.ServiceAccounts;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.ServiceAccounts.CreateServiceAccount;

/// <summary>Handles <see cref="CreateServiceAccountCommand"/>.</summary>
public sealed class CreateServiceAccountCommandHandler : ICommandHandler<CreateServiceAccountCommand, CreateServiceAccountResponse>
{
    private readonly IServiceAccountRepository _serviceAccounts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TenantGuard _tenantGuard;

    /// <summary>Initializes a new instance of the <see cref="CreateServiceAccountCommandHandler"/> class.</summary>
    public CreateServiceAccountCommandHandler(
        IServiceAccountRepository serviceAccounts,
        IUnitOfWork unitOfWork,
        TenantGuard tenantGuard)
    {
        _serviceAccounts = serviceAccounts;
        _unitOfWork = unitOfWork;
        _tenantGuard = tenantGuard;
    }

    /// <inheritdoc />
    public async Task<Result<CreateServiceAccountResponse>> HandleAsync(CreateServiceAccountCommand request, CancellationToken cancellationToken = default)
    {
        Result guard = _tenantGuard.Ensure(request.TenantId);
        if (guard.IsFailure)
        {
            return Result.Failure<CreateServiceAccountResponse>(guard.Error);
        }

        var accountResult = ServiceAccount.Create(ServiceAccountId.New(), TenantId.From(request.TenantId), request.Name);

        if (accountResult.IsFailure)
        {
            return Result.Failure<CreateServiceAccountResponse>(accountResult.Error);
        }

        await _serviceAccounts.AddAsync(accountResult.Value, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new CreateServiceAccountResponse(accountResult.Value.Id.Value));
    }
}
