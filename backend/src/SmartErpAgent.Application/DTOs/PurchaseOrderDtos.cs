namespace SmartErpAgent.Application.DTOs;

public record LowStockAlertDto(
    string SKU,
    string Name,
    int StockQuantity,
    int ReorderThreshold,
    int SuggestedReorderQuantity,
    decimal UnitPrice,
    decimal EstimatedRestockCost
);

public record PurchaseOrderLineItemDto(
    Guid Id,
    Guid InventoryItemId,
    string SKU,
    string ItemName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);

public record DraftPurchaseOrderResultDto(
    bool Success,
    Guid? PurchaseOrderId,
    string OrderNumber,
    string SupplierName,
    DateTime OrderDate,
    decimal TotalAmount,
    string Currency,
    string Status,
    int LineItemCount,
    List<PurchaseOrderLineItemDto> LineItems,
    string Message
);
