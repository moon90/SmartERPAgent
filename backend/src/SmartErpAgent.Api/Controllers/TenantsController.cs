using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartErpAgent.Application.Common.Interfaces;
using SmartErpAgent.Application.DTOs;
using SmartErpAgent.Core.Entities;

namespace SmartErpAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(IApplicationDbContext dbContext, ILogger<TenantsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<TenantDto>>> GetTenants(CancellationToken cancellationToken)
    {
        var tenants = await _dbContext.Tenants
            .OrderBy(t => t.Name)
            .Select(t => new TenantDto(
                t.Id,
                t.Code,
                t.Name,
                t.AdminEmail,
                t.SubscriptionTier,
                t.IsActive,
                t.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(tenants);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantDto>> GetTenantById(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant == null)
            return NotFound(new { Message = $"Tenant with ID {id} was not found." });

        return Ok(new TenantDto(
            tenant.Id,
            tenant.Code,
            tenant.Name,
            tenant.AdminEmail,
            tenant.SubscriptionTier,
            tenant.IsActive,
            tenant.CreatedAtUtc));
    }

    [HttpPost]
    public async Task<ActionResult<TenantDto>> CreateTenant([FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Tenants
            .AnyAsync(t => t.Code == request.Code, cancellationToken);

        if (existing)
            return BadRequest(new { Message = $"Tenant with Code '{request.Code}' already exists." });

        var tenant = new Tenant
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            AdminEmail = request.AdminEmail.Trim(),
            SubscriptionTier = string.IsNullOrWhiteSpace(request.SubscriptionTier) ? "Standard" : request.SubscriptionTier.Trim(),
            IsActive = true
        };

        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tenant '{Code}' ({Id}) created successfully.", tenant.Code, tenant.Id);

        var dto = new TenantDto(
            tenant.Id,
            tenant.Code,
            tenant.Name,
            tenant.AdminEmail,
            tenant.SubscriptionTier,
            tenant.IsActive,
            tenant.CreatedAtUtc);

        return CreatedAtAction(nameof(GetTenantById), new { id = tenant.Id }, dto);
    }
}
