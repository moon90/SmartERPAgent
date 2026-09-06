using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SmartErpAgent.Application.Common.Interfaces;
using SmartErpAgent.Core.Entities;
using SmartErpAgent.Core.Interfaces;

namespace SmartErpAgent.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLineItem> PurchaseOrderLineItems => Set<PurchaseOrderLineItem>();

    public Guid? CurrentTenantId => _tenantContext.CurrentTenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply fluent configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure multi-tenant global query filter for all entities implementing ITenantEntity
        modelBuilder.Entity<Invoice>()
            .HasQueryFilter(e => !e.IsDeleted && (CurrentTenantId == null || e.TenantId == CurrentTenantId));

        modelBuilder.Entity<InventoryItem>()
            .HasQueryFilter(e => !e.IsDeleted && (CurrentTenantId == null || e.TenantId == CurrentTenantId));

        modelBuilder.Entity<InvoiceLineItem>()
            .HasQueryFilter(e => !e.IsDeleted && (CurrentTenantId == null || e.Invoice.TenantId == CurrentTenantId));

        modelBuilder.Entity<PurchaseOrder>()
            .HasQueryFilter(e => !e.IsDeleted && (CurrentTenantId == null || e.TenantId == CurrentTenantId));

        modelBuilder.Entity<PurchaseOrderLineItem>()
            .HasQueryFilter(e => !e.IsDeleted && (CurrentTenantId == null || e.TenantId == CurrentTenantId));
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = now;
                    break;
                case EntityState.Deleted:
                    // Soft-delete interceptor
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAtUtc = now;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty && _tenantContext.CurrentTenantId.HasValue)
            {
                entry.Entity.TenantId = _tenantContext.CurrentTenantId.Value;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
