import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TenantService, Tenant } from '@smart-erp/data-access';

@Component({
  selector: 'smart-erp-tenant-selector',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="tenant-selector-container">
      <div class="tenant-badge-icon">
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <path d="M3 21h18"/>
          <path d="M9 8h1"/>
          <path d="M9 12h1"/>
          <path d="M9 16h1"/>
          <path d="M14 8h1"/>
          <path d="M14 12h1"/>
          <path d="M14 16h1"/>
          <path d="M5 21V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v16"/>
        </svg>
      </div>

      <div class="selector-content">
        <span class="label">ACTIVE TENANT</span>
        <select
          class="tenant-dropdown"
          [ngModel]="tenantService.currentTenantId()"
          (ngModelChange)="onTenantChange($event)"
        >
          <option [ngValue]="null" disabled>Select Tenant Context...</option>
          @for (tenant of tenantService.tenants(); track tenant.id) {
            <option [value]="tenant.id">{{ tenant.name }} ({{ tenant.code }})</option>
          }
        </select>
      </div>
    </div>
  `,
  styles: [`
    .tenant-selector-container {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 8px 16px;
      background: rgba(30, 41, 59, 0.7);
      border: 1px solid rgba(148, 163, 184, 0.2);
      border-radius: 10px;
      backdrop-filter: blur(8px);
    }

    .tenant-badge-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      border-radius: 8px;
      background: rgba(99, 102, 241, 0.2);
      color: #818cf8;
    }

    .selector-content {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .label {
      font-size: 0.65rem;
      font-weight: 700;
      color: #94a3b8;
      letter-spacing: 0.05em;
    }

    .tenant-dropdown {
      background: transparent;
      border: none;
      color: #f8fafc;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      outline: none;

      option {
        background: #0f172a;
        color: #f8fafc;
      }
    }
  `]
})
export class TenantSelectorComponent {
  readonly tenantService = inject(TenantService);

  onTenantChange(tenantId: string): void {
    const selected = this.tenantService.tenants().find(t => t.id === tenantId);
    if (selected) {
      this.tenantService.setTenant(selected);
    }
  }
}
