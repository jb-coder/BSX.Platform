using BSX.Modules.Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValueObjects = BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(role => role.Id);

        builder.Property(role => role.Id).HasConversion(IdentityValueConverters.RoleId);
        builder.Property(role => role.TenantId).HasConversion(value => value!.Value, value => ValueObjects.TenantId.From(value));
        builder.Property(role => role.Name).HasConversion(IdentityValueConverters.RoleName).HasMaxLength(100).IsRequired();
        builder.Property(role => role.Description).HasMaxLength(500);
        builder.Property(role => role.IsSystem).IsRequired();
        builder.Property(role => role.IsDeleted).IsRequired();

        builder.HasIndex(role => new { role.TenantId, role.Name }).IsUnique();
        builder.HasIndex(role => role.Name).IsUnique().HasFilter("\"TenantId\" IS NULL");

        builder.Ignore(role => role.DomainEvents);

        builder.OwnsMany(role => role.Permissions, permissions =>
        {
            permissions.ToTable("role_permissions");
            permissions.WithOwner().HasForeignKey("RoleId");
            permissions.Property(permission => permission.Code).HasColumnName("code").HasMaxLength(200);
            permissions.Property(permission => permission.Description).HasColumnName("description").HasMaxLength(500);
            permissions.HasKey("RoleId", "Code");
        });

        builder.ApplyAuditing(softDelete: false);
        builder.HasQueryFilter(role => !role.IsDeleted);
    }
}
