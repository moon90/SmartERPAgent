# Implementation Plan: Executive Business Intelligence Reporting Plugin

**Branch**: `007-reporting-agent-plugin` | **Date**: 2026-09-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/007-reporting-agent-plugin/spec.md`

---

## Summary

Implement an executive business intelligence native plugin (`ReportingAgentPlugin`) in `SmartErpAgent.AgentEngine` using Microsoft Semantic Kernel. The plugin injects `IApplicationDbContext` to execute tenant-isolated analytical queries against inventory stock and customer invoice ledgers. It exposes two core `[KernelFunction]` tools:
1. `GetInventoryValuationAsync`: Aggregates active inventory monetary holding value (`StockQuantity * UnitPrice`) and identifies the top 3 highest-value SKUs.
2. `GetFinancialSummaryAsync`: Computes time-bounded invoice revenue and issued invoice volume over a parameterized day window (defaulting to 30 days).
Both tools are registered in the Semantic Kernel instance in `SemanticKernelAgentOrchestrator` alongside existing domain plugins, with an intelligent fallback rule pipeline for local/offline execution.

---

## Technical Context

**Language/Version**: C# 12, .NET 8.0 SDK (`<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`)  
**Primary Dependencies**: Microsoft.SemanticKernel (v1.40+), Microsoft.EntityFrameworkCore (v8.0.13), Microsoft.Extensions.Logging  
**Storage**: Microsoft SQL Server 2022 via EF Core `ApplicationDbContext` (implementing `IApplicationDbContext`)  
**Testing**: xUnit, Moq, Microsoft.EntityFrameworkCore.InMemory (Unit & Integration tests)  
**Target Platform**: Cross-platform Web API / Docker / Linux / macOS  
**Project Type**: Enterprise B2B SaaS Clean Architecture Web Service (`Core`, `Application`, `Infrastructure`, `AgentEngine`, `Api`)  
**Performance Goals**: Sub-second execution (<250ms p95) for all reporting analytical queries via database-side LINQ projections  
**Constraints**: Strict multi-tenant data isolation (Principle II), Decoupled Semantic Kernel agent function calling (Principle V), zero cross-tenant data leakage  
**Scale/Scope**: Multi-tenant catalogs ranging from small businesses to enterprise operations with thousands of SKUs and invoices  

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Constitution Principle | Requirement | Status | Design Compliance Notes |
| :--- | :--- | :--- | :--- |
| **I. Clean Architecture & Controller Isolation** | Controllers must act purely as presentation routing adapters. Domain logic must reside in Core/Application/AgentEngine. | **PASS** | `ReportingAgentPlugin` is implemented strictly in `SmartErpAgent.AgentEngine`, DTOs in `SmartErpAgent.Application`, without polluting API controllers. |
| **II. Strict Multi-Tenancy** | All entity access must respect tenant boundaries via EF Core Global Query Filters. | **PASS** | All queries execute via `IApplicationDbContext` which enforces `CurrentTenantId == null || e.TenantId == CurrentTenantId` at the database level. |
| **III. Production-Grade Standards** | Nullable enabled, strong typing, records, structured logging, zero raw SQL strings. | **PASS** | Enforces C# 12 records (`TopValuableSkuDto`, `InventoryValuationReportDto`, `FinancialPeriodSummaryDto`) and structured logging. |
| **IV. Modular Monorepo Architecture** | Frontend modularity preserved. | **PASS** | Backend changes adhere strictly to clean architectural boundaries without breaking frontend workspace contracts. |
| **V. Decoupled AI Agent Orchestration** | Semantic Kernel function calling via scoped interfaces; no arbitrary database code in LLM. | **PASS** | `[KernelFunction]` and `[Description]` attributes expose strictly bounded, validated analytical routines to the Semantic Kernel planner. |

---

## Project Structure

### Documentation (this feature)

```text
specs/007-reporting-agent-plugin/
├── spec.md              # Feature specification
├── plan.md              # Implementation plan (this document)
├── research.md          # Phase 0: Technical research & design decisions
├── data-model.md        # Phase 1: Analytical data models & records
├── contracts/           # Phase 1: Semantic Kernel plugin schema definition
│   └── reporting-agent-plugin.json
├── quickstart.md        # Phase 1: End-to-end verification scenarios
└── checklists/
    └── requirements.md  # Quality assurance checklist
```

### Source Code Changes

```text
backend/
├── src/
│   ├── SmartErpAgent.Application/
│   │   └── DTOs/
│   │       └── ReportingDtos.cs                       # [NEW] TopValuableSkuDto, InventoryValuationReportDto, FinancialPeriodSummaryDto
│   ├── SmartErpAgent.AgentEngine/
│   │   ├── Plugins/
│   │   │   └── ReportingAgentPlugin.cs                # [NEW] Native Semantic Kernel reporting plugin
│   │   ├── Services/
│   │   │   └── SemanticKernelAgentOrchestrator.cs     # [MODIFY] Register ReportingAgentPlugin & add fallback keyword routing
│   │   └── DependencyInjection.cs                     # [MODIFY] Register ReportingAgentPlugin in DI container
└── tests/
    └── SmartErpAgent.UnitTests/
        └── ReportingPluginTests.cs                    # [NEW] Comprehensive unit tests for valuation, financial summary, and multi-tenancy
```

---

## Complexity Tracking

*No violations. Clean Architecture and Constitution boundaries are strictly preserved.*
