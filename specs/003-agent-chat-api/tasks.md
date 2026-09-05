# Tasks: Agent Chat API Presentation Endpoint

**Feature**: `003-agent-chat-api`  
**Specification**: [specs/003-agent-chat-api/spec.md](./spec.md)  
**Implementation Plan**: [specs/003-agent-chat-api/plan.md](./plan.md)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify API dependencies, project references, and routing setup

- [X] T001 Verify API project references to Application and AgentEngine in `backend/src/SmartErpAgent.Api/SmartErpAgent.Api.csproj`
- [X] T002 [P] Verify controller registration and routing in `backend/src/SmartErpAgent.Api/Program.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core DTO and interface contracts required across the API presentation layer

**⚠️ CRITICAL**: Must complete before any user story can be implemented

- [X] T003 Create `AgentChatRequestDto` contract in `backend/src/SmartErpAgent.Application/DTOs/AgentChatRequestDto.cs`
- [X] T004 [P] Ensure `IAgentOrchestrator` defines `ExecutePromptAsync` in `backend/src/SmartErpAgent.Application/Common/Interfaces/IAgentOrchestrator.cs`

**Checkpoint**: Core contracts ready - controller implementation can begin.

---

## Phase 3: User Story 1 - Conversational AI Interaction via HTTP API (Priority: P1) 🎯 MVP

**Goal**: Expose `POST /api/agent/chat` endpoint accepting `AgentChatRequestDto`, delegating to `IAgentOrchestrator`, and returning the AI response in an `IActionResult`.

**Independent Test**: Unit test `AgentController.Chat` action with a valid prompt; assert HTTP 200 OK containing the orchestrator response.

### Implementation for User Story 1

- [X] T005 [P] [US1] Create controller unit test suite in `backend/tests/SmartErpAgent.UnitTests/AgentControllerTests.cs`
- [X] T006 [US1] Implement `AgentController` decorated with `[ApiController]` and `[Route("api/[controller]")]` in `backend/src/SmartErpAgent.Api/Controllers/AgentController.cs`
- [X] T007 [US1] Inject `IAgentOrchestrator` into `AgentController` constructor in `backend/src/SmartErpAgent.Api/Controllers/AgentController.cs`
- [X] T008 [US1] Implement `[HttpPost("chat")]` endpoint accepting `AgentChatRequestDto` in `backend/src/SmartErpAgent.Api/Controllers/AgentController.cs`
- [X] T009 [US1] Validate prompt and dispatch to `IAgentOrchestrator.ExecutePromptAsync` in `backend/src/SmartErpAgent.Api/Controllers/AgentController.cs`

**Checkpoint**: User Story 1 functional and independently testable (MVP ready).

---

## Phase 4: User Story 2 - Strongly Typed Data Contracts & Validation (Priority: P2)

**Goal**: Enforce request validation returning HTTP 400 Bad Request on empty/whitespace input.

**Independent Test**: Send null or empty prompt to `AgentController.Chat` and assert HTTP 400 Bad Request with error message.

### Implementation for User Story 2

- [X] T010 [P] [US2] Add model validation test cases for `AgentChatRequestDto` in `backend/tests/SmartErpAgent.UnitTests/AgentControllerTests.cs`
- [X] T011 [US2] Return HTTP 400 Bad Request when `Prompt` is null, empty, or whitespace in `backend/src/SmartErpAgent.Api/Controllers/AgentController.cs`

**Checkpoint**: User Stories 1 and 2 operate in tandem with robust validation.

---

## Phase 5: User Story 3 - Production Dependency Wiring and Route Mapping (Priority: P3)

**Goal**: Ensure `Program.cs` maps controllers and registers `AddAgentEngine()`.

**Independent Test**: Spin up service provider and verify `AgentController` resolves all constructor dependencies.

### Implementation for User Story 3

- [X] T012 [P] [US3] Verify `AddAgentEngine()` and `MapControllers()` in `backend/src/SmartErpAgent.Api/Program.cs`
- [X] T013 [US3] Add DI container resolution test for `AgentController` in `backend/tests/SmartErpAgent.UnitTests/AgentControllerTests.cs`

**Checkpoint**: Presentation pipeline fully wired into host startup.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Logging, formatting, and end-to-end verification

- [X] T014 [P] Verify structured logging of chat invocations in `backend/src/SmartErpAgent.Api/Controllers/AgentController.cs`
- [X] T015 Run end-to-end verification suite per `specs/003-agent-chat-api/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1; blocks all user stories.
- **User Story 1 (Phase 3 - MVP)**: Depends on Phase 2; no dependencies on other stories.
- **User Story 2 (Phase 4)**: Enhances User Story 1 validation.
- **User Story 3 (Phase 5)**: Depends on controller completion.
- **Polish (Phase 6)**: Final verification across all phases.

### Parallel Execution Opportunities

- T002 can run in parallel with T001.
- T004 can run in parallel with T003.
- T005 can run in parallel with T006.
- T010 can run in parallel with T011.
- T012 can run in parallel with T013.
- T014 can run in parallel with T015.

---

## Implementation Strategy

### MVP First (User Story 1 Only)
1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (`AgentChatRequestDto.cs`)
3. Complete Phase 3: User Story 1 (`AgentController.cs` + `[HttpPost("chat")]` + Unit Tests)
4. **VALIDATE**: Run `dotnet test --filter FullyQualifiedName~AgentControllerTests`

### Incremental Delivery
1. Foundation Complete → `AgentChatRequestDto` ready
2. Add US1 → `AgentController.Chat` action calling `IAgentOrchestrator`
3. Add US2 → Input validation & 400 Bad Request handling
4. Add US3 → Startup registration verification in `Program.cs`
5. Polish → End-to-end test execution
