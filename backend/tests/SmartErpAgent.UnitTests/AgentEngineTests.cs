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
using SmartErpAgent.Core.Interfaces;
using SmartErpAgent.Infrastructure.Persistence;
using SmartErpAgent.Infrastructure.Tenancy;
using Xunit;

using Microsoft.AspNetCore.SignalR;
using SmartErpAgent.Application.Hubs;

namespace SmartErpAgent.UnitTests;

public class AgentEngineTests
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
    public async Task CheckStockLevelAsync_ShouldReturnStockQuantity_ForValidSku()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        var item = new InventoryItem
        {
            TenantId = tenantId,
            SKU = "SKU-123",
            Name = "Precision Ball Bearing",
            UnitPrice = 45.00m,
            StockQuantity = 120,
            ReorderThreshold = 20,
            IsActive = true
        };
        dbContext.InventoryItems.Add(item);
        await dbContext.SaveChangesAsync();

        var plugin = new InventoryAgentPlugin(dbContext);

        // Act
        var resultJson = await plugin.CheckStockLevelAsync("SKU-123");

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Found").GetBoolean());
        Assert.Equal("SKU-123", root.GetProperty("SKU").GetString());
        Assert.Equal("Precision Ball Bearing", root.GetProperty("Name").GetString());
        Assert.Equal(120, root.GetProperty("StockQuantity").GetInt32());
        Assert.Equal("In Stock", root.GetProperty("Status").GetString());
        Assert.False(root.GetProperty("IsLowStock").GetBoolean());
    }

    [Fact]
    public async Task CheckStockLevelAsync_ShouldIndicateLowStock_WhenQuantityBelowThreshold()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "TEST_CORP");

        var item = new InventoryItem
        {
            TenantId = tenantId,
            SKU = "WIDGET-LOW",
            Name = "Low Stock Widget",
            UnitPrice = 15.00m,
            StockQuantity = 5,
            ReorderThreshold = 25,
            IsActive = true
        };
        dbContext.InventoryItems.Add(item);
        await dbContext.SaveChangesAsync();

        var plugin = new InventoryAgentPlugin(dbContext);

        // Act
        var resultJson = await plugin.CheckStockLevelAsync("WIDGET-LOW");

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Found").GetBoolean());
        Assert.Equal(5, root.GetProperty("StockQuantity").GetInt32());
        Assert.True(root.GetProperty("IsLowStock").GetBoolean());
        Assert.Contains("Low Stock", root.GetProperty("Status").GetString());
    }

    [Fact]
    public async Task CheckStockLevelAsync_ShouldReturnNotFound_ForNonExistentSku()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        tenantContext.SetTenant(Guid.NewGuid(), "TEST_CORP");

        var plugin = new InventoryAgentPlugin(dbContext);

        // Act
        var resultJson = await plugin.CheckStockLevelAsync("NON-EXISTENT-SKU");

        // Assert
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Found").GetBoolean());
        Assert.Contains("not found", root.GetProperty("Message").GetString());
    }

    [Fact]
    public async Task CheckStockLevelAsync_ShouldEnforceTenantIsolation()
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
                SKU = "SECRET-SKU",
                Name = "Proprietary Part",
                StockQuantity = 50,
                ReorderThreshold = 10,
                UnitPrice = 99m,
                IsActive = true
            });
            await contextA.SaveChangesAsync();
        }

        // Act - Query as Tenant B
        tenantContext.SetTenant(tenantBId, "TENANT_B");
        using (var contextB = new ApplicationDbContext(options, tenantContext))
        {
            var plugin = new InventoryAgentPlugin(contextB);
            var resultJson = await plugin.CheckStockLevelAsync("SECRET-SKU");

            // Assert
            using var doc = JsonDocument.Parse(resultJson);
            Assert.False(doc.RootElement.GetProperty("Found").GetBoolean());
        }
    }

    [Fact]
    public async Task Orchestrator_ShouldExecuteStockCheckPrompt_AndReturnGroundedAnswer()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "ACME_IND");

        dbContext.InventoryItems.Add(new InventoryItem
        {
            TenantId = tenantId,
            SKU = "SKU-123",
            Name = "Heavy Duty Motor",
            StockQuantity = 88,
            ReorderThreshold = 10,
            UnitPrice = 250m,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var config = new ConfigurationBuilder().Build();
        var invoicePlugin = new InvoiceAgentPlugin(dbContext);
        var inventoryPlugin = new InventoryAgentPlugin(dbContext);
        var orchestrator = new SemanticKernelAgentOrchestrator(
            config,
            NullLogger<SemanticKernelAgentOrchestrator>.Instance,
            invoicePlugin,
            inventoryPlugin);

        // Act
        var answer = await orchestrator.ExecutePromptAsync("Do we have enough stock for SKU-123?");

        // Assert
        Assert.NotNull(answer);
        Assert.Contains("SKU-123", answer);
        Assert.Contains("88 units", answer);
        Assert.Contains("Heavy Duty Motor", answer);
    }

    [Fact]
    public async Task AddAgentEngine_ShouldResolveServicesFromContainer()
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

        var orchestrator = provider.GetService<IAgentOrchestrator>();
        var inventoryPlugin = provider.GetService<InventoryAgentPlugin>();
        var invoicePlugin = provider.GetService<InvoiceAgentPlugin>();

        // Assert
        Assert.NotNull(orchestrator);
        Assert.NotNull(inventoryPlugin);
        Assert.NotNull(invoicePlugin);
    }

    [Fact]
    public async Task ExecutePromptAsync_ShouldStreamThoughtProcess_WhenHubContextProvided()
    {
        // Arrange
        var (dbContext, tenantContext) = CreateTestDbContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId, "STREAM_CORP");

        var item = new InventoryItem
        {
            TenantId = tenantId,
            SKU = "SKU-999",
            Name = "Hydraulic Pump",
            UnitPrice = 500m,
            StockQuantity = 45,
            ReorderThreshold = 10,
            IsActive = true
        };
        dbContext.InventoryItems.Add(item);
        await dbContext.SaveChangesAsync();

        var config = new ConfigurationBuilder().Build();
        var invoicePlugin = new InvoiceAgentPlugin(dbContext);
        var inventoryPlugin = new InventoryAgentPlugin(dbContext);
        var testHub = new TestHubContext();

        var orchestrator = new SemanticKernelAgentOrchestrator(
            config,
            NullLogger<SemanticKernelAgentOrchestrator>.Instance,
            invoicePlugin,
            inventoryPlugin,
            testHub);

        // Act
        var answer = await orchestrator.ExecutePromptAsync("Do we have enough stock for SKU-999?");

        // Assert
        Assert.NotNull(answer);
        Assert.Contains("SKU-999", answer);
        Assert.NotEmpty(testHub.StreamedThoughts);
        Assert.Contains(testHub.StreamedThoughts, t => t.Contains("SKU-999"));
        Assert.Contains(testHub.StreamedThoughts, t => t.Contains("Synthesizing final ERP response"));
    }

    private class TestHubContext : IHubContext<AgentHub>
    {
        public List<string> StreamedThoughts { get; } = new();
        public IHubClients Clients => new TestHubClients(StreamedThoughts);
        public IGroupManager Groups => throw new NotImplementedException();
    }

    private class TestHubClients : IHubClients
    {
        private readonly List<string> _thoughts;
        public TestHubClients(List<string> thoughts) => _thoughts = thoughts;

        public IClientProxy All => new TestClientProxy(_thoughts);
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Client(string connectionId) => throw new NotImplementedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotImplementedException();
        public IClientProxy Group(string groupName) => throw new NotImplementedException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotImplementedException();
        public IClientProxy User(string userId) => throw new NotImplementedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotImplementedException();
    }

    private class TestClientProxy : IClientProxy
    {
        private readonly List<string> _thoughts;
        public TestClientProxy(List<string> thoughts) => _thoughts = thoughts;

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            if (method == "ReceiveThoughtProcess" && args.Length > 0 && args[0] is string msg)
            {
                _thoughts.Add(msg);
            }
            return Task.CompletedTask;
        }
    }
}
