using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VenuePass.Modules.Events.Infrastructure.Outbox;

namespace VenuePass.Modules.Events.Infrastructure.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(x => x.OccurredOn)
            .HasColumnName("occurred_on")
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.AttemptCount)
            .HasColumnName("attempt_count")
            .IsRequired();

        builder.Property(x => x.LastAttemptedOn)
            .HasColumnName("last_attempted_on");

        builder.Property(x => x.NextAttemptOn)
            .HasColumnName("next_attempt_on");

        builder.Property(x => x.ProcessedOn)
            .HasColumnName("processed_on");

        builder.Property(x => x.Error)
            .HasColumnName("error")
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(x => new { x.ProcessedOn, x.NextAttemptOn })
            .HasDatabaseName("IX_outbox_messages_dispatch");

        builder.HasIndex(x => x.OccurredOn)
            .HasDatabaseName("IX_outbox_messages_occurred_on");
    }
}