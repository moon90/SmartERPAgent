# Data Model & Schemas: AI Agent Orchestration

**Feature**: `002-ai-agent-orchestration`  
**Date**: 2026-09-05  
**Spec**: [specs/002-ai-agent-orchestration/spec.md](./spec.md)

---

## 1. Domain Entities Involved

### `InventoryItem` (in `SmartErpAgent.Core.Entities`)
Represents warehouse inventory under tenant ownership.

| Field | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key, Non-null | Unique identifier |
| `TenantId` | `Guid` | Indexed, Non-null | Multi-tenant discriminator (`ITenantEntity`) |
| `SKU` | `string` | Max 64 chars, Non-null | Unique per tenant Stock Keeping Unit |
| `Name` | `string` | Max 256 chars, Non-null | Product title |
| `Description` | `string?` | Max 1024 chars | Product description |
| `UnitPrice` | `decimal` | `decimal(18,2)` | Sales unit price |
| `StockQuantity` | `int` | Non-negative | Current on-hand quantity |
| `ReorderThreshold` | `int` | Non-negative | Low-stock threshold trigger |
| `IsActive` | `bool` | Default `true` | Product active status |
| `IsDeleted` | `bool` | Default `false` | Soft-delete flag |
| `CreatedAtUtc` | `DateTime` | UTC | Entity creation timestamp |
| `UpdatedAtUtc` | `DateTime?` | UTC | Entity last update timestamp |

---

## 2. Plugin Transfer Schemas

### `StockLevelResult` (JSON payload returned by `CheckStockLevelAsync`)

```json
{
  "sku": "WIDGET-01",
  "name": "Heavy Duty Industrial Widget",
  "stockQuantity": 45,
  "reorderThreshold": 10,
  "isLowStock": false,
  "status": "In Stock",
  "unitPrice": 25.50
}
```

When SKU is not found:
```json
{
  "sku": "NON-EXISTENT",
  "name": null,
  "stockQuantity": 0,
  "reorderThreshold": 0,
  "isLowStock": false,
  "status": "Product Not Found",
  "unitPrice": 0
}
```

---

## 3. Application Contracts & DTOs

### `AgentRequestDto`
Input payload submitted to the AI agent endpoint:
```csharp
public record AgentRequestDto(
    string Prompt,
    string? SessionId = null
);
```

### `AgentResponseDto`
Response payload returned by the AI agent endpoint:
```csharp
public record AgentResponseDto(
    string Response,
    string Status = "Success",
    DateTime TimestampUtc = default
);
```
