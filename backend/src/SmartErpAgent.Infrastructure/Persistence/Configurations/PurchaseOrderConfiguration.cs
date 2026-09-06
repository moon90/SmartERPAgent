using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartErpAgent.Core.Entities;

namespace SmartErpAgent.Infrastructure.Persistence.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");

        builder.HasKey(po => po.Id);

        builder.Property(po => po.TenantId)
            .IsRequired();

        builder.Property(po => po.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(po => new { po.TenantId, po.OrderNumber })
            .IsUnique();

        builder.Property(po => po.SupplierName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(po => po.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(po => po.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("USD");

        builder.Property(po => po.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(po => po.Notes)
            .HasMaxLength(1000);

        // Foreign Key to Tenant
        builder.HasOne(po => po.Tenant)
            .WithMany(t => t.PurchaseOrders)
            .HasForeignKey(po => po.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many relationship with LineItems
        builder.HasMany(po => po.LineItems)
            .WithOne(li => li.PurchaseOrder)
            .HasForeignKey(li => li.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PurchaseOrderLineItemConfiguration : IEntityTypeConfiguration<PurchaseOrderLineItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLineItem> builder)
    {
        builder.ToTable("PurchaseOrderLineItems");

        builder.HasKey(li => li.Id);

        builder.Property(li => li.TenantId)
            .IsRequired();

        builder.Property(li => li.SKU)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(li => li.ItemName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(li => li.Quantity)
            .IsRequired();

        builder.Property(li => li.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(li => li.TotalPrice)
            .HasPrecision(18, 2);

        // Optional relationship with InventoryItem
        builder.HasOne(li => li.InventoryItem)
            .WithMany()
            .HasForeignKey(li => li.InventoryItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
