using SmartErpAgent.Core.Interfaces;

namespace SmartErpAgent.Core.Entities;

public class PurchaseOrderLineItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid? InventoryItemId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    // Navigation properties
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public InventoryItem? InventoryItem { get; set; }
}
