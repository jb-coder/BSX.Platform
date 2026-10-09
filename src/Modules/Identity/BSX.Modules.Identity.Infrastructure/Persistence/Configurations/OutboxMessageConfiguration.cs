using BSX.Modules.Identity.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Kind).HasMaxLength(32).IsRequired();
        builder.Property(message => message.EventType).HasMaxLength(512).IsRequired();
        builder.Property(message => message.Payload).IsRequired();
        builder.Property(message => message.IsSensitive).IsRequired();
        builder.Property(message => message.Status).HasMaxLength(32).IsRequired();
        builder.Property(message => message.OccurredOnUtc).IsRequired();
        builder.Property(message => message.CreatedOnUtc).IsRequired();
        builder.Property(message => message.ProcessedOnUtc);
        builder.Property(message => message.RetryCount).IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(2048);

        builder.HasIndex(message => new { message.Status, message.OccurredOnUtc });
    }
}
