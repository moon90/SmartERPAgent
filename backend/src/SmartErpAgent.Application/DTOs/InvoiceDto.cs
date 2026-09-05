using SmartErpAgent.Core.Enums;

namespace SmartErpAgent.Application.DTOs;

public record InvoiceDto(
    Guid Id,
    Guid TenantId,
    string InvoiceNumber,
    string CustomerName,
    string CustomerEmail,
    DateTime IssueDate,
    DateTime DueDate,
    decimal SubTotal,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    InvoiceStatus Status,
    string? Notes,
    List<InvoiceLineItemDto> LineItems
);

public record InvoiceLineItemDto(
    Guid Id,
    Guid? InventoryItemId,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);

public record CreateInvoiceRequest(
    string CustomerName,
    string CustomerEmail,
    DateTime DueDate,
    string Currency,
    string? Notes,
    List<CreateInvoiceLineItemRequest> LineItems
);

public record CreateInvoiceLineItemRequest(
    Guid? InventoryItemId,
    string Description,
    int Quantity,
    decimal UnitPrice
);
