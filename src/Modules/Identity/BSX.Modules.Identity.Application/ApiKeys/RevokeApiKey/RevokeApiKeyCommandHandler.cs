using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.ServiceAccounts;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.ApiKeys.RevokeApiKey;

/// <summary>Handles <see cref="RevokeApiKeyCommand"/>.</summary>
public sealed class RevokeApiKeyCommandHandler : ICommandHandler<RevokeApiKeyCommand>
{
    private readonly IApiKeyRepository _apiKeys;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="RevokeApiKeyCommandHandler"/> class.</summary>
    public RevokeApiKeyCommandHandler(IApiKeyRepository apiKeys, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _apiKeys = apiKeys;
        _unitOfWork = unitOfWork;
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

        if (apiKey is null)
        {
            return Result.Failure(IdentityApplicationErrors.ApiKeyNotFound);
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
