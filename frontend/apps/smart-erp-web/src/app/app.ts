import { Component, OnInit, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import {
  TenantService,
  ErpApiService,
  Invoice,
  InventoryItem,
  InvoiceStatus
} from '@smart-erp/data-access';
import { TenantSelectorComponent, BadgeComponent } from '@smart-erp/ui';
import { AgentChatComponent } from '@smart-erp/feature-chat';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    TenantSelectorComponent,
    BadgeComponent,
    AgentChatComponent,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  readonly tenantService = inject(TenantService);
  readonly erpApi = inject(ErpApiService);

  readonly activeTab = signal<'invoices' | 'inventory'>('invoices');
  readonly invoices = signal<readonly Invoice[]>([]);
  readonly inventory = signal<readonly InventoryItem[]>([]);
  readonly isDataLoading = signal<boolean>(false);

  readonly InvoiceStatus = InvoiceStatus;

  constructor() {
    // Automatically reload data whenever the user switches tenant in the header dropdown
    effect(() => {
      const tenantId = this.tenantService.currentTenantId();
      if (tenantId) {
        this.refreshData();
      }
    });
  }

  ngOnInit(): void {
    this.tenantService.loadTenants().subscribe({
      next: (tenants) => {
        if (tenants.length > 0 && !this.tenantService.currentTenant()) {
          this.tenantService.setTenant(tenants[0]);
        }
      },
      error: (err) => {
        console.error('Failed to load tenants from backend API:', err);
      }
    });
  }

  setTab(tab: 'invoices' | 'inventory'): void {
    this.activeTab.set(tab);
    this.refreshData();
  }

  refreshData(): void {
    if (!this.tenantService.currentTenantId()) return;

    this.isDataLoading.set(true);
    if (this.activeTab() === 'invoices') {
      this.erpApi.getInvoices().subscribe({
        next: (items) => {
          this.invoices.set(items);
          this.isDataLoading.set(false);
        },
        error: () => this.isDataLoading.set(false)
      });
    } else {
      this.erpApi.getInventory().subscribe({
        next: (items) => {
          this.inventory.set(items);
          this.isDataLoading.set(false);
        },
        error: () => this.isDataLoading.set(false)
      });
    }
  }

  getStatusVariant(status: InvoiceStatus): 'success' | 'warning' | 'danger' | 'neutral' | 'primary' {
    switch (status) {
      case InvoiceStatus.Paid: return 'success';
      case InvoiceStatus.Sent: return 'primary';
      case InvoiceStatus.Overdue: return 'danger';
      case InvoiceStatus.Draft: return 'neutral';
      case InvoiceStatus.Cancelled: return 'warning';
      default: return 'neutral';
    }
  }

  getStatusLabel(status: InvoiceStatus): string {
    return InvoiceStatus[status] ?? 'Unknown';
  }
}
