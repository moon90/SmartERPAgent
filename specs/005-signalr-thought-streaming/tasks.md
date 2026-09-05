# Tasks: Real-Time SignalR Agent Communication & Thought Streaming

**Feature**: `005-signalr-thought-streaming`  
**Specification**: [specs/005-signalr-thought-streaming/spec.md](./spec.md)  
**Implementation Plan**: [specs/005-signalr-thought-streaming/plan.md](./plan.md)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Install SignalR client dependencies and configure project framework references

- [X] T001 Add `@microsoft/signalr` package dependency to `frontend/package.json` and install dependencies via npm
- [X] T002 [P] Add `Microsoft.AspNetCore.App` framework references to `backend/src/SmartErpAgent.Application/SmartErpAgent.Application.csproj` and `backend/src/SmartErpAgent.AgentEngine/SmartErpAgent.AgentEngine.csproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core hub contracts, interfaces, and frontend event models

**⚠️ CRITICAL**: Must complete before any user story can be implemented

- [X] T003 Create base `AgentHub` class in `backend/src/SmartErpAgent.Application/Hubs/AgentHub.cs`
- [X] T004 [P] Create `ThoughtProcessUpdate` and update `ChatMessage` in `frontend/libs/shared/data-access/src/lib/models/agent.model.ts`

**Checkpoint**: Core hub type and data models ready - user story implementation can begin.

---

## Phase 3: User Story 1 - Real-Time Agent Thought Streaming in Chat UI (Priority: P1) 🎯 MVP

**Goal**: Display real-time streaming thought process dynamically beneath user prompt in `AgentChatComponent` until final response arrives.

**Independent Test**: Connect to `/hubs/agent`, dispatch a prompt, and verify intermediate thoughts stream into the UI in real time with italicized styling and pulsing indicator before the final response renders.

### Implementation for User Story 1

- [X] T005 [US1] Create `AgentHub` in `backend/src/SmartErpAgent.Api/Hubs/AgentHub.cs` inheriting from `Hub` and delegating to `IAgentOrchestrator`
- [X] T006 [P] [US1] Register `AddSignalR` and map `/hubs/agent` route in `backend/src/SmartErpAgent.Api/Program.cs`
- [X] T007 [US1] Update `SemanticKernelAgentOrchestrator` to inject `IHubContext<AgentHub>` and stream `ReceiveThoughtProcess` events in `backend/src/SmartErpAgent.AgentEngine/Services/SemanticKernelAgentOrchestrator.cs`
- [X] T008 [US1] Update `AgentChatComponent` template to render streaming thoughts with italicized styling and live pulsing indicator in `frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.html`
- [X] T009 [P] [US1] Update `AgentChatComponent` styling for `.thought-stream` in `frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.scss`
- [X] T010 [US1] Update `AgentChatComponent` logic to subscribe to `thoughtProcess$` and `finalResponse$` in `frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.ts`

**Checkpoint**: Real-time thought streaming functional end-to-end and visible in the chat UI (MVP ready).

---

## Phase 4: User Story 2 - Persistent Bidirectional Communication Hub & Service (Priority: P2)

**Goal**: Refactor `AgentApiService` in shared data-access to use SignalR `HubConnection`, exposing reactive observables and hub send method.

**Independent Test**: Establish connection to `/hubs/agent` via `AgentApiService`, send prompt, and verify emissions on `thoughtProcess$` and `finalResponse$`.

### Implementation for User Story 2

- [X] T011 [US2] Refactor `AgentApiService` to initialize SignalR `HubConnection` to `/hubs/agent` with automatic reconnect in `frontend/libs/shared/data-access/src/lib/services/agent-api.service.ts`
- [X] T012 [P] [US2] Expose `thoughtProcess$` and `finalResponse$` observables in `frontend/libs/shared/data-access/src/lib/services/agent-api.service.ts`
- [X] T013 [US2] Implement `sendPrompt` method dispatching over `HubConnection` in `frontend/libs/shared/data-access/src/lib/services/agent-api.service.ts`

**Checkpoint**: Client data-access service manages persistent WebSocket connection and reactive streams.

---

## Phase 5: User Story 3 - Resilient Connection Lifecycle & Graceful Fallback (Priority: P3)

**Goal**: Ensure graceful connection recovery, error notifications on disconnects, and fallback compatibility.

**Independent Test**: Simulate connection drop, assert automatic reconnect attempts with backoff, and verify error alert appears if disconnected.

### Implementation for User Story 3

- [X] T014 [US3] Add reconnection lifecycle logging and error handling in `frontend/libs/shared/data-access/src/lib/services/agent-api.service.ts`
- [X] T015 [P] [US3] Add connection status notice and fallback error handling in `frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.ts`

**Checkpoint**: Connection resilience and error recovery validated.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Test suite maintenance and end-to-end build verification

- [X] T016 [P] Update unit test suite for `SemanticKernelAgentOrchestrator` with `IHubContext` mock in `backend/tests/SmartErpAgent.UnitTests/AgentEngineTests.cs` and `AgentControllerTests.cs`
- [X] T017 Execute full backend and frontend validation suites per `specs/005-signalr-thought-streaming/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 - BLOCKS all user stories.
- **User Story 1 (Phase 3 - MVP)**: Depends on Phase 2 base hub and models.
- **User Story 2 (Phase 4)**: Implements client SignalR data-access service.
- **User Story 3 (Phase 5)**: Hardens connection lifecycle and error recovery.
- **Polish (Phase 6)**: Final test suite updates and quickstart verification.

### Parallel Execution Opportunities

- T002 can run in parallel with T001.
- T004 can run in parallel with T003.
- T006 and T009 can run in parallel with T005 / T008.
- T012 can run in parallel with T011.
- T015 can run in parallel with T014.
- T016 can run in parallel with T017.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (`@microsoft/signalr`, framework references)
2. Complete Phase 2: Foundational (`AgentHub` base, event models)
3. Complete Phase 3: User Story 1 (`AgentHub.cs`, `Program.cs`, `SemanticKernelAgentOrchestrator`, `AgentChatComponent`)
4. **VALIDATE**: Run `dotnet build backend/SmartErpAgent.sln` and `npm --prefix frontend run build`.

### Incremental Delivery

1. Setup + Foundation Complete → SignalR dependencies ready
2. Add US1 → Real-time thought streaming and UI display
3. Add US2 → Decoupled `AgentApiService` with reactive observables
4. Add US3 → Connection resilience and reconnect handling
5. Polish → Unit test suite updates and full verification
