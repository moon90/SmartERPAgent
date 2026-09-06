# Research: Automated Restocking & Purchase Order Agent Plugin

**Feature Branch**: `009-purchase-order-agent`  
**Date**: 2026-09-06  
**Status**: Completed

---

## 1. Context & Architectural Objectives

The goal is to implement `PurchaseOrderAgentPlugin` in `SmartErpAgent.AgentEngine` using Microsoft Semantic Kernel. The plugin empowers the AI agent to:
1. Identify all inventory items at or below their reorder thresholds (`StockQuantity <= ReorderThreshold`).
2. Calculate recommended replenishment quantities and estimated supplier costs.
3. Autonomously persist a new `PurchaseOrder` record (with `Draft` status) and line items in the database under strict multi-tenant isolation.
4. Provide semantic descriptions enabling autonomous LLM tool selection alongside existing inventory, invoicing, extraction, and reporting tools.

---

## 2. Research Decisions

### Decision 1: Entity Design & Database Storage
- **Chosen**: Create dedicated domain entities `PurchaseOrder` and `PurchaseOrderLineItem` in `SmartErpAgent.Core.Entities` implementing `ITenantEntity` and `BaseEntity`.
  - Enum `PurchaseOrderStatus`: `Draft = 0`, `Submitted = 1`, `Approved = 2`, `Received = 3`, `Cancelled = 4`.
  - Expose `DbSet<PurchaseOrder> PurchaseOrders { get; }` and `DbSet<PurchaseOrderLineItem> PurchaseOrderLineItems { get; }` in `IApplicationDbContext`.
  - Apply EF Core global query filters (`!e.IsDeleted && (CurrentTenantId == null || e.TenantId == CurrentTenantId)`).
- **Rationale**:
  - Aligns directly with Constitution Principle I (Clean Architecture) and Principle II (Strict Multi-Tenancy).
  - Matches the established pattern of `Invoice` and `InvoiceLineItem`.
- **Alternatives Considered**:
  - *Storing purchase orders in JSON notes*: Discarded as non-relational, prevents future reporting, supplier integration, and ledger reconciliation.

---

### Decision 2: Replenishment Calculation Model
- **Chosen**: Target replenishment formula:
  $$\text{TargetStock} = \text{ReorderThreshold} \times 2$$
  $$\text{ReplenishQuantity} = \max(1, \text{TargetStock} - \text{StockQuantity})$$
  $$\text{LineItemCost} = \text{ReplenishQuantity} \times \text{UnitPrice}$$
- **Rationale**:
  - Provides standard dynamic economic order sizing without requiring complex lead-time forecasting in v1.
  - Guarantees positive restock quantities even if stock has dropped below zero.
- **Alternatives Considered**:
  - *Fixed batch size (e.g., always 10 units)*: Fails for high-volume items (e.g. pallets vs motors).

---

### Decision 3: Plugin Interface & Kernel Function Signatures
- **Chosen**:
  ```csharp
  public class PurchaseOrderAgentPlugin
  {
      private readonly IApplicationDbContext _dbContext;

      public PurchaseOrderAgentPlugin(IApplicationDbContext dbContext) => _dbContext = dbContext;

      [KernelFunction, Description("Scans the warehouse inventory catalog to identify all active items where current stock quantity is at or below the reorder threshold. Use when asked 'Which items are low in stock?', 'Check stock alerts', or 'Show reorder alerts'.")]
      public async Task<string> GetLowStockAlertsAsync(CancellationToken cancellationToken = default);

      [KernelFunction, Description("Autonomously generates and saves a draft Purchase Order in the database to replenish inventory for specified low-stock SKUs or all items currently below reorder threshold. Use when asked 'Generate draft purchase orders to replenish our inventory' or 'Create PO for low stock items'.")]
      public async Task<string> CreateDraftPurchaseOrderAsync(
          [Description("Optional comma-separated list of SKUs to replenish. If empty or null, replenishes all active items currently below reorder threshold.")] string? targetSkus = null,
          CancellationToken cancellationToken = default);
  }
  ```
- **Rationale**:
  - Functions return structured JSON strings suitable for both LLM context ingestion and orchestrator UI formatting.
  - Optional `targetSkus` parameter allows targeted restocking ("Create PO for SKU-LOW") or blanket warehouse restocking ("Replenish all low stock").

---

### Decision 4: Orchestrator Integration & Thought Streaming
- **Chosen**:
  - Register `PurchaseOrderAgentPlugin` in DI as a scoped service (`services.AddScoped<PurchaseOrderAgentPlugin>()`).
  - Import into `SemanticKernelAgentOrchestrator` kernel builder.
  - Implement rule-based fallback keyword routing:
    - Low stock check: `(?i)(?:which\s+items\s+are\s+low|low\s+stock\s+alerts?|reorder\s+alerts?)`
    - PO generation: `(?i)(?:generate\s+(?:draft\s+)?purchase\s+orders?|replenish\s+(?:our\s+)?inventory|create\s+(?:a\s+)?(?:draft\s+)?purchase\s+order)`
  - Emit real-time cognitive thoughts via SignalR (`AgentHub`).
