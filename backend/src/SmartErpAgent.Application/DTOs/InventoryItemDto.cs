namespace SmartErpAgent.Application.DTOs;

public record InventoryItemDto(
    Guid Id,
    Guid TenantId,
    string SKU,
    string Name,
    string Description,
    decimal UnitPrice,
    int StockQuantity,
    int ReorderThreshold,
    bool IsActive,
    bool IsLowStock
);

public record CreateInventoryItemRequest(
    string SKU,
    string Name,
    string Description,
    decimal UnitPrice,
    int StockQuantity,
    int ReorderThreshold
);

public record UpdateStockRequest(
    int QuantityDelta,
    string Reason
);
