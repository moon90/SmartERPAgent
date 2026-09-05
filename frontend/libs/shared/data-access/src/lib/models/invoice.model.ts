export enum InvoiceStatus {
  Draft = 0,
  Sent = 1,
  Paid = 2,
  Overdue = 3,
  Cancelled = 4,
}

export interface InvoiceLineItem {
  readonly id: string;
  readonly inventoryItemId?: string;
  readonly description: string;
  readonly quantity: number;
  readonly unitPrice: number;
  readonly totalPrice: number;
}

export interface Invoice {
  readonly id: string;
  readonly tenantId: string;
  readonly invoiceNumber: string;
  readonly customerName: string;
  readonly customerEmail: string;
  readonly issueDate: string;
  readonly dueDate: string;
  readonly subTotal: number;
  readonly taxAmount: number;
  readonly totalAmount: number;
  readonly currency: string;
  readonly status: InvoiceStatus;
  readonly notes?: string;
  readonly lineItems: readonly InvoiceLineItem[];
}

export interface CreateInvoiceRequest {
  readonly customerName: string;
  readonly customerEmail: string;
  readonly dueDate: string;
  readonly currency: string;
  readonly notes?: string;
  readonly lineItems: ReadonlyArray<{
    readonly inventoryItemId?: string;
    readonly description: string;
    readonly quantity: number;
    readonly unitPrice: number;
  }>;
}
