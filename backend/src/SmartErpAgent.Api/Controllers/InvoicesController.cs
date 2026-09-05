using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartErpAgent.Application.Common.Interfaces;
using SmartErpAgent.Application.DTOs;
using SmartErpAgent.Core.Entities;
using SmartErpAgent.Core.Enums;
using SmartErpAgent.Core.Interfaces;

namespace SmartErpAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(
        IApplicationDbContext dbContext,
        ITenantContext tenantContext,
        ILogger<InvoicesController> logger)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<InvoiceDto>>> GetInvoices(CancellationToken cancellationToken)
    {
        if (!_tenantContext.CurrentTenantId.HasValue)
            return BadRequest(new { Message = "Header 'X-Tenant-ID' is required to access tenant invoices." });

        var invoices = await _dbContext.Invoices
            .Include(i => i.LineItems)
            .OrderByDescending(i => i.CreatedAtUtc)
            .Select(i => MapToDto(i))
            .ToListAsync(cancellationToken);

        return Ok(invoices);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoiceById(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantContext.CurrentTenantId.HasValue)
            return BadRequest(new { Message = "Header 'X-Tenant-ID' is required to access tenant invoices." });

        var invoice = await _dbContext.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (invoice == null)
            return NotFound(new { Message = $"Invoice with ID {id} was not found for this tenant." });

        return Ok(MapToDto(invoice));
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> CreateInvoice(
        [FromBody] CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantContext.CurrentTenantId.HasValue)
            return BadRequest(new { Message = "Header 'X-Tenant-ID' is required to create invoices." });

        var count = await _dbContext.Invoices.CountAsync(cancellationToken);
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMM}-{count + 1:D4}";

        decimal subTotal = 0;
        var lineItems = new List<InvoiceLineItem>();

        foreach (var itemReq in request.LineItems)
        {
            var totalPrice = itemReq.Quantity * itemReq.UnitPrice;
            subTotal += totalPrice;

            lineItems.Add(new InvoiceLineItem
            {
                InventoryItemId = itemReq.InventoryItemId,
                Description = itemReq.Description,
                Quantity = itemReq.Quantity,
                UnitPrice = itemReq.UnitPrice,
                TotalPrice = totalPrice
            });
        }

        var taxAmount = subTotal * 0.10m; // 10% tax baseline
        var totalAmount = subTotal + taxAmount;

        var invoice = new Invoice
        {
            TenantId = _tenantContext.CurrentTenantId.Value,
            InvoiceNumber = invoiceNumber,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            IssueDate = DateTime.UtcNow,
            DueDate = request.DueDate,
            SubTotal = subTotal,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency,
            Status = InvoiceStatus.Draft,
            Notes = request.Notes,
            LineItems = lineItems
        };

        _dbContext.Invoices.Add(invoice);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Invoice {Number} created for Tenant {TenantId}", invoice.InvoiceNumber, invoice.TenantId);

        return CreatedAtAction(nameof(GetInvoiceById), new { id = invoice.Id }, MapToDto(invoice));
    }

    private static InvoiceDto MapToDto(Invoice i) => new(
        i.Id,
        i.TenantId,
        i.InvoiceNumber,
        i.CustomerName,
        i.CustomerEmail,
        i.IssueDate,
        i.DueDate,
        i.SubTotal,
        i.TaxAmount,
        i.TotalAmount,
        i.Currency,
        i.Status,
        i.Notes,
        i.LineItems.Select(li => new InvoiceLineItemDto(
            li.Id,
            li.InventoryItemId,
            li.Description,
            li.Quantity,
            li.UnitPrice,
            li.TotalPrice)).ToList()
    );
}
