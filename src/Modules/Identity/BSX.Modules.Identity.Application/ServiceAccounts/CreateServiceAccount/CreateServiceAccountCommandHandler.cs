using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.ServiceAccounts;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.ServiceAccounts.CreateServiceAccount;

/// <summary>Handles <see cref="CreateServiceAccountCommand"/>.</summary>
public sealed class CreateServiceAccountCommandHandler : ICommandHandler<CreateServiceAccountCommand, CreateServiceAccountResponse>
{
    private readonly IServiceAccountRepository _serviceAccounts;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance of the <see cref="CreateServiceAccountCommandHandler"/> class.</summary>
    public CreateServiceAccountCommandHandler(IServiceAccountRepository serviceAccounts, IUnitOfWork unitOfWork)
    {
        _serviceAccounts = serviceAccounts;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<CreateServiceAccountResponse>> HandleAsync(CreateServiceAccountCommand request, CancellationToken cancellationToken = default)
    {
        if (request.TenantId == Guid.Empty)
        {
            return Result.Failure<CreateServiceAccountResponse>(IdentityApplicationErrors.InvalidTenantId);
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
