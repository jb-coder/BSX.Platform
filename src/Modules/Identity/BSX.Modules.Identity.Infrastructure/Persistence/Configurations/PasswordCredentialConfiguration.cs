using BSX.Modules.Identity.Domain.Users.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class PasswordCredentialConfiguration : IEntityTypeConfiguration<PasswordCredential>
{
    public void Configure(EntityTypeBuilder<PasswordCredential> builder)
    {
        builder.OwnsOne(credential => credential.Hash, hash =>
        {
            hash.Property(value => value.Hash).HasColumnName("password_hash").HasMaxLength(512);
            hash.Property(value => value.Algorithm).HasColumnName("password_algorithm").HasMaxLength(64);
        });
    }
}
