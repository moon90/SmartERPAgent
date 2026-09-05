using Microsoft.EntityFrameworkCore;
using SmartErpAgent.Core.Entities;

namespace SmartErpAgent.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLineItem> InvoiceLineItems { get; }
    DbSet<InventoryItem> InventoryItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
