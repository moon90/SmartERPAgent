using SmartErpAgent.Core.Interfaces;

namespace SmartErpAgent.Core.Entities;

public class InventoryItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public int ReorderThreshold { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
}
