using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sessions = BSX.Modules.Identity.Domain.Sessions;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<Sessions.RefreshToken>
{
    public void Configure(EntityTypeBuilder<Sessions.RefreshToken> builder)
    {
        builder.ToTable("session_refresh_tokens");
        builder.HasKey(token => token.Id);

        builder.Property(token => token.Id).HasConversion(IdentityValueConverters.RefreshTokenId);
        builder.Property(token => token.Hash).HasConversion(IdentityValueConverters.RefreshTokenHash).HasMaxLength(256).IsRequired();
        builder.Property(token => token.IssuedOnUtc).IsRequired();
        builder.Property(token => token.ExpiresOnUtc).IsRequired();
        builder.Property(token => token.UsedOnUtc);
        builder.Property(token => token.RevokedOnUtc);
        builder.Property(token => token.ReplacedByTokenId).HasConversion(value => value!.Value, value => Sessions.RefreshTokenId.From(value));

        builder.Ignore(token => token.IsUsed);
        builder.HasIndex(token => token.Hash);
    }
}
