export interface InventoryItem {
  readonly id: string;
  readonly tenantId: string;
  readonly sku: string;
  readonly name: string;
  readonly description: string;
  readonly unitPrice: number;
  readonly stockQuantity: number;
  readonly reorderThreshold: number;
  readonly isActive: boolean;
  readonly isLowStock: boolean;
}

export interface CreateInventoryItemRequest {
  readonly sku: string;
  readonly name: string;
  readonly description: string;
  readonly unitPrice: number;
  readonly stockQuantity: number;
  readonly reorderThreshold: number;
}

export interface UpdateStockRequest {
  readonly quantityDelta: number;
  readonly reason: string;
}
