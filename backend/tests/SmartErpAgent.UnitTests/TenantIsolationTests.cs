using Microsoft.EntityFrameworkCore;
using SmartErpAgent.Core.Entities;
using SmartErpAgent.Core.Enums;
using SmartErpAgent.Infrastructure.Persistence;
using SmartErpAgent.Infrastructure.Tenancy;
using Xunit;

namespace SmartErpAgent.UnitTests;

public class TenantIsolationTests
{
    [Fact]
    public async Task QueryFilter_ShouldStrictlyIsolateDataBetweenTenants()
    {
        // Arrange
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();

        var tenantContext = new TenantContext();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Seed data without tenant filter (admin/system scope)
        using (var seedContext = new ApplicationDbContext(options, tenantContext))
        {
            var tenantA = new Tenant { Id = tenantAId, Code = "TENANT_A", Name = "Company A", AdminEmail = "a@company.com" };
            var tenantB = new Tenant { Id = tenantBId, Code = "TENANT_B", Name = "Company B", AdminEmail = "b@company.com" };

            var invoiceA = new Invoice
            {
                TenantId = tenantAId,
                InvoiceNumber = "INV-A-001",
                CustomerName = "Client A",
                TotalAmount = 500m,
                Status = InvoiceStatus.Sent
            };

            var invoiceB = new Invoice
            {
                TenantId = tenantBId,
                InvoiceNumber = "INV-B-001",
                CustomerName = "Client B",
                TotalAmount = 1500m,
                Status = InvoiceStatus.Paid
            };

            seedContext.Tenants.AddRange(tenantA, tenantB);
            seedContext.Invoices.AddRange(invoiceA, invoiceB);
            await seedContext.SaveChangesAsync();
        }

        // Act & Assert for Tenant A
        tenantContext.SetTenant(tenantAId, "TENANT_A");
        using (var contextA = new ApplicationDbContext(options, tenantContext))
        {
            var invoices = await contextA.Invoices.ToListAsync();
            Assert.Single(invoices);
            Assert.Equal("INV-A-001", invoices[0].InvoiceNumber);
            Assert.Equal(tenantAId, invoices[0].TenantId);
        }

        // Act & Assert for Tenant B
        tenantContext.SetTenant(tenantBId, "TENANT_B");
        using (var contextB = new ApplicationDbContext(options, tenantContext))
        {
            var invoices = await contextB.Invoices.ToListAsync();
            Assert.Single(invoices);
            Assert.Equal("INV-B-001", invoices[0].InvoiceNumber);
            Assert.Equal(tenantBId, invoices[0].TenantId);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldAutomaticallyAssignCurrentTenantId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(tenantId, "ACME");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options, tenantContext);

        var item = new InventoryItem
        {
            SKU = "WIDGET-01",
            Name = "Super Widget",
            UnitPrice = 25.50m,
            StockQuantity = 100,
            ReorderThreshold = 10
        };

        // Act
        context.InventoryItems.Add(item);
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(tenantId, item.TenantId);
        Assert.NotEqual(default, item.CreatedAtUtc);
    }

    [Fact]
    public async Task DeleteAsync_ShouldPerformSoftDelete_AndExcludeFromQueries()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(tenantId, "ACME");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var itemId = Guid.NewGuid();
        using (var context = new ApplicationDbContext(options, tenantContext))
        {
            var item = new InventoryItem
            {
                Id = itemId,
                TenantId = tenantId,
                SKU = "WIDGET-DEL",
                Name = "Widget To Delete",
                UnitPrice = 10m,
                StockQuantity = 50,
                ReorderThreshold = 5
            };
            context.InventoryItems.Add(item);
            await context.SaveChangesAsync();
        }

        // Act - Soft Delete
        using (var context = new ApplicationDbContext(options, tenantContext))
        {
            var item = await context.InventoryItems.FindAsync(itemId);
            Assert.NotNull(item);
            context.InventoryItems.Remove(item);
            await context.SaveChangesAsync();
        }

        // Assert - Query excludes soft-deleted item
        using (var context = new ApplicationDbContext(options, tenantContext))
        {
            var items = await context.InventoryItems.ToListAsync();
            Assert.Empty(items);

            // Assert - IgnoreQueryFilters retrieves it with IsDeleted = true
            var deletedItem = await context.InventoryItems.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == itemId);
            Assert.NotNull(deletedItem);
            Assert.True(deletedItem.IsDeleted);
        }
    }
}
