namespace SmartErpAgent.Application.DTOs;

/// <summary>
/// Transport DTO representing parsed and extracted invoice data.
/// </summary>
public class ExtractedInvoiceData
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(14);
    public string Currency { get; set; } = "USD";
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public double ConfidenceScore { get; set; } = 1.0;
    public List<ExtractedInvoiceLineItem> LineItems { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Represents an individual item or service line extracted from the invoice.
/// </summary>
public class ExtractedInvoiceLineItem
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? MatchedSKU { get; set; }
    public Guid? MatchedInventoryItemId { get; set; }
    public decimal? CatalogUnitPrice { get; set; }
    public bool HasPriceDiscrepancy { get; set; }
}

/// <summary>
/// Result returned when an extracted invoice is staged into the ERP database.
/// </summary>
public class StagedInvoiceResultDto
{
    public bool Success { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public int LineItemCount { get; set; }
    public string Status { get; set; } = "Draft";
    public string Message { get; set; } = string.Empty;
}
