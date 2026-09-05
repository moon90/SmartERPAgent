# Data Model & DTOs: Agent Chat API Presentation Endpoint

**Feature**: `003-agent-chat-api`  
**Date**: 2026-09-05  
**Spec**: [specs/003-agent-chat-api/spec.md](./spec.md)

---

## 1. Request DTO (`AgentChatRequestDto`)

Located in `SmartErpAgent.Application.DTOs`:

```csharp
namespace SmartErpAgent.Application.DTOs;

public class AgentChatRequestDto
{
    public string Prompt { get; set; } = string.Empty;
}
```

### JSON Representation
```json
{
  "prompt": "Do we have enough stock for SKU-123?"
}
```

---

## 2. Response Models

### Success Response Payload (200 OK)
```json
{
  "response": "Product 'Precision Ball Bearing' (SKU: SKU-123) currently has 120 units in stock. Status: In Stock (Reorder threshold: 20)."
}
```

### Validation Error Payload (400 Bad Request)
```json
{
  "message": "Prompt cannot be empty."
}
```
