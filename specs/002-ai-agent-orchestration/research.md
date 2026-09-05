# Technical Research: AI Agent Orchestration with Semantic Kernel

**Feature**: `002-ai-agent-orchestration`  
**Date**: 2026-09-05  
**Spec**: [specs/002-ai-agent-orchestration/spec.md](./spec.md)

---

## 1. Inventory Plugin Architecture (`CheckStockLevelAsync`)

### Decision
Implement `InventoryAgentPlugin` in `SmartErpAgent.AgentEngine.Plugins` with a native `[KernelFunction]` named `CheckStockLevelAsync`:

```csharp
[KernelFunction, Description("Checks current warehouse stock quantity and availability for a specific product SKU.")]
public async Task<string> CheckStockLevelAsync(
    [Description("The unique stock-keeping unit (SKU) identifier of the product, e.g., 'WIDGET-01'")] string sku,
    CancellationToken cancellationToken = default)
```

The method queries `_dbContext.InventoryItems` using `EF.Functions.Like` or case-insensitive matching (`item.SKU.ToLower() == sku.ToLower()`), returning a serialized JSON response:
```json
{
  "sku": "WIDGET-01",
  "name": "Industrial Widget",
  "stockQuantity": 45,
  "reorderThreshold": 10,
  "isLowStock": false,
  "status": "In Stock"
}
```

### Rationale
- Detailed `[Description]` attributes on both the function and parameter are essential for Microsoft Semantic Kernel and OpenAI models to accurately assess function relevance during intent planning.
- Returning structured JSON allows both LLM reasoning engines and direct frontend callers to consume the output deterministically.
- Injecting `IApplicationDbContext` guarantees that the active `ITenantContext` and global query filters (`TenantId == CurrentTenantId`) automatically protect tenant isolation at the database layer.

### Alternatives Considered
- *Direct Database Access via LLM Generated SQL*: Rejected. Constitution Principle V strictly forbids direct generation or execution of arbitrary SQL by LLM agents due to prompt injection and tenant breach risks.
- *Returning Raw Domain Entity*: Rejected. Returning domain entities directly can serialize navigation properties or sensitive internal IDs; a compact projection is safer and faster.

---

## 2. Planner & Orchestrator Design (`SemanticKernelAgentOrchestrator`)

### Decision
Implement `SemanticKernelAgentOrchestrator` adhering to `IAgentOrchestrator`:
- Configures `Kernel` by registering `InventoryAgentPlugin` and `InvoiceAgentPlugin`.
- When an OpenAI API key is configured (`OpenAI:ApiKey` or `AI:OpenAI:ApiKey`), it initializes `FunctionCallingStepwisePlanner` or `AutoInvokeKernelFunctions` with OpenAI chat completion.
- When no API key is supplied (e.g., in offline development, local unit tests, or CI environments), it implements a deterministic fallback that inspects prompt intent, invokes `CheckStockLevelAsync` on the registered plugin, and formats a human-readable response.

### Rationale
- Enables full production capabilities using Microsoft Semantic Kernel's native stepwise planning and function calling.
- Guarantees 100% reliable local automated testing (`dotnet test`) without requiring external internet access or billable API keys.

### Alternatives Considered
- *Strictly Requiring Cloud LLM*: Rejected because local builds, CI/CD runners, and disconnected environments would fail unit tests.
- *Custom Hand-Rolled Regex Parser Only*: Rejected because the system must support autonomous multi-step reasoning when configured with an LLM.

---

## 3. Dependency Injection Pattern (`AgentEngineServiceCollectionExtensions`)

### Decision
Create `AgentEngineServiceCollectionExtensions` under `SmartErpAgent.AgentEngine`:
```csharp
namespace SmartErpAgent.AgentEngine;

public static class AgentEngineServiceCollectionExtensions
{
    public static IServiceCollection AddAgentEngine(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddScoped<InventoryAgentPlugin>();
        services.AddScoped<InvoiceAgentPlugin>();
        services.AddScoped<IAgentOrchestrator, SemanticKernelAgentOrchestrator>();
        return services;
    }
}
```

### Rationale
- Follows Microsoft's standard .NET framework naming convention (`<Library>ServiceCollectionExtensions`).
- Encapsulates all agent engine dependencies in a single, reusable registration call in `SmartErpAgent.Api/Program.cs`.

### Alternatives Considered
- *Individual Service Registrations in Program.cs*: Rejected because it exposes internal plugin types to the API presentation layer, violating Clean Architecture encapsulation.
