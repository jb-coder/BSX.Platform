using BSX.Modules.Identity.Domain.Users.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class ExternalProviderCredentialConfiguration : IEntityTypeConfiguration<ExternalProviderCredential>
{
    public void Configure(EntityTypeBuilder<ExternalProviderCredential> builder)
    {
        builder.Property(credential => credential.Provider)
            .HasColumnName("external_provider")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(credential => credential.SubjectId)
            .HasColumnName("external_subject_id")
            .HasMaxLength(512)
            .IsRequired();
    }
}
