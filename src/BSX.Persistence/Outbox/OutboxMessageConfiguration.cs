using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BSX.Persistence.Outbox;

/// <summary>Relational mapping for the platform outbox table (ADR-009).</summary>
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Kind).HasMaxLength(32).IsRequired();
        builder.Property(message => message.EventType).HasMaxLength(512).IsRequired();
        builder.Property(message => message.Payload).IsRequired();
        builder.Property(message => message.Status).HasMaxLength(32).IsRequired();
        builder.Property(message => message.OccurredOnUtc).IsRequired();
        builder.Property(message => message.CreatedOnUtc).IsRequired();
        builder.Property(message => message.ProcessedOnUtc);
        builder.Property(message => message.LastAttemptOnUtc);
        builder.Property(message => message.NextAttemptOnUtc);
        builder.Property(message => message.ProcessingStartedOnUtc);
        builder.Property(message => message.RetryCount).IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(2048);

        builder.HasIndex(message => new { message.Status, message.OccurredOnUtc });
        builder.HasIndex(message => new { message.Status, message.NextAttemptOnUtc });
        builder.HasIndex(message => message.EventId).IsUnique();
    }
}
