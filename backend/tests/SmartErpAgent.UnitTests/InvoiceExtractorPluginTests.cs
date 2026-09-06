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
using SmartErpAgent.Infrastructure.Persistence;
using SmartErpAgent.Infrastructure.Tenancy;
using Xunit;

namespace SmartErpAgent.UnitTests;

public class InvoiceExtractorPluginTests
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
    public async Task ExtractInvoiceDetailsAsync_ShouldExtractStandardInvoiceEmail()
    {
        // Arrange
        var (dbContext, _) = CreateTestDbContext();
        var plugin = new InvoiceExtractorPlugin(dbContext);

        var emailContent = @"From: billing@apex-mfg.com
Subject: Invoice INV-2026-0042 from Apex
Date: 2026-09-02
Due Date: 2026-09-16
Bill To: Contoso Solutions
Items:
Precision Ball Bearing (ABEC-7) 10 x $45.00 = $450.00
Total Amount: $450.00";

        // Act
        var invoice = await plugin.ExtractInvoiceDetailsAsync(emailContent);

        // Assert
        Assert.NotNull(invoice);
        Assert.Equal("INV-2026-0042", invoice.InvoiceNumber);
        Assert.Equal("Contoso Solutions", invoice.CustomerName);
        Assert.Equal("billing@apex-mfg.com", invoice.CustomerEmail);
        Assert.Equal(450.00m, invoice.TotalAmount);
        Assert.Equal("USD", invoice.Currency);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.Single(invoice.LineItems);
        Assert.Equal(10, invoice.LineItems[0].Quantity);
        Assert.Equal(45.00m, invoice.LineItems[0].UnitPrice);
        Assert.Equal(450.00m, invoice.LineItems[0].TotalPrice);
    }

    [Fact]
    public async Task ExtractInvoiceDetailsAsync_ShouldHandleOmittedDueDate_ByDefaultingToIssueDatePlus14Days()
    {
        // Arrange
        var (dbContext, _) = CreateTestDbContext();
        var plugin = new InvoiceExtractorPlugin(dbContext);

        var content = @"Invoice #: INV-9901
Customer: Starlight Dynamics
Date: 2026-10-01
Total: $1,200.00";

        // Act
        var invoice = await plugin.ExtractInvoiceDetailsAsync(content);

        // Assert
        Assert.Equal("INV-9901", invoice.InvoiceNumber);
        Assert.Equal("Starlight Dynamics", invoice.CustomerName);
        Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), invoice.IssueDate);
        Assert.Equal(new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc), invoice.DueDate);
        Assert.Equal(1200.00m, invoice.TotalAmount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \t\n")]
    public async Task ExtractInvoiceDetailsAsync_ShouldHandleEmptyOrWhitespaceContent_Gracefully(string? emptyContent)
    {
        // Arrange
        var (dbContext, _) = CreateTestDbContext();
        var plugin = new InvoiceExtractorPlugin(dbContext);

        // Act
        var invoice = await plugin.ExtractInvoiceDetailsAsync(emptyContent!);

        // Assert
        Assert.NotNull(invoice);
        Assert.Equal("INV-DRAFT-UNKNOWN", invoice.InvoiceNumber);
        Assert.Equal("Unknown Customer", invoice.CustomerName);
        Assert.Equal(0m, invoice.TotalAmount);
        Assert.Empty(invoice.LineItems);
    }

    [Fact]
    public async Task ExtractInvoiceDetailsAsync_ShouldExtractMultipleTabularLineItems()
    {
        // Arrange
        var (dbContext, _) = CreateTestDbContext();
        var plugin = new InvoiceExtractorPlugin(dbContext);

        var content = @"Invoice No: INV-MULTI-01
Bill To: Global Logistics Freight
Date: 2026-08-20
Due Date: 2026-09-03
Line Items:
Precision Ball Bearing 10 x $45.00 = $450.00
Hydraulic Control Valve 2 x $125.00 = $250.00
Total Amount: $700.00";

        // Act
        var invoice = await plugin.ExtractInvoiceDetailsAsync(content);

        // Assert
        Assert.Equal(2, invoice.LineItems.Count);
        Assert.Equal(450.00m, invoice.LineItems[0].TotalPrice);
        Assert.Equal(250.00m, invoice.LineItems[1].TotalPrice);
        Assert.Equal(700.00m, invoice.TotalAmount);
        Assert.Equal(700.00m, invoice.SubTotal);
    }

    [Fact]
    public async Task ExtractInvoiceDetailsAsync_ShouldDetectCurrencyAccurately()
    {
        // Arrange
        var (dbContext, _) = CreateTestDbContext();
        var plugin = new InvoiceExtractorPlugin(dbContext);

        var content = @"Invoice INV-EUR-01
Customer: EuroTech GmbH
Date: 2026-09-01
Total Amount: €2,500.00";

        // Act
        var invoice = await plugin.ExtractInvoiceDetailsAsync(content);

        // Assert
        Assert.Equal("EUR", invoice.Currency);
        Assert.Equal(2500.00m, invoice.TotalAmount);
    }

    [Fact]
    public async Task ExtractInvoiceDetailsAsync_ShouldStripCommasAndCurrencySymbols()
    {
        // Arrange
        var (dbContext, _) = CreateTestDbContext();
        var plugin = new InvoiceExtractorPlugin(dbContext);

        var content = @"Invoice: INV-123456
Client: Apex Manufacturing Inc
Total: $14,500.75";

        // Act
        var invoice = await plugin.ExtractInvoiceDetailsAsync(content);

        // Assert
        Assert.Equal(14500.75m, invoice.TotalAmount);
    }

    [Fact]
    public async Task Orchestrator_ShouldRouteProcessInvoicePrompt_ToInvoiceExtractorPlugin()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        var invoiceExtractorPlugin = new InvoiceExtractorPlugin(dbContext);
        var reportingPlugin = new ReportingAgentPlugin(dbContext);
        var inventoryPlugin = new InventoryAgentPlugin(dbContext);
        var invoicePlugin = new InvoiceAgentPlugin(dbContext);

        var config = new ConfigurationBuilder().Build();
        var orchestrator = new SemanticKernelAgentOrchestrator(
            config,
            NullLogger<SemanticKernelAgentOrchestrator>.Instance,
            invoicePlugin,
            inventoryPlugin,
            null,
            invoiceExtractorPlugin,
            reportingPlugin);

        var prompt = "Please process this invoice email:\nInvoice # INV-TEST-99\nCustomer: Test Enterprise\nTotal: $300.00";

        // Act
        var response = await orchestrator.ProcessPromptAsync(new AgentPromptRequestDto(prompt));

        // Assert
        Assert.Equal("Success", response.Status);
        Assert.Contains("Invoice Extracted", response.Content);
        Assert.Contains("INV-TEST-99", response.Content);
        Assert.Contains("Test Enterprise", response.Content);
        Assert.NotNull(response.ExecutedActions);
        Assert.Contains(response.ExecutedActions!, a => a.PluginName == "InvoiceExtractorPlugin" && a.FunctionName == "ExtractInvoiceDetailsAsync");
    }

    [Fact]
    public async Task Orchestrator_ShouldRouteExtractInvoiceDetailsPrompt_ToInvoiceExtractorPlugin()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        var invoiceExtractorPlugin = new InvoiceExtractorPlugin(dbContext);
        var reportingPlugin = new ReportingAgentPlugin(dbContext);
        var inventoryPlugin = new InventoryAgentPlugin(dbContext);
        var invoicePlugin = new InvoiceAgentPlugin(dbContext);

        var config = new ConfigurationBuilder().Build();
        var orchestrator = new SemanticKernelAgentOrchestrator(
            config,
            NullLogger<SemanticKernelAgentOrchestrator>.Instance,
            invoicePlugin,
            inventoryPlugin,
            null,
            invoiceExtractorPlugin,
            reportingPlugin);

        var prompt = "Extract invoice details from this text: Invoice INV-EXT-101 for Alpha Corp total $999.00";

        // Act
        var response = await orchestrator.ProcessPromptAsync(new AgentPromptRequestDto(prompt));

        // Assert
        Assert.Equal("Success", response.Status);
        Assert.Contains("Invoice Extracted", response.Content);
        Assert.Contains("INV-EXT-101", response.Content);
        Assert.NotNull(response.ExecutedActions);
        Assert.Contains(response.ExecutedActions!, a => a.PluginName == "InvoiceExtractorPlugin" && a.FunctionName == "ExtractInvoiceDetailsAsync");
    }

    [Fact]
    public void DependencyInjection_ShouldRegisterInvoiceExtractorPlugin()
    {
        // Arrange
        var services = new ServiceCollection();
        var (dbContext, _) = CreateTestDbContext();
        services.AddScoped<IApplicationDbContext>(_ => dbContext);
        services.AddLogging();
        services.AddAgentEngine();

        var provider = services.BuildServiceProvider();

        // Act
        var plugin = provider.GetService<InvoiceExtractorPlugin>();

        // Assert
        Assert.NotNull(plugin);
    }
}
