using BSX.Modules.Identity.Domain.Memberships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValueObjects = BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("tenant_memberships");
        builder.HasKey(membership => membership.Id);

        builder.Property(membership => membership.Id).HasConversion(IdentityValueConverters.MembershipId);
        builder.Property(membership => membership.TenantId).HasConversion(IdentityValueConverters.TenantId).IsRequired();
        builder.Property(membership => membership.UserId).HasConversion(IdentityValueConverters.UserId).IsRequired();
        builder.Property(membership => membership.InvitedByUserId).HasConversion(value => value!.Value, value => ValueObjects.UserId.From(value));
        builder.Property(membership => membership.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(membership => membership.InvitedOnUtc).IsRequired();
        builder.Property(membership => membership.JoinedOnUtc);
        builder.Property(membership => membership.SuspendedOnUtc);

        builder.HasIndex(membership => new { membership.TenantId, membership.UserId }).IsUnique();

        builder.Ignore(membership => membership.DomainEvents);

        builder.OwnsMany(membership => membership.RoleIds, roles =>
        {
            roles.ToTable("tenant_membership_roles");
            roles.WithOwner().HasForeignKey("MembershipId");
            roles.Property(roleId => roleId.Value).HasColumnName("role_id");
            roles.HasKey("MembershipId", "Value");
        });

        builder.ApplyAuditing();
        builder.HasQueryFilter(membership => !EF.Property<bool>(membership, "IsDeleted"));
    }
}
