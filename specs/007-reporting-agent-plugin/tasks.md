# Tasks: Executive Business Intelligence Reporting Plugin

**Feature Branch**: `007-reporting-agent-plugin`  
**Specification**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)  
**Status**: Ready for Implementation

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Define analytical data transfer objects and contracts.

- [ ] T001 [P] Create analytical DTO records (`TopValuableSkuDto`, `InventoryValuationReportDto`, `FinancialPeriodSummaryDto`) in `backend/src/SmartErpAgent.Application/DTOs/ReportingDtos.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core dependency injection registration required before user story integration.

- [ ] T002 [P] Register `ReportingAgentPlugin` in DI container in `backend/src/SmartErpAgent.AgentEngine/DependencyInjection.cs`

---

## Phase 3: User Story 1 - Real-Time Inventory Valuation & Top SKUs Analysis (Priority: P1) 🎯 MVP

**Goal**: Calculate total monetary holding value of active inventory items (`StockQuantity * UnitPrice`) and identify top 3 highest-value SKUs under strict multi-tenant isolation.

**Independent Test**: Execute `GetInventoryValuationAsync` with sample tenant catalogs and verify calculations, zero-item handling, and multi-tenant isolation.

### Tests for User Story 1
- [ ] T003 [P] [US1] Create unit tests for `GetInventoryValuationAsync` covering calculations, top 3 SKU ordering, empty catalog, and multi-tenant isolation in `backend/tests/SmartErpAgent.UnitTests/ReportingPluginTests.cs`

### Implementation for User Story 1
- [ ] T004 [US1] Implement `ReportingAgentPlugin` class injecting `IApplicationDbContext` with `[KernelFunction]` `GetInventoryValuationAsync` and semantic descriptions in `backend/src/SmartErpAgent.AgentEngine/Plugins/ReportingAgentPlugin.cs`

**Checkpoint**: At this point, User Story 1 (MVP) is fully functional and testable independently.

---

## Phase 4: User Story 2 - Time-Bounded Revenue & Financial Performance Reporting (Priority: P2)

**Goal**: Calculate total invoice revenue, invoice count, and currency breakdown issued within a specific number of past calendar days (default 30 days).

**Independent Test**: Execute `GetFinancialSummaryAsync` with explicit days (e.g. 14, 60), default/null days, and negative day boundary handling.

### Tests for User Story 2
- [ ] T005 [P] [US2] Add unit tests for `GetFinancialSummaryAsync` with custom timeframes, default 30-day period, empty period, and multi-tenant isolation in `backend/tests/SmartErpAgent.UnitTests/ReportingPluginTests.cs`

### Implementation for User Story 2
- [ ] T006 [US2] Implement `[KernelFunction]` `GetFinancialSummaryAsync` with parameter validation and description attributes in `backend/src/SmartErpAgent.AgentEngine/Plugins/ReportingAgentPlugin.cs`

**Checkpoint**: At this point, User Stories 1 and 2 are both independently functional.

---

## Phase 5: User Story 3 - Autonomous AI Tool Selection & Reasoning Streaming (Priority: P3)

**Goal**: Import `ReportingAgentPlugin` into Semantic Kernel and wire fallback keyword routing in the orchestrator.

**Independent Test**: Send analytical natural language prompts (valuation, financials) to `SemanticKernelAgentOrchestrator` and verify tool execution and thought streaming.

### Tests for User Story 3
- [ ] T007 [P] [US3] Add unit tests for `SemanticKernelAgentOrchestrator` verifying `ReportingAgentPlugin` tool execution on financial and valuation inquiries in `backend/tests/SmartErpAgent.UnitTests/ReportingPluginTests.cs`

### Implementation for User Story 3
- [ ] T008 [US3] Register `ReportingAgentPlugin` into Semantic Kernel kernel builder and add fallback analytical routing in `backend/src/SmartErpAgent.AgentEngine/Services/SemanticKernelAgentOrchestrator.cs`

**Checkpoint**: All three user stories are completely integrated and accessible via conversational AI copilot.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: End-to-end verification, test suite regression checks, and documentation.

- [ ] T009 [P] Verify 100% test suite pass rate across all unit tests via `dotnet test backend/SmartErpAgent.sln`
- [ ] T010 Execute end-to-end quickstart validation scenarios per `specs/007-reporting-agent-plugin/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies
- **Phase 1 (Setup)**: No dependencies — start immediately.
- **Phase 2 (Foundational)**: Depends on T001.
- **Phase 3 (User Story 1 - MVP)**: Depends on Phase 1 & 2.
- **Phase 4 (User Story 2)**: Depends on Phase 3 (extends `ReportingAgentPlugin`).
- **Phase 5 (User Story 3)**: Depends on Phase 3 & 4 (orchestrator integration).
- **Phase 6 (Polish)**: Depends on Phase 3, 4, and 5.

### Parallel Opportunities
- T001 and T002 can be prepared concurrently.
- Test tasks (T003, T005, T007) are designed to run in parallel in `ReportingPluginTests.cs`.
