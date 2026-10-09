using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ValueObjects = BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>Value converters for the Identity strongly typed identifiers and value objects.</summary>
internal static class IdentityValueConverters
{
    public static readonly ValueConverter<ValueObjects.UserId, Guid> UserId =
        new(value => value.Value, value => ValueObjects.UserId.From(value));

    public static readonly ValueConverter<ValueObjects.TenantId, Guid> TenantId =
        new(value => value.Value, value => ValueObjects.TenantId.From(value));

    public static readonly ValueConverter<ValueObjects.RoleId, Guid> RoleId =
        new(value => value.Value, value => ValueObjects.RoleId.From(value));

    public static readonly ValueConverter<ValueObjects.MembershipId, Guid> MembershipId =
        new(value => value.Value, value => ValueObjects.MembershipId.From(value));

    public static readonly ValueConverter<ValueObjects.ServiceAccountId, Guid> ServiceAccountId =
        new(value => value.Value, value => ValueObjects.ServiceAccountId.From(value));

    public static readonly ValueConverter<ValueObjects.ApiKeyId, Guid> ApiKeyId =
        new(value => value.Value, value => ValueObjects.ApiKeyId.From(value));

    public static readonly ValueConverter<ValueObjects.CredentialId, Guid> CredentialId =
        new(value => value.Value, value => ValueObjects.CredentialId.From(value));

    public static readonly ValueConverter<ValueObjects.PasswordResetTokenId, Guid> PasswordResetTokenId =
        new(value => value.Value, value => ValueObjects.PasswordResetTokenId.From(value));

    public static readonly ValueConverter<ValueObjects.PrincipalId, Guid> PrincipalId =
        new(value => value.Value, value => ValueObjects.PrincipalId.From(value));

    public static readonly ValueConverter<ValueObjects.Email, string> Email =
        new(value => value.Value, value => ValueObjects.Email.Create(value).Value);

    public static readonly ValueConverter<ValueObjects.PersonName, string> PersonName =
        new(value => value.Value, value => ValueObjects.PersonName.Create(value).Value);

    public static readonly ValueConverter<ValueObjects.RoleName, string> RoleName =
        new(value => value.Value, value => ValueObjects.RoleName.Create(value).Value);

    public static readonly ValueConverter<ValueObjects.SecurityStamp, string> SecurityStamp =
        new(value => value.Value, value => ValueObjects.SecurityStamp.From(value));

    public static readonly ValueConverter<ValueObjects.PasswordResetTokenHash, string> PasswordResetTokenHash =
        new(value => value.Value, value => ValueObjects.PasswordResetTokenHash.From(value));

    public static readonly ValueConverter<ValueObjects.RecoveryCodeHash, string> RecoveryCodeHash =
        new(value => value.Value, value => ValueObjects.RecoveryCodeHash.From(value));

    public static readonly ValueConverter<ValueObjects.TotpSecret, string> TotpSecret =
        new(value => value.EncryptedValue, value => ValueObjects.TotpSecret.From(value));

    public static readonly ValueConverter<ValueObjects.ApiKeyPrefix, string> ApiKeyPrefix =
        new(value => value.Value, value => ValueObjects.ApiKeyPrefix.From(value));

    public static readonly ValueConverter<ValueObjects.ApiKeyHash, string> ApiKeyHash =
        new(value => value.Value, value => ValueObjects.ApiKeyHash.From(value));
}
