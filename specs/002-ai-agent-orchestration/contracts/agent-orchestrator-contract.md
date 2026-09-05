# Interface & API Contracts: AI Agent Orchestration

**Feature**: `002-ai-agent-orchestration`  
**Date**: 2026-09-05  
**Spec**: [specs/002-ai-agent-orchestration/spec.md](../spec.md)

---

## 1. Application Layer Interface (`IAgentOrchestrator`)

Defined in `SmartErpAgent.Application.Common.Interfaces`:

```csharp
namespace SmartErpAgent.Application.Common.Interfaces;

public interface IAgentOrchestrator
{
    /// <summary>
    /// Executes a natural language prompt using Semantic Kernel and registered domain plugins.
    /// </summary>
    /// <param name="prompt">User natural language inquiry or command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Synthesized natural language response grounded in enterprise data</returns>
    Task<string> ExecutePromptAsync(string prompt, CancellationToken cancellationToken = default);
}
```

---

## 2. Native Plugin Contract (`InventoryAgentPlugin`)

Defined in `SmartErpAgent.AgentEngine.Plugins`:

```csharp
namespace SmartErpAgent.AgentEngine.Plugins;

public class InventoryAgentPlugin
{
    private readonly IApplicationDbContext _dbContext;

    public InventoryAgentPlugin(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [KernelFunction, Description("Checks current warehouse stock quantity and availability for a specific product SKU.")]
    public async Task<string> CheckStockLevelAsync(
        [Description("The unique stock-keeping unit (SKU) identifier of the product, e.g., 'WIDGET-01'")] string sku,
        CancellationToken cancellationToken = default);
}
```

---

## 3. Dependency Injection Contract (`AgentEngineServiceCollectionExtensions`)

Defined in `SmartErpAgent.AgentEngine`:

```csharp
namespace SmartErpAgent.AgentEngine;

public static class AgentEngineServiceCollectionExtensions
{
    /// <summary>
    /// Registers Semantic Kernel, AI orchestrator, and domain plugins into the service collection.
    /// </summary>
    public static IServiceCollection AddAgentEngine(
        this IServiceCollection services,
        IConfiguration? configuration = null);
}
```

---

## 4. HTTP Presentation Endpoint Contract

### `POST /api/agent/chat`

Executes natural language AI agent prompt.

#### Request Headers
| Header | Required | Format | Description |
| :--- | :---: | :--- | :--- |
| `Content-Type` | Yes | `application/json` | Request payload type |
| `X-Tenant-ID` | Yes | UUID (`Guid`) | Identifies the active tenant organization |

#### Request Body
```json
{
  "prompt": "Do we have enough stock for WIDGET-01?"
}
```

#### Response (200 OK)
```json
{
  "response": "Product 'Industrial Widget' (SKU: WIDGET-01) currently has 45 units available in stock. The stock level is healthy (reorder threshold: 10 units).",
  "status": "Success",
  "timestampUtc": "2026-09-05T02:20:00Z"
}
```

#### Response (400 Bad Request - Missing Prompt)
```json
{
  "error": "Prompt cannot be empty"
}
```
