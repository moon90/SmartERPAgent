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
    private readonly IHubContext<AgentHub>? _hubContext;

    public SemanticKernelAgentOrchestrator(
        IConfiguration configuration,
        ILogger<SemanticKernelAgentOrchestrator> logger,
        InvoiceAgentPlugin invoicePlugin,
        InventoryAgentPlugin inventoryPlugin,
        IHubContext<AgentHub>? hubContext = null,
        InvoiceExtractorPlugin? invoiceExtractorPlugin = null)
    {
        _configuration = configuration;
        _logger = logger;
        _invoicePlugin = invoicePlugin;
        _inventoryPlugin = inventoryPlugin;
        _hubContext = hubContext;
        _invoiceExtractorPlugin = invoiceExtractorPlugin;
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

        // Register domain plugins
        kernelBuilder.Plugins.AddFromObject(_inventoryPlugin, "InventoryAgentPlugin");
        kernelBuilder.Plugins.AddFromObject(_invoicePlugin, "InvoiceAgentPlugin");
        if (_invoiceExtractorPlugin != null)
        {
            kernelBuilder.Plugins.AddFromObject(_invoiceExtractorPlugin, "InvoiceExtractorPlugin");
        }

        var openAiKey = _configuration["SemanticKernel:ApiKey"] ?? _configuration["OpenAI:ApiKey"];
        var modelId = _configuration["SemanticKernel:ModelId"] ?? _configuration["OpenAI:ModelId"] ?? "gpt-4o";

        var executedActions = new List<AgentActionExecutedDto>();

        if (!string.IsNullOrWhiteSpace(openAiKey) && openAiKey != "YOUR_OPENAI_API_KEY")
        {
            try
            {
                await StreamThoughtAsync("Initializing Semantic Kernel chat completion planner...", cancellationToken);
                kernelBuilder.AddOpenAIChatCompletion(modelId, openAiKey);
                var kernel = kernelBuilder.Build();

                var plannerOptions = new FunctionCallingStepwisePlannerOptions
                {
                    MaxIterations = 10,
                    MaxTokens = 4000
                };

                var planner = new FunctionCallingStepwisePlanner(plannerOptions);
                await StreamThoughtAsync("Executing stepwise tool planner iterations...", cancellationToken);
                var planResult = await planner.ExecuteAsync(kernel, request.Prompt, cancellationToken: cancellationToken);

                await StreamThoughtAsync("Synthesizing final generative answer...", cancellationToken);

                return new AgentResponseDto(
                    Content: planResult.FinalAnswer ?? "Agent completed execution without output text.",
                    ThoughtProcess: "Executed using Microsoft Semantic Kernel FunctionCallingStepwisePlanner.",
                    ExecutedActions: executedActions,
                    Status: "Success"
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "FunctionCallingStepwisePlanner encountered an issue. Falling back to local domain handler.");
                await StreamThoughtAsync("Falling back to enterprise domain plugin execution pipeline...", cancellationToken);
            }
        }

        // Fallback rule-based execution for local testing and offline execution without cloud LLM
        var promptText = request.Prompt.Trim();
        var promptLower = promptText.ToLowerInvariant();
        string responseText;
        string actionResult;

        // 1. Check for Invoice Extraction or Staging prompt
        if (_invoiceExtractorPlugin != null && (promptLower.Contains("extract") || promptLower.Contains("parse") || promptLower.Contains("stage")) &&
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
        // 2. Check if inquiring about stock for a SKU
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
                           "Semantic Kernel orchestration is initialized with InventoryAgentPlugin, InvoiceAgentPlugin, and InvoiceExtractorPlugin. " +
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
