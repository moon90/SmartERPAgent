# Tasks: Automated Restocking & Purchase Order Agent Plugin

**Feature Branch**: `009-purchase-order-agent`  
**Specification**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)  
**Status**: Ready to Implement

---

## Phase 1: Setup (Shared Data Models & Contracts)

**Purpose**: Establish domain entities, enums, DTOs, and interface contracts for purchase orders and inventory alerts.

- [ ] T001 [P] Create `PurchaseOrderStatus.cs` enum in `backend/src/SmartErpAgent.Core/Enums/PurchaseOrderStatus.cs`
- [ ] T002 [P] Create `PurchaseOrder.cs` and `PurchaseOrderLineItem.cs` domain entities in `backend/src/SmartErpAgent.Core/Entities/`
- [ ] T003 [P] Create `PurchaseOrderDtos.cs` containing `LowStockAlertDto`, `DraftPurchaseOrderResultDto`, and `PurchaseOrderLineItemSummaryDto` in `backend/src/SmartErpAgent.Application/DTOs/PurchaseOrderDtos.cs`
- [ ] T004 Update `IApplicationDbContext.cs` to expose `DbSet<PurchaseOrder> PurchaseOrders` and `DbSet<PurchaseOrderLineItem> PurchaseOrderLineItems` in `backend/src/SmartErpAgent.Application/Common/Interfaces/IApplicationDbContext.cs`

---

## Phase 2: Foundational (Persistence & Infrastructure Wiring)

**Purpose**: Core EF Core configuration and dependency injection wiring required before plugin execution.

- [ ] T005 Update `ApplicationDbContext.cs` to declare `DbSet<PurchaseOrder>` and `DbSet<PurchaseOrderLineItem>` and configure global tenant query filters in `backend/src/SmartErpAgent.Infrastructure/Persistence/ApplicationDbContext.cs`
- [ ] T006 [P] Create EF Core configuration `PurchaseOrderConfiguration.cs` in `backend/src/SmartErpAgent.Infrastructure/Persistence/Configurations/PurchaseOrderConfiguration.cs`
- [ ] T007 Register `PurchaseOrderAgentPlugin` as a scoped service in `backend/src/SmartErpAgent.AgentEngine/DependencyInjection.cs`

**Checkpoint**: Foundation ready — database entities, persistence layer, and DI registration in place.

---

## Phase 3: User Story 1 - Low-Stock Detection & Alerting (Priority: P1) 🎯 MVP

**Goal**: Implement `[KernelFunction]` `GetLowStockAlertsAsync` on `PurchaseOrderAgentPlugin` that queries `InventoryItem` records where `StockQuantity <= ReorderThreshold`, calculates suggested replenishment quantities and estimated restock costs, and returns structured low-stock alerts under multi-tenant isolation.

**Independent Test**: Populate an in-memory database with inventory items having varying stock levels (above, at, and below threshold) across multiple tenants. Call `GetLowStockAlertsAsync` and verify that only active items at or below reorder threshold for the current tenant are returned with accurate replenishment math.

### Tests for User Story 1
- [ ] T008 [P] [US1] Create unit tests for `GetLowStockAlertsAsync` verifying shortage filtering (`StockQuantity <= ReorderThreshold`), suggested order math (`(ReorderThreshold * 2) - StockQuantity`), estimated restock costs, zero alerts when stocked, and tenant isolation in `backend/tests/SmartErpAgent.UnitTests/PurchaseOrderPluginTests.cs`

### Implementation for User Story 1
- [ ] T009 [US1] Implement `PurchaseOrderAgentPlugin` with `GetLowStockAlertsAsync`, injecting `IApplicationDbContext`, decorated with `[KernelFunction]` and `[Description]` attributes in `backend/src/SmartErpAgent.AgentEngine/Plugins/PurchaseOrderAgentPlugin.cs`

**Checkpoint**: User Story 1 (MVP) is fully functional, testable independently, and provides inventory shortage visibility.

---

## Phase 4: User Story 2 - Autonomous Draft Purchase Order Generation (Priority: P2)

**Goal**: Implement `[KernelFunction]` `CreateDraftPurchaseOrderAsync(string? targetSkus = null)` on `PurchaseOrderAgentPlugin` that calculates replenishment quantities for low-stock items, estimates supplier costs using catalog unit prices, generates a sequential PO number, persists a new `PurchaseOrder` with status `Draft` and associated line items in the database, and returns order summary details.

**Independent Test**: Call `CreateDraftPurchaseOrderAsync` when low-stock items exist. Verify that a new `PurchaseOrder` is saved in the database with status `Draft`, line items match the low-stock items with correct quantities and amounts, and total cost matches the sum of line items. Verify that calling with no shortages returns an informational response without creating blank orders.

### Tests for User Story 2
- [ ] T010 [P] [US2] Create unit tests for `CreateDraftPurchaseOrderAsync` verifying replenishment quantity calculations, database persistence of draft PO with line items, sequential order number generation, total cost computation, and handling of zero-shortage scenarios in `backend/tests/SmartErpAgent.UnitTests/PurchaseOrderPluginTests.cs`

### Implementation for User Story 2
- [ ] T011 [US2] Implement `CreateDraftPurchaseOrderAsync` in `PurchaseOrderAgentPlugin` with SKU filtering, replenishment quantity formulas, PO number generation, line item instantiation, and EF Core persistence in `backend/src/SmartErpAgent.AgentEngine/Plugins/PurchaseOrderAgentPlugin.cs`

**Checkpoint**: User Stories 1 and 2 are both independently functional and enable end-to-end inventory replenishment workflow.

---

## Phase 5: User Story 3 - Autonomous AI Copilot Triggering & Thought Streaming (Priority: P3)

**Goal**: Integrate `PurchaseOrderAgentPlugin` into `SemanticKernelAgentOrchestrator`, import it into the Kernel plugin collection, decorate methods with rich `[Description]` attributes for autonomous LLM tool calling, and add fallback routing with real-time SignalR thought streaming for natural language restocking inquiries.

**Independent Test**: Submit chat requests like "Which items are low in stock?" and "Generate draft purchase orders to replenish our inventory" to `SemanticKernelAgentOrchestrator` and verify tool execution, response generation, and thought step streaming.

### Tests for User Story 3
- [ ] T012 [P] [US3] Create unit tests for `SemanticKernelAgentOrchestrator` verifying tool import, execution, and offline fallback routing for low-stock queries and draft purchase order generation prompts in `backend/tests/SmartErpAgent.UnitTests/PurchaseOrderPluginTests.cs`

### Implementation for User Story 3
- [ ] T013 [US3] Inject `PurchaseOrderAgentPlugin` into `SemanticKernelAgentOrchestrator`, register in Kernel plugins collection, and add fallback routing for low-stock and PO generation prompts with SignalR thought streaming in `backend/src/SmartErpAgent.AgentEngine/Services/SemanticKernelAgentOrchestrator.cs`

**Checkpoint**: All three user stories are seamlessly integrated and accessible through the conversational AI copilot.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Verify end-to-end functionality, execute complete test suites, and validate against quickstart scenarios.

- [ ] T014 Run full unit test suite `dotnet test backend/SmartErpAgent.sln` to ensure 100% pass rate with zero regressions
- [ ] T015 Execute end-to-end quickstart validation scenarios per `specs/009-purchase-order-agent/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies
- **Phase 1 (Setup)**: Can start immediately. Defines types and interfaces needed by all downstream components.
- **Phase 2 (Foundational)**: Depends on Phase 1 completion. Wires EF Core configurations, DbSets, and DI.
- **Phase 3 (User Story 1 - MVP)**: Depends on Phase 1 & 2. Implements and tests low-stock detection.
- **Phase 4 (User Story 2)**: Depends on Phase 1, 2, and 3. Builds on low-stock detection to generate draft POs.
- **Phase 5 (User Story 3)**: Depends on Phase 3 & 4. Integrates the completed plugin into the orchestrator.
- **Phase 6 (Polish)**: Depends on all prior phases. Validates complete system regression and quickstart scenarios.

### Parallel Opportunities
- In Phase 1, T001, T002, and T003 can be created in parallel as independent files.
- In Phase 2, T006 and T007 can be prepared in parallel once T004/T005 are in place.
- Unit tests T008, T010, and T012 can be structured in parallel within `PurchaseOrderPluginTests.cs`.
