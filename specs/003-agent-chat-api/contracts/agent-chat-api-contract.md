# Interface & API Contract: Agent Chat API

**Feature**: `003-agent-chat-api`  
**Date**: 2026-09-05  
**Spec**: [specs/003-agent-chat-api/spec.md](../spec.md)

---

## 1. HTTP Endpoint Specification

### `POST /api/agent/chat`

Handles natural language prompts from the frontend Angular application, invokes the Semantic Kernel orchestrator, and returns the AI response.

#### Request Headers
| Header | Required | Type | Description |
| :--- | :---: | :--- | :--- |
| `Content-Type` | Yes | `application/json` | Media type of request body |
| `X-Tenant-ID` | Yes | `Guid` (UUID) | Tenant organization identifier for data isolation |

#### Request Body Schema
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "AgentChatRequestDto",
  "type": "object",
  "required": ["prompt"],
  "properties": {
    "prompt": {
      "type": "string",
      "minLength": 1,
      "description": "Natural language inquiry or instruction from the user"
    }
  }
}
```

#### Response (200 OK)
```json
{
  "response": "Product 'Heavy Duty Motor' (SKU: SKU-123) currently has 88 units in stock. Status: In Stock."
}
```

#### Response (400 Bad Request)
Returned when `prompt` is null, empty, or whitespace.
```json
{
  "message": "Prompt cannot be empty."
}
```

---

## 2. Controller Signature (`AgentController`)

```csharp
namespace SmartErpAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    public AgentController(IAgentOrchestrator agentOrchestrator, ILogger<AgentController> logger);

    [HttpPost("chat")]
    public Task<IActionResult> Chat([FromBody] AgentChatRequestDto request, CancellationToken cancellationToken);
}
```
