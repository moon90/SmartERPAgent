# Quickstart & Verification Guide: Frontend Agent Chat

**Feature**: `004-frontend-agent-chat`  
**Date**: 2026-09-05  
**Spec**: [specs/004-frontend-agent-chat/spec.md](./spec.md)

---

## 1. Prerequisites

- Node.js 18+ and npm installed.
- Dependencies installed in the `frontend` workspace:
  ```bash
  npm --prefix frontend install
  ```

---

## 2. Validation Scenario 1: Automated Workspace Build & Type Check

Verifies that all TypeScript code conforms to strict compilation rules, that library path mappings resolve properly across the Nx monorepo (`@smart-erp/data-access`, `@smart-erp/feature-chat`), and that the host application compiles without errors.

### Command
```bash
npm --prefix frontend run build
```

### Expected Output
- `NX Successfully ran target build for project smart-erp-web`
- Output bundle generated in `frontend/dist/apps/smart-erp-web`
- 0 TypeScript compiler errors.

---

## 3. Validation Scenario 2: Interactive Browser Verification

Validates the full chat interaction flow with the Semantic Kernel backend:

1. **Start the Backend API**:
   ```bash
   dotnet run --project backend/src/SmartErpAgent.Api
   ```

2. **Start the Frontend Dev Server**:
   ```bash
   npm --prefix frontend start
   ```

3. **Verify the Interaction**:
   - Open browser at `http://localhost:4200` (or configured dev port).
   - Verify the chat component renders with a welcome greeting from the AI assistant.
   - Enter a query into the input field:
     ```text
     What is the current inventory status of SKU-123?
     ```
   - Click the "Send" button (or press `Enter`).
   - Observe immediate UI changes:
     - The user's query is added to the message stream immediately.
     - The input field and send button are disabled.
     - The typing/loading indicator appears with pulsing animation.
   - Observe response:
     - The backend agent's response arrives and is appended to the message stream.
     - The typing indicator disappears.
     - The input field and send button are re-enabled for the next inquiry.
