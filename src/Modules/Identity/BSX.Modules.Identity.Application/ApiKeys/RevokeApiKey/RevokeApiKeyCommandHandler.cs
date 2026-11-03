using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.ServiceAccounts;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.ApiKeys.RevokeApiKey;

/// <summary>Handles <see cref="RevokeApiKeyCommand"/>.</summary>
public sealed class RevokeApiKeyCommandHandler : ICommandHandler<RevokeApiKeyCommand>
{
    private readonly IApiKeyRepository _apiKeys;
    private readonly IServiceAccountRepository _serviceAccounts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TenantGuard _tenantGuard;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="RevokeApiKeyCommandHandler"/> class.</summary>
    public RevokeApiKeyCommandHandler(
        IApiKeyRepository apiKeys,
        IServiceAccountRepository serviceAccounts,
        IUnitOfWork unitOfWork,
        TenantGuard tenantGuard,
        TimeProvider timeProvider)
    {
        _apiKeys = apiKeys;
        _serviceAccounts = serviceAccounts;
        _unitOfWork = unitOfWork;
        _tenantGuard = tenantGuard;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(RevokeApiKeyCommand request, CancellationToken cancellationToken = default)
    {
        if (request.ApiKeyId == Guid.Empty)
        {
            return Result.Failure(IdentityApplicationErrors.InvalidApiKeyId);
        }

        ApiKey? apiKey = await _apiKeys.GetByIdAsync(ApiKeyId.From(request.ApiKeyId), cancellationToken).ConfigureAwait(false);

        if (apiKey is null || apiKey.PrincipalType != PrincipalType.ServiceAccount)
        {
            return Result.Failure(IdentityApplicationErrors.ApiKeyNotFound);
        }

        ServiceAccount? account = await _serviceAccounts
            .GetByIdAsync(ServiceAccountId.From(apiKey.PrincipalId.Value), cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            // The key references a missing principal: fail closed without revealing whether the key
            // or its owner is the missing record.
            return Result.Failure(IdentityApplicationErrors.ApiKeyNotFound);
        }

        Result tenantGuard = _tenantGuard.Ensure(account.TenantId.Value);
        if (tenantGuard.IsFailure)
        {
            return tenantGuard;
        }

        Result result = apiKey.Revoke(_timeProvider.GetUtcNow());

        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
