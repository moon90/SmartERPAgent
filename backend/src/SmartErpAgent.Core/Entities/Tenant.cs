namespace SmartErpAgent.Core.Entities;

public class Tenant : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = "Standard";
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
