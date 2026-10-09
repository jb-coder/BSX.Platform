using BSX.Modules.Identity.Domain.Users.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class WebAuthnCredentialConfiguration : IEntityTypeConfiguration<WebAuthnCredential>
{
    public void Configure(EntityTypeBuilder<WebAuthnCredential> builder)
    {
        builder.Property(credential => credential.CredentialIdentifier)
            .HasColumnName("webauthn_credential_id")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(credential => credential.PublicKey).HasColumnName("webauthn_public_key");
        builder.Property(credential => credential.SignCount).HasColumnName("webauthn_sign_count");
        builder.Property(credential => credential.Name).HasColumnName("webauthn_name").HasMaxLength(200);
    }
}
