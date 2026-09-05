# Implementation Plan: AI Agent Orchestration with Semantic Kernel

**Branch**: `002-ai-agent-orchestration` | **Date**: 2026-09-05 | **Spec**: [specs/002-ai-agent-orchestration/spec.md](./spec.md)

**Input**: Feature specification from `specs/002-ai-agent-orchestration/spec.md`

---

## Summary

Implement the AI agent orchestration layer in `SmartErpAgent.AgentEngine` using Microsoft Semantic Kernel 1.40+. The system provides:
1. `InventoryAgentPlugin`: A native C# plugin with a `[KernelFunction]` named `CheckStockLevelAsync` that queries `InventoryItem` via `IApplicationDbContext` by SKU and returns live stock quantity with reorder status.
2. `SemanticKernelAgentOrchestrator`: Implements `IAgentOrchestrator`, initializes the Semantic Kernel with domain plugins, and coordinates tool execution for conversational queries using `FunctionCallingStepwisePlanner` with deterministic local fallback.
3. `AgentEngineServiceCollectionExtensions`: Extension class providing `AddAgentEngine()` to cleanly register the orchestrator, plugins, and kernel dependencies in the DI container.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8 LTS (`<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`)

**Primary Dependencies**: 
- `Microsoft.SemanticKernel` (1.40.1)
- `Microsoft.SemanticKernel.Planners.OpenAI` (1.40.1-preview)
- `Microsoft.EntityFrameworkCore` (8.0.x)
- `Microsoft.Extensions.DependencyInjection` (8.0.x)

**Storage**: Microsoft SQL Server 2022 / In-Memory (testing) accessed through `IApplicationDbContext` with EF Core multi-tenant global query filters

**Testing**: xUnit, `Microsoft.EntityFrameworkCore.InMemory` in `backend/tests/SmartErpAgent.UnitTests/`

**Target Platform**: Linux / macOS / Windows Docker containers & Kestrel host

**Project Type**: Clean Architecture Class Library (`SmartErpAgent.AgentEngine`) consumed by ASP.NET Core Web API (`SmartErpAgent.Api`)

**Performance Goals**:
- Plugin stock level query execution: < 50ms in-process
- End-to-end agent planning and execution: < 3000ms

**Constraints**:
- Clean Architecture (Constitution Principle I): Zero domain or AI orchestration logic in API controllers
- Strict Multi-Tenancy (Constitution Principle II): Plugins query solely via `IApplicationDbContext`, inheriting tenant isolation query filters
- Safety & Decoupling (Constitution Principle V): No arbitrary LLM SQL generation; all data access restricted to pre-validated `[KernelFunction]` plugins

**Scale/Scope**: Enterprise multi-tenant catalog with tens of thousands of SKUs per tenant; concurrent agent chat sessions

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Requirement | Assessment | Status |
| :--- | :--- | :--- | :---: |
| **I. Clean Architecture** | Controllers MUST act purely as thin presentation adapters; AI orchestration MUST reside in `SmartErpAgent.AgentEngine` | Orchestrator exposed via `IAgentOrchestrator` abstraction; controllers only forward requests | **✓ PASS** |
| **II. Strict Multi-Tenancy** | Database queries MUST enforce tenant isolation | Plugins inject `IApplicationDbContext`, which automatically applies EF Core global query filters | **✓ PASS** |
| **III. Strict Code Standards** | Nullable reference types enabled, strong typing, zero unvalidated types | `<Nullable>enable</Nullable>` enforced across all projects; strongly-typed DTOs | **✓ PASS** |
| **IV. Modular Monorepo** | Frontend in Nx monorepo with shared libraries | Frontend chat components consume backend API via `@smart-erp/data-access` | **✓ PASS** |
| **V. Agent Tool Decoupling** | Multi-agent reasoning MUST use Semantic Kernel; no raw LLM DB access | Semantic Kernel with native plugins only; direct LLM queries prohibited | **✓ PASS** |

---

## Project Structure

### Documentation (this feature)

```text
specs/002-ai-agent-orchestration/
├── spec.md              # Feature specification
├── plan.md              # Technical implementation plan
├── research.md          # Phase 0: Technical decisions and research findings
├── data-model.md        # Phase 1: Entities, DTOs, and result schemas
├── quickstart.md        # Phase 1: Runnable verification guide
├── contracts/           # Phase 1: Interface contracts & plugin signatures
│   └── agent-orchestrator-contract.md
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2: Actionable task list (created by /speckit-tasks)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── SmartErpAgent.Core/
│   │   └── Entities/InventoryItem.cs
│   ├── SmartErpAgent.Application/
│   │   └── Common/Interfaces/
│   │       ├── IAgentOrchestrator.cs
│   │       └── IApplicationDbContext.cs
│   ├── SmartErpAgent.Infrastructure/
│   │   └── Persistence/ApplicationDbContext.cs
│   ├── SmartErpAgent.AgentEngine/
│   │   ├── DependencyInjection.cs (AgentEngineServiceCollectionExtensions)
│   │   ├── Plugins/
│   │   │   ├── InventoryAgentPlugin.cs (CheckStockLevelAsync)
│   │   │   └── InvoiceAgentPlugin.cs
│   │   └── Services/
│   │       └── SemanticKernelAgentOrchestrator.cs
│   └── SmartErpAgent.Api/
│       └── Controllers/AgentController.cs
└── tests/
    └── SmartErpAgent.UnitTests/
        ├── TenantIsolationTests.cs
        └── AgentEngineTests.cs
```

**Structure Decision**: Clean Architecture with separated `AgentEngine` class library. Decouples the Semantic Kernel SDK and planner dependencies from the core application layer and presentation controllers.

---

## Complexity Tracking

> **No violations of Constitution principles detected.**
