namespace SmartErpAgent.Application.DTOs;

/// <summary>
/// Represents a high-value SKU contribution to warehouse asset holding.
/// </summary>
public record TopValuableSkuDto(
    string SKU,
    string Name,
    int StockQuantity,
    decimal UnitPrice,
    decimal TotalItemValue
);

/// <summary>
/// Aggregated inventory valuation snapshot for the active tenant organization.
/// </summary>
public record InventoryValuationReportDto(
    decimal TotalValuation,
    int TotalActiveSkuCount,
    int TotalUnitsInStock,
    IReadOnlyList<TopValuableSkuDto> TopValuableSkus
);

/// <summary>
/// Aggregated revenue and invoicing metrics over a parameterized calendar day window.
/// </summary>
public record FinancialPeriodSummaryDto(
    int PeriodDays,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    decimal TotalRevenue,
    int InvoiceCount,
    string Currency
);
