# Implementation Plan: Agent Chat API Presentation Endpoint

**Branch**: `003-agent-chat-api` | **Date**: 2026-09-05 | **Spec**: [specs/003-agent-chat-api/spec.md](./spec.md)

**Input**: Feature specification from `specs/003-agent-chat-api/spec.md`

---

## Summary

Implement the API presentation layer in `SmartErpAgent.Api` to expose the Semantic Kernel agent orchestrator to the frontend Angular application. Deliver:
1. `AgentChatRequestDto` in `SmartErpAgent.Application/DTOs/AgentChatRequestDto.cs` containing `public string Prompt { get; set; } = string.Empty;`.
2. `AgentController` in `SmartErpAgent.Api/Controllers/AgentController.cs` decorated with `[ApiController]` and `[Route("api/[controller]")]`, injecting `IAgentOrchestrator`.
3. A `[HttpPost("chat")]` endpoint accepting `AgentChatRequestDto`, validating the input prompt, calling `IAgentOrchestrator.ExecutePromptAsync(request.Prompt)`, and returning the AI response in an `IActionResult`.
4. Confirmation that `SmartErpAgent.Api/Program.cs` registers `builder.Services.AddAgentEngine()` and calls `app.MapControllers()`.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8 LTS (`<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`)

**Primary Dependencies**: 
- `Microsoft.AspNetCore.Mvc`
- `SmartErpAgent.Application`
- `SmartErpAgent.AgentEngine`
- `SmartErpAgent.Core`

**Storage**: N/A (Presentation layer only; state delegated through `IAgentOrchestrator`)

**Testing**: xUnit controller unit tests in `backend/tests/SmartErpAgent.UnitTests/AgentControllerTests.cs`

**Target Platform**: Kestrel HTTP Server on macOS / Linux / Windows / Docker

**Project Type**: ASP.NET Core Web API presentation layer

**Performance Goals**: Controller dispatch overhead < 5ms (excluding model inference)

**Constraints**:
- Clean Architecture (Constitution Principle I): Zero domain, database, or AI orchestration logic inside `AgentController`
- Strict Multi-Tenancy (Constitution Principle II): Controller preserves active tenant context established by `MultiTenantMiddleware`
- Strict Typing (Constitution Principle III): Public contracts use strongly typed DTOs with nullable annotations

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Requirement | Assessment | Status |
| :--- | :--- | :--- | :---: |
| **I. Clean Architecture** | Controllers MUST act purely as thin presentation adapters; zero domain logic | `AgentController` only validates prompt emptiness and forwards to `_agentOrchestrator.ExecutePromptAsync` | **✓ PASS** |
| **II. Strict Multi-Tenancy** | Request pipeline MUST preserve tenant boundaries | Multi-tenant context resolved from `X-Tenant-ID` header prior to controller execution | **✓ PASS** |
| **III. Strict Code Standards** | Nullable reference types enabled, strong typing, zero unvalidated types | `AgentChatRequestDto` with non-null `Prompt`, `<Nullable>enable</Nullable>` | **✓ PASS** |
| **IV. Modular Monorepo** | REST endpoint conforms to frontend `@smart-erp/data-access` contracts | `POST /api/agent/chat` returns standard JSON payload consumed by Angular copilot | **✓ PASS** |
| **V. Agent Tool Decoupling** | AI reasoning delegated exclusively to `AgentEngine` | Controller has no direct dependency on Semantic Kernel internals; talks solely to `IAgentOrchestrator` | **✓ PASS** |

---

## Project Structure

### Documentation (this feature)

```text
specs/003-agent-chat-api/
├── spec.md              # Feature specification
├── plan.md              # Technical implementation plan
├── research.md          # Technical decisions and research findings
├── data-model.md        # DTO schemas and transfer models
├── quickstart.md        # Runnable verification guide
├── contracts/           # Endpoint & DTO contracts
│   └── agent-chat-api-contract.md
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Actionable task breakdown (created by /speckit-tasks)
```

### Source Code Touchpoints

```text
backend/
├── src/
│   ├── SmartErpAgent.Application/
│   │   ├── Common/Interfaces/IAgentOrchestrator.cs
│   │   └── DTOs/AgentChatRequestDto.cs
│   ├── SmartErpAgent.Api/
│   │   ├── Controllers/AgentController.cs
│   │   └── Program.cs
│   └── SmartErpAgent.AgentEngine/
│       └── DependencyInjection.cs
└── tests/
    └── SmartErpAgent.UnitTests/
        └── AgentControllerTests.cs
```

---

## Complexity Tracking

> **No violations of Constitution principles detected.**
