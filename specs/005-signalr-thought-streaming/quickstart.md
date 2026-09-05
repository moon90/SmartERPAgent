# Quickstart & Verification Guide: Real-Time SignalR Thought Streaming

**Feature**: `005-signalr-thought-streaming`  
**Date**: 2026-09-05  
**Spec**: [specs/005-signalr-thought-streaming/spec.md](./spec.md)

---

## 1. Prerequisites

- .NET 8.0 SDK installed
- Node.js 18+ and npm installed
- `@microsoft/signalr` installed in `frontend` workspace

---

## 2. Validation Scenario 1: Backend Solution Build & Unit Tests

Verifies that `AgentHub`, `IHubContext<AgentHub>` injection, and SignalR registration compile and pass all tests:

### Commands
```bash
dotnet build backend/SmartErpAgent.sln
dotnet test backend/SmartErpAgent.sln
```

### Expected Output
- Build succeeded (0 errors, 0 warnings).
- 14/14 unit tests passed.

---

## 3. Validation Scenario 2: Frontend Nx Workspace Build

Verifies that `@microsoft/signalr` integrates cleanly, TypeScript compiles in strict mode, and all libraries (`@smart-erp/data-access`, `@smart-erp/feature-chat`) resolve without errors:

### Command
```bash
npm --prefix frontend run build
```

### Expected Output
- `NX Successfully ran target build for project smart-erp-web`
- 0 TypeScript compiler errors.

---

## 4. Validation Scenario 3: Live End-to-End Thought Streaming

1. Start the API server:
   ```bash
   dotnet run --project backend/src/SmartErpAgent.Api
   ```
2. Start the Angular application:
   ```bash
   npm --prefix frontend start
   ```
3. Open `http://localhost:4200` in the browser.
4. Send inquiry: `"Do we have enough stock for SKU-123?"`.
5. Observe:
   - Real-time WebSocket connection established to `/hubs/agent`.
   - Streaming thought milestones appear below the user's prompt in italicized typography (e.g., *"Analyzing stock thresholds for SKU-123 via InventoryAgentPlugin..."*).
   - Once tool execution completes, final response is rendered in the agent message bubble.
