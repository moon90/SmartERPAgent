using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using SmartErpAgent.Application.Common.Interfaces;
using SmartErpAgent.Application.DTOs;
using SmartErpAgent.Core.Entities;
using SmartErpAgent.Core.Enums;

namespace SmartErpAgent.AgentEngine.Plugins;

/// <summary>
/// Semantic Kernel Plugin providing document intelligence and invoice extraction capabilities,
/// including raw text/email parsing, catalog SKU correlation, and draft invoice staging.
/// </summary>
public class InvoiceExtractorPlugin
{
    private readonly IApplicationDbContext _dbContext;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public InvoiceExtractorPlugin(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Extracts structured invoice header fields and line items from unstructured text, email, or OCR string.
    /// </summary>
    [KernelFunction, Description("Extracts customer details, line items, quantities, and prices from unstructured invoice text or email body.")]
    public Task<string> ExtractInvoiceFromTextAsync(
        [Description("Raw text, email, or OCR string representing the invoice")] string documentText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentText))
        {
            var emptyResult = new ExtractedInvoiceData
            {
                CustomerName = "Unknown",
                Warnings = new List<string> { "Empty document text provided." }
            };
            return Task.FromResult(JsonSerializer.Serialize(emptyResult, JsonOptions));
        }

        var data = new ExtractedInvoiceData
        {
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(14),
            Currency = "USD"
        };

        // 1. Extract Customer Name
        var customerMatch = Regex.Match(documentText, @"(?:Customer|Client|Bill To|To):\s*([^\r\n,]+)", RegexOptions.IgnoreCase);
        if (customerMatch.Success)
        {
            data.CustomerName = customerMatch.Groups[1].Value.Trim();
        }
        else
        {
            data.CustomerName = "Valued Customer";
            data.Warnings.Add("Customer name not explicitly detected; defaulted to 'Valued Customer'.");
        }

        // 2. Extract Customer Email
        var emailMatch = Regex.Match(documentText, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
        if (emailMatch.Success)
        {
            data.CustomerEmail = emailMatch.Value.Trim();
        }
        else
        {
            data.CustomerEmail = "billing@customer.com";
            data.Warnings.Add("Customer email not found; defaulted to 'billing@customer.com'.");
        }

        // 3. Extract Vendor / Company
        var vendorMatch = Regex.Match(documentText, @"(?:Vendor|From|Company|Seller):\s*([^\r\n,]+)", RegexOptions.IgnoreCase);
        if (vendorMatch.Success)
        {
            data.VendorName = vendorMatch.Groups[1].Value.Trim();
        }

        // 4. Extract Invoice Number / Reference
        var invNumMatch = Regex.Match(documentText, @"(?:Invoice|Bill)\s*(?:#|No\.?|Number)?\s*[:#]\s*([A-Za-z0-9-]+)", RegexOptions.IgnoreCase);
        if (!invNumMatch.Success)
        {
            invNumMatch = Regex.Match(documentText, @"\b(INV-[A-Za-z0-9-]+)\b", RegexOptions.IgnoreCase);
        }

        if (invNumMatch.Success)
        {
            data.InvoiceNumber = invNumMatch.Groups[1].Value.Trim();
        }

        // 5. Extract Currency
        if (documentText.Contains("EUR", StringComparison.OrdinalIgnoreCase) || documentText.Contains('€'))
        {
            data.Currency = "EUR";
        }
        else if (documentText.Contains("GBP", StringComparison.OrdinalIgnoreCase) || documentText.Contains('£'))
        {
            data.Currency = "GBP";
        }

        // 6. Extract Line Items
        // Pattern A: e.g. "- SKU-123 Precision Ball Bearing, Qty: 5, Unit Price: $45.00"
        // Pattern B: e.g. "Precision Ball Bearing (SKU-123) 5 @ $45.00"
        // Pattern C: e.g. "- Item Description, 10 units at $20.00"
        var lines = documentText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var lineTrim = line.Trim();
            if (lineTrim.StartsWith("Items:", StringComparison.OrdinalIgnoreCase) ||
                lineTrim.StartsWith("Line Items", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Match description, quantity, price
            var itemMatch = Regex.Match(lineTrim,
                @"(?:-\s*)?(?<desc>.+?)(?:,\s*|\s+-\s+|\s+)(?:Qty|Quantity)?:?\s*(?<qty>\d+)(?:,\s*|\s+)(?:Unit Price|Price|@)?:?\s*\$?(?<price>\d+(?:\.\d{1,2})?)",
                RegexOptions.IgnoreCase);

            if (itemMatch.Success)
            {
                var desc = itemMatch.Groups["desc"].Value.Trim().TrimStart('-', '*', ' ');
                var qty = int.TryParse(itemMatch.Groups["qty"].Value, out var q) ? Math.Max(1, q) : 1;
                var price = decimal.TryParse(itemMatch.Groups["price"].Value, out var p) ? p : 0m;

                data.LineItems.Add(new ExtractedInvoiceLineItem
                {
                    Description = desc,
                    Quantity = qty,
                    UnitPrice = price,
                    TotalPrice = qty * price
                });
            }
            else
            {
                // Simple fallback: "- Description: $Price"
                var simpleMatch = Regex.Match(lineTrim, @"^-\s*(?<desc>[^:]+):\s*\$?(?<price>\d+(?:\.\d{1,2})?)$");
                if (simpleMatch.Success)
                {
                    var desc = simpleMatch.Groups["desc"].Value.Trim();
                    var price = decimal.TryParse(simpleMatch.Groups["price"].Value, out var p) ? p : 0m;
                    data.LineItems.Add(new ExtractedInvoiceLineItem
                    {
                        Description = desc,
                        Quantity = 1,
                        UnitPrice = price,
                        TotalPrice = price
                    });
                }
            }
        }

        // If no line items parsed, check for a single whole-invoice amount
        if (data.LineItems.Count == 0)
        {
            var totalMatch = Regex.Match(documentText, @"(?:Total|Amount|Due):\s*\$?(?<total>\d+(?:\.\d{1,2})?)", RegexOptions.IgnoreCase);
            if (totalMatch.Success && decimal.TryParse(totalMatch.Groups["total"].Value, out var singleTotal))
            {
                data.LineItems.Add(new ExtractedInvoiceLineItem
                {
                    Description = "General Goods / Services",
                    Quantity = 1,
                    UnitPrice = singleTotal,
                    TotalPrice = singleTotal
                });
                data.Warnings.Add("Single lump-sum line item created from detected total.");
            }
        }

        // 7. Calculate totals
        data.SubTotal = data.LineItems.Sum(li => li.TotalPrice);
        data.TaxAmount = Math.Round(data.SubTotal * 0.10m, 2); // 10% standard tax
        data.TotalAmount = data.SubTotal + data.TaxAmount;
        data.ConfidenceScore = data.LineItems.Count > 0 ? 0.95 : 0.60;

        return Task.FromResult(JsonSerializer.Serialize(data, JsonOptions));
    }

    /// <summary>
    /// Correlates extracted line items with the tenant inventory catalog by matching SKU codes and product names.
    /// </summary>
    [KernelFunction, Description("Correlates extracted invoice line items with the tenant inventory catalog by matching SKU codes and product names.")]
    public async Task<string> CorrelateWithInventoryCatalogAsync(
        [Description("JSON representation of extracted invoice data")] string extractedJson,
        CancellationToken cancellationToken = default)
    {
        ExtractedInvoiceData? data;
        try
        {
            data = JsonSerializer.Deserialize<ExtractedInvoiceData>(extractedJson, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Invalid JSON payload: {ex.Message}" });
        }

        if (data == null)
        {
            return JsonSerializer.Serialize(new { error = "Null invoice data provided." });
        }

        // Retrieve active tenant inventory items (enforces tenant isolation via global query filter)
        var inventoryItems = await _dbContext.InventoryItems
            .Where(i => i.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var line in data.LineItems)
        {
            // 1. Try exact or regex SKU match (e.g. SKU-123)
            var skuMatch = Regex.Match(line.Description, @"SKU-[\w\d]+", RegexOptions.IgnoreCase);
            InventoryItem? matchedItem = null;

            if (skuMatch.Success)
            {
                var candidateSku = skuMatch.Value;
                matchedItem = inventoryItems.FirstOrDefault(i =>
                    string.Equals(i.SKU, candidateSku, StringComparison.OrdinalIgnoreCase));
            }

            // 2. Try product title matching if SKU was not found
            if (matchedItem == null)
            {
                matchedItem = inventoryItems.FirstOrDefault(i =>
                    line.Description.Contains(i.Name, StringComparison.OrdinalIgnoreCase) ||
                    i.Name.Contains(line.Description, StringComparison.OrdinalIgnoreCase));
            }

            if (matchedItem != null)
            {
                line.MatchedSKU = matchedItem.SKU;
                line.MatchedInventoryItemId = matchedItem.Id;
                line.CatalogUnitPrice = matchedItem.UnitPrice;

                // Check price discrepancy
                if (Math.Abs(line.UnitPrice - matchedItem.UnitPrice) > 0.01m)
                {
                    line.HasPriceDiscrepancy = true;
                    data.Warnings.Add($"Price discrepancy on SKU {matchedItem.SKU}: extracted unit price is ${line.UnitPrice:F2} vs catalog ${matchedItem.UnitPrice:F2}.");
                }
            }
        }

        return JsonSerializer.Serialize(data, JsonOptions);
    }

    /// <summary>
    /// Saves the validated invoice data into the ERP database as a new Draft invoice with associated line items.
    /// </summary>
    [KernelFunction, Description("Saves the validated invoice data into the ERP database as a new Draft invoice with associated line items.")]
    public async Task<string> StageExtractedInvoiceAsync(
        [Description("JSON representation of validated extracted invoice data")] string extractedJson,
        CancellationToken cancellationToken = default)
    {
        ExtractedInvoiceData? data;
        try
        {
            data = JsonSerializer.Deserialize<ExtractedInvoiceData>(extractedJson, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new StagedInvoiceResultDto
            {
                Success = false,
                Message = $"Failed to parse invoice JSON: {ex.Message}"
            });
        }

        if (data == null || data.LineItems.Count == 0)
        {
            return JsonSerializer.Serialize(new StagedInvoiceResultDto
            {
                Success = false,
                Message = "Invoice must contain at least one line item to be staged."
            });
        }

        var count = await _dbContext.Invoices.CountAsync(cancellationToken);
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMM}-{count + 1:D4}";

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            CustomerName = string.IsNullOrWhiteSpace(data.CustomerName) ? "Valued Customer" : data.CustomerName,
            CustomerEmail = string.IsNullOrWhiteSpace(data.CustomerEmail) ? "billing@customer.com" : data.CustomerEmail,
            IssueDate = data.IssueDate,
            DueDate = data.DueDate,
            SubTotal = data.SubTotal,
            TaxAmount = data.TaxAmount,
            TotalAmount = data.TotalAmount,
            Currency = string.IsNullOrWhiteSpace(data.Currency) ? "USD" : data.Currency,
            Status = InvoiceStatus.Draft,
            Notes = $"Extracted automatically by Smart ERP Agent. Confidence: {data.ConfidenceScore:P0}.",
            LineItems = data.LineItems.Select(li => new InvoiceLineItem
            {
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                TotalPrice = li.TotalPrice,
                InventoryItemId = li.MatchedInventoryItemId
            }).ToList()
        };

        _dbContext.Invoices.Add(invoice);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var result = new StagedInvoiceResultDto
        {
            Success = true,
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerName = invoice.CustomerName,
            TotalAmount = invoice.TotalAmount,
            Currency = invoice.Currency,
            LineItemCount = invoice.LineItems.Count,
            Status = invoice.Status.ToString(),
            Message = $"Draft invoice {invoice.InvoiceNumber} staged successfully in ERP ledger."
        };

        return JsonSerializer.Serialize(result, JsonOptions);
    }
}
