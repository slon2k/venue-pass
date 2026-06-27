namespace VenuePass.Modules.Attendance.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using VenuePass.Modules.Attendance.Domain.Common;
using VenuePass.Modules.Attendance.Domain.PublishedEvents;
using VenuePass.Modules.Attendance.Domain.ScanAttempts;

public sealed class ScanAttemptConfiguration : IEntityTypeConfiguration<ScanAttempt>
{
    public void Configure(EntityTypeBuilder<ScanAttempt> builder)
    {
        builder.ToTable("scan_attempts");

        builder.Ignore(x => x.DomainEvents);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new ScanAttemptId(value))
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(x => x.PublishedEventReferenceId)
            .HasConversion(
                id => id.Value,
                value => new PublishedEventReferenceId(value))
            .HasColumnName("published_event_reference_id")
            .IsRequired();

        builder.Property(x => x.SubmittedTicketCode)
            .HasMaxLength(SubmittedTicketCode.MaxLength)
            .HasConversion(
                code => code.Value,
                value => new SubmittedTicketCode(value))
            .HasColumnName("submitted_ticket_code")
            .IsRequired();

        builder.Property(x => x.NormalizedTicketCode)
            .HasMaxLength(TicketCode.Length)
            .IsFixedLength()
            .HasConversion(
                code => code.HasValue ? code.Value.Value : null,
                value => value != null ? new TicketCode(value) : null)
            .HasColumnName("normalized_ticket_code");

        builder.Property(x => x.Outcome)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasColumnName("outcome")
            .IsRequired();

        builder.Property(x => x.RejectionCategory)
            .HasConversion<string>()
            .HasMaxLength(64)
            .HasColumnName("rejection_category")
            .IsRequired();

        builder.Property(x => x.ScannedAt)
            .HasColumnName("scanned_at")
            .IsRequired();

        builder.Property(x => x.TicketId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value == null ? null : new TicketId(value.Value))
            .HasColumnName("ticket_id");

        builder.HasIndex(x => new { x.PublishedEventReferenceId, x.ScannedAt })
            .HasDatabaseName("IX_scan_attempts_published_event_reference_id_scanned_at");

        builder.HasIndex(x => x.TicketId)
            .HasDatabaseName("IX_scan_attempts_ticket_id");

        builder.HasIndex(x => x.NormalizedTicketCode)
            .HasDatabaseName("IX_scan_attempts_normalized_ticket_code");

        builder.HasOne<PublishedEventReference>()
            .WithMany()
            .HasForeignKey(x => x.PublishedEventReferenceId)
            .OnDelete(DeleteBehavior.Restrict);     
    }
}