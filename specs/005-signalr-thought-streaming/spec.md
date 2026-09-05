# Feature Specification: Real-Time Agent Communication & Thought Process Streaming

**Feature Branch**: `005-signalr-thought-streaming`

**Created**: 2026-09-05

**Status**: Draft

**Input**: User description: "Upgrade the agent communication from standard HTTP to real-time WebSockets using ASP.NET Core SignalR to stream the AI's thought process. 1. Backend (API): Create an `AgentHub` class in a new `Hubs` folder in the SmartErpAgent.Api project, inheriting from `Hub`. Update `Program.cs` to add `builder.Services.AddSignalR()` and map the endpoint using `app.MapHub<AgentHub>("/hubs/agent")`. 2. Backend (AgentEngine): Update `IAgentOrchestrator` and `SemanticKernelAgentOrchestrator` to inject `IHubContext<AgentHub>`. Modify the orchestration logic to stream intermediate planner steps (e.g., tool execution status) to the client using `SendAsync("ReceiveThoughtProcess", message)` before returning the final result. 3. Frontend (Nx Workspace): Ensure `@microsoft/signalr` is added to the package.json and installed. 4. Frontend (Service): Refactor `AgentApiService` in `libs/shared/data-access` to use a SignalR `HubConnection` pointing to `/hubs/agent`. Expose RxJS Observables for `thoughtProcess$` and `finalResponse$`. Add a method to send the prompt via the Hub instead of HTTP POST. 5. Frontend (UI): Update `AgentChatComponent` to subscribe to these real-time observables. The UI should display the streaming `thoughtProcess$` dynamically below the user's prompt (e.g., using a smaller italicized font) until the `finalResponse$` arrives."

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Real-Time Agent Thought Streaming in Chat UI (Priority: P1) 🎯 MVP

As an enterprise ERP operator, I want to observe the AI copilot's intermediate thought process and tool execution milestones in real time as my request is being processed, so that I have immediate transparency into how the agent is analyzing stock levels, ledger entries, or invoices rather than waiting in the dark.

**Why this priority**: Delivers direct user value by eliminating perceived latency, providing real-time cognitive transparency, and building trust in the autonomous actions executed by the agent.

**Independent Test**: Can be tested independently by typing a complex inquiry (e.g. "Do we have enough stock for SKU-123?") into the chat window and verifying that intermediate reasoning steps ("Querying inventory database...", "Evaluating safety stock threshold...") appear progressively in the UI before the final conversational reply arrives.

**Acceptance Scenarios**:

1. **Given** the chat copilot is open, **When** the user sends a prompt, **Then** an active thinking state appears immediately below the user's message, streaming real-time thought messages as the agent runs planner steps.
2. **Given** intermediate thought messages are streaming, **When** each new thought update arrives, **Then** the UI updates smoothly in real time without screen flicker.
3. **Given** the agent finishes its tool execution and synthesis, **When** the final answer is delivered, **Then** the thought stream transitions into the completed response bubble and the in-flight loading state is cleared.

---

### User Story 2 - Persistent Bidirectional Communication Hub & Service (Priority: P2)

As a frontend client application, I want to communicate with the agent orchestrator via a persistent real-time connection, so that user prompts can be dispatched and intermediate events can be streamed down without HTTP polling overhead.

**Why this priority**: Establishes the real-time architectural pipeline connecting the Angular client to the ASP.NET Core backend, decoupling event distribution from one-off HTTP request-response cycles.

**Independent Test**: Can be tested independently by establishing a connection to `/hubs/agent`, invoking prompt transmission over the real-time hub, and verifying that the connection receives discrete `ReceiveThoughtProcess` events followed by the final resolution.

**Acceptance Scenarios**:

1. **Given** the application initializes, **When** the real-time service connects to `/hubs/agent`, **Then** the connection state becomes active and ready to transmit prompts.
2. **Given** an active connection, **When** the client dispatches a prompt, **Then** the backend orchestrator receives the prompt and streams `ReceiveThoughtProcess` events to the caller.
3. **Given** multiple events dispatched during orchestration, **When** received by the client, **Then** reactive streams (`thoughtProcess$` and `finalResponse$`) emit data to subscribers in correct sequence.

---

### User Story 3 - Resilient Connection Lifecycle & Graceful Fallback (Priority: P3)

As an enterprise user working in fluctuating network conditions, I want the real-time communication channel to automatically reconnect and handle transient network interruptions gracefully, so that my chat experience is not permanently broken if a connection drops.

**Why this priority**: Ensures enterprise-grade reliability and seamless recovery during momentary network drops or server restarts.

**Independent Test**: Can be tested by disconnecting network connectivity or stopping the hub server, verifying that the client attempts automatic reconnection and displays a non-intrusive status notice.

**Acceptance Scenarios**:

1. **Given** a drop in the real-time socket connection, **When** connectivity is interrupted, **Then** the client automatically attempts to reconnect with backoff.
2. **Given** the connection is successfully re-established, **When** the user submits a new prompt, **Then** normal streaming operations resume immediately.
3. **Given** an unrecoverable failure occurs, **When** an error is raised, **Then** the user is informed with a helpful inline notification and the loading state is reset.

---

### Edge Cases

- **Rapid Prompt Submission**: Submissions while an active thought stream is in flight must be prevented by disabling input controls until completion.
- **Empty or Whitespace Thought**: Intermediate messages must be trimmed and validated before broadcasting to avoid displaying empty bubbles.
- **Connection Drops Mid-Stream**: If the connection breaks during thought streaming, the UI must gracefully finalize the turn with an informative connection error notice.
- **Server Cold Start**: If the backend is compiling or restarting, the frontend client should handle initial connection failure gracefully and retry automatically.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The backend API MUST establish a real-time communication hub (`AgentHub`) mapped to route `/hubs/agent`.
- **FR-002**: The agent orchestrator (`IAgentOrchestrator` / `SemanticKernelAgentOrchestrator`) MUST inject the hub context (`IHubContext<AgentHub>`) to broadcast real-time events.
- **FR-003**: The orchestrator MUST stream intermediate planner milestones and tool execution status to the client via `SendAsync("ReceiveThoughtProcess", message)` before returning the final response.
- **FR-004**: The orchestrator MUST maintain multi-tenant data isolation, ensuring thought processes and tool actions only access data belonging to the active tenant.
- **FR-005**: The frontend workspace MUST include and install `@microsoft/signalr`.
- **FR-006**: `AgentApiService` in `libs/shared/data-access` MUST manage a real-time `HubConnection` targeting `/hubs/agent` with automatic reconnection.
- **FR-007**: `AgentApiService` MUST expose reactive RxJS observables for `thoughtProcess$` (streaming intermediate reasoning) and `finalResponse$` (final synthesized output).
- **FR-008**: `AgentApiService` MUST provide a method to dispatch user prompts over the real-time hub connection.
- **FR-009**: `AgentChatComponent` in `libs/agents/feature-chat` MUST subscribe to the real-time observables and render the streaming thought process dynamically below the active prompt.
- **FR-010**: The thought process text MUST be visually differentiated from conversational responses (e.g., distinct italicized typography, subtle background, or live cognitive pulse icon).
- **FR-011**: Upon receiving the final response, the UI MUST transition smoothly to display the final response bubble and clear the active thought indicator.

---

### Key Entities

- **ThoughtProcessEvent**: Represents an intermediate reasoning or tool-invocation update emitted during agent execution (e.g. `message: string`, `timestamp: Date`).
- **AgentStreamPayload**: Payload transmitted across the real-time hub containing the user's prompt and tenant context.
- **FinalResponseEvent**: Represents the completed conversational answer synthesized by the agent orchestrator.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The first thought process streaming update is rendered in the UI within 500ms of agent planner initiation.
- **SC-002**: 100% of tool executions performed by the agent engine broadcast a corresponding intermediate status event to the client before final response delivery.
- **SC-003**: In-flight thought process transitions to the completed response bubble within 100ms of final response delivery with 0 orphan loading spinners.
- **SC-004**: The client automatically attempts reconnection with backoff when the real-time socket is temporarily disconnected.
- **SC-005**: Both backend solution (`SmartErpAgent.sln`) and frontend Nx workspace (`smart-erp-web`) build and pass all tests with 0 errors.

---

## Assumptions

- ASP.NET Core SignalR uses WebSockets as the primary transport protocol with fallback to Server-Sent Events or Long Polling if WebSockets are unavailable in the browser environment.
- The browser supports modern WebSockets and ES2022+ features.
- Tenant identification is passed either via connection query string, headers, or method arguments to enforce Constitution Principle II (Strict Multi-Tenancy).
- The existing HTTP endpoint `/api/agent/chat` can remain available for non-streaming backward compatibility.
