# Interface Contracts: ApplicationDbContext & Multi-Tenancy

**Feature**: `001-ef-core-multitenancy`  
**Date**: 2026-09-05

---

## 1. Domain & Application Contracts

### `ITenantEntity` (Core Boundary)
```csharp
namespace SmartErpAgent.Core.Interfaces;

public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
```

### `ITenantContext` (Scoped Resolution)
```csharp
namespace SmartErpAgent.Core.Interfaces;

public interface ITenantContext
{
    Guid? CurrentTenantId { get; }
    string? TenantCode { get; }
    void SetTenant(Guid tenantId, string? tenantCode = null);
}
```

### `IApplicationDbContext` (Application Persistence Contract)
```csharp
namespace SmartErpAgent.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLineItem> InvoiceLineItems { get; }
    DbSet<InventoryItem> InventoryItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

---

## 2. Global Query Filter Behavior Contract

```csharp
// Evaluated dynamically per query execution against active DbContext instance
modelBuilder.Entity<Invoice>()
    .HasQueryFilter(e => !e.IsDeleted && (CurrentTenantId == null || e.TenantId == CurrentTenantId));

modelBuilder.Entity<InventoryItem>()
    .HasQueryFilter(e => !e.IsDeleted && (CurrentTenantId == null || e.TenantId == CurrentTenantId));

modelBuilder.Entity<InvoiceLineItem>()
    .HasQueryFilter(e => !e.IsDeleted && (CurrentTenantId == null || e.Invoice.TenantId == CurrentTenantId));
```

---

## 3. SaveChangesAsync Interceptor Contract

1. **Audit Stamping**:
   - `EntityState.Added`: `CreatedAtUtc = DateTime.UtcNow`
   - `EntityState.Modified`: `UpdatedAtUtc = DateTime.UtcNow`
   - `EntityState.Deleted`: Intercepted to soft-delete: `State = EntityState.Modified`, `IsDeleted = true`, `UpdatedAtUtc = DateTime.UtcNow`
2. **Tenant Identity Stamping**:
   - For all added `ITenantEntity` where `TenantId == Guid.Empty`, if `CurrentTenantId.HasValue`, automatically assign `TenantId = CurrentTenantId.Value`.
