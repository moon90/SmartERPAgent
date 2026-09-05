# Feature Specification: Angular Agent Chat Interface & API Client

**Feature Branch**: `004-frontend-agent-chat`

**Created**: 2026-09-05

**Status**: Draft

**Input**: User description: "Implement the Angular frontend components to interact with the Semantic Kernel Agent API. All work must be done inside the 'frontend' Nx workspace. 1. In the 'libs/shared/data-access' library, create an Angular service named `AgentApiService`. Inject `HttpClient` and implement a method `sendMessage(prompt: string)` that makes a POST request to `/api/agent/chat` with the payload `{ prompt: prompt }`. 2. In the 'libs/agents/feature-chat' library, create a standalone Angular component named `AgentChatComponent`. 3. The component must manage a state array of chat messages (storing 'user' or 'agent' roles and the text content) and a boolean `isLoading` flag. 4. Implement a chat interface in the component's HTML/SCSS: a scrollable message history area, an input text field, and a submit button. 5. When the user submits a prompt, immediately append their message to the UI, set `isLoading` to true, call `AgentApiService.sendMessage()`, and upon receiving the response, append the AI's message and set `isLoading` back to false."

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Interactive Conversational Copilot UI (Priority: P1) 🎯 MVP

As an enterprise ERP operator, I want to submit natural language inquiries to the AI copilot through an interactive chat window, so that I can inspect inventory stock levels and business data in real time directly from the web browser.

**Why this priority**: Delivers the visual, interactive interface for end users to engage with the AI agent platform, completing the end-to-end loop from web UI to backend Semantic Kernel plugins.

**Independent Test**: Can be tested independently by typing a message into the input field, submitting it, and verifying that the message immediately renders in the chat log, an active loading state is displayed, and the agent's response is appended once received.

**Acceptance Scenarios**:

1. **Given** the chat view is displayed, **When** a user types "Do we have enough stock for SKU-123?" and clicks Send (or presses Enter), **Then** the user's message is immediately added to the chat stream with role 'user', the input is cleared, and `isLoading` is set to true.
2. **Given** a prompt has been submitted, **When** the backend returns an AI response, **Then** the message is appended with role 'agent' and `isLoading` is reverted to false.
3. **Given** multiple conversational exchanges, **When** messages accumulate, **Then** the chat stream scrolls smoothly and preserves message ordering.

---

### User Story 2 - Decoupled Frontend API Data Access Service (Priority: P2)

As a frontend software engineer, I want the HTTP communication logic to be encapsulated inside an `AgentApiService` in the shared data-access library, so that API communication, endpoint routes, and serialization models are decoupled from UI components.

**Why this priority**: Adheres to Nx monorepo modularity (Constitution Principle IV), ensuring reusable data-access services that can be consumed across future micro-frontends and host apps.

**Independent Test**: Can be tested independently by calling `AgentApiService.sendMessage('test')` and verifying that an HTTP POST request is sent to `/api/agent/chat` with `{ prompt: 'test' }`.

**Acceptance Scenarios**:

1. **Given** `AgentApiService`, **When** `sendMessage("Check inventory")` is invoked, **Then** an HTTP POST request is sent to `/api/agent/chat` with body `{ prompt: "Check inventory" }`.
2. **Given** an active tenant selected in the ERP portal, **When** the request is transmitted, **Then** the shared `tenantInterceptor` forwards the `X-Tenant-ID` header automatically.

---

### User Story 3 - Robust Component State Management & Error Handling (Priority: P3)

As a user interacting with the AI agent, I want the chat component to handle submission edge cases and network disconnects gracefully, so that the UI does not freeze or submit empty requests.

**Why this priority**: Guarantees visual polish, prevents accidental duplicate submissions, and informs users when network issues occur.

**Independent Test**: Can be tested by attempting to submit whitespace, and simulating network failure to ensure loading flags reset and error notices appear.

**Acceptance Scenarios**:

1. **Given** an empty or whitespace-only input in the text box, **When** the user attempts to submit, **Then** no message is added to the UI and no network request is made.
2. **Given** an active request in progress (`isLoading` is true), **When** viewing the UI, **Then** the submit button is disabled and a typing/loading indicator is visible.
3. **Given** an HTTP failure from the backend, **When** the error occurs, **Then** `isLoading` is reset to false and an error message is displayed in the conversation history.

---

### Edge Cases

- **Double-Clicking Submit**: The submit button must be disabled while `isLoading` is true to prevent duplicate dispatches.
- **Empty or Whitespace Input**: The input must be trimmed and validated before triggering submission.
- **Keyboard Submission**: Pressing the `Enter` key (without `Shift`) should trigger message submission.
- **Network Failure / Server Offline**: A clear error alert message should be appended to the chat stream when the API fails.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST create an `AgentApiService` in `frontend/libs/shared/data-access/src/lib/services/agent-api.service.ts`.
- **FR-002**: `AgentApiService` MUST inject `HttpClient` and implement `sendMessage(prompt: string)` making an HTTP POST request to `/api/agent/chat` with `{ prompt }`.
- **FR-003**: The system MUST export `AgentApiService` from `frontend/libs/shared/data-access/src/index.ts`.
- **FR-004**: The system MUST create a standalone Angular component `AgentChatComponent` in `frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.ts`.
- **FR-005**: `AgentChatComponent` MUST manage a state array of chat messages storing `role: 'user' | 'agent'`, `content: string`, and a boolean `isLoading` flag.
- **FR-006**: `AgentChatComponent` MUST implement a responsive template and SCSS styles with a scrollable message history container, an input text field, and a submit button.
- **FR-007**: When submitting a prompt, `AgentChatComponent` MUST immediately append the user message, set `isLoading` to true, invoke `AgentApiService.sendMessage()`, append the AI response, and set `isLoading` to false.
- **FR-008**: The component MUST disable the submit button when `isLoading` is true or the input is empty.

---

### Key Entities & UI Models

- **ChatMessage**: Represents a single bubble in the conversation stream:
  ```typescript
  export interface ChatMessage {
    id: string;
    role: 'user' | 'agent';
    content: string;
    timestamp: Date;
  }
  ```
- **AgentChatResponse**: Represents the API response:
  ```typescript
  export interface AgentChatResponse {
    response: string;
  }
  ```

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: User prompts appear in the chat view immediately (< 50ms) upon submission.
- **SC-002**: 100% of outgoing requests from `AgentApiService` target `/api/agent/chat` with `{ prompt }`.
- **SC-003**: 100% of network failures reset `isLoading` to false and display an inline error indicator.
- **SC-004**: The frontend Nx workspace (`smart-erp-web` and all shared libraries) builds cleanly with 0 TypeScript and SCSS errors.

---

## Assumptions

- The backend API is hosted at the same origin or reverse-proxied under `/api`.
- The existing functional `tenantInterceptor` in `@smart-erp/data-access` attaches the required `X-Tenant-ID` header.
- Angular 19 standalone component conventions with signals or reactive properties are used.
