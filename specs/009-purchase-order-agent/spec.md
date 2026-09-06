# Feature Specification: Automated Restocking & Purchase Order Agent Plugin

**Feature Branch**: `009-purchase-order-agent`  
**Created**: 2026-09-06  
**Status**: Ready for Planning  
**Input**: User description: "Implement the PurchaseOrderAgentPlugin in the SmartErpAgent.AgentEngine project to autonomously manage inventory restocking and draft Purchase Orders.
1. Create a native C# class named `PurchaseOrderAgentPlugin` and inject `IApplicationDbContext`.
2. Add a `[KernelFunction]` named `GetLowStockAlertsAsync`. This method must query the `InventoryItem` entity to find all items where `StockQuantity <= ReorderThreshold` and return a list of their SKUs, Names, and current quantities.
3. Add a second `[KernelFunction]` named `CreateDraftPurchaseOrderAsync`. It should accept a list of low-stock SKUs, calculate a replenishment quantity (e.g., bringing stock back to a predefined maximum or doubling the threshold), estimate the total supplier cost (using UnitPrice), and save a new `PurchaseOrder` record (status: Draft) in the database.
4. Decorate all methods and parameters with detailed `[Description]` attributes so the Semantic Kernel LLM orchestrator knows to use this plugin when a user asks: 'Which items are low in stock?' or 'Generate draft purchase orders to replenish our inventory.'
5. Register this new plugin in the `SemanticKernelAgentOrchestrator` alongside the existing plugins."

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Real-Time Low-Stock Detection & Alerting (Priority: P1) 🎯 MVP

A warehouse inventory controller or procurement specialist needs immediate visibility into items that have reached or breached their minimum reorder thresholds. The operator asks the AI Copilot: "Which items are low in stock?" or "Show me stock alerts". The AI agent invokes `GetLowStockAlertsAsync`, evaluating all active inventory records for the current tenant, identifies items where `StockQuantity <= ReorderThreshold`, and presents an executive alert summary with SKU, Name, current stock quantity, reorder threshold, and calculated shortage.

**Why this priority**: Preventing stockouts and production halts is the primary mission of inventory operations. Delivering instant low-stock visibility provides an immediate standalone MVP.

**Independent Test**: Can be tested independently by querying for low-stock items in a tenant with known shortages (e.g. ACME_CORP where `SKU-LOW` has 4 units vs threshold 25) and verifying that the returned list contains exact shortage metrics while omitting adequately stocked items.

**Acceptance Scenarios**:

1. **Given** an authenticated tenant with items where `StockQuantity <= ReorderThreshold`,  
   **When** the user asks "Which items are low in stock?" or `GetLowStockAlertsAsync` is invoked,  
   **Then** the system returns a list of low-stock items with SKU, Name, StockQuantity, ReorderThreshold, and SuggestedReorderQuantity.
2. **Given** a tenant where all items are above their reorder thresholds,  
   **When** queried for low-stock alerts,  
   **Then** the agent cleanly reports that all items are adequately stocked with zero alerts.
3. **Given** multiple tenants with distinct inventory states,  
   **When** tenant A queries for low-stock alerts,  
   **Then** zero inventory records belonging to tenant B are evaluated or returned.

---

### User Story 2 - Autonomous Draft Purchase Order Generation (Priority: P2)

Once low-stock items are identified, a procurement manager wants the AI Copilot to generate a replenishment Purchase Order without manual spreadsheet calculations. The user instructs the Copilot: "Generate draft purchase orders to replenish our inventory" (or specifies SKUs). The agent invokes `CreateDraftPurchaseOrderAsync`, determines replenishment quantities (e.g., doubling the threshold or bringing stock to `ReorderThreshold * 2`), estimates total supplier cost using catalog `UnitPrice`, creates a new `PurchaseOrder` record with status `Draft` and associated line items, saves it into the database, and returns the order summary.

**Why this priority**: Eliminates friction between stockout detection and supplier ordering, turning analytical insights into immediate ERP ledger actions.

**Independent Test**: Can be tested by invoking `CreateDraftPurchaseOrderAsync` with low-stock SKUs, verifying that a new `PurchaseOrder` is saved in the database with status `Draft`, line items match the requested SKUs, and total monetary cost is accurately calculated.

**Acceptance Scenarios**:

1. **Given** low-stock SKUs in the catalog,  
   **When** the user requests draft purchase order creation,  
   **Then** a new `PurchaseOrder` record is persisted in the database with a unique PO number (e.g., `PO-202609-0001`), status `Draft`, calculated line items, and total amount.
2. **Given** an empty list of SKUs or no low-stock items,  
   **When** draft PO creation is triggered,  
   **Then** the agent reports that no items require replenishment without creating unnecessary blank orders.
3. **Given** a generated draft purchase order,  
   **When** inspected,  
   **Then** line items record `Description`, `Quantity`, `UnitPrice`, `TotalPrice`, and link to the relevant `InventoryItemId`.

---

### User Story 3 - Autonomous AI Copilot Triggering & Thought Streaming (Priority: P3)

An ERP operator interacts via the conversational chat interface, using varied natural language phrases such as "Which items are low in stock?", "Check our reorder alerts", or "Generate draft purchase orders to replenish our inventory". The Semantic Kernel orchestrator autonomously selects `PurchaseOrderAgentPlugin`, streams real-time reasoning steps via SignalR, executes the relevant function, and returns a formatted executive response.

**Why this priority**: Delivers a fluid, conversational ERP copilot experience where operators do not need to memorize exact command syntax or menu paths.

**Independent Test**: Can be tested by sending prompts like "Which items are low in stock?" and "Generate draft purchase orders to replenish our inventory" to `SemanticKernelAgentOrchestrator` and verifying that `PurchaseOrderAgentPlugin` functions are invoked and logged in `executedActions`.

**Acceptance Scenarios**:

1. **Given** a prompt inquiring about low inventory or stock alerts,  
   **When** processed by the orchestrator,  
   **Then** `PurchaseOrderAgentPlugin.GetLowStockAlertsAsync` is executed and intermediate thought steps are streamed.
2. **Given** a prompt requesting restocking or PO generation,  
   **When** processed by the orchestrator,  
   **Then** `PurchaseOrderAgentPlugin.CreateDraftPurchaseOrderAsync` is executed and order confirmation details are returned.

---

## Edge Cases

- **Zero Shortage**: When no items are below reorder threshold, PO creation exits gracefully with a clear informational message.
- **Negative or Zero Stock**: Handles depleted stock (`StockQuantity <= 0`) by calculating replenishment from 0.
- **Inactive Items**: Items with `IsActive == false` or `IsDeleted == true` are excluded from low-stock queries and PO generation.
- **Tenant Context Enforcement**: `PurchaseOrder` and `PurchaseOrderLineItem` records strictly inherit `CurrentTenantId`.
- **Large Catalog Performance**: Queries use SQL Server projection rather than pulling the entire catalog into memory.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST create a native C# class named `PurchaseOrderAgentPlugin` in `SmartErpAgent.AgentEngine.Plugins` injecting `IApplicationDbContext`.
- **FR-002**: System MUST implement a `[KernelFunction]` named `GetLowStockAlertsAsync` that queries `InventoryItem` records where `IsActive == true` and `StockQuantity <= ReorderThreshold`, returning a list of low-stock alerts (`LowStockAlertDto`).
- **FR-003**: System MUST implement a `[KernelFunction]` named `CreateDraftPurchaseOrderAsync` accepting an optional collection of SKUs (or defaulting to all currently low-stock SKUs), calculating replenishment quantities, estimating total supplier costs, and persisting a new `PurchaseOrder` entity with status `Draft`.
- **FR-004**: System MUST define `PurchaseOrder` and `PurchaseOrderLineItem` domain entities in `SmartErpAgent.Core.Entities` implementing `ITenantEntity`.
- **FR-005**: System MUST update `IApplicationDbContext` and `ApplicationDbContext` to expose `DbSet<PurchaseOrder>` and `DbSet<PurchaseOrderLineItem>`.
- **FR-006**: All plugin methods and parameters MUST be decorated with precise `[Description]` attributes enabling Semantic Kernel LLM tool discovery for restocking and low-stock inquiries.
- **FR-007**: System MUST register `PurchaseOrderAgentPlugin` in the dependency injection container and import it into `SemanticKernelAgentOrchestrator`.
- **FR-008**: System MUST support offline rule-based fallback routing in `SemanticKernelAgentOrchestrator` for low-stock and replenishment prompts.

### Key Entities

- **LowStockAlertDto**:
  - `SKU`: Product catalog SKU.
  - `Name`: Product name.
  - `StockQuantity`: Current inventory units available.
  - `ReorderThreshold`: Minimum stock threshold.
  - `SuggestedReorderQuantity`: Recommended order units (e.g. `(ReorderThreshold * 2) - StockQuantity`).
  - `UnitPrice`: Catalog unit cost.
  - `EstimatedRestockCost`: Total estimated restock cost (`SuggestedReorderQuantity * UnitPrice`).

- **PurchaseOrder**:
  - `Id`: Unique GUID identifier.
  - `TenantId`: Scoped tenant organization GUID.
  - `OrderNumber`: Formatted PO code (e.g. `PO-202609-0001`).
  - `SupplierName`: Vendor name (defaults to "Primary Replenishment Supplier").
  - `OrderDate`: Creation timestamp.
  - `ExpectedDeliveryDate`: Estimated receipt timestamp (e.g. `OrderDate + 7 days`).
  - `TotalAmount`: Sum of line items.
  - `Currency`: ISO currency code (USD).
  - `Status`: `PurchaseOrderStatus.Draft`.
  - `Notes`: Reason for purchase order generation.
  - `LineItems`: Collection of `PurchaseOrderLineItem`.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Operators can identify all low-stock items in the catalog in under 200 milliseconds.
- **SC-002**: Generating a complete draft Purchase Order with line items and cost estimations completes in under 500 milliseconds.
- **SC-003**: 100% of generated Purchase Orders strictly adhere to multi-tenant isolation, saving only records tagged with the caller's `TenantId`.
- **SC-004**: Automated unit test suite achieves 100% pass rate across low-stock detection, replenishment calculations, PO persistence, and orchestrator routing.

---

## Assumptions

- Replenishment formula: Target stock level is set to `ReorderThreshold * 2`. The suggested order quantity is `Target - CurrentStock` (minimum 1 unit).
- Supplier defaults: When supplier is not specified in natural language, "Primary Replenishment Supplier" is used as a default placeholder.
- Multi-currency: Uses the tenant's standard primary currency (USD).
