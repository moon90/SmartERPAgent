using SmartErpAgent.Core.Interfaces;

namespace SmartErpAgent.Api.Middlewares;

public class MultiTenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MultiTenantMiddleware> _logger;

    public const string TenantHeaderKey = "X-Tenant-ID";
    public const string TenantCodeHeaderKey = "X-Tenant-Code";

    public MultiTenantMiddleware(RequestDelegate next, ILogger<MultiTenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // 1. Check for X-Tenant-ID header
        if (context.Request.Headers.TryGetValue(TenantHeaderKey, out var headerValues) &&
            Guid.TryParse(headerValues.FirstOrDefault(), out var tenantId))
        {
            string? tenantCode = null;
            if (context.Request.Headers.TryGetValue(TenantCodeHeaderKey, out var codeValues))
            {
                tenantCode = codeValues.FirstOrDefault();
            }

            tenantContext.SetTenant(tenantId, tenantCode);
            _logger.LogDebug("Resolved Tenant: {TenantId} (Code: {TenantCode}) from HTTP headers", tenantId, tenantCode);
        }
        // 2. Query param fallback for convenient developer testing
        else if (context.Request.Query.TryGetValue("tenantId", out var queryValues) &&
                 Guid.TryParse(queryValues.FirstOrDefault(), out var queryTenantId))
        {
            tenantContext.SetTenant(queryTenantId);
            _logger.LogDebug("Resolved Tenant: {TenantId} from query string", queryTenantId);
        }

        await _next(context);
    }
}

public static class MultiTenantMiddlewareExtensions
{
    public static IApplicationBuilder UseMultiTenant(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MultiTenantMiddleware>();
    }
}
