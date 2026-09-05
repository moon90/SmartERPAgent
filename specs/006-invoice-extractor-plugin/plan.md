# Implementation Plan: Invoice Extractor & Document Intelligence Plugin

**Branch**: `006-invoice-extractor-plugin` | **Date**: 2026-09-05 | **Spec**: [specs/006-invoice-extractor-plugin/spec.md](./spec.md)

**Input**: Feature specification from `specs/006-invoice-extractor-plugin/spec.md`

---

## Summary

Implement an `InvoiceExtractorPlugin` in `SmartErpAgent.AgentEngine.Plugins` enabling the Semantic Kernel orchestrator to ingest unstructured text/emails/documents, parse invoice headers and tabular line items, correlate line items with active tenant `InventoryItems` (SKU matching), and stage validated `Draft` invoices into the ERP database with strict multi-tenant isolation.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8.0  
**AI Framework**: Microsoft Semantic Kernel 1.40+  
**Database**: Microsoft SQL Server 2022 via Entity Framework Core 8  
**Architecture Layering**:
- `SmartErpAgent.Application`: DTOs (`ExtractedInvoiceData`, `ExtractedInvoiceLineItem`)
- `SmartErpAgent.AgentEngine`: `InvoiceExtractorPlugin`, `DependencyInjection.cs`, `SemanticKernelAgentOrchestrator.cs`
- `SmartErpAgent.Api`: DI registration in `Program.cs`
- `SmartErpAgent.UnitTests`: Unit and integration test suite (`InvoiceExtractorTests.cs`)

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Requirement | Assessment | Status |
| :--- | :--- | :--- | :---: |
| **I. Clean Architecture** | Pure presentation controllers; logic in Application/Engine | Controllers do not contain extraction or parsing logic; all extraction handled in `AgentEngine.Plugins` | **✓ PASS** |
| **II. Strict Multi-Tenancy** | EF Core Global Query Filters; zero data leakage | All SKU lookups and draft invoice inserts query `IApplicationDbContext` with tenant context | **✓ PASS** |
| **III. Strict Code Standards** | `<Nullable>enable</Nullable>`, strong typing, no raw SQL | Strongly-typed DTOs, full C# 12 nullable references, LINQ queries | **✓ PASS** |
| **IV. Modular Monorepo** | Frontend modularity | Reusable chat drawer in `@smart-erp/feature-chat` visualizes agent milestones | **✓ PASS** |
| **V. Agent Tool Decoupling** | Safe AI orchestration | Native Semantic Kernel plugin with `[KernelFunction]` annotations and deterministic fallback | **✓ PASS** |

---

## Project Structure

### Documentation (this feature)

```text
specs/006-invoice-extractor-plugin/
├── spec.md              # Feature specification
├── plan.md              # Technical implementation plan
├── research.md          # Information extraction, SKU correlation & persistence decisions
├── data-model.md        # Extraction DTOs, database mappings & state transitions
├── contracts/           # Plugin interface and sample JSON contracts
│   └── invoice-extractor-contracts.md
├── checklists/
│   └── requirements.md  # Spec quality checklist
├── quickstart.md        # Runnable verification guide
└── tasks.md             # Actionable task breakdown (created by /speckit-tasks)
```

### Source Code Touchpoints

```text
backend/
├── src/
│   ├── SmartErpAgent.Application/
│   │   └── DTOs/
│   │       └── InvoiceExtractionDto.cs         # ExtractedInvoiceData & ExtractedInvoiceLineItem
│   ├── SmartErpAgent.AgentEngine/
│   │   ├── Plugins/
│   │   │   └── InvoiceExtractorPlugin.cs       # Extraction, SKU correlation, and staging plugin
│   │   ├── Services/
│   │   │   └── SemanticKernelAgentOrchestrator.cs # Register plugin & stream extraction thoughts
│   │   └── DependencyInjection.cs              # AddScoped<InvoiceExtractorPlugin>()
│   └── SmartErpAgent.Api/
│       └── Program.cs                          # Verified DI wire-up
└── tests/
    └── SmartErpAgent.UnitTests/
        └── InvoiceExtractorTests.cs            # Unit tests for text parsing, SKU matching & staging
```

---

## Complexity Tracking

> **No violations of Constitution principles detected.**
