# Research & Architecture Decisions: Foundational EF Core Multi-Tenancy

**Feature**: `001-ef-core-multitenancy`  
**Date**: 2026-09-05  
**Status**: Completed

---

## 1. Multi-Tenancy Strategy: Single Database with Discriminator Column

### Decision
Use a shared database architecture where all tenant records reside in shared tables differentiated by a `TenantId: Guid` column, strictly isolated at the ORM layer using EF Core Global Query Filters.

### Rationale
- **Cost & Operational Efficiency**: Allows hosting thousands of B2B tenants on a unified Microsoft SQL Server pool without provisioning separate databases per tenant.
- **Maintenance**: Database migrations apply globally in a single migration pass, eliminating per-tenant schema synchronization lag.
- **Engine-Level Isolation**: EF Core's `HasQueryFilter` modifies every generated SQL `WHERE` clause dynamically, guaranteeing that no developer or agent can inadvertently query cross-tenant data.

### Alternatives Considered
- **Database-per-Tenant**: Highest physical isolation, but excessively high infrastructure overhead, expensive cold starts, and complex migration orchestration for early-to-mid-stage SaaS.
- **Schema-per-Tenant**: Good balance in PostgreSQL, but poorly supported and operationally clumsy in Microsoft SQL Server.

---

## 2. Dynamic Tenant Resolution & ITenantContext

### Decision
Implement a request-scoped `TenantContext : ITenantContext` populated by an ASP.NET Core middleware (`MultiTenantMiddleware`) from the `X-Tenant-ID` HTTP header (with query parameter fallback for dev/testing).

### Rationale
- `ITenantContext` is registered as `Scoped` in DI.
- `ApplicationDbContext` directly injects `ITenantContext`, ensuring that its lifetime matches the HTTP request or background scope.
- In `OnModelCreating`, the query filter evaluates `CurrentTenantId` dynamically per query:
  `e => !e.IsDeleted && (CurrentTenantId == null || e.TenantId == CurrentTenantId)`
- When `CurrentTenantId == null` (e.g. platform admin migrations or batch jobs), all non-deleted rows can be accessed. When set, only that tenant's rows are accessible.

### Alternatives Considered
- **AsyncLocal / Static Thread Context**: Can cause memory leaks and context bleeding across async tasks in .NET. Request-scoped DI is the recommended pattern.

---

## 3. Automated Tenant Stamping & Audit via SaveChangesAsync

### Decision
Override `ApplicationDbContext.SaveChangesAsync` to intercept `ChangeTracker.Entries<ITenantEntity>()` and `ChangeTracker.Entries<BaseEntity>()`.

### Rationale
- Automatically sets `CreatedAtUtc = DateTime.UtcNow` on new entities and `UpdatedAtUtc = DateTime.UtcNow` on modifications.
- If an entity implements `ITenantEntity` and has `TenantId == Guid.Empty`, it automatically injects `_tenantContext.CurrentTenantId.Value`.
- If an entity is marked for deletion, it converts the operation into a soft-delete (`IsDeleted = true`, `UpdatedAtUtc = DateTime.UtcNow`, `EntityState.Modified`), ensuring compliance and recovery capabilities.

### Alternatives Considered
- **Manual Assignment in Handlers/Services**: Prone to human error, missed assignments by AI agents, and code duplication.
- **EF Core SaveChangesInterceptor**: Separate interceptor class is clean, but overriding `SaveChangesAsync` directly in `ApplicationDbContext` keeps the multi-tenancy core self-contained with direct access to DbContext properties.

---

## 4. Decimal Precision in Microsoft SQL Server

### Decision
Configure explicit decimal precision on all monetary columns via Fluent API configurations: `builder.Property(x => x.TotalAmount).HasPrecision(18, 2)`.

### Rationale
- Default decimal mapping in SQL Server without precision can result in silent truncation or EF Core warnings. Explicit `(18, 2)` satisfies standard financial ERP ledger requirements.
