using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using SmartErpAgent.Application.Common.Interfaces;
using SmartErpAgent.Application.DTOs;

namespace SmartErpAgent.AgentEngine.Plugins;

/// <summary>
/// Executive business intelligence and financial reporting native plugin for Microsoft Semantic Kernel.
/// Injects IApplicationDbContext to execute tenant-isolated analytical aggregations.
/// </summary>
public class ReportingAgentPlugin
{
    private readonly IApplicationDbContext _dbContext;

    public ReportingAgentPlugin(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [KernelFunction, Description("Calculates the total monetary holding value of all active inventory items (StockQuantity * UnitPrice) and identifies the top 3 most valuable SKUs for the current tenant organization.")]
    public async Task<string> GetInventoryValuationAsync(CancellationToken cancellationToken = default)
    {
        var activeItems = _dbContext.InventoryItems.Where(i => i.IsActive);

        var totalValuation = await activeItems
            .SumAsync(i => (decimal?)((decimal)i.StockQuantity * i.UnitPrice) ?? 0m, cancellationToken);

        var totalActiveSkuCount = await activeItems.CountAsync(cancellationToken);
        var totalUnitsInStock = await activeItems.SumAsync(i => (int?)i.StockQuantity ?? 0, cancellationToken);

        var topSkus = await activeItems
            .Select(i => new
            {
                i.SKU,
                i.Name,
                i.StockQuantity,
                i.UnitPrice,
                TotalItemValue = (decimal)i.StockQuantity * i.UnitPrice
            })
            .OrderByDescending(i => i.TotalItemValue)
            .ThenBy(i => i.SKU)
            .Take(3)
            .ToListAsync(cancellationToken);

        var report = new InventoryValuationReportDto(
            TotalValuation: totalValuation,
            TotalActiveSkuCount: totalActiveSkuCount,
            TotalUnitsInStock: totalUnitsInStock,
            TopValuableSkus: topSkus.Select(s => new TopValuableSkuDto(
                s.SKU,
                s.Name,
                s.StockQuantity,
                s.UnitPrice,
                s.TotalItemValue
            )).ToList()
        );

        return JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
    }

    [KernelFunction, Description("Calculates total invoice revenue, invoice count, and currency breakdown issued within a specific number of past calendar days (e.g. 30, 60, 90). Defaults to 30 days if not specified.")]
    public async Task<string> GetFinancialSummaryAsync(
        [Description("Number of past calendar days to evaluate (e.g. 7, 30, 60, 90). Defaults to 30 if not specified or non-positive.")] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var normalizedDays = days <= 0 ? 30 : days;
        var nowUtc = DateTime.UtcNow;
        var startDateUtc = nowUtc.AddDays(-normalizedDays);

        var periodInvoices = _dbContext.Invoices
            .Where(inv => inv.IssueDate >= startDateUtc);

        var totalRevenue = await periodInvoices
            .SumAsync(inv => (decimal?)inv.TotalAmount ?? 0m, cancellationToken);

        var invoiceCount = await periodInvoices.CountAsync(cancellationToken);

        var primaryCurrency = await periodInvoices
            .Select(inv => inv.Currency)
            .FirstOrDefaultAsync(cancellationToken) ?? "USD";

        var summary = new FinancialPeriodSummaryDto(
            PeriodDays: normalizedDays,
            StartDateUtc: startDateUtc,
            EndDateUtc: nowUtc,
            TotalRevenue: totalRevenue,
            InvoiceCount: invoiceCount,
            Currency: primaryCurrency
        );

        return JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
    }
}
