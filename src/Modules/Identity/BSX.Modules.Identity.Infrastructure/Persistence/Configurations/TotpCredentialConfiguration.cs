using BSX.Modules.Identity.Domain.Users.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class TotpCredentialConfiguration : IEntityTypeConfiguration<TotpCredential>
{
    public void Configure(EntityTypeBuilder<TotpCredential> builder)
    {
        builder.Property(credential => credential.Secret)
            .HasConversion(IdentityValueConverters.TotpSecret)
            .HasColumnName("totp_secret")
            .HasMaxLength(1024);

        builder.Property(credential => credential.VerifiedOnUtc).HasColumnName("totp_verified_on_utc");
    }
}
