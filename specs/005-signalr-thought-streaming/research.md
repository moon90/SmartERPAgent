# Technical Research: Real-Time SignalR Agent Communication & Thought Streaming

**Feature**: `005-signalr-thought-streaming`  
**Date**: 2026-09-05  
**Spec**: [specs/005-signalr-thought-streaming/spec.md](./spec.md)

---

## 1. Real-Time Transport Protocol & Endpoint Mapping

### Decision
Use **ASP.NET Core SignalR** over WebSockets (with automatic fallback to Server-Sent Events / Long Polling) mapped to `/hubs/agent`.

### Rationale
- SignalR provides transparent connection management, automatic reconnection, and bidirectional full-duplex messaging.
- Allows the server to push intermediate thought updates (`ReceiveThoughtProcess`) during execution without client polling.
- Enables clients to invoke `SendPrompt` or listen on topic streams with minimal protocol overhead.

### Alternatives Considered
- *Server-Sent Events (SSE)*: One-way only; client prompts still require separate HTTP POST requests.
- *Raw WebSockets*: Requires custom framing, manual reconnection, heartbeats, and serialization protocols. SignalR encapsulates this natively.

---

## 2. Hub Architecture & Clean Dependency Separation

### Decision
- Base `AgentHub` definition in `SmartErpAgent.Application.Hubs` inheriting from `Microsoft.AspNetCore.SignalR.Hub`.
- Presentation hub `AgentHub` in `SmartErpAgent.Api.Hubs` in `backend/src/SmartErpAgent.Api/Hubs/AgentHub.cs`.
- `Program.cs` maps `app.MapHub<AgentHub>("/hubs/agent")` and bridges `IHubContext` so `SemanticKernelAgentOrchestrator` in `SmartErpAgent.AgentEngine` injects `IHubContext<AgentHub>` cleanly.
- Intermediate planner steps and plugin invocations broadcast `SendAsync("ReceiveThoughtProcess", message)` to clients.

### Rationale
- Adheres to Constitution Principle I (Clean Architecture): dependencies point inwards.
- Eliminates circular dependencies between `SmartErpAgent.Api` and `SmartErpAgent.AgentEngine`.
- Directly fulfills the requirement for `SemanticKernelAgentOrchestrator` to inject `IHubContext<AgentHub>`.

---

## 3. Frontend Client Architecture (`@smart-erp/data-access` & `@smart-erp/feature-chat`)

### Decision
- Install `@microsoft/signalr` in `frontend/package.json`.
- Refactor `AgentApiService` in `frontend/libs/shared/data-access/src/lib/services/agent-api.service.ts`:
  - Maintains a managed `HubConnection` with `.withUrl('/hubs/agent')` and `.withAutomaticReconnect()`.
  - Exposes RxJS `Subject` / `Observable`:
    - `thoughtProcess$: Observable<string>` for intermediate thought milestones.
    - `finalResponse$: Observable<string>` for final synthesized agent output.
  - Implements `sendPrompt(prompt: string): Promise<void>` or `sendMessage(prompt: string): Observable<AgentChatResponse>` delegating over the real-time hub.
- Update `AgentChatComponent` in `frontend/libs/agents/feature-chat`:
  - Subscribes to `thoughtProcess$` and `finalResponse$`.
  - Renders the streaming thought text directly beneath the user's prompt in italicized typography with a live pulsing indicator.
  - Automatically resets when `finalResponse$` arrives, transitioning smoothly into the final response bubble.

### Rationale
- RxJS Observables fit naturally into Angular's reactive paradigm.
- Automatic reconnection ensures seamless recovery from transient network interruptions.
