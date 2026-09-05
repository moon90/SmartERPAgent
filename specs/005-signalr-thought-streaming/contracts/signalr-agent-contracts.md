# Interface & SignalR Contract: Real-Time Agent Communication

**Feature**: `005-signalr-thought-streaming`  
**Date**: 2026-09-05  
**Spec**: [specs/005-signalr-thought-streaming/spec.md](../spec.md)

---

## 1. Backend SignalR Hub Contract (`AgentHub`)

**Location**: `backend/src/SmartErpAgent.Api/Hubs/AgentHub.cs`  
**Route**: `/hubs/agent`

### Hub Definition
```csharp
namespace SmartErpAgent.Api.Hubs;

public class AgentHub : Hub
{
    private readonly IAgentOrchestrator _orchestrator;

    public AgentHub(IAgentOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// Invoked by the client to send a prompt over the real-time hub.
    /// </summary>
    public async Task SendPrompt(string prompt)
    {
        var response = await _orchestrator.ExecutePromptAsync(prompt);
        await Clients.Caller.SendAsync("ReceiveFinalResponse", response);
    }
}
```

### Server-to-Client Invocations
| Event Name | Parameter | Description |
| :--- | :--- | :--- |
| `ReceiveThoughtProcess` | `string message` | Streamed intermediate planner steps and plugin execution notifications |
| `ReceiveFinalResponse` | `string response` | Delivered upon full completion of orchestration synthesis |

---

## 2. Orchestrator Contract (`IAgentOrchestrator` / `SemanticKernelAgentOrchestrator`)

**Location**: `backend/src/SmartErpAgent.AgentEngine/Services/SemanticKernelAgentOrchestrator.cs`

```csharp
public class SemanticKernelAgentOrchestrator : IAgentOrchestrator
{
    public SemanticKernelAgentOrchestrator(
        IConfiguration configuration,
        ILogger<SemanticKernelAgentOrchestrator> logger,
        InvoiceAgentPlugin invoicePlugin,
        InventoryAgentPlugin inventoryPlugin,
        IHubContext<AgentHub> hubContext)
    {
        // ...
    }

    // Streams:
    // await _hubContext.Clients.All.SendAsync("ReceiveThoughtProcess", message);
}
```

---

## 3. Frontend Data Access Service Contract (`AgentApiService`)

**Location**: `frontend/libs/shared/data-access/src/lib/services/agent-api.service.ts`

```typescript
@Injectable({
  providedIn: 'root',
})
export class AgentApiService {
  readonly thoughtProcess$: Observable<string>;
  readonly finalResponse$: Observable<string>;

  /**
   * Initializes or returns the existing SignalR HubConnection.
   */
  startConnection(): Promise<void>;

  /**
   * Sends a prompt to the agent orchestrator over the real-time SignalR hub.
   *
   * @param prompt User's natural language inquiry
   */
  sendPrompt(prompt: string): Promise<void>;

  /**
   * Compatibility method returning an Observable that completes with the final response.
   */
  sendMessage(prompt: string): Observable<AgentChatResponse>;
}
```

---

## 4. Feature Component UI Contract (`AgentChatComponent`)

**Location**: `frontend/libs/agents/feature-chat/src/lib/agent-chat/agent-chat.component.ts`

- Subscribes to `agentApi.thoughtProcess$` to update `currentThought: string | null`.
- Subscribes to `agentApi.finalResponse$` to append the completed `ChatMessage` with `role: 'agent'`.
- Renders streaming thoughts dynamically in `.thought-stream` below the active prompt with italicized styling and animated indicator dots.
