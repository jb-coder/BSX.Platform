using BSX.Modules.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");
        builder.HasKey(token => token.Id);

        builder.Property(token => token.Id).HasConversion(IdentityValueConverters.PasswordResetTokenId);
        builder.Property(token => token.Hash).HasConversion(IdentityValueConverters.PasswordResetTokenHash).HasMaxLength(256).IsRequired();
        builder.Property(token => token.IssuedOnUtc).IsRequired();
        builder.Property(token => token.ExpiresOnUtc).IsRequired();
        builder.Property(token => token.UsedOnUtc);

        builder.Ignore(token => token.IsUsed);

        builder.HasIndex(token => token.Hash);
    }
}
