using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using SmartErpAgent.Application.Common.Interfaces;
using SmartErpAgent.Core.Entities;
using SmartErpAgent.Core.Enums;

namespace SmartErpAgent.AgentEngine.Plugins;

public class InvoiceAgentPlugin
{
    private readonly IApplicationDbContext _dbContext;

    public InvoiceAgentPlugin(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [KernelFunction, Description("Retrieves recent invoices for the current tenant context.")]
    public async Task<string> GetRecentInvoicesAsync(
        [Description("Maximum number of invoices to return (default is 5)")] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var invoices = await _dbContext.Invoices
            .Include(i => i.LineItems)
            .OrderByDescending(i => i.CreatedAtUtc)
            .Take(limit)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.CustomerName,
                Status = i.Status.ToString(),
                i.TotalAmount,
                i.Currency,
                i.DueDate,
                LineItemCount = i.LineItems.Count
            })
            .ToListAsync(cancellationToken);

        return JsonSerializer.Serialize(invoices);
    }

    [KernelFunction, Description("Provides an aggregate summary of unpaid and overdue invoices for the tenant.")]
    public async Task<string> GetInvoiceFinancialSummaryAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await _dbContext.Invoices
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var totalOutstanding = invoices.Sum(i => i.TotalAmount);
        var overdueCount = invoices.Count(i => i.DueDate < DateTime.UtcNow);

        var summary = new
        {
            TotalOutstandingAmount = totalOutstanding,
            UnpaidInvoiceCount = invoices.Count,
            OverdueInvoiceCount = overdueCount,
            Currency = invoices.FirstOrDefault()?.Currency ?? "USD"
        };

        return JsonSerializer.Serialize(summary);
    }

    [KernelFunction, Description("Creates a new draft invoice with a line item for a customer.")]
    public async Task<string> CreateDraftInvoiceAsync(
        [Description("Name of the customer")] string customerName,
        [Description("Email address of the customer")] string customerEmail,
        [Description("Item or service description")] string itemDescription,
        [Description("Quantity ordered")] int quantity,
        [Description("Unit price per item")] decimal unitPrice,
        CancellationToken cancellationToken = default)
    {
        var subTotal = quantity * unitPrice;
        var tax = subTotal * 0.10m; // standard 10% tax baseline
        var total = subTotal + tax;

        var count = await _dbContext.Invoices.CountAsync(cancellationToken);
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMM}-{count + 1:D4}";

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(14),
            SubTotal = subTotal,
            TaxAmount = tax,
            TotalAmount = total,
            Status = InvoiceStatus.Draft,
            Notes = "Generated automatically by Smart ERP Agent.",
            LineItems = new List<InvoiceLineItem>
            {
                new InvoiceLineItem
                {
                    Description = itemDescription,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = subTotal
                }
            }
        };

        _dbContext.Invoices.Add(invoice);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return JsonSerializer.Serialize(new
        {
            Success = true,
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            invoice.TotalAmount,
            Message = $"Draft invoice {invoiceNumber} created successfully."
        });
    }
}
