# Implementation Plan: Automated Restocking & Purchase Order Agent Plugin

**Branch**: `009-purchase-order-agent` | **Date**: 2026-09-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/009-purchase-order-agent/spec.md`

---

## Summary

Implement the `PurchaseOrderAgentPlugin` in `SmartErpAgent.AgentEngine` to autonomously manage inventory replenishment and draft purchase orders. The feature introduces `PurchaseOrder` and `PurchaseOrderLineItem` domain entities in `Core` with EF Core persistence and tenant isolation in `Infrastructure`. It implements `GetLowStockAlertsAsync` to detect items at or below reorder thresholds and `CreateDraftPurchaseOrderAsync` to calculate replenishment requirements, estimate supplier costs, and persist draft purchase orders. It integrates with `SemanticKernelAgentOrchestrator` with autonomous tool discovery and offline fallback routing.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8 (`<Nullable>enable</Nullable>`)  
**Primary Dependencies**: Microsoft Semantic Kernel 1.40+, Entity Framework Core 8, Microsoft.AspNetCore.SignalR  
**Storage**: Microsoft SQL Server 2022 (via `ApplicationDbContext`)  
**Testing**: xUnit, FluentAssertions, Moq (`dotnet test`)  
**Target Platform**: ASP.NET Core 8 Web API  
**Project Type**: Clean Architecture enterprise ERP micro-service  
**Performance Goals**: Low-stock scan in < 100ms, draft PO generation in < 250ms  
**Constraints**: Strict multi-tenant isolation (`TenantId` global query filters), zero raw SQL, zero warnings  
**Scale/Scope**: Multi-tenant organizations managing hundreds of active SKUs and purchase orders  

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate Status | Architecture Compliance Notes |
| :--- | :--- | :--- |
| **I. Clean Architecture & Controller Isolation** | ✅ PASSED | Domain entities in `Core`, persistence in `Infrastructure`, DTOs in `Application`, AI plugin in `AgentEngine`. Controllers remain purely presentation-layer adapters. |
| **II. Strict Multi-Tenancy** | ✅ PASSED | `PurchaseOrder` and `PurchaseOrderLineItem` implement `ITenantEntity`. Ambient `CurrentTenantId` is automatically assigned in `ApplicationDbContext.SaveChangesAsync` and filtered at the engine level. |
| **III. Strict Production-Grade Code Standards** | ✅ PASSED | Strict C# nullable reference types, strongly typed records, defensive input validation, and zero compiler warnings. |
| **IV. Modular Monorepo Architecture** | ✅ PASSED | Backend API contracts mirror standard JSON representations consumable by Angular `@smart-erp/data-access`. |
| **V. Decoupled AI Agent Orchestration** | ✅ PASSED | Native Semantic Kernel plugin using `[KernelFunction]` and `[Description]` attributes, imported into the Kernel without model coupling. |

---

## Project Structure

### Documentation (this feature)

```text
specs/009-purchase-order-agent/
├── spec.md                              # Requirements and user stories
├── plan.md                              # This implementation plan
├── research.md                          # Decisions on entities, math, and plugin design
├── data-model.md                        # ER diagrams, schemas, and DTO definitions
├── contracts/
│   └── purchase-order-plugin.json       # JSON tool contract schema
├── checklists/
│   └── requirements.md                  # Quality specification checklist
└── quickstart.md                        # Verification guide and test commands
```

### Source Code Touch-points

```text
backend/
├── src/
│   ├── SmartErpAgent.Core/
│   │   ├── Entities/
│   │   │   ├── PurchaseOrder.cs         # [NEW] Domain entity
│   │   │   └── PurchaseOrderLineItem.cs # [NEW] Domain entity
│   │   └── Enums/
│   │       └── PurchaseOrderStatus.cs   # [NEW] Order status enum
│   ├── SmartErpAgent.Application/
│   │   ├── Common/Interfaces/
│   │   │   └── IApplicationDbContext.cs # [MODIFY] Add PurchaseOrders and LineItems
│   │   └── DTOs/
│   │       └── PurchaseOrderDtos.cs     # [NEW] LowStockAlertDto & DraftPurchaseOrderResultDto
│   ├── SmartErpAgent.Infrastructure/
│   │   └── Persistence/
│   │       ├── ApplicationDbContext.cs  # [MODIFY] Expose DbSets and apply query filters
│   │       └── Configurations/
│   │           └── PurchaseOrderConfiguration.cs # [NEW] EF Core entity configurations
│   └── SmartErpAgent.AgentEngine/
│       ├── Plugins/
│       │   └── PurchaseOrderAgentPlugin.cs # [NEW] Native Semantic Kernel plugin
│       ├── DependencyInjection.cs       # [MODIFY] Register PurchaseOrderAgentPlugin in DI
│       └── Services/
│           └── SemanticKernelAgentOrchestrator.cs # [MODIFY] Import into Kernel & add fallback routing
└── tests/
    └── SmartErpAgent.UnitTests/
        └── PurchaseOrderPluginTests.cs  # [NEW] Comprehensive unit test suite
```

---

## Implementation Phases

### Phase 1: Domain Entities & Database Persistence Wiring
- Create `PurchaseOrderStatus.cs` enum in `SmartErpAgent.Core.Enums`.
- Create `PurchaseOrder.cs` and `PurchaseOrderLineItem.cs` in `SmartErpAgent.Core.Entities`.
- Create `PurchaseOrderDtos.cs` in `SmartErpAgent.Application.DTOs`.
- Update `IApplicationDbContext.cs` to expose `DbSet<PurchaseOrder>` and `DbSet<PurchaseOrderLineItem>`.
- Update `ApplicationDbContext.cs` to implement DbSets and apply tenant query filters.
- Add EF Core configuration in `PurchaseOrderConfiguration.cs`.

### Phase 2: User Story 1 - Low-Stock Detection & Alerting (P1 - MVP)
- Create `PurchaseOrderAgentPlugin.cs` in `SmartErpAgent.AgentEngine.Plugins`.
- Implement `[KernelFunction]` `GetLowStockAlertsAsync`:
  - Query `InventoryItems.Where(i => i.IsActive && i.StockQuantity <= i.ReorderThreshold)`.
  - Calculate `SuggestedReorderQuantity = Math.Max(1, (i.ReorderThreshold * 2) - i.StockQuantity)`.
  - Calculate `EstimatedRestockCost = SuggestedReorderQuantity * i.UnitPrice`.
  - Return JSON serialized `List<LowStockAlertDto>`.
- Add unit tests for `GetLowStockAlertsAsync` covering shortage detection, zero alerts when stocked, and multi-tenant isolation in `PurchaseOrderPluginTests.cs`.

### Phase 3: User Story 2 - Autonomous Draft Purchase Order Generation (P2)
- Implement `[KernelFunction]` `CreateDraftPurchaseOrderAsync(string? targetSkus = null)`:
  - Query eligible low-stock items (or filter by specified SKUs).
  - Return early if no items need restocking.
  - Generate sequential `OrderNumber` (`PO-{yyyyMM}-{count+1:D4}`).
  - Persist new `PurchaseOrder` with `Status = PurchaseOrderStatus.Draft` and associated line items.
  - Save to database via `_dbContext.SaveChangesAsync()`.
  - Return JSON serialized `DraftPurchaseOrderResultDto`.
- Add unit tests for `CreateDraftPurchaseOrderAsync` verifying calculation, database persistence, and line item creation in `PurchaseOrderPluginTests.cs`.

### Phase 4: User Story 3 - Orchestrator Integration & Thought Streaming (P3)
- Register `PurchaseOrderAgentPlugin` in `DependencyInjection.cs`.
- Update `SemanticKernelAgentOrchestrator.cs`:
  - Inject `PurchaseOrderAgentPlugin`.
  - Register in Kernel builder (`builder.Plugins.AddFromObject(_purchaseOrderPlugin, nameof(PurchaseOrderAgentPlugin))`).
  - Add offline fallback routing for:
    - Low-stock inquiries: `"Which items are low in stock?"`, `"stock alerts"`.
    - Replenishment inquiries: `"Generate draft purchase orders to replenish our inventory"`.
  - Stream cognitive progress messages via SignalR (`AgentHub`).
- Add unit tests verifying orchestrator prompt routing in `PurchaseOrderPluginTests.cs`.

### Phase 5: Verification & Polish
- Execute full test suite `dotnet test backend/SmartErpAgent.sln` to achieve 100% pass rate.
- Run end-to-end quickstart validation scenarios per `quickstart.md`.
