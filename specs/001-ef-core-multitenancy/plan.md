# Implementation Plan: Foundational EF Core Multi-Tenancy DbContext

**Branch**: `001-ef-core-multitenancy` | **Date**: 2026-09-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-ef-core-multitenancy/spec.md`

---

## Summary

Implement the core data persistence layer for the Smart ERP Agent platform using Entity Framework Core 8 and Microsoft SQL Server. The persistence engine enforces zero-leakage multi-tenancy across all customer organizations through automated EF Core Global Query Filters (`TenantId == CurrentTenantId`), request-scoped tenant resolution (`ITenantContext`), automated tenant identity stamping and audit tracking on insert via `ApplicationDbContext.SaveChangesAsync`, and soft-deletion.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8.0 (LTS)  
**Primary Dependencies**: `Microsoft.EntityFrameworkCore` 8.0.13, `Microsoft.EntityFrameworkCore.SqlServer` 8.0.13, `Microsoft.EntityFrameworkCore.Design` 8.0.13  
**Storage**: Microsoft SQL Server 2022 / Azure SQL Database  
**Testing**: xUnit with `Microsoft.EntityFrameworkCore.InMemory`  
**Target Platform**: Linux / macOS / Windows containerized Kestrel Web API  
**Project Type**: Enterprise Web Service / Clean Architecture Data Layer  
**Performance Goals**: Sub-50ms database query execution overhead; zero query parsing degradation from global query filters  
**Constraints**: Zero cross-tenant data leakage; all monetary fields must enforce precision `(18, 2)`  
**Scale/Scope**: Multi-tenant B2B SaaS architecture supporting thousands of isolated tenant organizations  

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Constitutional Principle | Requirement | Plan Conformance |
| :--- | :--- | :---: |
| **I. Clean Architecture & Controller Isolation** | Domain entities in `Core`, persistence in `Infrastructure`, no logic in controllers | **PASS** |
| **II. Strict Multi-Tenancy** | All tenant entities implement `ITenantEntity`; global query filters mandatory | **PASS** |
| **III. Strict Production-Grade Code** | `<Nullable>enable</Nullable>`, strong typing, no raw string queries | **PASS** |
| **IV. Modular Monorepo Architecture** | Clean separation of frontend and backend boundaries | **PASS** |
| **V. Decoupled AI Agent Orchestration** | DbContext exposed through `IApplicationDbContext` for Semantic Kernel plugins | **PASS** |

**Gate Status**: **PASSED**. No constitutional violations detected.

---

## Project Structure

### Documentation (this feature)

```text
specs/001-ef-core-multitenancy/
├── plan.md              # This file
├── research.md          # Multi-tenancy & EF Core query filter research
├── data-model.md        # Detailed entity schemas, keys & indexes
├── quickstart.md        # End-to-end testing and verification guide
├── contracts/
│   └── dbcontext-contract.md # C# interfaces & filter behavioral contracts
└── checklists/
    └── requirements.md  # Validated specification checklist
```

### Source Code Layout (Clean Architecture)

```text
backend/
├── src/
│   ├── SmartErpAgent.Core/
│   │   ├── Entities/
│   │   │   ├── BaseEntity.cs            # Root entity with Id, CreatedAtUtc, UpdatedAtUtc, IsDeleted
│   │   │   ├── Tenant.cs                # Tenant organization entity
│   │   │   ├── Invoice.cs               # Multi-tenant invoice header entity
│   │   │   ├── InvoiceLineItem.cs       # Invoice line item entity
│   │   │   └── InventoryItem.cs         # Multi-tenant inventory stock entity
│   │   ├── Interfaces/
│   │   │   ├── ITenantEntity.cs         # Marks entities bound to a TenantId
│   │   │   └── ITenantContext.cs        # Request-scoped tenant accessor contract
│   │   └── Enums/
│   │       └── InvoiceStatus.cs         # Draft, Sent, Paid, Overdue, Cancelled
│   │
│   ├── SmartErpAgent.Application/
│   │   ├── Common/Interfaces/
│   │   │   └── IApplicationDbContext.cs # Persistence abstraction for use cases & agents
│   │   └── DTOs/                        # TenantDto, InvoiceDto, InventoryItemDto
│   │
│   ├── SmartErpAgent.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs  # SQL Server DbContext with query filters & SaveChangesAsync
│   │   │   └── Configurations/          # Fluent API mappings (precision, indexes, keys)
│   │   │       ├── TenantConfiguration.cs
│   │   │       ├── InvoiceConfiguration.cs
│   │   │       └── InventoryItemConfiguration.cs
│   │   ├── Tenancy/
│   │   │   └── TenantContext.cs         # Scoped tenant resolution context
│   │   └── DependencyInjection.cs       # SQL Server connection string & DbContext registration
│   │
│   ├── SmartErpAgent.Api/
│   │   ├── Middlewares/
│   │   │   └── MultiTenantMiddleware.cs # Extracts X-Tenant-ID header and sets ITenantContext
│   │   └── Controllers/                 # TenantsController, InvoicesController, InventoryController
│   │
│   └── SmartErpAgent.AgentEngine/       # Semantic Kernel plugins utilizing IApplicationDbContext
│
└── tests/
    └── SmartErpAgent.UnitTests/
        └── TenantIsolationTests.cs      # Automated tests for query filters & tenant stamping
```

---

## Complexity Tracking

*No constitutional violations or unjustified complexity introduced.*
