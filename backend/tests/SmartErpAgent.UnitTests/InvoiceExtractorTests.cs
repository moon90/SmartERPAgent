using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SmartErpAgent.AgentEngine;
using SmartErpAgent.AgentEngine.Plugins;
using SmartErpAgent.AgentEngine.Services;
using SmartErpAgent.Application.Common.Interfaces;
using SmartErpAgent.Application.DTOs;
using SmartErpAgent.Core.Entities;
using SmartErpAgent.Core.Enums;
using SmartErpAgent.Core.Interfaces;
using SmartErpAgent.Infrastructure.Persistence;
using SmartErpAgent.Infrastructure.Tenancy;
using Xunit;

namespace SmartErpAgent.UnitTests;

public class InvoiceExtractorTests
{
    private (ApplicationDbContext dbContext, TenantContext tenantContext) CreateTestDbContext()
    {
        var tenantContext = new TenantContext();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new ApplicationDbContext(options, tenantContext);
        return (dbContext, tenantContext);
    }

    [Fact]
    public async Task ExtractInvoiceFromText_ShouldExtractCustomerAndLineItems()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        tenantContext.SetTenant(Guid.NewGuid(), "TEST_CORP");

        var plugin = new InvoiceExtractorPlugin(dbContext);
        var rawText = @"
Invoice From: Global Supply Corp
Bill To: Apex Engineering
Customer Email: accounts@apexeng.com
Invoice #: INV-2026-889
Date: 2026-09-01

Items:
- SKU-101 Hydraulic Valve, Qty: 4, Unit Price: $125.00
- SKU-102 Steel Gasket, Qty: 10, Unit Price: $15.50
";

        // Act
        var resultJson = await plugin.ExtractInvoiceFromTextAsync(rawText);

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        Assert.Equal("Apex Engineering", root.GetProperty("CustomerName").GetString());
        Assert.Equal("accounts@apexeng.com", root.GetProperty("CustomerEmail").GetString());
        Assert.Equal("INV-2026-889", root.GetProperty("InvoiceNumber").GetString());
        Assert.Equal("USD", root.GetProperty("Currency").GetString());

        var lineItems = root.GetProperty("LineItems");
        Assert.Equal(2, lineItems.GetArrayLength());

        var firstItem = lineItems[0];
        Assert.Contains("Hydraulic Valve", firstItem.GetProperty("Description").GetString());
        Assert.Equal(4, firstItem.GetProperty("Quantity").GetInt32());
        Assert.Equal(125.00m, firstItem.GetProperty("UnitPrice").GetDecimal());
        Assert.Equal(500.00m, firstItem.GetProperty("TotalPrice").GetDecimal());

        // SubTotal = 4*125 (500) + 10*15.50 (155) = 655
        Assert.Equal(655.00m, root.GetProperty("SubTotal").GetDecimal());
        // Tax = 65.50
        Assert.Equal(65.50m, root.GetProperty("TaxAmount").GetDecimal());
        // Total = 720.50
        Assert.Equal(720.50m, root.GetProperty("TotalAmount").GetDecimal());
    }

    [Fact]
    public async Task CorrelateWithInventoryCatalog_ShouldLinkMatchedSKUs_AndDetectPriceDiscrepancies()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        var inventoryItem = new InventoryItem
        {
            TenantId = tenantId,
            SKU = "SKU-900",
            Name = "Precision Ball Bearing",
            UnitPrice = 45.00m,
            StockQuantity = 100,
            ReorderThreshold = 10,
            IsActive = true
        };
        dbContext.InventoryItems.Add(inventoryItem);
        await dbContext.SaveChangesAsync();

        var plugin = new InvoiceExtractorPlugin(dbContext);
        var extractedData = new ExtractedInvoiceData
        {
            CustomerName = "Acme Corp",
            CustomerEmail = "acme@example.com",
            LineItems = new List<ExtractedInvoiceLineItem>
            {
                new ExtractedInvoiceLineItem
                {
                    Description = "High Grade Precision Ball Bearing (SKU-900)",
                    Quantity = 2,
                    UnitPrice = 50.00m, // Price is $50, catalog is $45
                    TotalPrice = 100.00m
                }
            }
        };

        var extractedJson = JsonSerializer.Serialize(extractedData);

        // Act
        var resultJson = await plugin.CorrelateWithInventoryCatalogAsync(extractedJson);

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;
        var lineItems = root.GetProperty("LineItems");
        var matchedLine = lineItems[0];

        Assert.Equal("SKU-900", matchedLine.GetProperty("MatchedSKU").GetString());
        Assert.Equal(inventoryItem.Id, matchedLine.GetProperty("MatchedInventoryItemId").GetGuid());
        Assert.Equal(45.00m, matchedLine.GetProperty("CatalogUnitPrice").GetDecimal());
        Assert.True(matchedLine.GetProperty("HasPriceDiscrepancy").GetBoolean());
        Assert.NotEmpty(root.GetProperty("Warnings").EnumerateArray());
    }

    [Fact]
    public async Task CorrelateWithInventoryCatalog_ShouldEnforceTenantIsolation()
    {
        // Arrange
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();
        var tenantContext = new TenantContext();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Seed under Tenant A
        tenantContext.SetTenant(tenantAId, "TENANT_A");
        using (var contextA = new ApplicationDbContext(options, tenantContext))
        {
            contextA.InventoryItems.Add(new InventoryItem
            {
                TenantId = tenantAId,
                SKU = "SECRET-PART",
                Name = "Tenant A Proprietary Device",
                UnitPrice = 999m,
                StockQuantity = 10,
                IsActive = true
            });
            await contextA.SaveChangesAsync();
        }

        // Act - Query as Tenant B
        tenantContext.SetTenant(tenantBId, "TENANT_B");
        using (var contextB = new ApplicationDbContext(options, tenantContext))
        {
            var plugin = new InvoiceExtractorPlugin(contextB);
            var extractedData = new ExtractedInvoiceData
            {
                CustomerName = "Target Customer",
                LineItems = new List<ExtractedInvoiceLineItem>
                {
                    new ExtractedInvoiceLineItem
                    {
                        Description = "SECRET-PART",
                        Quantity = 1,
                        UnitPrice = 999m,
                        TotalPrice = 999m
                    }
                }
            };

            var resultJson = await plugin.CorrelateWithInventoryCatalogAsync(JsonSerializer.Serialize(extractedData));

            // Assert
            using var doc = JsonDocument.Parse(resultJson);
            var lineItems = doc.RootElement.GetProperty("LineItems");
            var item = lineItems[0];
            Assert.True(item.GetProperty("MatchedSKU").ValueKind == JsonValueKind.Null);
            Assert.True(item.GetProperty("MatchedInventoryItemId").ValueKind == JsonValueKind.Null);
        }
    }

    [Fact]
    public async Task StageExtractedInvoice_ShouldPersistDraftInvoice_AndLineItems()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        var plugin = new InvoiceExtractorPlugin(dbContext);
        var extractedData = new ExtractedInvoiceData
        {
            CustomerName = "Zenith Dynamics",
            CustomerEmail = "finance@zenith.com",
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            SubTotal = 300.00m,
            TaxAmount = 30.00m,
            TotalAmount = 330.00m,
            Currency = "USD",
            LineItems = new List<ExtractedInvoiceLineItem>
            {
                new ExtractedInvoiceLineItem
                {
                    Description = "Consulting & Installation",
                    Quantity = 3,
                    UnitPrice = 100.00m,
                    TotalPrice = 300.00m
                }
            }
        };

        // Act
        var resultJson = await plugin.StageExtractedInvoiceAsync(JsonSerializer.Serialize(extractedData));

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Draft", root.GetProperty("Status").GetString());
        Assert.Equal(330.00m, root.GetProperty("TotalAmount").GetDecimal());

        var invoiceId = root.GetProperty("InvoiceId").GetGuid();
        var savedInvoice = await dbContext.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        Assert.NotNull(savedInvoice);
        Assert.Equal(tenantId, savedInvoice.TenantId);
        Assert.Equal("Zenith Dynamics", savedInvoice.CustomerName);
        Assert.Equal(InvoiceStatus.Draft, savedInvoice.Status);
        Assert.Single(savedInvoice.LineItems);
        Assert.Equal("Consulting & Installation", savedInvoice.LineItems.First().Description);
    }

    [Fact]
    public async Task Orchestrator_ShouldExecuteExtractionPrompt_AndStreamMilestones()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "ORCH_CORP");

        var item = new InventoryItem
        {
            TenantId = tenantId,
            SKU = "SKU-444",
            Name = "Air Filter Assembly",
            UnitPrice = 80.00m,
            StockQuantity = 25,
            IsActive = true
        };
        dbContext.InventoryItems.Add(item);
        await dbContext.SaveChangesAsync();

        var config = new ConfigurationBuilder().Build();
        var invoicePlugin = new InvoiceAgentPlugin(dbContext);
        var inventoryPlugin = new InventoryAgentPlugin(dbContext);
        var extractorPlugin = new InvoiceExtractorPlugin(dbContext);

        var orchestrator = new SemanticKernelAgentOrchestrator(
            config,
            NullLogger<SemanticKernelAgentOrchestrator>.Instance,
            invoicePlugin,
            inventoryPlugin,
            null,
            extractorPlugin);

        var prompt = @"
Extract and stage this invoice:
Customer: Nexus Technologies (nexus@tech.com)
Items:
- SKU-444 Air Filter Assembly, Qty: 2, Unit Price: $80.00
";

        // Act
        var response = await orchestrator.ExecutePromptAsync(prompt);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("staged successfully", response, StringComparison.OrdinalIgnoreCase);

        var stagedInvoice = await dbContext.Invoices.FirstOrDefaultAsync(i => i.CustomerName.Contains("Nexus"));
        Assert.NotNull(stagedInvoice);
        Assert.Equal(tenantId, stagedInvoice.TenantId);
    }

    [Fact]
    public async Task AddAgentEngine_ShouldResolveInvoiceExtractorPlugin()
    {
        // Arrange
        var services = new ServiceCollection();
        var (dbContext, tenantContext) = CreateTestDbContext();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IApplicationDbContext>(dbContext);
        services.AddSingleton<ITenantContext>(tenantContext);
        services.AddLogging();

        // Act
        services.AddAgentEngine();
        var provider = services.BuildServiceProvider();

        var extractor = provider.GetService<InvoiceExtractorPlugin>();
        var orchestrator = provider.GetService<IAgentOrchestrator>();

        // Assert
        Assert.NotNull(extractor);
        Assert.NotNull(orchestrator);
    }
}
