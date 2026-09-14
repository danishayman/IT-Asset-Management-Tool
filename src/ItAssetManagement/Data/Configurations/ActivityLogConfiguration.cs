using ItAssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItAssetManagement.Data.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Action)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(l => l.Details).IsRequired().HasMaxLength(500);
        builder.Property(l => l.Timestamp).IsRequired();

        // SetNull, not Cascade: the audit trail has to outlive the asset it describes,
        // otherwise the Delete entry would be destroyed by the very delete it records.
        builder.HasOne(l => l.Asset)
            .WithMany(a => a.ActivityLogs)
            .HasForeignKey(l => l.AssetId)
            .OnDelete(DeleteBehavior.SetNull);

        // Restrict: who performed the action is the whole point of an audit row, so a
        // user with history cannot be hard-deleted. The admin UI offers deactivation.
        builder.HasOne(l => l.PerformedByUser)
            .WithMany(u => u.ActivityLogs)
            .HasForeignKey(l => l.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.Timestamp);
        builder.HasIndex(l => l.Action);
        builder.HasIndex(l => l.AssetId);
    }
}
