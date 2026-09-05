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

public class ReportingPluginTests
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
    public async Task GetInventoryValuationAsync_ShouldCalculateTotalValuationAndTop3Skus()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        var items = new List<InventoryItem>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenantId, SKU = "SKU-VALVE", Name = "Hydraulic Valve", StockQuantity = 10, UnitPrice = 100.00m, IsActive = true }, // $1,000
            new() { Id = Guid.NewGuid(), TenantId = tenantId, SKU = "SKU-BEARING", Name = "Ball Bearing", StockQuantity = 20, UnitPrice = 45.00m, IsActive = true },   // $900
            new() { Id = Guid.NewGuid(), TenantId = tenantId, SKU = "SKU-MOTOR", Name = "Electric Motor", StockQuantity = 2, UnitPrice = 800.00m, IsActive = true },    // $1,600 (Top 1)
            new() { Id = Guid.NewGuid(), TenantId = tenantId, SKU = "SKU-GASKET", Name = "Flange Gasket", StockQuantity = 15, UnitPrice = 10.00m, IsActive = true }    // $150 (4th, not in top 3)
        };
        dbContext.InventoryItems.AddRange(items);
        await dbContext.SaveChangesAsync();

        var plugin = new ReportingAgentPlugin(dbContext);

        // Act
        var resultJson = await plugin.GetInventoryValuationAsync();

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        // Total = 1000 + 900 + 1600 + 150 = 3650
        Assert.Equal(3650.00m, root.GetProperty("TotalValuation").GetDecimal());
        Assert.Equal(4, root.GetProperty("TotalActiveSkuCount").GetInt32());
        Assert.Equal(47, root.GetProperty("TotalUnitsInStock").GetInt32()); // 10 + 20 + 2 + 15 = 47

        var topSkus = root.GetProperty("TopValuableSkus");
        Assert.Equal(3, topSkus.GetArrayLength());

        // 1st: SKU-MOTOR ($1,600)
        Assert.Equal("SKU-MOTOR", topSkus[0].GetProperty("SKU").GetString());
        Assert.Equal(1600.00m, topSkus[0].GetProperty("TotalItemValue").GetDecimal());

        // 2nd: SKU-VALVE ($1,000)
        Assert.Equal("SKU-VALVE", topSkus[1].GetProperty("SKU").GetString());
        Assert.Equal(1000.00m, topSkus[1].GetProperty("TotalItemValue").GetDecimal());

        // 3rd: SKU-BEARING ($900)
        Assert.Equal("SKU-BEARING", topSkus[2].GetProperty("SKU").GetString());
        Assert.Equal(900.00m, topSkus[2].GetProperty("TotalItemValue").GetDecimal());
    }

    [Fact]
    public async Task GetInventoryValuationAsync_ShouldExcludeInactiveAndDeletedItems()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        var activeItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SKU = "SKU-ACTIVE",
            Name = "Active Product",
            StockQuantity = 5,
            UnitPrice = 50.00m,
            IsActive = true
        };
        var inactiveItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SKU = "SKU-INACTIVE",
            Name = "Discontinued Item",
            StockQuantity = 100,
            UnitPrice = 100.00m,
            IsActive = false
        };
        var deletedItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SKU = "SKU-DELETED",
            Name = "Deleted Item",
            StockQuantity = 50,
            UnitPrice = 100.00m,
            IsActive = true,
            IsDeleted = true
        };

        dbContext.InventoryItems.AddRange(activeItem, inactiveItem, deletedItem);
        await dbContext.SaveChangesAsync();

        var plugin = new ReportingAgentPlugin(dbContext);

        // Act
        var resultJson = await plugin.GetInventoryValuationAsync();

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        Assert.Equal(250.00m, root.GetProperty("TotalValuation").GetDecimal()); // Only 5 * 50
        Assert.Equal(1, root.GetProperty("TotalActiveSkuCount").GetInt32());
        Assert.Equal(5, root.GetProperty("TotalUnitsInStock").GetInt32());
    }

    [Fact]
    public async Task GetInventoryValuationAsync_ShouldIsolateBetweenTenants()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();

        tenantContext.SetTenant(tenant1, "TENANT_ONE");
        dbContext.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenant1,
            SKU = "SKU-TENANT-1",
            Name = "Tenant 1 Asset",
            StockQuantity = 10,
            UnitPrice = 100.00m,
            IsActive = true
        });

        tenantContext.SetTenant(tenant2, "TENANT_TWO");
        dbContext.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenant2,
            SKU = "SKU-TENANT-2",
            Name = "Tenant 2 Asset",
            StockQuantity = 50,
            UnitPrice = 200.00m,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var plugin = new ReportingAgentPlugin(dbContext);

        // Act - query for tenant 1
        tenantContext.SetTenant(tenant1, "TENANT_ONE");
        var resultJsonTenant1 = await plugin.GetInventoryValuationAsync();

        // Act - query for tenant 2
        tenantContext.SetTenant(tenant2, "TENANT_TWO");
        var resultJsonTenant2 = await plugin.GetInventoryValuationAsync();

        // Assert
        using var doc1 = JsonDocument.Parse(resultJsonTenant1);
        Assert.Equal(1000.00m, doc1.RootElement.GetProperty("TotalValuation").GetDecimal());

        using var doc2 = JsonDocument.Parse(resultJsonTenant2);
        Assert.Equal(10000.00m, doc2.RootElement.GetProperty("TotalValuation").GetDecimal());
    }

    [Fact]
    public async Task GetFinancialSummaryAsync_ShouldCalculateRevenueWithinSpecifiedDays()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        var now = DateTime.UtcNow;
        var invoices = new List<Invoice>
        {
            // Within 14 days
            new() { Id = Guid.NewGuid(), TenantId = tenantId, InvoiceNumber = "INV-01", CustomerName = "C1", IssueDate = now.AddDays(-5), TotalAmount = 500.00m, Currency = "USD" },
            // Within 30 days
            new() { Id = Guid.NewGuid(), TenantId = tenantId, InvoiceNumber = "INV-02", CustomerName = "C2", IssueDate = now.AddDays(-20), TotalAmount = 300.00m, Currency = "USD" },
            // Older than 30 days (45 days ago)
            new() { Id = Guid.NewGuid(), TenantId = tenantId, InvoiceNumber = "INV-03", CustomerName = "C3", IssueDate = now.AddDays(-45), TotalAmount = 1200.00m, Currency = "USD" }
        };
        dbContext.Invoices.AddRange(invoices);
        await dbContext.SaveChangesAsync();

        var plugin = new ReportingAgentPlugin(dbContext);

        // Act - 14 days
        var json14 = await plugin.GetFinancialSummaryAsync(14);
        using var doc14 = JsonDocument.Parse(json14);
        Assert.Equal(14, doc14.RootElement.GetProperty("PeriodDays").GetInt32());
        Assert.Equal(500.00m, doc14.RootElement.GetProperty("TotalRevenue").GetDecimal());
        Assert.Equal(1, doc14.RootElement.GetProperty("InvoiceCount").GetInt32());

        // Act - 30 days
        var json30 = await plugin.GetFinancialSummaryAsync(30);
        using var doc30 = JsonDocument.Parse(json30);
        Assert.Equal(30, doc30.RootElement.GetProperty("PeriodDays").GetInt32());
        Assert.Equal(800.00m, doc30.RootElement.GetProperty("TotalRevenue").GetDecimal()); // 500 + 300
        Assert.Equal(2, doc30.RootElement.GetProperty("InvoiceCount").GetInt32());

        // Act - 60 days
        var json60 = await plugin.GetFinancialSummaryAsync(60);
        using var doc60 = JsonDocument.Parse(json60);
        Assert.Equal(60, doc60.RootElement.GetProperty("PeriodDays").GetInt32());
        Assert.Equal(2000.00m, doc60.RootElement.GetProperty("TotalRevenue").GetDecimal()); // 500 + 300 + 1200
        Assert.Equal(3, doc60.RootElement.GetProperty("InvoiceCount").GetInt32());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task GetFinancialSummaryAsync_ShouldDefaultTo30Days_WhenDaysIsZeroOrNegative(int invalidDays)
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        var plugin = new ReportingAgentPlugin(dbContext);

        // Act
        var resultJson = await plugin.GetFinancialSummaryAsync(invalidDays);

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        Assert.Equal(30, doc.RootElement.GetProperty("PeriodDays").GetInt32());
    }

    [Fact]
    public async Task Orchestrator_ShouldRouteValuationPrompt_ToReportingPlugin()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        dbContext.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SKU = "SKU-VAL-01",
            Name = "Warehouse Turbine",
            StockQuantity = 5,
            UnitPrice = 2000.00m,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

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
            null,
            reportingPlugin);

        // Act
        var response = await orchestrator.ProcessPromptAsync(new AgentPromptRequestDto("What is the total value of our inventory?"));

        // Assert
        Assert.Equal("Success", response.Status);
        Assert.Contains("Executive Valuation Report", response.Content);
        Assert.Contains("$10,000.00", response.Content);
        Assert.NotNull(response.ExecutedActions);
        Assert.Contains(response.ExecutedActions!, a => a.PluginName == "ReportingAgentPlugin" && a.FunctionName == "GetInventoryValuationAsync");
    }

    [Fact]
    public async Task Orchestrator_ShouldRouteFinancialSummaryPrompt_ToReportingPlugin()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        dbContext.Invoices.Add(new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceNumber = "INV-2026-TEST",
            CustomerName = "Orchestrator Test Corp",
            IssueDate = DateTime.UtcNow.AddDays(-3),
            TotalAmount = 4500.00m,
            Currency = "USD"
        });
        await dbContext.SaveChangesAsync();

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
            null,
            reportingPlugin);

        // Act
        var response = await orchestrator.ProcessPromptAsync(new AgentPromptRequestDto("Give me a summary of our financials for the last 14 days"));

        // Assert
        Assert.Equal("Success", response.Status);
        Assert.Contains("Executive Financial Summary (14 Days)", response.Content);
        Assert.Contains("$4,500.00", response.Content);
        Assert.NotNull(response.ExecutedActions);
        Assert.Contains(response.ExecutedActions!, a => a.PluginName == "ReportingAgentPlugin" && a.FunctionName == "GetFinancialSummaryAsync");
    }

    [Fact]
    public void DependencyInjection_ShouldRegisterReportingAgentPlugin()
    {
        // Arrange
        var services = new ServiceCollection();
        var (dbContext, _) = CreateTestDbContext();
        services.AddScoped<IApplicationDbContext>(_ => dbContext);
        services.AddLogging();
        services.AddAgentEngine();

        var provider = services.BuildServiceProvider();

        // Act
        var plugin = provider.GetService<ReportingAgentPlugin>();

        // Assert
        Assert.NotNull(plugin);
    }
}
