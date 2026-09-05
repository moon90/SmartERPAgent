# Feature Specification: Agent Chat API Presentation Endpoint

**Feature Branch**: `003-agent-chat-api`

**Created**: 2026-09-05

**Status**: Draft

**Input**: User description: "Implement the API presentation layer in the SmartErpAgent.Api project to expose the Semantic Kernel orchestrator to the frontend. 1. Create a Data Transfer Object named AgentChatRequestDto in the Application layer containing a single property: public string Prompt { get; set; }. 2. Create an AgentController in the Controllers folder of the Api project. Decorate it with [ApiController] and [Route(\"api/[controller]\")]. 3. Inject the IAgentOrchestrator into the AgentController constructor. 4. Add a [HttpPost(\"chat\")] endpoint that accepts the AgentChatRequestDto. This endpoint must call the orchestrator with the user's prompt and return the AI's response in an IActionResult. 5. Ensure the Program.cs file maps the controllers and registers the AgentEngineServiceCollectionExtensions.AddAgentEngine() method."

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Conversational AI Interaction via HTTP API (Priority: P1) 🎯 MVP

As an ERP user or frontend web client, I want to submit natural language inquiries to a dedicated `POST /api/agent/chat` endpoint and receive grounded AI responses, so that I can interactively interrogate warehouse stock, invoices, and business status directly from the web portal.

**Why this priority**: Exposing the AI orchestrator via a standardized HTTP endpoint is the critical bridge connecting the frontend Angular application to the backend Semantic Kernel engine.

**Independent Test**: Can be tested by sending an HTTP POST request with `{ "prompt": "Check stock for SKU-123" }` to `/api/agent/chat` and verifying that the response contains an HTTP 200 OK with the AI's answer.

**Acceptance Scenarios**:

1. **Given** a valid prompt from an authenticated user under Tenant A, **When** submitted to `POST /api/agent/chat`, **Then** the endpoint dispatches the prompt to `IAgentOrchestrator` and returns HTTP 200 OK containing the synthesized response.
2. **Given** an empty, null, or whitespace-only prompt, **When** submitted to `POST /api/agent/chat`, **Then** the endpoint returns HTTP 400 Bad Request with an informative error message.
3. **Given** a conversational request, **When** processed by the controller, **Then** the controller acts strictly as a thin transport adapter, delegating all reasoning to the orchestrator.

---

### User Story 2 - Strongly Typed Data Contracts & Validation (Priority: P2)

As a software engineer, I want a dedicated `AgentChatRequestDto` data contract in the Application layer, so that input serialization, model binding, and contract validation are decoupled from presentation controllers.

**Why this priority**: Clean Architecture mandates that presentation and transport layers share typed contracts defined in the Application layer rather than arbitrary parameters.

**Independent Test**: Can be tested by verifying that `AgentChatRequestDto` is located in `SmartErpAgent.Application` and serializes/deserializes correctly.

**Acceptance Scenarios**:

1. **Given** a client payload `{ "prompt": "What is our low stock status?" }`, **When** received by ASP.NET Core model binding, **Then** it cleanly binds to `AgentChatRequestDto.Prompt`.

---

### User Story 3 - Production Dependency Wiring and Route Mapping (Priority: P3)

As a DevOps engineer and API consumer, I want the agent controller and `AddAgentEngine()` services registered in `Program.cs`, so that the endpoint is discoverable in Swagger UI and ready to serve traffic.

**Why this priority**: Guarantees that the service host starts cleanly without unresolved dependencies or missing route registrations.

**Independent Test**: Can be tested by spinning up the Web API test server and asserting that `POST /api/agent/chat` is reachable and documented in OpenAPI metadata.

**Acceptance Scenarios**:

1. **Given** the application startup sequence, **When** `Program.cs` executes, **Then** `AddAgentEngine()` registers all necessary services and `MapControllers()` exposes `api/agent/chat`.

---

### Edge Cases

- **Missing or Empty Prompt**: The endpoint must validate that `Prompt` is neither null nor empty, returning an HTTP 400 Bad Request rather than invoking the orchestrator.
- **Tenant Context Resolution**: If `X-Tenant-ID` header is missing, the request should be handled according to platform policy (default system context or validation error).
- **Orchestration Timeout / Error**: If the underlying model or planner fails, the endpoint returns an appropriate HTTP 500 status with structured error details instead of crashing the process.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST define `AgentChatRequestDto` in the Application layer containing a public `string Prompt` property.
- **FR-002**: The system MUST provide an `AgentController` class in `SmartErpAgent.Api.Controllers` decorated with `[ApiController]` and `[Route("api/[controller]")]`.
- **FR-003**: The `AgentController` MUST inject `IAgentOrchestrator` via constructor dependency injection.
- **FR-004**: The `AgentController` MUST provide a `[HttpPost("chat")]` endpoint accepting `AgentChatRequestDto` and returning an `IActionResult`.
- **FR-005**: The `[HttpPost("chat")]` endpoint MUST validate input and invoke `IAgentOrchestrator.ExecutePromptAsync(request.Prompt)` (or `ProcessPromptAsync`) and return the result.
- **FR-006**: The `Program.cs` file MUST map controllers and register `AgentEngineServiceCollectionExtensions.AddAgentEngine()`.

---

### Key Entities & Contracts

- **AgentChatRequestDto**: Data Transfer Object representing user chat input. Property: `string Prompt`.
- **AgentChatResponseDto / ObjectResult**: Response payload carrying the AI message and HTTP status.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of valid requests sent to `POST /api/agent/chat` return an HTTP 200 OK with the AI response.
- **SC-002**: 100% of requests with empty or missing prompts return an HTTP 400 Bad Request.
- **SC-003**: Zero business logic resides inside `AgentController`, maintaining 100% compliance with Constitution Principle I.
- **SC-004**: Automated controller tests pass with a 100% success rate.

---

## Assumptions

- The frontend sends requests with the `Content-Type: application/json` header.
- Multi-tenancy context is resolved via the existing `MultiTenantMiddleware` from the `X-Tenant-ID` header.
