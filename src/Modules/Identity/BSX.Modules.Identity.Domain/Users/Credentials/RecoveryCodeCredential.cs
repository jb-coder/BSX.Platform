using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Errors;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.Users.Credentials;

/// <summary>A set of single-use recovery codes used for MFA recovery.</summary>
public sealed class RecoveryCodeCredential : Credential
{
    private readonly List<RecoveryCodeHash> _codes = [];
    private readonly HashSet<RecoveryCodeHash> _used = [];

    internal RecoveryCodeCredential(CredentialId id, IEnumerable<RecoveryCodeHash> codes, DateTimeOffset createdOnUtc)
        : base(id, createdOnUtc)
        => _codes.AddRange(codes);

    private RecoveryCodeCredential()
    {
    }

    /// <inheritdoc />
    public override CredentialType Type => CredentialType.RecoveryCodes;

    /// <summary>Gets all configured recovery code hashes.</summary>
    public IReadOnlyCollection<RecoveryCodeHash> Codes => _codes.AsReadOnly();

    /// <summary>Gets the number of unused recovery codes.</summary>
    public int RemainingCount => _codes.Count - _used.Count;

    /// <summary>Consumes a recovery code.</summary>
    /// <param name="code">The hashed code to consume.</param>
    public Result Consume(RecoveryCodeHash code)
    {
        if (!_codes.Contains(code))
        {
            return Result.Failure(IdentityErrors.RecoveryCodeInvalid);
        }

        if (_used.Contains(code))
        {
            return Result.Failure(IdentityErrors.RecoveryCodeUsed);
        }

        _used.Add(code);
        return Result.Success();
    }
}
