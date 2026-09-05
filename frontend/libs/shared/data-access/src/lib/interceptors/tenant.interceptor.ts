import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TenantService } from '../services/tenant.service';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const tenantService = inject(TenantService);
  const tenantId = tenantService.currentTenantId();
  const tenantCode = tenantService.currentTenantCode();

  let modifiedReq = req;

  if (tenantId) {
    let headers = req.headers.set('X-Tenant-ID', tenantId);
    if (tenantCode) {
      headers = headers.set('X-Tenant-Code', tenantCode);
    }
    modifiedReq = req.clone({ headers });
  }

  return next(modifiedReq);
};
