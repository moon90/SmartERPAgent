using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartErpAgent.Core.Entities;

namespace SmartErpAgent.Infrastructure.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.TenantId)
            .IsRequired();

        builder.Property(item => item.SKU)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(item => new { item.TenantId, item.SKU })
            .IsUnique();

        builder.Property(item => item.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(item => item.Description)
            .HasMaxLength(1000);

        builder.Property(item => item.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(item => item.StockQuantity)
            .IsRequired();

        builder.Property(item => item.ReorderThreshold)
            .IsRequired()
            .HasDefaultValue(10);

        builder.Property(item => item.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(item => item.Tenant)
            .WithMany(t => t.InventoryItems)
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
