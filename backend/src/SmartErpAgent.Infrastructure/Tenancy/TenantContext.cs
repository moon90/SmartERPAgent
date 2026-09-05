using SmartErpAgent.Core.Interfaces;

namespace SmartErpAgent.Infrastructure.Tenancy;

public class TenantContext : ITenantContext
{
    public Guid? CurrentTenantId { get; private set; }
    public string? TenantCode { get; private set; }

    public void SetTenant(Guid tenantId, string? tenantCode = null)
    {
        CurrentTenantId = tenantId;
        TenantCode = tenantCode;
    }
}
