using BSX.Modules.Identity.Domain.Users.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValueObjects = BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class RecoveryCodeCredentialConfiguration : IEntityTypeConfiguration<RecoveryCodeCredential>
{
    public void Configure(EntityTypeBuilder<RecoveryCodeCredential> builder)
    {
        builder.Ignore(credential => credential.Codes);
        builder.Ignore(credential => credential.RemainingCount);

        builder.OwnsMany<ValueObjects.RecoveryCodeHash>("_codes", codes =>
        {
            codes.ToTable("recovery_codes");
            codes.WithOwner().HasForeignKey("CredentialId");
            codes.Property(code => code.Value).HasColumnName("code").HasMaxLength(256);
            codes.HasKey("CredentialId", "Value");
        });

        builder.OwnsMany<ValueObjects.RecoveryCodeHash>("_used", used =>
        {
            used.ToTable("recovery_codes_used");
            used.WithOwner().HasForeignKey("CredentialId");
            used.Property(code => code.Value).HasColumnName("code").HasMaxLength(256);
            used.HasKey("CredentialId", "Value");
        });
    }
}
