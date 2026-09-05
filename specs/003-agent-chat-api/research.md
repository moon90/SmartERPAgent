# Technical Research: Agent Chat API Presentation Endpoint

**Feature**: `003-agent-chat-api`  
**Date**: 2026-09-05  
**Spec**: [specs/003-agent-chat-api/spec.md](./spec.md)

---

## 1. DTO Specification (`AgentChatRequestDto`)

### Decision
Define `AgentChatRequestDto` in `SmartErpAgent.Application.DTOs`:

```csharp
namespace SmartErpAgent.Application.DTOs;

public class AgentChatRequestDto
{
    public string Prompt { get; set; } = string.Empty;
}
```

### Rationale
- Placing `AgentChatRequestDto` in the `SmartErpAgent.Application` layer ensures that contract definitions remain independent of the presentation framework (`Microsoft.AspNetCore.Mvc`).
- The property `Prompt` cleanly binds to JSON bodies produced by frontend clients and Angular services:
  ```json
  {
    "prompt": "Do we have enough stock for SKU-123?"
  }
  ```

### Alternatives Considered
- *Using C# record*: While records are idiomatic for immutable messages, standard class DTO with get/set fulfills the explicit requirement and works seamlessly with all model binding configurations.

---

## 2. Controller Design & Action Signature (`AgentController`)

### Decision
Implement `AgentController` in `SmartErpAgent.Api.Controllers`:

```csharp
namespace SmartErpAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IAgentOrchestrator _agentOrchestrator;
    private readonly ILogger<AgentController> _logger;

    public AgentController(
        IAgentOrchestrator agentOrchestrator,
        ILogger<AgentController> logger)
    {
        _agentOrchestrator = agentOrchestrator;
        _logger = logger;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat(
        [FromBody] AgentChatRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(new { Message = "Prompt cannot be empty." });
        }

        var response = await _agentOrchestrator.ExecutePromptAsync(request.Prompt, cancellationToken);
        return Ok(new { Response = response });
    }
}
```

### Rationale
- Strictly conforms to Constitution Principle I: The controller acts purely as a thin transport adapter. No business, domain, or AI orchestration logic exists within the controller.
- Returns an `IActionResult` (`Ok` with response payload on success, `BadRequest` with message on empty input).

### Alternatives Considered
- *Calling Semantic Kernel directly in Controller*: Rejected. Violates Clean Architecture and Constitution Principle I & V.

---

## 3. Host Pipeline Integration in `Program.cs`

### Decision
Verify that `SmartErpAgent.Api/Program.cs` registers:
```csharp
builder.Services.AddControllers();
builder.Services.AddAgentEngine();
...
app.MapControllers();
```

### Rationale
- Ensures all controller endpoints (`/api/agent/chat`) are routed properly and all agent orchestrator dependencies are resolved.
