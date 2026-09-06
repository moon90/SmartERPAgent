using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning;
using SmartErpAgent.AgentEngine.Plugins;
using SmartErpAgent.Application.Common.Interfaces;
using SmartErpAgent.Application.DTOs;
using SmartErpAgent.Application.Hubs;

namespace SmartErpAgent.AgentEngine.Services;

public class SemanticKernelAgentOrchestrator : IAgentOrchestrator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SemanticKernelAgentOrchestrator> _logger;
    private readonly InvoiceAgentPlugin _invoicePlugin;
    private readonly InventoryAgentPlugin _inventoryPlugin;
    private readonly InvoiceExtractorPlugin? _invoiceExtractorPlugin;
    private readonly ReportingAgentPlugin? _reportingPlugin;
    private readonly PurchaseOrderAgentPlugin? _purchaseOrderPlugin;
    private readonly IHubContext<AgentHub>? _hubContext;

    public SemanticKernelAgentOrchestrator(
        IConfiguration configuration,
        ILogger<SemanticKernelAgentOrchestrator> logger,
        InvoiceAgentPlugin invoicePlugin,
        InventoryAgentPlugin inventoryPlugin,
        IHubContext<AgentHub>? hubContext = null,
        InvoiceExtractorPlugin? invoiceExtractorPlugin = null,
        ReportingAgentPlugin? reportingPlugin = null,
        PurchaseOrderAgentPlugin? purchaseOrderPlugin = null)
    {
        _configuration = configuration;
        _logger = logger;
        _invoicePlugin = invoicePlugin;
        _inventoryPlugin = inventoryPlugin;
        _hubContext = hubContext;
        _invoiceExtractorPlugin = invoiceExtractorPlugin;
        _reportingPlugin = reportingPlugin;
        _purchaseOrderPlugin = purchaseOrderPlugin;
    }

    public async Task<string> ExecutePromptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var response = await ProcessPromptAsync(new AgentPromptRequestDto(prompt), cancellationToken);
        return response.Content;
    }

    public async Task<AgentResponseDto> ProcessPromptAsync(
        AgentPromptRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing agent prompt: {Prompt} for Tenant: {TenantId}", request.Prompt, request.TenantId);

        await StreamThoughtAsync($"Analyzing prompt: \"{request.Prompt}\"...", cancellationToken);

        var kernelBuilder = Kernel.CreateBuilder();

        // Register domain plugins into Semantic Kernel
        kernelBuilder.Plugins.AddFromObject(_inventoryPlugin, "InventoryAgentPlugin");
        kernelBuilder.Plugins.AddFromObject(_invoicePlugin, "InvoiceAgentPlugin");
        if (_invoiceExtractorPlugin != null)
        {
            kernelBuilder.Plugins.AddFromObject(_invoiceExtractorPlugin, "InvoiceExtractorPlugin");
        }
        if (_reportingPlugin != null)
        {
            kernelBuilder.Plugins.AddFromObject(_reportingPlugin, "ReportingAgentPlugin");
        }
        if (_purchaseOrderPlugin != null)
        {
            kernelBuilder.Plugins.AddFromObject(_purchaseOrderPlugin, "PurchaseOrderAgentPlugin");
        }

        var openAiKey = _configuration["SemanticKernel:ApiKey"] ?? _configuration["OpenAI:ApiKey"];
        var modelId = _configuration["SemanticKernel:ModelId"] ?? _configuration["OpenAI:ModelId"] ?? "gpt-4o";

        var executedActions = new List<AgentActionExecutedDto>();

        if (!string.IsNullOrWhiteSpace(openAiKey))
        {
            try
            {
                await StreamThoughtAsync("Connecting to OpenAI Semantic Kernel dynamic function planner...", cancellationToken);

                kernelBuilder.AddOpenAIChatCompletion(modelId, openAiKey);
                var kernel = kernelBuilder.Build();

#pragma warning disable SKEXP0060
                var planner = new FunctionCallingStepwisePlanner();
                var planResult = await planner.ExecuteAsync(kernel, request.Prompt, cancellationToken: cancellationToken);
#pragma warning restore SKEXP0060

                await StreamThoughtAsync("Successfully synthesized response via AI function calling.", cancellationToken);

                return new AgentResponseDto(
                    Content: planResult.FinalAnswer ?? "Execution completed.",
                    ThoughtProcess: "Executed autonomously via Semantic Kernel Stepwise Planner.",
                    ExecutedActions: executedActions,
                    Status: "Success"
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenAI Kernel planning encountered an error. Falling back to local deterministic rule routing.");
                await StreamThoughtAsync($"Dynamic reasoning unavailable ({ex.Message}). Engaging deterministic ERP rule routing...", cancellationToken);
            }
        }

        // Deterministic Fallback Routing Engine
        var promptText = request.Prompt.Trim();
        var promptLower = promptText.ToLowerInvariant();
        string responseText;
        string actionResult;

        // 1. Check for Inventory Valuation & Executive Asset Summary
        if (_reportingPlugin != null && (promptLower.Contains("valuation") || promptLower.Contains("inventory value") || promptLower.Contains("stock value") || promptLower.Contains("asset value") || promptLower.Contains("value of our warehouse") || promptLower.Contains("value of our inventory") || promptLower.Contains("value of inventory")))
        {
            await StreamThoughtAsync("Aggregating active inventory holding values via ReportingAgentPlugin.GetInventoryValuationAsync...", cancellationToken);
            actionResult = await _reportingPlugin.GetInventoryValuationAsync(cancellationToken);

            executedActions.Add(new AgentActionExecutedDto(
                PluginName: "ReportingAgentPlugin",
                FunctionName: "GetInventoryValuationAsync",
                ArgumentsJson: "{}",
                ResultJson: actionResult
            ));

            using var doc = JsonDocument.Parse(actionResult);
            var totalValuation = doc.RootElement.GetProperty("TotalValuation").GetDecimal();
            var activeCount = doc.RootElement.GetProperty("TotalActiveSkuCount").GetInt32();
            var totalUnits = doc.RootElement.GetProperty("TotalUnitsInStock").GetInt32();

            await StreamThoughtAsync($"Calculated warehouse valuation: ${totalValuation:N2} across {activeCount} active SKUs ({totalUnits} total units).", cancellationToken);
            responseText = $"[Executive Valuation Report]\nTotal active inventory valuation: ${totalValuation:N2} ({totalUnits} total units across {activeCount} SKUs).\nDetails:\n{actionResult}";
        }
        // 2. Check for Financial Summary / Revenue / Periodic Performance
        else if (_reportingPlugin != null && (promptLower.Contains("financial summary") || promptLower.Contains("summary of our financials") || (promptLower.Contains("financial") && promptLower.Contains("summary")) || (promptLower.Contains("revenue") && (promptLower.Contains("summary") || promptLower.Contains("total") || promptLower.Contains("report")))))
        {
            int days = 30;
            var daysMatch = Regex.Match(promptText, @"(?i)(?:last|past|in)\s+(\d+)\s+days?");
            if (daysMatch.Success && int.TryParse(daysMatch.Groups[1].Value, out var parsedDays))
            {
                days = parsedDays;
            }

            await StreamThoughtAsync($"Aggregating revenue and invoicing volume over the past {days} days via ReportingAgentPlugin.GetFinancialSummaryAsync...", cancellationToken);
            actionResult = await _reportingPlugin.GetFinancialSummaryAsync(days, cancellationToken);

            executedActions.Add(new AgentActionExecutedDto(
                PluginName: "ReportingAgentPlugin",
                FunctionName: "GetFinancialSummaryAsync",
                ArgumentsJson: JsonSerializer.Serialize(new { days }),
                ResultJson: actionResult
            ));

            using var doc = JsonDocument.Parse(actionResult);
            var totalRev = doc.RootElement.GetProperty("TotalRevenue").GetDecimal();
            var count = doc.RootElement.GetProperty("InvoiceCount").GetInt32();
            var currency = doc.RootElement.GetProperty("Currency").GetString();

            await StreamThoughtAsync($"Retrieved financial summary: {count} invoices issued with total revenue of {currency} ${totalRev:N2}.", cancellationToken);
            responseText = $"[Executive Financial Summary ({days} Days)]\nTotal Revenue: {currency} ${totalRev:N2} across {count} issued invoices.\nDetails:\n{actionResult}";
        }
        // 3. Check for Restocking / Purchase Order Generation prompt
        else if (_purchaseOrderPlugin != null &&
            (promptLower.Contains("purchase order") || promptLower.Contains("replenish") || promptLower.Contains("restock") || promptLower.Contains("order stock")) &&
            (promptLower.Contains("draft") || promptLower.Contains("generate") || promptLower.Contains("create") || promptLower.Contains("po")))
        {
            await StreamThoughtAsync("Analyzing depleted inventory and calculating replenishment quantities via PurchaseOrderAgentPlugin...", cancellationToken);
            actionResult = await _purchaseOrderPlugin.CreateDraftPurchaseOrderAsync(null, cancellationToken);

            executedActions.Add(new AgentActionExecutedDto(
                PluginName: "PurchaseOrderAgentPlugin",
                FunctionName: "CreateDraftPurchaseOrderAsync",
                ArgumentsJson: JsonSerializer.Serialize(new { targetSkus = (string?)null }),
                ResultJson: actionResult
            ));

            using var doc = JsonDocument.Parse(actionResult);
            if (doc.RootElement.TryGetProperty("Success", out var successProp) && successProp.GetBoolean())
            {
                var poNum = doc.RootElement.GetProperty("OrderNumber").GetString();
                var total = doc.RootElement.GetProperty("TotalAmount").GetDecimal();
                var itemCount = doc.RootElement.GetProperty("LineItemCount").GetInt32();
                var supplier = doc.RootElement.GetProperty("SupplierName").GetString();

                await StreamThoughtAsync($"Persisted draft purchase order '{poNum}' with {itemCount} line items totaling ${total:N2}.", cancellationToken);
                responseText = $"[Smart ERP Purchase Order Agent]\nDraft Purchase Order '{poNum}' generated successfully.\n" +
                               $"Supplier: {supplier}\n" +
                               $"Items: {itemCount} | Total Amount: ${total:N2} USD\n" +
                               $"Status: Draft (Pending approval and supplier dispatch)";
            }
            else
            {
                var msg = doc.RootElement.TryGetProperty("Message", out var msgProp) ? msgProp.GetString() : "No items require restocking.";
                responseText = $"[Smart ERP Purchase Order Agent]\n{msg}";
            }
        }
        // 4. Check for Low-stock alerts / shortage detection
        else if (_purchaseOrderPlugin != null &&
            (promptLower.Contains("low in stock") || promptLower.Contains("which items are low") || promptLower.Contains("items are low") || promptLower.Contains("reorder alert") || promptLower.Contains("stock alert") || promptLower.Contains("low-stock")))
        {
            await StreamThoughtAsync("Scanning inventory safety thresholds via PurchaseOrderAgentPlugin.GetLowStockAlertsAsync...", cancellationToken);
            actionResult = await _purchaseOrderPlugin.GetLowStockAlertsAsync(cancellationToken);

            executedActions.Add(new AgentActionExecutedDto(
                PluginName: "PurchaseOrderAgentPlugin",
                FunctionName: "GetLowStockAlertsAsync",
                ArgumentsJson: "{}",
                ResultJson: actionResult
            ));

            using var doc = JsonDocument.Parse(actionResult);
            var itemCount = doc.RootElement.GetProperty("TotalLowStockItems").GetInt32();
            var totalCost = doc.RootElement.GetProperty("TotalEstimatedCost").GetDecimal();

            await StreamThoughtAsync($"Identified {itemCount} depleted items requiring restocking (${totalCost:N2} estimated restock cost).", cancellationToken);
            responseText = $"[Smart ERP Purchase Order Agent]\nIdentified {itemCount} low-stock items (Total estimated restock cost: ${totalCost:N2}).\nDetails:\n{actionResult}";
        }
        // 5. Check for ExtractInvoiceDetailsAsync prompt (e.g. "process an invoice", "read this email", "extract invoice details")
        else if (_invoiceExtractorPlugin != null && (
            Regex.IsMatch(promptText, @"(?i)(?:process\s+(?:an?\s+|this\s+)?invoice|read\s+(?:this\s+)?email|extract\s+(?:the\s+)?invoice\s+details)") ||
            (promptLower.Contains("read") && promptLower.Contains("email")) ||
            (promptLower.Contains("process") && promptLower.Contains("invoice")) ||
            (promptLower.Contains("extract") && promptLower.Contains("invoice details"))))
        {
            await StreamThoughtAsync("Processing document text and extracting invoice details via InvoiceExtractorPlugin...", cancellationToken);
            var invoiceDto = await _invoiceExtractorPlugin.ExtractInvoiceDetailsAsync(promptText, cancellationToken);
            var invoiceJson = JsonSerializer.Serialize(invoiceDto, new JsonSerializerOptions { WriteIndented = true });

            executedActions.Add(new AgentActionExecutedDto(
                PluginName: "InvoiceExtractorPlugin",
                FunctionName: "ExtractInvoiceDetailsAsync",
                ArgumentsJson: JsonSerializer.Serialize(new { rawContent = promptText }),
                ResultJson: invoiceJson
            ));

            await StreamThoughtAsync("Formatting structured invoice response...", cancellationToken);
            var itemsSummary = invoiceDto.LineItems.Count > 0
                ? string.Join("\n", invoiceDto.LineItems.Select(li => $"  - {li.Description}: {li.Quantity} x ${li.UnitPrice:N2} = ${li.TotalPrice:N2}"))
                : "  (No individual line items parsed)";

            responseText = $"[Smart ERP Agent - Invoice Extracted]\n" +
                           $"Successfully extracted invoice {invoiceDto.InvoiceNumber} for {invoiceDto.CustomerName}.\n" +
                           $"Invoice #: {invoiceDto.InvoiceNumber}\n" +
                           $"Customer: {invoiceDto.CustomerName} ({invoiceDto.CustomerEmail})\n" +
                           $"Issue Date: {invoiceDto.IssueDate:yyyy-MM-dd} | Due Date: {invoiceDto.DueDate:yyyy-MM-dd}\n" +
                           $"Total Amount: {invoiceDto.Currency} ${invoiceDto.TotalAmount:N2} (Subtotal: ${invoiceDto.SubTotal:N2}, Tax: ${invoiceDto.TaxAmount:N2})\n" +
                           $"Line Items ({invoiceDto.LineItems.Count}):\n{itemsSummary}";
        }
        // 6. Check for Legacy Invoice Extraction or Staging prompt
        else if (_invoiceExtractorPlugin != null && (promptLower.Contains("extract") || promptLower.Contains("parse") || promptLower.Contains("stage")) &&
            (promptLower.Contains("invoice") || promptLower.Contains("receipt") || promptLower.Contains("bill") || promptLower.Contains("items:")))
        {
            await StreamThoughtAsync("Parsing document text and extracting invoice line items via InvoiceExtractorPlugin...", cancellationToken);
            var extractedJson = await _invoiceExtractorPlugin.ExtractInvoiceFromTextAsync(promptText, cancellationToken);

            executedActions.Add(new AgentActionExecutedDto(
                PluginName: "InvoiceExtractorPlugin",
                FunctionName: "ExtractInvoiceFromTextAsync",
                ArgumentsJson: JsonSerializer.Serialize(new { documentText = promptText }),
                ResultJson: extractedJson
            ));

            await StreamThoughtAsync("Correlating extracted line items with tenant inventory catalog...", cancellationToken);
            var correlatedJson = await _invoiceExtractorPlugin.CorrelateWithInventoryCatalogAsync(extractedJson, cancellationToken);

            executedActions.Add(new AgentActionExecutedDto(
                PluginName: "InvoiceExtractorPlugin",
                FunctionName: "CorrelateWithInventoryCatalogAsync",
                ArgumentsJson: extractedJson,
                ResultJson: correlatedJson
            ));

            if (promptLower.Contains("stage") || promptLower.Contains("create") || promptLower.Contains("save"))
            {
                await StreamThoughtAsync("Staging validated draft invoice in ERP ledger...", cancellationToken);
                var stagedJson = await _invoiceExtractorPlugin.StageExtractedInvoiceAsync(correlatedJson, cancellationToken);

                executedActions.Add(new AgentActionExecutedDto(
                    PluginName: "InvoiceExtractorPlugin",
                    FunctionName: "StageExtractedInvoiceAsync",
                    ArgumentsJson: correlatedJson,
                    ResultJson: stagedJson
                ));

                responseText = $"[Smart ERP Agent]\nDraft invoice staged successfully:\n{stagedJson}";
            }
            else
            {
                responseText = $"[Smart ERP Agent]\nExtracted invoice data:\n{correlatedJson}";
            }
        }
        // 7. Check if inquiring about stock for a SKU
        else if (Regex.IsMatch(promptText, @"(?i)(?:stock\s+(?:level\s+)?(?:for|of)?\s+|sku[:\s-]+)([A-Za-z0-9_-]+)") ||
                 (Regex.IsMatch(promptText, @"(?i)\b([A-Z0-9]+-[A-Z0-9]+)\b") && (promptLower.Contains("stock") || promptLower.Contains("quantity") || promptLower.Contains("have enough"))))
        {
            var skuMatch = Regex.Match(promptText, @"(?i)(?:stock\s+(?:level\s+)?(?:for|of)?\s+|sku[:\s-]+)([A-Za-z0-9_-]+)");
            if (!skuMatch.Success)
            {
                skuMatch = Regex.Match(promptText, @"(?i)\b([A-Z0-9]+-[A-Z0-9]+)\b");
            }

            var detectedSku = skuMatch.Groups[1].Value.Trim();
            await StreamThoughtAsync($"Executing InventoryAgentPlugin.CheckStockLevelAsync for SKU '{detectedSku}'...", cancellationToken);

            actionResult = await _inventoryPlugin.CheckStockLevelAsync(detectedSku, cancellationToken);

            executedActions.Add(new AgentActionExecutedDto(
                PluginName: "InventoryAgentPlugin",
                FunctionName: "CheckStockLevelAsync",
                ArgumentsJson: JsonSerializer.Serialize(new { sku = detectedSku }),
                ResultJson: actionResult
            ));

            using var doc = JsonDocument.Parse(actionResult);
            if (doc.RootElement.TryGetProperty("Found", out var foundProp) && foundProp.GetBoolean())
            {
                var name = doc.RootElement.GetProperty("Name").GetString();
                var qty = doc.RootElement.GetProperty("StockQuantity").GetInt32();
                var status = doc.RootElement.GetProperty("Status").GetString();
                var threshold = doc.RootElement.GetProperty("ReorderThreshold").GetInt32();

                await StreamThoughtAsync($"Retrieved catalog item '{name}': {qty} in stock (safety threshold: {threshold}).", cancellationToken);
                responseText = $"Product '{name}' (SKU: {detectedSku}) currently has {qty} units in stock. Status: {status} (Reorder threshold: {threshold}).";
            }
            else
            {
                await StreamThoughtAsync($"SKU '{detectedSku}' not located in active tenant catalog.", cancellationToken);
                responseText = $"Product with SKU '{detectedSku}' was not found in the current tenant's inventory catalog.";
            }
        }
        else if (promptLower.Contains("low stock") || promptLower.Contains("reorder"))
        {
            await StreamThoughtAsync("Evaluating inventory threshold alerts via InventoryAgentPlugin.GetLowStockAlertsAsync...", cancellationToken);
            actionResult = await _inventoryPlugin.GetLowStockAlertsAsync(cancellationToken);
            executedActions.Add(new AgentActionExecutedDto(
                PluginName: "InventoryAgentPlugin",
                FunctionName: "GetLowStockAlertsAsync",
                ArgumentsJson: "{}",
                ResultJson: actionResult
            ));
            responseText = $"[Smart ERP Agent]\nChecked inventory threshold levels:\n{actionResult}";
        }
        else if (promptLower.Contains("invoice") || promptLower.Contains("financial") || promptLower.Contains("outstanding"))
        {
            await StreamThoughtAsync("Evaluating tenant ledger receivables via InvoiceAgentPlugin.GetInvoiceFinancialSummaryAsync...", cancellationToken);
            actionResult = await _invoicePlugin.GetInvoiceFinancialSummaryAsync(cancellationToken);
            executedActions.Add(new AgentActionExecutedDto(
                PluginName: "InvoiceAgentPlugin",
                FunctionName: "GetInvoiceFinancialSummaryAsync",
                ArgumentsJson: "{}",
                ResultJson: actionResult
            ));
            responseText = $"[Smart ERP Agent]\nRetrieved invoice financial summary:\n{actionResult}";
        }
        else
        {
            await StreamThoughtAsync("Readying Semantic Kernel plugin execution response...", cancellationToken);
            responseText = $"[Smart ERP Agent Ready]\nReceived prompt: '{request.Prompt}'. " +
                           "Semantic Kernel orchestration is initialized with InventoryAgentPlugin, InvoiceAgentPlugin, InvoiceExtractorPlugin, ReportingAgentPlugin, and PurchaseOrderAgentPlugin. " +
                           "Configure 'OpenAI:ApiKey' or 'SemanticKernel:ApiKey' for dynamic generative reasoning.";
        }

        await StreamThoughtAsync("Synthesizing final ERP response...", cancellationToken);

        return new AgentResponseDto(
            Content: responseText,
            ThoughtProcess: "Evaluated via Semantic Kernel Execution Pipeline.",
            ExecutedActions: executedActions,
            Status: "Success"
        );
    }

    private async Task StreamThoughtAsync(string message, CancellationToken cancellationToken)
    {
        if (_hubContext != null)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveThoughtProcess", message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to stream thought process via SignalR: {Message}", message);
            }
        }
    }
}
