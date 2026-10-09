using BSX.Modules.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id).HasConversion(IdentityValueConverters.UserId);
        builder.Property(user => user.Email).HasConversion(IdentityValueConverters.Email).HasMaxLength(320).IsRequired();
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Property(user => user.Name).HasConversion(IdentityValueConverters.PersonName).HasMaxLength(200).IsRequired();
        builder.Property(user => user.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(user => user.SecurityStamp).HasConversion(IdentityValueConverters.SecurityStamp).HasMaxLength(64).IsRequired();
        builder.Property(user => user.RegisteredOnUtc).IsRequired();
        builder.Property(user => user.AccessFailedCount).IsRequired();
        builder.Property(user => user.LockoutEndUtc);

        builder.Ignore(user => user.DomainEvents);
        builder.Ignore(user => user.Principal);
        builder.Ignore(user => user.HasAnyCredential);
        builder.Ignore(user => user.IsMfaEnabled);

        builder.OwnsMany(user => user.PlatformRoleIds, roles =>
        {
            roles.ToTable("user_platform_roles");
            roles.WithOwner().HasForeignKey("UserId");
            roles.Property(roleId => roleId.Value).HasColumnName("role_id");
            roles.HasKey("UserId", "Value");
        });

        builder.HasMany(user => user.Credentials)
            .WithOne()
            .HasForeignKey("UserId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(user => user.PasswordResetToken)
            .WithOne()
            .HasForeignKey<PasswordResetToken>("UserId")
            .OnDelete(DeleteBehavior.Cascade);

        // Plain shadow columns mirroring the searchable value objects, populated by the auditing
        // interceptor so read queries can filter without touching value-converted columns.
        builder.Property<string>("SearchEmail").HasMaxLength(320);
        builder.Property<string>("SearchName").HasMaxLength(200);
        builder.HasIndex("SearchEmail");

        builder.ApplyAuditing();
        builder.HasQueryFilter(user => !EF.Property<bool>(user, "IsDeleted"));
    }
}
