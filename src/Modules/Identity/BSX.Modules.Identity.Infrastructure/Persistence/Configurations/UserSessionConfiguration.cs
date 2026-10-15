using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sessions = BSX.Modules.Identity.Domain.Sessions;
using ValueObjects = BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<Sessions.UserSession>
{
    public void Configure(EntityTypeBuilder<Sessions.UserSession> builder)
    {
        builder.ToTable("user_sessions");
        builder.HasKey(session => session.Id);

        builder.Property(session => session.Id).HasConversion(IdentityValueConverters.SessionId);
        builder.Property(session => session.UserId).HasConversion(IdentityValueConverters.UserId).IsRequired();
        builder.Property(session => session.TenantId).HasConversion(value => value!.Value, value => ValueObjects.TenantId.From(value));
        builder.Property(session => session.SecurityStamp).HasMaxLength(64).IsRequired();
        builder.Property(session => session.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(session => session.CreatedOnUtc).IsRequired();
        builder.Property(session => session.LastRefreshedOnUtc).IsRequired();
        builder.Property(session => session.IdleExpiresOnUtc).IsRequired();
        builder.Property(session => session.AbsoluteExpiresOnUtc).IsRequired();
        builder.Property(session => session.RevokedOnUtc);
        builder.Property(session => session.RevokedReason).HasMaxLength(128);

        builder.Ignore(session => session.DomainEvents);
        builder.Ignore(session => session.CurrentToken);

        builder.OwnsOne(session => session.Metadata, metadata =>
        {
            metadata.Property(value => value.UserAgent).HasColumnName("user_agent").HasMaxLength(512);
            metadata.Property(value => value.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
        });

        builder.HasMany(session => session.RefreshTokens)
            .WithOne()
            .HasForeignKey("SessionId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(session => new { session.UserId, session.Status });

        builder.ApplyAuditing(softDelete: false);
    }
}
