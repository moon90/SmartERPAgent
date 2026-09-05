# Interface & Service Contracts: Frontend Agent Chat

**Feature**: `004-frontend-agent-chat`  
**Date**: 2026-09-05  
**Spec**: [specs/004-frontend-agent-chat/spec.md](../spec.md)

---

## 1. Data Access Service Contract (`AgentApiService`)

**Location**: `frontend/libs/shared/data-access/src/lib/services/agent-api.service.ts`  
**Export**: `frontend/libs/shared/data-access/src/index.ts`

### TypeScript Signature

```typescript
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AgentChatResponse } from '../models/agent.model';

@Injectable({
  providedIn: 'root',
})
export abstract class AgentApiService {
  /**
   * Dispatches a natural language prompt to the Semantic Kernel agent backend.
   *
   * @param prompt The user prompt to send to the agent orchestrator.
   * @returns Observable emitting the agent's textual response.
   */
  abstract sendMessage(prompt: string): Observable<AgentChatResponse>;
}
```

### HTTP Transport Details

| Property | Value |
| :--- | :--- |
| **Method** | `POST` |
| **Endpoint** | `/api/agent/chat` |
| **Headers** | `Content-Type: application/json`<br>`X-Tenant-ID: <GUID>` (injected automatically via `tenantInterceptor`) |
| **Request Payload** | `{ "prompt": string }` |
| **Success Response** | `200 OK` with `{ "response": string }` |
| **Client Error Response** | `400 Bad Request` with `{ "message": string }` |

---

## 2. Feature Component Contract (`AgentChatComponent`)

**Location**: `frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.ts`  
**Export**: `frontend/libs/agents/feature-chat/src/index.ts`

### Component Metadata

```typescript
@Component({
  selector: 'smart-erp-agent-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './agent-chat.component.html',
  styleUrl: './agent-chat.component.scss',
})
export class AgentChatComponent {
  // Public state exposed to template
  messages: ChatMessage[];
  isLoading: boolean;
  userPrompt: string;

  // Actions
  onSendMessage(): void;
}
```

### Template Elements & Data Bindings

| Element | Selector / Role | Behavior / Contract |
| :--- | :--- | :--- |
| **Chat Container** | `.chat-container` | Enclosing card containing stream and footer |
| **Message Stream** | `.message-stream` | Scrollable container with vertical auto-scroll |
| **User Message Bubble** | `.message.user` | Right-aligned bubble with user accent styling |
| **Agent Message Bubble** | `.message.agent` | Left-aligned bubble with AI avatar badge |
| **Typing Indicator** | `.typing-indicator` | Displayed if and only if `isLoading === true` |
| **Input Field** | `textarea.chat-input` or `input.chat-input` | Bound to `userPrompt` via `[(ngModel)]`, disabled when `isLoading === true` |
| **Submit Button** | `button.btn-send` | Triggers `onSendMessage()`, disabled when `isLoading === true` or `!userPrompt.trim()` |

---

## 3. Host Application Wiring (`smart-erp-web`)

In `frontend/apps/smart-erp-web/src/app/app.component.ts`:
- Import `AgentChatComponent` from `@smart-erp/feature-chat`.
- Add `AgentChatComponent` to `imports: [..., AgentChatComponent]`.
- Place `<smart-erp-agent-chat />` in the app's template or dashboard view.
