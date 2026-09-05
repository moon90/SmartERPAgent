-- =====================================================================================
-- Smart ERP Agent - Demo Tenant & Inventory Seed Script
-- Database: SmartErpAgentDb
-- Tenants: ACME_CORP, GLOBAL_LOGISTICS, BIOTECH_MED
-- =====================================================================================

USE [SmartErpAgentDb];
GO

-- 1. SEED TENANTS
IF NOT EXISTS (SELECT 1 FROM [Tenants] WHERE [Code] = 'ACME_CORP')
BEGIN
    INSERT INTO [Tenants] ([Id], [Code], [Name], [AdminEmail], [SubscriptionTier], [IsActive], [CreatedAtUtc], [IsDeleted])
    VALUES ('11111111-1111-1111-1111-111111111111', 'ACME_CORP', 'Acme Industrial Corporation', 'admin@acme-corp.com', 'Enterprise', 1, GETUTCDATE(), 0);
END

IF NOT EXISTS (SELECT 1 FROM [Tenants] WHERE [Code] = 'GLOBAL_LOGISTICS')
BEGIN
    INSERT INTO [Tenants] ([Id], [Code], [Name], [AdminEmail], [SubscriptionTier], [IsActive], [CreatedAtUtc], [IsDeleted])
    VALUES ('22222222-2222-2222-2222-222222222222', 'GLOBAL_LOGISTICS', 'Global Logistics & Freight Solutions Ltd', 'operations@globallogistics.com', 'Professional', 1, GETUTCDATE(), 0);
END

IF NOT EXISTS (SELECT 1 FROM [Tenants] WHERE [Code] = 'BIOTECH_MED')
BEGIN
    INSERT INTO [Tenants] ([Id], [Code], [Name], [AdminEmail], [SubscriptionTier], [IsActive], [CreatedAtUtc], [IsDeleted])
    VALUES ('33333333-3333-3333-3333-333333333333', 'BIOTECH_MED', 'BioTech Medical Diagnostic Instruments', 'admin@biotech-med.com', 'Enterprise', 1, GETUTCDATE(), 0);
END
GO

-- 2. SEED INVENTORY ITEMS FOR ACME_CORP
DECLARE @AcmeId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';

IF NOT EXISTS (SELECT 1 FROM [InventoryItems] WHERE [TenantId] = @AcmeId AND [SKU] = 'SKU-101')
    INSERT INTO [InventoryItems] ([Id], [TenantId], [SKU], [Name], [Description], [UnitPrice], [StockQuantity], [ReorderThreshold], [IsActive], [CreatedAtUtc], [IsDeleted])
    VALUES (NEWID(), @AcmeId, 'SKU-101', 'High Pressure Hydraulic Control Valve', 'Precision industrial valve rated up to 5000 PSI.', 125.00, 45, 15, 1, GETUTCDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM [InventoryItems] WHERE [TenantId] = @AcmeId AND [SKU] = 'SKU-102')
    INSERT INTO [InventoryItems] ([Id], [TenantId], [SKU], [Name], [Description], [UnitPrice], [StockQuantity], [ReorderThreshold], [IsActive], [CreatedAtUtc], [IsDeleted])
    VALUES (NEWID(), @AcmeId, 'SKU-102', 'Steel Flange Gasket (2-inch ANSI)', 'Spiral wound graphite filled gasket for high temp steam pipelines.', 15.50, 120, 30, 1, GETUTCDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM [InventoryItems] WHERE [TenantId] = @AcmeId AND [SKU] = 'SKU-123')
    INSERT INTO [InventoryItems] ([Id], [TenantId], [SKU], [Name], [Description], [UnitPrice], [StockQuantity], [ReorderThreshold], [IsActive], [CreatedAtUtc], [IsDeleted])
    VALUES (NEWID(), @AcmeId, 'SKU-123', 'Precision Ball Bearing (ABEC-7 Industrial)', 'High speed deep groove bearing with hardened chrome steel balls.', 45.00, 88, 20, 1, GETUTCDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM [InventoryItems] WHERE [TenantId] = @AcmeId AND [SKU] = 'SKU-LOW')
    INSERT INTO [InventoryItems] ([Id], [TenantId], [SKU], [Name], [Description], [UnitPrice], [StockQuantity], [ReorderThreshold], [IsActive], [CreatedAtUtc], [IsDeleted])
    VALUES (NEWID(), @AcmeId, 'SKU-LOW', 'Industrial Lithium Grease Cartridge 400g', 'High temperature anti-friction multi-purpose machinery lubricant.', 12.00, 4, 25, 1, GETUTCDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM [InventoryItems] WHERE [TenantId] = @AcmeId AND [SKU] = 'SKU-MOTOR')
    INSERT INTO [InventoryItems] ([Id], [TenantId], [SKU], [Name], [Description], [UnitPrice], [StockQuantity], [ReorderThreshold], [IsActive], [CreatedAtUtc], [IsDeleted])
    VALUES (NEWID(), @AcmeId, 'SKU-MOTOR', 'Heavy Duty 3-Phase Electric Motor 5HP', 'Totally enclosed fan-cooled industrial AC motor 1750 RPM.', 850.00, 12, 5, 1, GETUTCDATE(), 0);
GO

-- 3. SEED INVENTORY ITEMS FOR GLOBAL_LOGISTICS
DECLARE @GlobalId UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';

IF NOT EXISTS (SELECT 1 FROM [InventoryItems] WHERE [TenantId] = @GlobalId AND [SKU] = 'SKU-PALLET-01')
    INSERT INTO [InventoryItems] ([Id], [TenantId], [SKU], [Name], [Description], [UnitPrice], [StockQuantity], [ReorderThreshold], [IsActive], [CreatedAtUtc], [IsDeleted])
    VALUES (NEWID(), @GlobalId, 'SKU-PALLET-01', 'Standard Heat-Treated Euro Wooden Pallet', 'EPAL certified 1200x800mm heat treated wooden pallet.', 28.00, 350, 50, 1, GETUTCDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM [InventoryItems] WHERE [TenantId] = @GlobalId AND [SKU] = 'SKU-STRAP-02')
    INSERT INTO [InventoryItems] ([Id], [TenantId], [SKU], [Name], [Description], [UnitPrice], [StockQuantity], [ReorderThreshold], [IsActive], [CreatedAtUtc], [IsDeleted])
    VALUES (NEWID(), @GlobalId, 'SKU-STRAP-02', 'Polypropylene Strapping Roll (16mm x 1000m)', 'High tensile embossed packaging strap for heavy freight security.', 65.00, 8, 20, 1, GETUTCDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM [InventoryItems] WHERE [TenantId] = @GlobalId AND [SKU] = 'SKU-SEAL-09')
    INSERT INTO [InventoryItems] ([Id], [TenantId], [SKU], [Name], [Description], [UnitPrice], [StockQuantity], [ReorderThreshold], [IsActive], [CreatedAtUtc], [IsDeleted])
    VALUES (NEWID(), @GlobalId, 'SKU-SEAL-09', 'High Security Container Bolt Seal (ISO 17712)', 'Tamper evident serialized bolt seal for ocean shipping containers.', 4.50, 500, 100, 1, GETUTCDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM [InventoryItems] WHERE [TenantId] = @GlobalId AND [SKU] = 'SKU-WRAP-04')
    INSERT INTO [InventoryItems] ([Id], [TenantId], [SKU], [Name], [Description], [UnitPrice], [StockQuantity], [ReorderThreshold], [IsActive], [CreatedAtUtc], [IsDeleted])
    VALUES (NEWID(), @GlobalId, 'SKU-WRAP-04', 'Heavy Duty Stretch Film Roll 500mm', 'Cast hand stretch wrap 23 micron puncture resistant film.', 22.50, 65, 25, 1, GETUTCDATE(), 0);
GO

-- 4. SEED INVENTORY ITEMS FOR BIOTECH_MED
DECLARE @BioTechId UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';

IF NOT EXISTS (SELECT 1 FROM [InventoryItems] WHERE [TenantId] = @BioTechId AND [SKU] = 'SKU-REAGENT-A')
    INSERT INTO [InventoryItems] ([Id], [TenantId], [SKU], [Name], [Description], [UnitPrice], [StockQuantity], [ReorderThreshold], [IsActive], [CreatedAtUtc], [IsDeleted])
    VALUES (NEWID(), @BioTechId, 'SKU-REAGENT-A', 'Enzyme Reaction Buffer Solution 500ml', 'Molecular biology grade reaction buffer for real-time PCR.', 145.00, 60, 15, 1, GETUTCDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM [InventoryItems] WHERE [TenantId] = @BioTechId AND [SKU] = 'SKU-PIPETTE-03')
    INSERT INTO [InventoryItems] ([Id], [TenantId], [SKU], [Name], [Description], [UnitPrice], [StockQuantity], [ReorderThreshold], [IsActive], [CreatedAtUtc], [IsDeleted])
    VALUES (NEWID(), @BioTechId, 'SKU-PIPETTE-03', 'Sterile Disposable Pipette Tips (Box of 960)', 'Low retention universal pipette tips 10-200 microliter filtered.', 38.00, 140, 40, 1, GETUTCDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM [InventoryItems] WHERE [TenantId] = @BioTechId AND [SKU] = 'SKU-VIAL-CRY')
    INSERT INTO [InventoryItems] ([Id], [TenantId], [SKU], [Name], [Description], [UnitPrice], [StockQuantity], [ReorderThreshold], [IsActive], [CreatedAtUtc], [IsDeleted])
    VALUES (NEWID(), @BioTechId, 'SKU-VIAL-CRY', 'Cryogenic Specimen Vials 2.0ml (Pack of 500)', 'Polypropylene internal thread storage tubes rated to -196C.', 85.00, 5, 20, 1, GETUTCDATE(), 0);
GO

PRINT 'Demo tenants and inventory seeding completed successfully.';
GO
