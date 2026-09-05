using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartErpAgent.Application.Common.Interfaces;
using SmartErpAgent.Application.DTOs;
using SmartErpAgent.Core.Entities;
using SmartErpAgent.Core.Interfaces;

namespace SmartErpAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        IApplicationDbContext dbContext,
        ITenantContext tenantContext,
        ILogger<InventoryController> logger)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<InventoryItemDto>>> GetInventory(CancellationToken cancellationToken)
    {
        if (!_tenantContext.CurrentTenantId.HasValue)
            return BadRequest(new { Message = "Header 'X-Tenant-ID' is required to access inventory." });

        var items = await _dbContext.InventoryItems
            .OrderBy(item => item.Name)
            .Select(item => MapToDto(item))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<List<InventoryItemDto>>> GetLowStockItems(CancellationToken cancellationToken)
    {
        if (!_tenantContext.CurrentTenantId.HasValue)
            return BadRequest(new { Message = "Header 'X-Tenant-ID' is required to access inventory." });

        var items = await _dbContext.InventoryItems
            .Where(item => item.IsActive && item.StockQuantity <= item.ReorderThreshold)
            .OrderBy(item => item.StockQuantity)
            .Select(item => MapToDto(item))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<InventoryItemDto>> CreateItem(
        [FromBody] CreateInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantContext.CurrentTenantId.HasValue)
            return BadRequest(new { Message = "Header 'X-Tenant-ID' is required to create inventory items." });

        var sku = request.SKU.Trim().ToUpperInvariant();
        var exists = await _dbContext.InventoryItems
            .AnyAsync(i => i.SKU == sku, cancellationToken);

        if (exists)
            return BadRequest(new { Message = $"An item with SKU '{sku}' already exists in this tenant." });

        var item = new InventoryItem
        {
            TenantId = _tenantContext.CurrentTenantId.Value,
            SKU = sku,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            UnitPrice = request.UnitPrice,
            StockQuantity = request.StockQuantity,
            ReorderThreshold = request.ReorderThreshold,
            IsActive = true
        };

        _dbContext.InventoryItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Inventory item {SKU} created for Tenant {TenantId}", item.SKU, item.TenantId);

        return Ok(MapToDto(item));
    }

    [HttpPatch("{id:guid}/stock")]
    public async Task<ActionResult<InventoryItemDto>> UpdateStock(
        Guid id,
        [FromBody] UpdateStockRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantContext.CurrentTenantId.HasValue)
            return BadRequest(new { Message = "Header 'X-Tenant-ID' is required to update stock." });

        var item = await _dbContext.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (item == null)
            return NotFound(new { Message = $"Inventory item with ID {id} was not found." });

        item.StockQuantity += request.QuantityDelta;
        if (item.StockQuantity < 0) item.StockQuantity = 0;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Stock updated for {SKU} by delta {Delta}. New level: {Level}. Reason: {Reason}",
            item.SKU, request.QuantityDelta, item.StockQuantity, request.Reason);

        return Ok(MapToDto(item));
    }

    private static InventoryItemDto MapToDto(InventoryItem item) => new(
        item.Id,
        item.TenantId,
        item.SKU,
        item.Name,
        item.Description,
        item.UnitPrice,
        item.StockQuantity,
        item.ReorderThreshold,
        item.IsActive,
        item.StockQuantity <= item.ReorderThreshold
    );
}
