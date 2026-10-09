using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Users.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class CredentialConfiguration : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        builder.ToTable("credentials");
        builder.HasKey(credential => credential.Id);

        builder.Property(credential => credential.Id).HasConversion(IdentityValueConverters.CredentialId);
        builder.Property(credential => credential.CreatedOnUtc).IsRequired();

        builder.Ignore(credential => credential.Type);

        builder.HasDiscriminator<string>("credential_type")
            .HasValue<PasswordCredential>(nameof(CredentialType.Password))
            .HasValue<TotpCredential>(nameof(CredentialType.Totp))
            .HasValue<RecoveryCodeCredential>(nameof(CredentialType.RecoveryCodes))
            .HasValue<WebAuthnCredential>(nameof(CredentialType.WebAuthn))
            .HasValue<ExternalProviderCredential>(nameof(CredentialType.ExternalProvider));
    }
}
