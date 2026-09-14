using ItAssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItAssetManagement.Data.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AssetTag).IsRequired().HasMaxLength(30);
        builder.HasIndex(a => a.AssetTag).IsUnique();

        builder.Property(a => a.Name).IsRequired().HasMaxLength(120);
        builder.Property(a => a.SerialNumber).IsRequired().HasMaxLength(60);
        builder.Property(a => a.Location).IsRequired().HasMaxLength(80);

        // Persisted as text, not as the underlying int. Costs nothing at runtime and
        // means anyone reading the table or seed.sql sees "Repair" rather than "2".
        builder.Property(a => a.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(a => a.PurchaseCost).IsRequired().HasPrecision(18, 2);
        builder.Property(a => a.PurchaseDate).IsRequired();
        builder.Property(a => a.WarrantyExpiry).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();
        // UpdatedAt doubles as the optimistic concurrency token. Every update already
        // rewrites it, so it uniquely identifies a row's revision without adding a column.
        // EF puts its original value in the UPDATE's WHERE clause, so if someone else saved
        // first the statement matches no rows and the second save is refused rather than
        // silently overwriting their work. Postgres's xmin would be the textbook choice, but
        // EF Core 10 insists on emitting DDL to create a column of that reserved name.
        builder.Property(a => a.UpdatedAt).IsRequired().IsConcurrencyToken();

        // Restrict: a category that still has assets on it must be emptied first,
        // rather than silently taking its assets down with it.
        builder.HasOne(a => a.Category)
            .WithMany(c => c.Assets)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Removing a user releases their hardware back to the pool instead of deleting it.
        builder.HasOne(a => a.AssignedToUser)
            .WithMany(u => u.AssignedAssets)
            .HasForeignKey(a => a.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Supports the list filters and the search box without a sequential scan.
        builder.HasIndex(a => a.Name);
        builder.HasIndex(a => a.SerialNumber);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.CategoryId);
        builder.HasIndex(a => a.AssignedToUserId);
    }
}
