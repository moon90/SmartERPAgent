# Implementation Plan: Real-Time SignalR Agent Communication & Thought Streaming

**Branch**: `005-signalr-thought-streaming` | **Date**: 2026-09-05 | **Spec**: [specs/005-signalr-thought-streaming/spec.md](./spec.md)

**Input**: Feature specification from `specs/005-signalr-thought-streaming/spec.md`

---

## Summary

Upgrade the communication channel between the Angular frontend and Semantic Kernel orchestrator from standard HTTP requests to real-time WebSockets using ASP.NET Core SignalR:
1. **Backend (API)**: Create `AgentHub` class in `backend/src/SmartErpAgent.Api/Hubs/AgentHub.cs` inheriting from `Hub`. Update `Program.cs` to add `builder.Services.AddSignalR()` and map the hub endpoint using `app.MapHub<AgentHub>("/hubs/agent")`.
2. **Backend (AgentEngine)**: Update `IAgentOrchestrator` and `SemanticKernelAgentOrchestrator` to inject `IHubContext<AgentHub>`. Modify orchestration logic to stream intermediate planner steps and tool execution status via `SendAsync("ReceiveThoughtProcess", message)` before returning the final response.
3. **Frontend (Nx Workspace)**: Install `@microsoft/signalr` in `frontend/package.json`.
4. **Frontend (Service)**: Refactor `AgentApiService` in `libs/shared/data-access` to manage a SignalR `HubConnection` to `/hubs/agent`, exposing reactive `thoughtProcess$` and `finalResponse$` observables and a method to dispatch prompts over the hub.
5. **Frontend (UI)**: Update `AgentChatComponent` in `libs/agents/feature-chat` to display streaming `thoughtProcess$` dynamically below the user's prompt (in smaller italicized font) until the `finalResponse$` arrives.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8.0 & TypeScript 5.6+ / Angular 19
**Real-Time Framework**: ASP.NET Core SignalR / `@microsoft/signalr`
**Monorepo Framework**: Nx 23.2.0
**Workspace Libraries**:
- `@smart-erp/data-access` (`frontend/libs/shared/data-access`)
- `@smart-erp/feature-chat` (`frontend/libs/agents/feature-chat`)
**Backend Projects**:
- `SmartErpAgent.Api` (`Hubs/AgentHub.cs`, `Program.cs`)
- `SmartErpAgent.AgentEngine` (`Services/SemanticKernelAgentOrchestrator.cs`)
- `SmartErpAgent.Application` (`Hubs/AgentHub.cs`, `Common/Interfaces/IAgentOrchestrator.cs`)
**Testing**: `dotnet test` (xUnit, Moq, FluentAssertions) & `nx build`

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Requirement | Assessment | Status |
| :--- | :--- | :--- | :---: |
| **I. Clean Architecture** | Inward dependency flow | Base hub context placed in Application layer avoiding circular dependencies; Presentation maps the route | **✓ PASS** |
| **II. Strict Multi-Tenancy** | Preserve tenant isolation | Prompts and execution contexts maintain tenant scope over real-time connections | **✓ PASS** |
| **III. Strict Code Standards** | Strict TypeScript and C# nullability | Strongly typed hub events and contracts; zero `any` types | **✓ PASS** |
| **IV. Modular Monorepo** | Logic in shared workspace libraries | Real-time service in `@smart-erp/data-access`; UI in `@smart-erp/feature-chat` | **✓ PASS** |
| **V. Agent Tool Decoupling** | Safe AI interaction | Live thoughts reflect tool execution status without leaking unformatted internal state | **✓ PASS** |

---

## Project Structure

### Documentation (this feature)

```text
specs/005-signalr-thought-streaming/
├── spec.md              # Feature specification
├── plan.md              # Technical implementation plan
├── research.md          # Technical decisions and research findings
├── data-model.md        # Real-time event models and UI state transitions
├── quickstart.md        # Runnable verification guide
├── contracts/           # SignalR hub and client contracts
│   └── signalr-agent-contracts.md
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Actionable task breakdown (created by /speckit-tasks)
```

### Source Code Touchpoints

```text
backend/
├── src/
│   ├── SmartErpAgent.Application/
│   │   └── Hubs/
│   │       └── AgentHub.cs
│   ├── SmartErpAgent.AgentEngine/
│   │   └── Services/
│   │       └── SemanticKernelAgentOrchestrator.cs
│   └── SmartErpAgent.Api/
│       ├── Hubs/
│       │   └── AgentHub.cs
│       └── Program.cs
└── tests/
    └── SmartErpAgent.UnitTests/

frontend/
├── package.json
├── libs/
│   ├── shared/data-access/
│   │   └── src/
│   │       └── lib/
│   │           ├── models/agent.model.ts
│   │           └── services/agent-api.service.ts
│   └── agents/feature-chat/
│       └── src/
│           └── lib/
│               └── agent-chat/
│                   ├── agent-chat.component.ts
│                   ├── agent-chat.component.html
│                   └── agent-chat.component.scss
└── apps/smart-erp-web/
```

---

## Complexity Tracking

> **No violations of Constitution principles detected.**
