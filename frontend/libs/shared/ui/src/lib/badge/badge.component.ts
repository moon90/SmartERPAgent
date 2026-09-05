import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'smart-erp-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span class="erp-badge" [ngClass]="badgeClass">
      <span class="indicator-dot"></span>
      <ng-content></ng-content>
    </span>
  `,
  styles: [`
    .erp-badge {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 3px 10px;
      border-radius: 9999px;
      font-size: 0.75rem;
      font-weight: 600;
      letter-spacing: 0.025em;
      text-transform: uppercase;
    }

    .indicator-dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
    }

    .success {
      background: rgba(16, 185, 129, 0.15);
      color: #10b981;
      .indicator-dot { background: #10b981; }
    }

    .warning {
      background: rgba(245, 158, 11, 0.15);
      color: #f59e0b;
      .indicator-dot { background: #f59e0b; }
    }

    .danger {
      background: rgba(239, 68, 68, 0.15);
      color: #ef4444;
      .indicator-dot { background: #ef4444; }
    }

    .neutral {
      background: rgba(100, 116, 139, 0.15);
      color: #94a3b8;
      .indicator-dot { background: #94a3b8; }
    }

    .primary {
      background: rgba(99, 102, 241, 0.15);
      color: #818cf8;
      .indicator-dot { background: #818cf8; }
    }
  `]
})
export class BadgeComponent {
  @Input() variant: 'success' | 'warning' | 'danger' | 'neutral' | 'primary' = 'neutral';

  get badgeClass(): string {
    return this.variant;
  }
}
