# Tasks: Angular Agent Chat Interface & API Client

**Feature**: `004-frontend-agent-chat`  
**Specification**: [specs/004-frontend-agent-chat/spec.md](./spec.md)  
**Implementation Plan**: [specs/004-frontend-agent-chat/plan.md](./plan.md)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify Nx monorepo workspace configuration, path mappings, and HTTP client providers

- [X] T001 Verify Nx workspace paths and library tsconfig mappings for `@smart-erp/data-access` and `@smart-erp/feature-chat` in `frontend/tsconfig.base.json`
- [X] T002 [P] Verify HttpClient and TenantInterceptor registration in `frontend/apps/smart-erp-web/src/app/app.config.ts`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core TypeScript interfaces and shared models for agent chat messaging

**⚠️ CRITICAL**: Must complete before any user story can be implemented

- [X] T003 Create `ChatMessage`, `AgentChatRequest`, and `AgentChatResponse` interfaces in `frontend/libs/shared/data-access/src/lib/models/agent.model.ts`
- [X] T004 [P] Export agent models from `frontend/libs/shared/data-access/src/index.ts`

**Checkpoint**: Core models ready - user story implementation can begin.

---

## Phase 3: User Story 1 - Interactive Conversational Copilot UI (Priority: P1) 🎯 MVP

**Goal**: Deliver the interactive chat copilot UI rendering user/agent messages, input field, submit button, and integrating with the host app.

**Independent Test**: Mount `AgentChatComponent`, submit a prompt, and verify that the user's message immediately appears in the message stream, `isLoading` toggles, and the AI's response is appended.

### Implementation for User Story 1

- [X] T005 [US1] Create standalone `AgentChatComponent` with state management (`messages` array, `isLoading`) in `frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.ts`
- [X] T006 [P] [US1] Create HTML template with scrollable message stream, message bubbles, input field, and submit button in `frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.html`
- [X] T007 [P] [US1] Implement modern responsive SCSS styles for chat window and message bubbles in `frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.scss`
- [X] T008 [US1] Export `AgentChatComponent` from `frontend/libs/agents/feature-chat/src/index.ts`
- [X] T009 [US1] Mount `AgentChatComponent` in the host dashboard in `frontend/apps/smart-erp-web/src/app/app.component.ts` and `frontend/apps/smart-erp-web/src/app/app.component.html`

**Checkpoint**: User Story 1 is functional and visible in the host application (MVP ready).

---

## Phase 4: User Story 2 - Decoupled Frontend API Data Access Service (Priority: P2)

**Goal**: Encapsulate HTTP communication within `AgentApiService` in the shared data-access library to dispatch POST requests to `/api/agent/chat` with `{ prompt }`.

**Independent Test**: Invoke `AgentApiService.sendMessage(prompt)` and verify that an HTTP POST request is sent to `/api/agent/chat` with body `{ prompt }` and active `X-Tenant-ID`.

### Implementation for User Story 2

- [X] T010 [US2] Implement `AgentApiService` with `sendMessage` method injecting `HttpClient` in `frontend/libs/shared/data-access/src/lib/services/agent-api.service.ts`
- [X] T011 [P] [US2] Export `AgentApiService` from `frontend/libs/shared/data-access/src/index.ts`
- [X] T012 [US2] Inject `AgentApiService` into `AgentChatComponent` and wire prompt dispatching in `frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.ts`

**Checkpoint**: UI component communicates with backend API via decoupled data access service.

---

## Phase 5: User Story 3 - Robust Component State Management & Error Handling (Priority: P3)

**Goal**: Enforce whitespace validation, disable controls during loading, render typing indicator, and handle network errors gracefully.

**Independent Test**: Attempt whitespace submission (verify blocked), submit valid prompt (verify controls disabled and typing indicator displayed), and simulate network failure (verify error notice rendered).

### Implementation for User Story 3

- [X] T013 [US3] Enforce non-empty trimmed prompt validation and enter-key submission in `frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.ts`
- [X] T014 [P] [US3] Add typing indicator element and disabled state bindings for submit button and input in `frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.html`
- [X] T015 [US3] Implement error handling in `AgentChatComponent` to revert `isLoading` and append error message in `frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.ts`

**Checkpoint**: Component state management handles edge cases and network disconnects reliably.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Autoscroll behavior, visual styling refinements, and full workspace build verification

- [X] T016 [P] Implement auto-scroll to bottom behavior on new chat messages in `frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.ts`
- [X] T017 Execute full frontend build verification suite via `npm --prefix frontend run build` per `specs/004-frontend-agent-chat/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 - BLOCKS all user stories.
- **User Story 1 (Phase 3 - MVP)**: Depends on Phase 2 (Foundational models).
- **User Story 2 (Phase 4)**: Implements and wires `AgentApiService` with `AgentChatComponent`.
- **User Story 3 (Phase 5)**: Adds validation, disabled state, typing indicator, and error handling.
- **Polish (Phase 6)**: Final autoscroll and build verification.

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2).
- **User Story 2 (P2)**: Integrates with US1 components and shared data-access library.
- **User Story 3 (P3)**: Builds on US1 and US2 to harden edge cases.

### Parallel Execution Opportunities

- T002 can run in parallel with T001.
- T004 can run in parallel with T003.
- T006 and T007 can run in parallel with T005.
- T011 can run in parallel with T010.
- T014 can run in parallel with T013.
- T016 can run in parallel with T017.

---

## Parallel Example: User Story 1

```bash
# Launch HTML template and SCSS styling in parallel:
Task: "Create HTML template with scrollable message stream in frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.html"
Task: "Implement modern responsive SCSS styles in frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.scss"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (`tsconfig.base.json`, `app.config.ts`)
2. Complete Phase 2: Foundational (`agent.model.ts`, exports)
3. Complete Phase 3: User Story 1 (`AgentChatComponent` + HTML/SCSS + App Mount)
4. **VALIDATE**: Run `npm --prefix frontend run build` to verify initial MVP compilation.

### Incremental Delivery

1. Complete Setup + Foundational → Domain models ready
2. Add User Story 1 → Interactive chat UI rendered in host application
3. Add User Story 2 → `AgentApiService` HTTP dispatches to `/api/agent/chat`
4. Add User Story 3 → Whitespace validation, typing indicator, error handling
5. Polish → Autoscroll and clean production bundle verification
