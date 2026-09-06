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

public class PurchaseOrderPluginTests
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

    #region User Story 1: Low-Stock Detection & Alerting Tests (T008)

    [Fact]
    public async Task GetLowStockAlertsAsync_ShouldReturnItemsAtOrBelowReorderThreshold()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        var item1 = new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SKU = "SKU-LOW-1",
            Name = "Low Stock Item 1",
            StockQuantity = 5,
            ReorderThreshold = 10,
            UnitPrice = 20.00m,
            IsActive = true
        };

        var item2 = new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SKU = "SKU-EXACT-2",
            Name = "Exact Threshold Item 2",
            StockQuantity = 10,
            ReorderThreshold = 10,
            UnitPrice = 50.00m,
            IsActive = true
        };

        var item3 = new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SKU = "SKU-HIGH-3",
            Name = "High Stock Item 3",
            StockQuantity = 25,
            ReorderThreshold = 10,
            UnitPrice = 15.00m,
            IsActive = true
        };

        dbContext.InventoryItems.AddRange(item1, item2, item3);
        await dbContext.SaveChangesAsync();

        var plugin = new PurchaseOrderAgentPlugin(dbContext);

        // Act
        var resultJson = await plugin.GetLowStockAlertsAsync();

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var totalItems = doc.RootElement.GetProperty("TotalLowStockItems").GetInt32();
        var totalCost = doc.RootElement.GetProperty("TotalEstimatedCost").GetDecimal();
        var alerts = doc.RootElement.GetProperty("Alerts");

        Assert.Equal(2, totalItems);

        // Item 1: Threshold 10 * 2 = 20 target. 20 - 5 = 15 suggested. Cost = 15 * 20 = $300.00
        // Item 2: Threshold 10 * 2 = 20 target. 20 - 10 = 10 suggested. Cost = 10 * 50 = $500.00
        // Total cost = $800.00
        Assert.Equal(800.00m, totalCost);
        Assert.Equal(2, alerts.GetArrayLength());

        var alertSkus = alerts.EnumerateArray().Select(a => a.GetProperty("SKU").GetString()).ToList();
        Assert.Contains("SKU-LOW-1", alertSkus);
        Assert.Contains("SKU-EXACT-2", alertSkus);
        Assert.DoesNotContain("SKU-HIGH-3", alertSkus);
    }

    [Fact]
    public async Task GetLowStockAlertsAsync_WhenAllStockIsSufficient_ShouldReturnZeroAlerts()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        dbContext.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SKU = "SKU-SUFFICIENT",
            Name = "Sufficient Stock",
            StockQuantity = 100,
            ReorderThreshold = 20,
            UnitPrice = 10.00m,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var plugin = new PurchaseOrderAgentPlugin(dbContext);

        // Act
        var resultJson = await plugin.GetLowStockAlertsAsync();

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        Assert.Equal(0, doc.RootElement.GetProperty("TotalLowStockItems").GetInt32());
        Assert.Equal(0m, doc.RootElement.GetProperty("TotalEstimatedCost").GetDecimal());
        Assert.Empty(doc.RootElement.GetProperty("Alerts").EnumerateArray());
    }

    [Fact]
    public async Task GetLowStockAlertsAsync_ShouldEnforceStrictTenantIsolation()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // Seed tenant A with low stock
        dbContext.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantA,
            SKU = "SKU-TENANT-A",
            Name = "Tenant A Product",
            StockQuantity = 2,
            ReorderThreshold = 10,
            UnitPrice = 25.00m,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        // Query as tenant B
        tenantContext.SetTenant(tenantB, "TENANT_B");
        var plugin = new PurchaseOrderAgentPlugin(dbContext);

        // Act
        var resultJson = await plugin.GetLowStockAlertsAsync();

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        Assert.Equal(0, doc.RootElement.GetProperty("TotalLowStockItems").GetInt32());
    }

    [Fact]
    public async Task GetLowStockAlertsAsync_ShouldExcludeInactiveAndDeletedItems()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        dbContext.InventoryItems.AddRange(
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SKU = "SKU-INACTIVE",
                Name = "Inactive Product",
                StockQuantity = 0,
                ReorderThreshold = 10,
                UnitPrice = 100.00m,
                IsActive = false
            },
            new InventoryItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SKU = "SKU-DELETED",
                Name = "Deleted Product",
                StockQuantity = 0,
                ReorderThreshold = 10,
                UnitPrice = 100.00m,
                IsActive = true,
                IsDeleted = true
            }
        );
        await dbContext.SaveChangesAsync();

        var plugin = new PurchaseOrderAgentPlugin(dbContext);

        // Act
        var resultJson = await plugin.GetLowStockAlertsAsync();

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        Assert.Equal(0, doc.RootElement.GetProperty("TotalLowStockItems").GetInt32());
    }

    #endregion

    #region User Story 2: Autonomous Draft Purchase Order Generation Tests (T010)

    [Fact]
    public async Task CreateDraftPurchaseOrderAsync_ShouldCreateAndPersistDraftPOWithLineItems()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        var item1 = new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SKU = "SKU-PO-1",
            Name = "Industrial Filter",
            StockQuantity = 4,
            ReorderThreshold = 10,
            UnitPrice = 35.00m,
            IsActive = true
        };

        var item2 = new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SKU = "SKU-PO-2",
            Name = "Gasket Seal",
            StockQuantity = 8,
            ReorderThreshold = 15,
            UnitPrice = 12.50m,
            IsActive = true
        };

        dbContext.InventoryItems.AddRange(item1, item2);
        await dbContext.SaveChangesAsync();

        var plugin = new PurchaseOrderAgentPlugin(dbContext);

        // Act
        var resultJson = await plugin.CreateDraftPurchaseOrderAsync();

        // Assert
        var result = JsonSerializer.Deserialize<DraftPurchaseOrderResultDto>(resultJson);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.PurchaseOrderId);
        Assert.StartsWith("PO-", result.OrderNumber);
        Assert.Equal("Draft", result.Status);
        Assert.Equal("Primary Replenishment Supplier", result.SupplierName);
        Assert.Equal(2, result.LineItemCount);

        // Replenishment quantities:
        // Item 1: (10 * 2) - 4 = 16 units @ $35.00 = $560.00
        // Item 2: (15 * 2) - 8 = 22 units @ $12.50 = $275.00
        // Total amount = $835.00
        Assert.Equal(835.00m, result.TotalAmount);

        // Verify database persistence
        var savedPo = await dbContext.PurchaseOrders
            .Include(po => po.LineItems)
            .FirstOrDefaultAsync(po => po.Id == result.PurchaseOrderId);

        Assert.NotNull(savedPo);
        Assert.Equal(PurchaseOrderStatus.Draft, savedPo.Status);
        Assert.Equal(835.00m, savedPo.TotalAmount);
        Assert.Equal(2, savedPo.LineItems.Count);
        Assert.Equal(tenantId, savedPo.TenantId);
    }

    [Fact]
    public async Task CreateDraftPurchaseOrderAsync_WithSpecificTargetSkus_ShouldOnlyReplenishTargetedSkus()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        var item1 = new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SKU = "SKU-TARGET-A",
            Name = "Target Product A",
            StockQuantity = 2,
            ReorderThreshold = 10,
            UnitPrice = 50.00m,
            IsActive = true
        };

        var item2 = new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SKU = "SKU-TARGET-B",
            Name = "Target Product B",
            StockQuantity = 3,
            ReorderThreshold = 10,
            UnitPrice = 30.00m,
            IsActive = true
        };

        dbContext.InventoryItems.AddRange(item1, item2);
        await dbContext.SaveChangesAsync();

        var plugin = new PurchaseOrderAgentPlugin(dbContext);

        // Act - Only target SKU-TARGET-A
        var resultJson = await plugin.CreateDraftPurchaseOrderAsync("SKU-TARGET-A");

        // Assert
        var result = JsonSerializer.Deserialize<DraftPurchaseOrderResultDto>(resultJson);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(1, result.LineItemCount);
        Assert.Equal("SKU-TARGET-A", result.LineItems[0].SKU);
    }

    [Fact]
    public async Task CreateDraftPurchaseOrderAsync_WhenNoItemsNeedRestocking_ShouldReturnNoOpResult()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        dbContext.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SKU = "SKU-WELL-STOCKED",
            Name = "Well Stocked Item",
            StockQuantity = 100,
            ReorderThreshold = 10,
            UnitPrice = 25.00m,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var plugin = new PurchaseOrderAgentPlugin(dbContext);

        // Act
        var resultJson = await plugin.CreateDraftPurchaseOrderAsync();

        // Assert
        var result = JsonSerializer.Deserialize<DraftPurchaseOrderResultDto>(resultJson);
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Null(result.PurchaseOrderId);
        Assert.Equal(0, result.LineItemCount);
        Assert.Contains("No inventory items require replenishment", result.Message);

        var poCount = await dbContext.PurchaseOrders.CountAsync();
        Assert.Equal(0, poCount);
    }

    [Fact]
    public async Task CreateDraftPurchaseOrderAsync_WithDepletedStock_ShouldHandleZeroOrNegativeStockGracefully()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        dbContext.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SKU = "SKU-DEPLETED",
            Name = "Completely Depleted Item",
            StockQuantity = 0,
            ReorderThreshold = 15,
            UnitPrice = 10.00m,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var plugin = new PurchaseOrderAgentPlugin(dbContext);

        // Act
        var resultJson = await plugin.CreateDraftPurchaseOrderAsync();

        // Assert
        var result = JsonSerializer.Deserialize<DraftPurchaseOrderResultDto>(resultJson);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(1, result.LineItemCount);
        // Target: 15 * 2 = 30 units
        Assert.Equal(30, result.LineItems[0].Quantity);
        Assert.Equal(300.00m, result.TotalAmount);
    }

    [Fact]
    public async Task CreateDraftPurchaseOrderAsync_ShouldEnforceStrictTenantIsolation()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // Tenant A has low stock item
        dbContext.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantA,
            SKU = "SKU-TENANT-A-DEPLETED",
            Name = "Tenant A Product",
            StockQuantity = 1,
            ReorderThreshold = 10,
            UnitPrice = 50.00m,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        // Switch to Tenant B
        tenantContext.SetTenant(tenantB, "TENANT_B");
        var plugin = new PurchaseOrderAgentPlugin(dbContext);

        // Act
        var resultJson = await plugin.CreateDraftPurchaseOrderAsync();

        // Assert
        var result = JsonSerializer.Deserialize<DraftPurchaseOrderResultDto>(resultJson);
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(0, result.LineItemCount);

        // Verify zero purchase orders created for Tenant B or globally
        var poCount = await dbContext.PurchaseOrders.CountAsync();
        Assert.Equal(0, poCount);
    }

    #endregion

    #region User Story 3: Orchestrator Integration & Autonomous Routing Tests (T012)

    [Fact]
    public async Task Orchestrator_WhenPromptAsksWhichItemsLowStock_ShouldRouteToPurchaseOrderAgentPlugin()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        dbContext.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SKU = "SKU-ORCH-LOW",
            Name = "Orchestrator Low Item",
            StockQuantity = 3,
            ReorderThreshold = 10,
            UnitPrice = 40.00m,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var poPlugin = new PurchaseOrderAgentPlugin(dbContext);
        var invPlugin = new InventoryAgentPlugin(dbContext);
        var invcPlugin = new InvoiceAgentPlugin(dbContext);

        var config = new ConfigurationBuilder().Build();
        var orchestrator = new SemanticKernelAgentOrchestrator(
            config,
            NullLogger<SemanticKernelAgentOrchestrator>.Instance,
            invcPlugin,
            invPlugin,
            null,
            null,
            null,
            poPlugin);

        // Act
        var response = await orchestrator.ProcessPromptAsync(new AgentPromptRequestDto("Which items are low in stock?"));

        // Assert
        Assert.Equal("Success", response.Status);
        Assert.Contains("Smart ERP Purchase Order Agent", response.Content);
        Assert.Contains("SKU-ORCH-LOW", response.Content);
        Assert.NotNull(response.ExecutedActions);
        Assert.Contains(response.ExecutedActions!, a => a.PluginName == "PurchaseOrderAgentPlugin" && a.FunctionName == "GetLowStockAlertsAsync");
    }

    [Fact]
    public async Task Orchestrator_WhenPromptAsksToGenerateDraftPO_ShouldRouteToCreateDraftPurchaseOrderAsync()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        dbContext.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SKU = "SKU-PO-GEN",
            Name = "Restock Widget",
            StockQuantity = 2,
            ReorderThreshold = 10,
            UnitPrice = 25.00m,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var poPlugin = new PurchaseOrderAgentPlugin(dbContext);
        var invPlugin = new InventoryAgentPlugin(dbContext);
        var invcPlugin = new InvoiceAgentPlugin(dbContext);

        var config = new ConfigurationBuilder().Build();
        var orchestrator = new SemanticKernelAgentOrchestrator(
            config,
            NullLogger<SemanticKernelAgentOrchestrator>.Instance,
            invcPlugin,
            invPlugin,
            null,
            null,
            null,
            poPlugin);

        // Act
        var response = await orchestrator.ProcessPromptAsync(new AgentPromptRequestDto("Generate draft purchase orders to replenish our inventory"));

        // Assert
        Assert.Equal("Success", response.Status);
        Assert.Contains("Draft Purchase Order", response.Content);
        Assert.NotNull(response.ExecutedActions);
        Assert.Contains(response.ExecutedActions!, a => a.PluginName == "PurchaseOrderAgentPlugin" && a.FunctionName == "CreateDraftPurchaseOrderAsync");

        // Verify a draft PO was created in the database
        var createdPo = await dbContext.PurchaseOrders.FirstOrDefaultAsync();
        Assert.NotNull(createdPo);
        Assert.Equal(PurchaseOrderStatus.Draft, createdPo.Status);
    }

    [Fact]
    public void DependencyInjection_ShouldRegisterPurchaseOrderAgentPlugin()
    {
        // Arrange
        var services = new ServiceCollection();
        var (dbContext, _) = CreateTestDbContext();
        services.AddScoped<IApplicationDbContext>(_ => dbContext);
        services.AddLogging();
        services.AddAgentEngine();

        var provider = services.BuildServiceProvider();

        // Act
        var plugin = provider.GetService<PurchaseOrderAgentPlugin>();

        // Assert
        Assert.NotNull(plugin);
    }

    #endregion
}
