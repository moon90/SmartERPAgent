# Quickstart & Validation Guide: AI Agent Orchestration

**Feature**: `002-ai-agent-orchestration`  
**Date**: 2026-09-05  
**Spec**: [specs/002-ai-agent-orchestration/spec.md](./spec.md)

---

## 1. Prerequisites

- .NET 8.0 SDK installed (`dotnet --version`)
- All projects restored: `dotnet restore backend/SmartErpAgent.sln`

---

## 2. Validation Scenario 1: Unit Testing `InventoryAgentPlugin.CheckStockLevelAsync`

Verifies that the native plugin queries the underlying DbContext by SKU, respects tenant isolation, and formats stock details.

### Test Execution
```bash
dotnet test backend/tests/SmartErpAgent.UnitTests/ --filter FullyQualifiedName~AgentEngine
```

### Expected Behavior
1. Seeding item with SKU `WIDGET-01`, Quantity `50`, ReorderThreshold `10`.
2. Calling `CheckStockLevelAsync("WIDGET-01")` returns JSON with `"stockQuantity": 50`, `"status": "In Stock"`.
3. Calling `CheckStockLevelAsync("NON-EXISTENT")` returns JSON indicating `"status": "Product Not Found"`.
4. Tenant isolation is verified: querying from Tenant B for Tenant A's SKU returns Not Found.

---

## 3. Validation Scenario 2: End-to-End Orchestrator Execution

Verifies that `SemanticKernelAgentOrchestrator.ExecutePromptAsync` parses the prompt, invokes the plugin tool, and returns an answer.

### Sample Test Case
```csharp
var response = await orchestrator.ExecutePromptAsync("Do we have enough stock for WIDGET-01?");
Assert.Contains("50 units", response);
Assert.Contains("WIDGET-01", response);
```

---

## 4. Validation Scenario 3: Dependency Injection Verification

Verifies that `AddAgentEngine()` registers all necessary components in the DI container.

### Test Case
```csharp
var services = new ServiceCollection();
services.AddDbContext<ApplicationDbContext>(opts => opts.UseInMemoryDatabase("TestDb"));
services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
services.AddAgentEngine();

var provider = services.BuildServiceProvider();
var orchestrator = provider.GetService<IAgentOrchestrator>();
var plugin = provider.GetService<InventoryAgentPlugin>();

Assert.NotNull(orchestrator);
Assert.NotNull(plugin);
```

---

## 5. Validation Scenario 4: HTTP API Validation via Curl

Start backend API:
```bash
dotnet run --project backend/src/SmartErpAgent.Api
```

Submit stock inquiry:
```bash
curl -X POST http://localhost:5000/api/agent/chat \
  -H "Content-Type: application/json" \
  -H "X-Tenant-ID: 11111111-1111-1111-1111-111111111111" \
  -d '{"prompt": "Do we have enough stock for WIDGET-01?"}'
```

Expected JSON Response:
```json
{
  "response": "Product 'WIDGET-01' has 50 units in stock. Status: In Stock.",
  "status": "Success",
  "timestampUtc": "2026-09-05T02:22:00Z"
}
```
