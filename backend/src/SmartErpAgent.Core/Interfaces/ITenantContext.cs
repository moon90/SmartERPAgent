namespace SmartErpAgent.Core.Interfaces;

public interface ITenantContext
{
    Guid? CurrentTenantId { get; }
    string? TenantCode { get; }
    void SetTenant(Guid tenantId, string? tenantCode = null);
}
