using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartErpAgent.Core.Entities;
using SmartErpAgent.Core.Enums;

namespace SmartErpAgent.Infrastructure.Persistence;

/// <summary>
/// Provides idempotent database seeding for multi-tenant organizations, product inventory, and invoices.
/// </summary>
public static class DatabaseSeeder
{
    public static readonly Guid AcmeTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid GlobalLogisticsTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid BioTechTenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static async Task<SeedResult> SeedAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Checking database seed status for Smart ERP Agent demo tenants...");

        int tenantsAdded = 0;
        int itemsAdded = 0;
        int invoicesAdded = 0;

        // -------------------------------------------------------------
        // TENANT 1: ACME_CORP (Industrial Manufacturing)
        // -------------------------------------------------------------
        if (!await context.Tenants.AnyAsync(t => t.Code == "ACME_CORP", cancellationToken))
        {
            logger.LogInformation("Seeding ACME_CORP tenant and inventory...");
            var acmeTenant = new Tenant
            {
                Id = AcmeTenantId,
                Code = "ACME_CORP",
                Name = "Acme Industrial Corporation",
                AdminEmail = "admin@acme-corp.com",
                SubscriptionTier = "Enterprise",
                IsActive = true
            };

            var acmeItems = new List<InventoryItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    TenantId = AcmeTenantId,
                    SKU = "SKU-101",
                    Name = "High Pressure Hydraulic Control Valve",
                    Description = "Precision industrial valve rated up to 5000 PSI with dual port flow control.",
                    UnitPrice = 125.00m,
                    StockQuantity = 45,
                    ReorderThreshold = 15,
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    TenantId = AcmeTenantId,
                    SKU = "SKU-102",
                    Name = "Steel Flange Gasket (2-inch ANSI)",
                    Description = "Spiral wound graphite filled gasket for high temperature steam pipelines.",
                    UnitPrice = 15.50m,
                    StockQuantity = 120,
                    ReorderThreshold = 30,
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    TenantId = AcmeTenantId,
                    SKU = "SKU-123",
                    Name = "Precision Ball Bearing (ABEC-7 Industrial)",
                    Description = "High speed deep groove bearing with hardened chrome steel balls.",
                    UnitPrice = 45.00m,
                    StockQuantity = 88,
                    ReorderThreshold = 20,
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    TenantId = AcmeTenantId,
                    SKU = "SKU-LOW",
                    Name = "Industrial Lithium Grease Cartridge 400g",
                    Description = "High temperature anti-friction multi-purpose machinery lubricant.",
                    UnitPrice = 12.00m,
                    StockQuantity = 4, // Below reorder threshold (Alert!)
                    ReorderThreshold = 25,
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    TenantId = AcmeTenantId,
                    SKU = "SKU-MOTOR",
                    Name = "Heavy Duty 3-Phase Electric Motor 5HP",
                    Description = "Totally enclosed fan-cooled industrial AC motor 1750 RPM.",
                    UnitPrice = 850.00m,
                    StockQuantity = 12,
                    ReorderThreshold = 5,
                    IsActive = true
                }
            };

            var acmeInvoice1 = new Invoice
            {
                Id = Guid.NewGuid(),
                TenantId = AcmeTenantId,
                InvoiceNumber = "INV-202608-0001",
                CustomerName = "Apex Manufacturing Inc",
                CustomerEmail = "ap@apex-mfg.com",
                IssueDate = DateTime.UtcNow.AddDays(-20),
                DueDate = DateTime.UtcNow.AddDays(-5),
                SubTotal = 1250.00m,
                TaxAmount = 125.00m,
                TotalAmount = 1375.00m,
                Currency = "USD",
                Status = InvoiceStatus.Paid,
                Notes = "Settled via automated ACH transfer.",
                LineItems = new List<InvoiceLineItem>
                {
                    new()
                    {
                        Description = "Precision Ball Bearing (ABEC-7 Industrial)",
                        Quantity = 20,
                        UnitPrice = 45.00m,
                        TotalPrice = 900.00m,
                        InventoryItemId = acmeItems[2].Id
                    },
                    new()
                    {
                        Description = "Steel Flange Gasket (2-inch ANSI)",
                        Quantity = 20,
                        UnitPrice = 17.50m,
                        TotalPrice = 350.00m,
                        InventoryItemId = acmeItems[1].Id
                    }
                }
            };

            var acmeInvoice2 = new Invoice
            {
                Id = Guid.NewGuid(),
                TenantId = AcmeTenantId,
                InvoiceNumber = "INV-202609-0002",
                CustomerName = "Starlight Dynamics",
                CustomerEmail = "billing@starlightdynamics.com",
                IssueDate = DateTime.UtcNow.AddDays(-2),
                DueDate = DateTime.UtcNow.AddDays(12),
                SubTotal = 500.00m,
                TaxAmount = 50.00m,
                TotalAmount = 550.00m,
                Currency = "USD",
                Status = InvoiceStatus.Sent,
                Notes = "Standard Net-14 terms.",
                LineItems = new List<InvoiceLineItem>
                {
                    new()
                    {
                        Description = "High Pressure Hydraulic Control Valve",
                        Quantity = 4,
                        UnitPrice = 125.00m,
                        TotalPrice = 500.00m,
                        InventoryItemId = acmeItems[0].Id
                    }
                }
            };

            context.Tenants.Add(acmeTenant);
            context.InventoryItems.AddRange(acmeItems);
            context.Invoices.AddRange(acmeInvoice1, acmeInvoice2);

            tenantsAdded++;
            itemsAdded += acmeItems.Count;
            invoicesAdded += 2;
        }

        // -------------------------------------------------------------
        // TENANT 2: GLOBAL_LOGISTICS (Freight & Warehousing)
        // -------------------------------------------------------------
        if (!await context.Tenants.AnyAsync(t => t.Code == "GLOBAL_LOGISTICS", cancellationToken))
        {
            logger.LogInformation("Seeding GLOBAL_LOGISTICS tenant and inventory...");
            var globalTenant = new Tenant
            {
                Id = GlobalLogisticsTenantId,
                Code = "GLOBAL_LOGISTICS",
                Name = "Global Logistics & Freight Solutions Ltd",
                AdminEmail = "operations@globallogistics.com",
                SubscriptionTier = "Professional",
                IsActive = true
            };

            var globalItems = new List<InventoryItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    TenantId = GlobalLogisticsTenantId,
                    SKU = "SKU-PALLET-01",
                    Name = "Standard Heat-Treated Euro Wooden Pallet",
                    Description = "EPAL certified 1200x800mm heat treated wooden pallet for export freight.",
                    UnitPrice = 28.00m,
                    StockQuantity = 350,
                    ReorderThreshold = 50,
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    TenantId = GlobalLogisticsTenantId,
                    SKU = "SKU-STRAP-02",
                    Name = "Polypropylene Strapping Roll (16mm x 1000m)",
                    Description = "High tensile embossed packaging strap for heavy freight security.",
                    UnitPrice = 65.00m,
                    StockQuantity = 8, // Below reorder threshold (Alert!)
                    ReorderThreshold = 20,
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    TenantId = GlobalLogisticsTenantId,
                    SKU = "SKU-SEAL-09",
                    Name = "High Security Container Bolt Seal (ISO 17712)",
                    Description = "Tamper evident serialized bolt seal for ocean shipping containers.",
                    UnitPrice = 4.50m,
                    StockQuantity = 500,
                    ReorderThreshold = 100,
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    TenantId = GlobalLogisticsTenantId,
                    SKU = "SKU-WRAP-04",
                    Name = "Heavy Duty Stretch Film Roll 500mm",
                    Description = "Cast hand stretch wrap 23 micron puncture resistant film.",
                    UnitPrice = 22.50m,
                    StockQuantity = 65,
                    ReorderThreshold = 25,
                    IsActive = true
                }
            };

            var globalInvoice1 = new Invoice
            {
                Id = Guid.NewGuid(),
                TenantId = GlobalLogisticsTenantId,
                InvoiceNumber = "INV-202609-0010",
                CustomerName = "Trans-Pacific Ocean Shipping Co",
                CustomerEmail = "finance@transpacific.com",
                IssueDate = DateTime.UtcNow.AddDays(-1),
                DueDate = DateTime.UtcNow.AddDays(14),
                SubTotal = 2800.00m,
                TaxAmount = 280.00m,
                TotalAmount = 3080.00m,
                Currency = "USD",
                Status = InvoiceStatus.Draft,
                Notes = "Drafted for export container loading.",
                LineItems = new List<InvoiceLineItem>
                {
                    new()
                    {
                        Description = "Standard Heat-Treated Euro Wooden Pallet",
                        Quantity = 100,
                        UnitPrice = 28.00m,
                        TotalPrice = 2800.00m,
                        InventoryItemId = globalItems[0].Id
                    }
                }
            };

            context.Tenants.Add(globalTenant);
            context.InventoryItems.AddRange(globalItems);
            context.Invoices.Add(globalInvoice1);

            tenantsAdded++;
            itemsAdded += globalItems.Count;
            invoicesAdded += 1;
        }

        // -------------------------------------------------------------
        // TENANT 3: BIOTECH_MED (Diagnostics & Healthcare)
        // -------------------------------------------------------------
        if (!await context.Tenants.AnyAsync(t => t.Code == "BIOTECH_MED", cancellationToken))
        {
            logger.LogInformation("Seeding BIOTECH_MED tenant and inventory...");
            var biotechTenant = new Tenant
            {
                Id = BioTechTenantId,
                Code = "BIOTECH_MED",
                Name = "BioTech Medical Diagnostic Instruments",
                AdminEmail = "admin@biotech-med.com",
                SubscriptionTier = "Enterprise",
                IsActive = true
            };

            var biotechItems = new List<InventoryItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    TenantId = BioTechTenantId,
                    SKU = "SKU-REAGENT-A",
                    Name = "Enzyme Reaction Buffer Solution 500ml",
                    Description = "Molecular biology grade reaction buffer for real-time PCR diagnostics.",
                    UnitPrice = 145.00m,
                    StockQuantity = 60,
                    ReorderThreshold = 15,
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    TenantId = BioTechTenantId,
                    SKU = "SKU-PIPETTE-03",
                    Name = "Sterile Disposable Pipette Tips (Box of 960)",
                    Description = "Low retention universal pipette tips 10-200 microliter filtered.",
                    UnitPrice = 38.00m,
                    StockQuantity = 140,
                    ReorderThreshold = 40,
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    TenantId = BioTechTenantId,
                    SKU = "SKU-VIAL-CRY",
                    Name = "Cryogenic Specimen Vials 2.0ml (Pack of 500)",
                    Description = "Polypropylene internal thread storage tubes rated to -196C.",
                    UnitPrice = 85.00m,
                    StockQuantity = 5, // Below reorder threshold (Alert!)
                    ReorderThreshold = 20,
                    IsActive = true
                }
            };

            context.Tenants.Add(biotechTenant);
            context.InventoryItems.AddRange(biotechItems);

            tenantsAdded++;
            itemsAdded += biotechItems.Count;
        }

        if (tenantsAdded > 0 || itemsAdded > 0 || invoicesAdded > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Database seeding completed! Added {Tenants} tenants, {Items} inventory SKUs, and {Invoices} invoices.",
                tenantsAdded, itemsAdded, invoicesAdded);
        }
        else
        {
            logger.LogInformation("All demo tenants (ACME_CORP, GLOBAL_LOGISTICS, BIOTECH_MED) already exist. No seeding required.");
        }

        return new SeedResult(tenantsAdded, itemsAdded, invoicesAdded);
    }
}

public record SeedResult(int TenantsAdded, int InventoryItemsAdded, int InvoicesAdded);
