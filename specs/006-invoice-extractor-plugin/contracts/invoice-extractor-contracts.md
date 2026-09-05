# Contracts: Invoice Extractor & Document Intelligence Plugin

**Feature**: `006-invoice-extractor-plugin`  
**Date**: 2026-09-05  
**Status**: Completed

---

## 1. `InvoiceExtractorPlugin` Interface Contract

```csharp
namespace SmartErpAgent.AgentEngine.Plugins;

public class InvoiceExtractorPlugin
{
    /// <summary>
    /// Extracts structured invoice data from unstructured document or email text.
    /// </summary>
    /// <param name="documentText">The raw text or OCR string of the invoice/email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON serialized string of ExtractedInvoiceData.</returns>
    [KernelFunction, Description("Extracts customer details, line items, quantities, and prices from unstructured invoice text or email body.")]
    public async Task<string> ExtractInvoiceFromTextAsync(
        [Description("Raw text, email, or OCR string representing the invoice")] string documentText,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Matches extracted line items against the active tenant's inventory catalog (SKUs).
    /// </summary>
    /// <param name="extractedJson">JSON serialized ExtractedInvoiceData.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON serialized string of correlated ExtractedInvoiceData.</returns>
    [KernelFunction, Description("Correlates extracted invoice line items with the tenant inventory catalog by matching SKU codes and product names.")]
    public async Task<string> CorrelateWithInventoryCatalogAsync(
        [Description("JSON representation of extracted invoice data")] string extractedJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages and creates a new Draft invoice in the ERP ledger from validated extraction data.
    /// </summary>
    /// <param name="extractedJson">JSON serialized ExtractedInvoiceData.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON confirmation containing InvoiceId, InvoiceNumber, and TotalAmount.</returns>
    [KernelFunction, Description("Saves the validated invoice data into the ERP database as a new Draft invoice with associated line items.")]
    public async Task<string> StageExtractedInvoiceAsync(
        [Description("JSON representation of validated extracted invoice data")] string extractedJson,
        CancellationToken cancellationToken = default);
}
```

---

## 2. Sample JSON Output Contract

### Sample Output from `ExtractInvoiceFromTextAsync` & `CorrelateWithInventoryCatalogAsync`
```json
{
  "customerName": "Acme Industrial Supplies",
  "customerEmail": "billing@acmeind.com",
  "vendorName": "Global Bearing Corp",
  "invoiceNumber": "INV-2026-091",
  "issueDate": "2026-09-01T00:00:00Z",
  "dueDate": "2026-09-15T00:00:00Z",
  "currency": "USD",
  "subTotal": 450.00,
  "taxAmount": 45.00,
  "totalAmount": 495.00,
  "confidenceScore": 0.95,
  "lineItems": [
    {
      "description": "Precision Ball Bearing (SKU-123)",
      "quantity": 10,
      "unitPrice": 45.00,
      "totalPrice": 450.00,
      "matchedSKU": "SKU-123",
      "matchedInventoryItemId": "b3c96561-3a05-4f38-89c7-5e9259de4372",
      "catalogUnitPrice": 45.00,
      "hasPriceDiscrepancy": false
    }
  ],
  "warnings": []
}
```

### Sample Output from `StageExtractedInvoiceAsync`
```json
{
  "success": true,
  "invoiceId": "d7e29b10-6c18-4b72-a059-e93574cf3012",
  "invoiceNumber": "INV-202609-0002",
  "customerName": "Acme Industrial Supplies",
  "totalAmount": 495.00,
  "currency": "USD",
  "lineItemCount": 1,
  "status": "Draft",
  "message": "Draft invoice INV-202609-0002 staged successfully in ERP ledger."
}
```
