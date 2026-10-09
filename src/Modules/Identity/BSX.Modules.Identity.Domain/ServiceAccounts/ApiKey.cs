using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Errors;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.ServiceAccounts;

/// <summary>
/// A machine credential owned by a principal. The plaintext secret is shown once and only its
/// hash is stored (ADR-014).
/// </summary>
public sealed class ApiKey : AggregateRoot<ApiKeyId>
{
    private readonly HashSet<Permission> _scopes = [];

    private ApiKey(
        ApiKeyId id,
        PrincipalId principalId,
        PrincipalType principalType,
        string name,
        ApiKeyPrefix prefix,
        ApiKeyHash hash,
        DateTimeOffset createdOnUtc,
        DateTimeOffset? expiresOnUtc)
        : base(id)
    {
        PrincipalId = principalId;
        PrincipalType = principalType;
        Name = name;
        Prefix = prefix;
        Hash = hash;
        CreatedOnUtc = createdOnUtc;
        ExpiresOnUtc = expiresOnUtc;
    }

    private ApiKey()
    {
    }

    /// <summary>Gets the owning principal identifier.</summary>
    public PrincipalId PrincipalId { get; private set; } = null!;

    /// <summary>Gets the owning principal kind.</summary>
    public PrincipalType PrincipalType { get; private set; }

    /// <summary>Gets the operator label.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Gets the non-secret display prefix.</summary>
    public ApiKeyPrefix Prefix { get; private set; } = null!;

    /// <summary>Gets the secret hash.</summary>
    public ApiKeyHash Hash { get; private set; } = null!;

    /// <summary>Gets the creation timestamp.</summary>
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <summary>Gets the optional expiry.</summary>
    public DateTimeOffset? ExpiresOnUtc { get; private set; }

    /// <summary>Gets the revocation timestamp, if revoked.</summary>
    public DateTimeOffset? RevokedOnUtc { get; private set; }

    /// <summary>Gets the last-used timestamp, if used.</summary>
    public DateTimeOffset? LastUsedOnUtc { get; private set; }

    /// <summary>Gets the granted scopes.</summary>
    public IReadOnlyCollection<Permission> Scopes => _scopes.ToArray();

    /// <summary>Gets a value indicating whether the key is revoked.</summary>
    public bool IsRevoked => RevokedOnUtc.HasValue;

    /// <summary>Determines whether the key has expired at the given time.</summary>
    /// <param name="nowUtc">The current UTC time.</param>
    public bool IsExpiredAt(DateTimeOffset nowUtc) => ExpiresOnUtc.HasValue && nowUtc >= ExpiresOnUtc.Value;

    /// <summary>Determines whether the key is usable at the given time.</summary>
    /// <param name="nowUtc">The current UTC time.</param>
    public bool IsActiveAt(DateTimeOffset nowUtc) => !IsRevoked && !IsExpiredAt(nowUtc);

    /// <summary>Creates an API key.</summary>
    /// <param name="id">The key identifier.</param>
    /// <param name="principalId">The owning principal identifier.</param>
    /// <param name="principalType">The owning principal kind.</param>
    /// <param name="name">The operator label.</param>
    /// <param name="prefix">The display prefix.</param>
    /// <param name="hash">The secret hash.</param>
    /// <param name="createdOnUtc">The creation timestamp.</param>
    /// <param name="expiresOnUtc">The optional expiry.</param>
    /// <param name="scopes">The optional granted scopes.</param>
    public static Result<ApiKey> Create(
        ApiKeyId id,
        PrincipalId principalId,
        PrincipalType principalType,
        string? name,
        ApiKeyPrefix prefix,
        ApiKeyHash hash,
        DateTimeOffset createdOnUtc,
        DateTimeOffset? expiresOnUtc = null,
        IEnumerable<Permission>? scopes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<ApiKey>(IdentityErrors.NameInvalid);
        }

        if (expiresOnUtc.HasValue && expiresOnUtc.Value <= createdOnUtc)
        {
            return Result.Failure<ApiKey>(IdentityErrors.ApiKeyExpiryInvalid);
        }

        var apiKey = new ApiKey(id, principalId, principalType, name.Trim(), prefix, hash, createdOnUtc, expiresOnUtc);

        if (scopes is not null)
        {
            foreach (Permission scope in scopes)
            {
                apiKey._scopes.Add(scope);
            }
        }

        apiKey.RaiseDomainEvent(new ApiKeyCreated(id, principalId, apiKey.Name));
        return Result.Success(apiKey);
    }

    /// <summary>Adds a scope to the key.</summary>
    /// <param name="scope">The scope to add.</param>
    public Result AddScope(Permission scope)
        => _scopes.Add(scope)
            ? Result.Success()
            : Result.Failure(IdentityErrors.ApiKeyScopeAlreadyPresent);

    /// <summary>Removes a scope from the key.</summary>
    /// <param name="scope">The scope to remove.</param>
    public Result RemoveScope(Permission scope)
        => _scopes.Remove(scope)
            ? Result.Success()
            : Result.Failure(IdentityErrors.ApiKeyScopeNotPresent);

    /// <summary>Revokes the key.</summary>
    /// <param name="nowUtc">The current UTC time.</param>
    public Result Revoke(DateTimeOffset nowUtc)
    {
        if (IsRevoked)
        {
            return Result.Failure(IdentityErrors.ApiKeyAlreadyRevoked);
        }

        RevokedOnUtc = nowUtc;
        RaiseDomainEvent(new ApiKeyRevoked(Id, PrincipalId));
        return Result.Success();
    }

    /// <summary>Records a successful use of the key.</summary>
    /// <param name="nowUtc">The current UTC time.</param>
    public Result MarkUsed(DateTimeOffset nowUtc)
    {
        if (IsRevoked)
        {
            return Result.Failure(IdentityErrors.ApiKeyRevoked);
        }

        if (IsExpiredAt(nowUtc))
        {
            return Result.Failure(IdentityErrors.ApiKeyExpired);
        }

        LastUsedOnUtc = nowUtc;
        return Result.Success();
    }
}
