using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using SmartErpAgent.Application.Common.Interfaces;

namespace SmartErpAgent.AgentEngine.Plugins;

public class InventoryAgentPlugin
{
    private readonly IApplicationDbContext _dbContext;

    public InventoryAgentPlugin(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [KernelFunction, Description("Checks current warehouse stock quantity and availability status for a specific product SKU.")]
    public async Task<string> CheckStockLevelAsync(
        [Description("The unique Stock Keeping Unit (SKU) identifier of the product, e.g., 'SKU-123' or 'WIDGET-01'")] string sku,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "SKU parameter cannot be empty."
            });
        }

        var cleanSku = sku.Trim().ToLower();
        var item = await _dbContext.InventoryItems
            .FirstOrDefaultAsync(i => i.IsActive && i.SKU.ToLower() == cleanSku, cancellationToken);

        if (item == null)
        {
            return JsonSerializer.Serialize(new
            {
                SKU = sku,
                Found = false,
                Message = $"Product with SKU '{sku}' was not found in the inventory catalog."
            });
        }

        var isLowStock = item.StockQuantity <= item.ReorderThreshold;
        var status = isLowStock ? "Low Stock (Reorder Needed)" : "In Stock";

        return JsonSerializer.Serialize(new
        {
            Found = true,
            item.Id,
            item.SKU,
            item.Name,
            item.StockQuantity,
            item.ReorderThreshold,
            IsLowStock = isLowStock,
            Status = status,
            item.UnitPrice
        });
    }

    [KernelFunction, Description("Lists inventory items that are currently below or at their reorder threshold.")]
    public async Task<string> GetLowStockAlertsAsync(CancellationToken cancellationToken = default)
    {
        var lowStockItems = await _dbContext.InventoryItems
            .Where(item => item.IsActive && item.StockQuantity <= item.ReorderThreshold)
            .OrderBy(item => item.StockQuantity)
            .Select(item => new
            {
                item.Id,
                item.SKU,
                item.Name,
                item.StockQuantity,
                item.ReorderThreshold,
                Deficit = item.ReorderThreshold - item.StockQuantity,
                item.UnitPrice
            })
            .ToListAsync(cancellationToken);

        return JsonSerializer.Serialize(lowStockItems);
    }

    [KernelFunction, Description("Searches for an inventory item by SKU or product name.")]
    public async Task<string> QueryInventoryAsync(
        [Description("SKU or keyword to search for")] string query,
        CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.InventoryItems
            .Where(item => item.IsActive && (item.SKU.Contains(query) || item.Name.Contains(query)))
            .Take(10)
            .Select(item => new
            {
                item.Id,
                item.SKU,
                item.Name,
                item.Description,
                item.StockQuantity,
                item.ReorderThreshold,
                item.UnitPrice,
                IsLowStock = item.StockQuantity <= item.ReorderThreshold
            })
            .ToListAsync(cancellationToken);

        return JsonSerializer.Serialize(items);
    }
}
