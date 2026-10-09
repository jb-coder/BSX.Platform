using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Common;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.ServiceAccounts;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.ApiKeys.CreateApiKey;

/// <summary>Handles <see cref="CreateApiKeyCommand"/>.</summary>
public sealed class CreateApiKeyCommandHandler : ICommandHandler<CreateApiKeyCommand, CreateApiKeyResponse>
{
    private const int SecretByteLength = 32;
    private const int PrefixLength = 8;

    private readonly IServiceAccountRepository _serviceAccounts;
    private readonly IApiKeyRepository _apiKeys;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly IApiKeyHasher _apiKeyHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="CreateApiKeyCommandHandler"/> class.</summary>
    public CreateApiKeyCommandHandler(
        IServiceAccountRepository serviceAccounts,
        IApiKeyRepository apiKeys,
        ISecureTokenGenerator tokenGenerator,
        IApiKeyHasher apiKeyHasher,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _serviceAccounts = serviceAccounts;
        _apiKeys = apiKeys;
        _tokenGenerator = tokenGenerator;
        _apiKeyHasher = apiKeyHasher;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<CreateApiKeyResponse>> HandleAsync(CreateApiKeyCommand request, CancellationToken cancellationToken = default)
    {
        if (request.ServiceAccountId == Guid.Empty)
        {
            return Result.Failure<CreateApiKeyResponse>(IdentityApplicationErrors.InvalidServiceAccountId);
        }

        ServiceAccount? account = await _serviceAccounts
            .GetByIdAsync(ServiceAccountId.From(request.ServiceAccountId), cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return Result.Failure<CreateApiKeyResponse>(IdentityApplicationErrors.ServiceAccountNotFound);
        }

        IReadOnlyCollection<Permission>? scopes = null;

        if (request.Scopes is { Count: > 0 })
        {
            var parsed = PermissionCodes.Parse(request.Scopes);

            if (parsed.IsFailure)
            {
                return Result.Failure<CreateApiKeyResponse>(parsed.Error);
            }

            scopes = parsed.Value;
        }

        string secret = _tokenGenerator.GenerateToken(SecretByteLength);
        string prefix = secret[..Math.Min(PrefixLength, secret.Length)];

        var apiKeyResult = ApiKey.Create(
            ApiKeyId.New(),
            account.Principal,
            PrincipalType.ServiceAccount,
            request.Name,
            ApiKeyPrefix.From(prefix),
            _apiKeyHasher.Hash(secret),
            _timeProvider.GetUtcNow(),
            request.ExpiresOnUtc,
            scopes);

        if (apiKeyResult.IsFailure)
        {
            return Result.Failure<CreateApiKeyResponse>(apiKeyResult.Error);
        }

        await _apiKeys.AddAsync(apiKeyResult.Value, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new CreateApiKeyResponse(apiKeyResult.Value.Id.Value, secret, prefix));
    }
}
