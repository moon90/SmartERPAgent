# Phase 0 Research: Executive Business Intelligence Reporting Plugin

**Feature**: `007-reporting-agent-plugin`  
**Date**: 2026-09-05  
**Author**: Antigravity (Semantic Kernel & Spec Kit Architecture)

---

## 1. Research Decisions & Design Choices

### Decision 1: Plugin Architecture & Dependency Injection
- **Decision**: Implement `ReportingAgentPlugin` as a native C# class located in `SmartErpAgent.AgentEngine/Plugins/` and inject `IApplicationDbContext` and `ILogger<ReportingAgentPlugin>`.
- **Rationale**: Follows the established Clean Architecture pattern seen in `InventoryAgentPlugin` and `InvoiceAgentPlugin`. Relying on `IApplicationDbContext` ensures that all queries automatically respect the scoped multi-tenant global query filter (`CurrentTenantId == null || e.TenantId == CurrentTenantId`), strictly preventing cross-tenant leakage at the database engine level (Constitution Principle II & V).
- **Alternatives Considered**:
  - Direct SQL queries via ADO.NET: Rejected because it bypasses EF Core global query filters and introduces SQL injection / maintenance risks.
  - Dedicated Reporting Repository: Rejected to maintain consistency with existing plugins which directly utilize `IApplicationDbContext` for high-performance read-only projections.

---

### Decision 2: High-Performance Database Aggregation for Inventory Valuation
- **Decision**: Perform calculations at the SQL Server database level using LINQ projection rather than pulling entire record graphs into memory.
  ```csharp
  // Total Valuation
  var totalValuation = await _dbContext.InventoryItems
      .Where(i => i.IsActive)
      .SumAsync(i => (decimal?)((decimal)i.StockQuantity * i.UnitPrice) ?? 0m, cancellationToken);

  // Top 3 Items by Total Asset Value
  var topItems = await _dbContext.InventoryItems
      .Where(i => i.IsActive)
      .Select(i => new
      {
          i.SKU,
          i.Name,
          i.StockQuantity,
          i.UnitPrice,
          TotalItemValue = (decimal)i.StockQuantity * i.UnitPrice
      })
      .OrderByDescending(i => i.TotalItemValue)
      .ThenBy(i => i.SKU)
      .Take(3)
      .ToListAsync(cancellationToken);
  ```
- **Rationale**: Catalogs may contain tens of thousands of SKUs. Computing aggregates inside SQL Server ensures sub-millisecond execution times and minimal network bandwidth.
- **Alternatives Considered**:
  - Client-side evaluation (`.ToList()` followed by LINQ to Objects): Rejected due to potential memory bloat and latency on large catalogs.

---

### Decision 3: Time-Bounded Revenue & Financial Performance Calculation
- **Decision**: Filter `Invoices` by `IssueDate >= startDateUtc` where `startDateUtc = DateTime.UtcNow.AddDays(-days)`. Exclude soft-deleted records.
  ```csharp
  var startDateUtc = DateTime.UtcNow.AddDays(-Math.Max(1, days));

  var summary = await _dbContext.Invoices
      .Where(inv => inv.IssueDate >= startDateUtc)
      .GroupBy(_ => 1)
      .Select(g => new
      {
          TotalRevenue = g.Sum(x => x.TotalAmount),
          InvoiceCount = g.Count()
      })
      .FirstOrDefaultAsync(cancellationToken);
  ```
- **Rationale**: `IssueDate` reflects the business transaction date when the invoice was generated and receivables established. Normalizing `days` with `Math.Max(1, days)` guarantees that zero or negative inputs default to a valid minimum range without throwing exceptions.
- **Alternatives Considered**:
  - Filter by `DueDate`: Rejected because due dates reflect settlement deadlines rather than chronological sales velocity.

---

### Decision 4: Semantic Kernel Native Function & Parameter Descriptions
- **Decision**: Annotate all methods with detailed `[KernelFunction]` and `[Description]` attributes formatted to provide rich semantic hints to the LLM planner:
  - `GetInventoryValuationAsync`: `"Calculates the total monetary holding value of all active inventory items (StockQuantity * UnitPrice) and identifies the top 3 most valuable SKUs for the current tenant organization."`
  - `GetFinancialSummaryAsync`: `"Calculates total invoice revenue, invoice count, and currency breakdown issued within a specific number of past calendar days (e.g. 30, 60, 90). Defaults to 30 days if not specified."`
  - Parameter `days`: `[Description("Number of past calendar days to evaluate (e.g., 7, 30, 60, 90). Defaults to 30 if not specified.")] int days = 30`
- **Rationale**: The Semantic Kernel Stepwise FunctionCalling Planner matches user intent against function and parameter descriptions. Explicit descriptions prevent false positives and guide prompt argument binding.

---

### Decision 5: Offline Rule-Based Orchestrator Fallback
- **Decision**: Extend `SemanticKernelAgentOrchestrator.ProcessPromptAsync` to route analytical keywords to `ReportingAgentPlugin` when OpenAI cloud keys are unconfigured:
  - Valuation triggers: `"valuation"`, `"value of inventory"`, `"stock value"`, `"asset value"`, `"holding value"`.
  - Financial summary triggers: `"financial summary"`, `"financials"`, `"revenue"`, `"sales summary"`, `"last {N} days"`.
- **Rationale**: Ensures continuous, robust local developer testing and demonstrations even without external cloud OpenAI connectivity.

---

## 2. Dependencies & Best Practices
- Microsoft Semantic Kernel 1.40+ (clean function calling).
- Entity Framework Core 8.0.13 with Global Query Filters (`CurrentTenantId == null || e.TenantId == CurrentTenantId`).
- C# 12 strong records/DTOs with `System.Text.Json` serialization.
