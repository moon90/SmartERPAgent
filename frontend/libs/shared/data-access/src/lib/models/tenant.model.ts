export interface Tenant {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly adminEmail: string;
  readonly subscriptionTier: string;
  readonly isActive: boolean;
  readonly createdAtUtc: string;
}

export interface CreateTenantRequest {
  readonly code: string;
  readonly name: string;
  readonly adminEmail: string;
  readonly subscriptionTier?: string;
}
