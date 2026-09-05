using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartErpAgent.Core.Entities;

namespace SmartErpAgent.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(t => t.Code)
            .IsUnique();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.AdminEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(t => t.SubscriptionTier)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Standard");

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired();

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
