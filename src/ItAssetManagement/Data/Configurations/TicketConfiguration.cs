using ItAssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItAssetManagement.Data.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(150);
        builder.Property(t => t.Description).IsRequired().HasMaxLength(2000);
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(t => t.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.HasOne(t => t.RaisedByUser)
            .WithMany(u => u.Tickets)
            .HasForeignKey(t => t.RaisedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // A ticket outlives the asset being scrapped, which is often why it was raised.
        builder.HasOne(t => t.Asset)
            .WithMany(a => a.Tickets)
            .HasForeignKey(t => t.AssetId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.CreatedAt);
    }
}
