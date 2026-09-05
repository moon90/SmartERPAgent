# Tasks: AI Agent Orchestration with Semantic Kernel

**Feature**: `002-ai-agent-orchestration`  
**Specification**: [specs/002-ai-agent-orchestration/spec.md](./spec.md)  
**Implementation Plan**: [specs/002-ai-agent-orchestration/plan.md](./plan.md)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify Semantic Kernel dependencies and project references

- [X] T001 Verify Microsoft.SemanticKernel 1.40+ and Microsoft.SemanticKernel.Planners.OpenAI packages in `backend/src/SmartErpAgent.AgentEngine/SmartErpAgent.AgentEngine.csproj`
- [X] T002 [P] Verify project references from AgentEngine to Core and Application in `backend/src/SmartErpAgent.AgentEngine/SmartErpAgent.AgentEngine.csproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core application contracts required across all agent features

**⚠️ CRITICAL**: Must complete before any user story can be implemented

- [X] T003 Define `IAgentOrchestrator` interface contract in `backend/src/SmartErpAgent.Application/Common/Interfaces/IAgentOrchestrator.cs`
- [X] T004 [P] Create `AgentRequestDto` and `AgentResponseDto` contracts in `backend/src/SmartErpAgent.Application/DTOs/AgentDto.cs`

**Checkpoint**: Core agent contracts established - user story implementation can begin.

---

## Phase 3: User Story 1 - Natural Language Inventory Stock Inquiries (Priority: P1) 🎯 MVP

**Goal**: Implement native `InventoryAgentPlugin` with `CheckStockLevelAsync` returning current warehouse stock levels by SKU.

**Independent Test**: Directly invoke `CheckStockLevelAsync` for an existing SKU, a low-stock SKU, and a missing SKU, verifying output and strict tenant isolation.

### Implementation for User Story 1

- [X] T005 [P] [US1] Create unit test suite for `InventoryAgentPlugin` in `backend/tests/SmartErpAgent.UnitTests/AgentEngineTests.cs`
- [X] T006 [US1] Implement `InventoryAgentPlugin` with `CheckStockLevelAsync` method in `backend/src/SmartErpAgent.AgentEngine/Plugins/InventoryAgentPlugin.cs`
- [X] T007 [US1] Annotate `CheckStockLevelAsync` with detailed `[KernelFunction]` and `[Description]` attributes in `backend/src/SmartErpAgent.AgentEngine/Plugins/InventoryAgentPlugin.cs`
- [X] T008 [US1] Enforce tenant-scoped SKU querying via `IApplicationDbContext` in `backend/src/SmartErpAgent.AgentEngine/Plugins/InventoryAgentPlugin.cs`

**Checkpoint**: User Story 1 is fully functional and delivers independent inventory stock checking (MVP ready).

---

## Phase 4: User Story 2 - Autonomous Function Calling & Intent Planning (Priority: P2)

**Goal**: Implement `SemanticKernelAgentOrchestrator` using Semantic Kernel to coordinate intent reasoning and tool execution with fallback.

**Independent Test**: Send conversational query "Do we have enough stock for SKU-123?" to the orchestrator and verify accurate tool selection and synthesized answer.

### Implementation for User Story 2

- [X] T009 [P] [US2] Add orchestrator unit test for prompt execution and fallback in `backend/tests/SmartErpAgent.UnitTests/AgentEngineTests.cs`
- [X] T010 [US2] Implement `SemanticKernelAgentOrchestrator` with Kernel initialization and plugin import in `backend/src/SmartErpAgent.AgentEngine/Services/SemanticKernelAgentOrchestrator.cs`
- [X] T011 [US2] Integrate `FunctionCallingStepwisePlanner` and deterministic local fallback in `backend/src/SmartErpAgent.AgentEngine/Services/SemanticKernelAgentOrchestrator.cs`
- [X] T012 [US2] Wire `AgentController` to `IAgentOrchestrator` presentation endpoint in `backend/src/SmartErpAgent.Api/Controllers/AgentController.cs`

**Checkpoint**: User Stories 1 and 2 operate in tandem with autonomous tool calling.

---

## Phase 5: User Story 3 - Clean Architecture Service Registration (Priority: P3)

**Goal**: Implement `AgentEngineServiceCollectionExtensions` with `AddAgentEngine()` to cleanly register all agent engine services in DI.

**Independent Test**: Assert that `IAgentOrchestrator` and `InventoryAgentPlugin` resolve cleanly from a service provider configured via `AddAgentEngine()`.

### Implementation for User Story 3

- [X] T013 [P] [US3] Add DI service resolution test in `backend/tests/SmartErpAgent.UnitTests/AgentEngineTests.cs`
- [X] T014 [US3] Implement `AgentEngineServiceCollectionExtensions` with `AddAgentEngine()` in `backend/src/SmartErpAgent.AgentEngine/DependencyInjection.cs`
- [X] T015 [US3] Register `AddAgentEngine()` in API startup pipeline in `backend/src/SmartErpAgent.Api/Program.cs`

**Checkpoint**: All user stories functional and registered with clean dependency injection.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: End-to-end integration and verification

- [X] T016 [P] Verify structured logging and error handling across all AgentEngine services in `backend/src/SmartErpAgent.AgentEngine/Services/SemanticKernelAgentOrchestrator.cs`
- [X] T017 Run end-to-end unit test suite per `specs/002-ai-agent-orchestration/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1; blocks all user stories.
- **User Story 1 (Phase 3 - MVP)**: Depends on Phase 2; no dependencies on other stories.
- **User Story 2 (Phase 4)**: Depends on Phase 3 (`InventoryAgentPlugin`).
- **User Story 3 (Phase 5)**: Depends on Phase 4.
- **Polish (Phase 6)**: Depends on completion of all user stories.

### Parallel Execution Opportunities

- T002 can run in parallel with T001.
- T004 can run in parallel with T003.
- T005 (Tests for US1) can run in parallel with T006.
- T009 (Tests for US2) can run in parallel with T010.
- T013 (Tests for US3) can run in parallel with T014.
- T016 can run in parallel with T017 in Polish phase.

---

## Implementation Strategy

### MVP First (User Story 1 Only)
1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1 (`InventoryAgentPlugin` + `CheckStockLevelAsync` + Unit Tests)
4. **VALIDATE**: Run `dotnet test --filter FullyQualifiedName~AgentEngine`

### Incremental Delivery
1. Foundation Complete → `IAgentOrchestrator` & DTO contracts ready
2. Add US1 → Native inventory plugin with `[KernelFunction]` and `CheckStockLevelAsync` (MVP)
3. Add US2 → Semantic Kernel orchestrator with planner and deterministic local fallback
4. Add US3 → Clean DI registration via `AgentEngineServiceCollectionExtensions.AddAgentEngine()`
5. Polish → End-to-end validation of all quickstart scenarios
