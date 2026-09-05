import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Tenant, CreateTenantRequest } from '../models/tenant.model';
import { API_BASE_URL } from '../config/api.config';

@Injectable({
  providedIn: 'root',
})
export class TenantService {
  private readonly apiUrl = `${API_BASE_URL}/api/tenants`;

  // Reactive state signals
  readonly tenants = signal<readonly Tenant[]>([]);
  readonly currentTenant = signal<Tenant | null>(null);
  readonly currentTenantId = computed(() => this.currentTenant()?.id ?? null);
  readonly currentTenantCode = computed(() => this.currentTenant()?.code ?? null);
  readonly isLoading = signal<boolean>(false);

  constructor(private readonly http: HttpClient) {}

  loadTenants(): Observable<Tenant[]> {
    this.isLoading.set(true);
    return this.http.get<Tenant[]>(this.apiUrl).pipe(
      tap((loadedTenants) => {
        this.tenants.set(loadedTenants);
        this.isLoading.set(false);
        if (!this.currentTenant() && loadedTenants.length > 0) {
          this.currentTenant.set(loadedTenants[0]);
        }
      })
    );
  }

  setTenant(tenant: Tenant): void {
    this.currentTenant.set(tenant);
  }

  createTenant(request: CreateTenantRequest): Observable<Tenant> {
    return this.http.post<Tenant>(this.apiUrl, request).pipe(
      tap((newTenant) => {
        this.tenants.update((list) => [...list, newTenant]);
        this.currentTenant.set(newTenant);
      })
    );
  }
}
