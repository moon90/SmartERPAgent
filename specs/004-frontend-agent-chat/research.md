# Technical Research: Angular Agent Chat Interface & API Client

**Feature**: `004-frontend-agent-chat`  
**Date**: 2026-09-05  
**Spec**: [specs/004-frontend-agent-chat/spec.md](./spec.md)

---

## 1. Data Access Service Architecture (`AgentApiService`)

### Decision
Create `AgentApiService` in `frontend/libs/shared/data-access/src/lib/services/agent-api.service.ts`:

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AgentChatResponse } from '../models/agent.model';

@Injectable({
  providedIn: 'root',
})
export class AgentApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/agent/chat';

  sendMessage(prompt: string): Observable<AgentChatResponse> {
    return this.http.post<AgentChatResponse>(this.apiUrl, { prompt });
  }
}
```

### Rationale
- Uses modern Angular `inject(HttpClient)` functional injection.
- Returns a strongly typed `Observable<AgentChatResponse>`, allowing callers to handle asynchronous responses and errors using standard RxJS patterns.
- Because `tenantInterceptor` is registered globally in the Angular application, all HTTP requests to `/api/agent/chat` automatically carry the active `X-Tenant-ID` header.

---

## 2. Component Design & State Management (`AgentChatComponent`)

### Decision
Create `AgentChatComponent` in `frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.ts`:
- Standalone component with `CommonModule`, `FormsModule`.
- Component state:
  - `messages: ChatMessage[] = [...]` (initialized with a helpful welcome message from the agent)
  - `isLoading: boolean = false`
  - `userPrompt: string = ''`
- Event handler `onSendMessage()`:
  - Validates `userPrompt.trim().length > 0`.
  - Appends `{ role: 'user', content: prompt, timestamp: new Date() }`.
  - Sets `isLoading = true` and clears input.
  - Subscribes to `agentApiService.sendMessage(prompt)`:
    - On success: Appends `{ role: 'agent', content: res.response, timestamp: new Date() }`.
    - On error: Appends error alert message.
    - On finalize: Sets `isLoading = false`.

### Rationale
- Clean unidirectional data flow.
- Clear separation between UI rendering and HTTP transport.
- Disables submit input and button while loading to prevent race conditions.

---

## 3. UI/UX & Responsive Styling Design

### Decision
- Chat card container with flexible height and auto-overflow scrolling.
- Modern visual styling:
  - User messages: Distinct primary accent bubble aligned right.
  - Agent messages: Polished card bubble with ERP badge icon aligned left.
  - Typing indicator: Three pulsing dots animation shown when `isLoading` is true.
  - Responsive footer: Sticky input row with button and enter-key submission.
