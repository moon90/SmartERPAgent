# Quickstart & Verification Guide: Agent Chat API Endpoint

**Feature**: `003-agent-chat-api`  
**Date**: 2026-09-05  
**Spec**: [specs/003-agent-chat-api/spec.md](./spec.md)

---

## 1. Prerequisites

- .NET 8.0 SDK installed
- Solution restored: `dotnet restore backend/SmartErpAgent.sln`

---

## 2. Validation Scenario 1: Automated Controller Unit Testing

Runs unit tests verifying that `AgentController.Chat` accepts `AgentChatRequestDto`, rejects empty prompts with HTTP 400, and returns the orchestrator's response in HTTP 200 OK.

### Command
```bash
dotnet test backend/tests/SmartErpAgent.UnitTests/ --filter FullyQualifiedName~AgentControllerTests
```

### Expected Output
- `Chat_ShouldReturnOkWithResponse_WhenPromptIsValid`: **PASSED**
- `Chat_ShouldReturnBadRequest_WhenPromptIsEmptyOrWhitespace`: **PASSED**
- `Chat_ShouldReturnBadRequest_WhenRequestIsNull`: **PASSED**

---

## 3. Validation Scenario 2: Live HTTP Request via Curl

Start the backend API:
```bash
dotnet run --project backend/src/SmartErpAgent.Api
```

Send a chat inquiry:
```bash
curl -i -X POST http://localhost:5000/api/agent/chat \
  -H "Content-Type: application/json" \
  -H "X-Tenant-ID: 11111111-1111-1111-1111-111111111111" \
  -d '{"prompt": "Do we have enough stock for SKU-123?"}'
```

### Expected Response
```http
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8

{
  "response": "..."
}
```
