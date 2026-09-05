# Implementation Plan: Angular Agent Chat Interface & API Client

**Branch**: `004-frontend-agent-chat` | **Date**: 2026-09-05 | **Spec**: [specs/004-frontend-agent-chat/spec.md](./spec.md)

**Input**: Feature specification from `specs/004-frontend-agent-chat/spec.md`

---

## Summary

Implement the Angular frontend client and interactive chat component inside the `frontend` Nx workspace.
1. `AgentApiService`: Located in `libs/shared/data-access`, injects Angular's `HttpClient`, and provides `sendMessage(prompt: string): Observable<AgentChatResponse>` making a POST request to `/api/agent/chat` with `{ prompt }`.
2. `AgentChatComponent`: Located in `libs/agents/feature-chat`, a standalone Angular component managing an array of messages (`role: 'user' | 'agent'`, `content: string`) and an `isLoading` boolean flag.
3. Chat UI: A modern, responsive interface with a scrollable conversation stream, styled message bubbles, loading indicator, input field, and submit button.
4. Host App Integration: Verified in `frontend/apps/smart-erp-web` and built with `npx nx build smart-erp-web`.

---

## Technical Context

**Language/Version**: TypeScript 5.6+ / Angular 19 (Standalone Components, Signals, SCSS, RxJS 7.8+)

**Monorepo Framework**: Nx 23.2.0

**Workspace Libraries**:
- `@smart-erp/data-access` (`frontend/libs/shared/data-access`)
- `@smart-erp/feature-chat` (`frontend/libs/agents/feature-chat`)
- `@smart-erp/ui` (`frontend/libs/shared/ui`)

**Primary Dependencies**:
- `@angular/core`, `@angular/common`, `@angular/forms`, `@angular/common/http`, `rxjs`

**Styling**: Vanilla SCSS with modern CSS custom properties, glassmorphism card surfaces, and responsive layouts.

**Constraints**:
- Constitution Principle IV (Modular Monorepo): Logic belongs in `libs/`, host app remains a lean shell.
- Constitution Principle III (Strict TypeScript): `"strict": true`, `"noImplicitOverride": true`, `"noImplicitReturns": true`. No `any` types in public contracts.
- Multi-Tenancy (Constitution Principle II): Automatic injection of `X-Tenant-ID` header via `tenantInterceptor`.

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Requirement | Assessment | Status |
| :--- | :--- | :--- | :---: |
| **I. Clean Architecture** | Backend controller isolation | Frontend communicates solely via standard `/api/agent/chat` HTTP interface | **✓ PASS** |
| **II. Strict Multi-Tenancy** | Requests MUST preserve tenant isolation | All outgoing HTTP requests pass through `tenantInterceptor`, stamping active `X-Tenant-ID` | **✓ PASS** |
| **III. Strict Code Standards** | Strict TypeScript, zero `any` types | Strongly typed interfaces (`ChatMessage`, `AgentChatResponse`), strict compiler mode | **✓ PASS** |
| **IV. Modular Monorepo** | Logic in shared workspace libraries | `AgentApiService` in `libs/shared/data-access`, `AgentChatComponent` in `libs/agents/feature-chat` | **✓ PASS** |
| **V. Agent Tool Decoupling** | Safe AI interaction | Frontend receives structured responses from orchestrator without raw DB access | **✓ PASS** |

---

## Project Structure

### Documentation (this feature)

```text
specs/004-frontend-agent-chat/
├── spec.md              # Feature specification
├── plan.md              # Technical implementation plan
├── research.md          # Technical decisions and research findings
├── data-model.md        # UI models and response types
├── quickstart.md        # Runnable verification guide
├── contracts/           # Service and component contracts
│   └── frontend-agent-contracts.md
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Actionable task breakdown (created by /speckit-tasks)
```

### Source Code Touchpoints

```text
frontend/
├── libs/
│   ├── shared/data-access/
│   │   └── src/
│   │       ├── index.ts
│   │       └── lib/
│   │           ├── models/agent.model.ts
│   │           └── services/agent-api.service.ts
│   └── agents/feature-chat/
│       └── src/
│           ├── index.ts
│           └── lib/
│               └── agent-chat/
│                   ├── agent-chat.component.ts
│                   ├── agent-chat.component.html
│                   └── agent-chat.component.scss
└── apps/smart-erp-web/
    └── src/
        └── app/
            ├── app.component.ts
            └── app.component.html
```

---

## Complexity Tracking

> **No violations of Constitution principles detected.**
