# Data Model: Foundational EF Core Multi-Tenancy

**Feature**: `001-ef-core-multitenancy`  
**Date**: 2026-09-05  
**Database**: Microsoft SQL Server 2022 / Azure SQL via EF Core 8

---

## 1. Entity Definitions & Schemas

### `BaseEntity` (Abstract Root)
All domain entities inherit from `BaseEntity`.

| Property | Type | Nullable | Constraints & Default | Description |
| :--- | :--- | :---: | :--- | :--- |
| `Id` | `Guid` | No | Primary Key, `Guid.NewGuid()` | Globally unique entity identifier. |
| `CreatedAtUtc` | `DateTime` | No | `DateTime.UtcNow` | Immutable creation timestamp in UTC. |
| `UpdatedAtUtc` | `DateTime` | Yes | `null` | Updated on every modification in UTC. |
| `IsDeleted` | `bool` | No | `false` | Soft-deletion flag. |

---

### `Tenant` (Organization Boundary)
Represents an isolated SaaS customer/tenant organization.

| Property | Type | Nullable | Constraints & Default | Description |
| :--- | :--- | :---: | :--- | :--- |
| `Id` | `Guid` | No | Primary Key | Tenant identifier. |
| `Code` | `string` | No | MaxLength(50), Unique Index | Unique uppercase identifier (e.g. `ACME-CORP`). |
| `Name` | `string` | No | MaxLength(200) | Organization display name. |
| `AdminEmail` | `string` | No | MaxLength(256) | Primary administrative email address. |
| `SubscriptionTier` | `string` | No | MaxLength(50), Default("Standard") | Subscription plan tier (`Standard`, `Enterprise`). |
| `IsActive` | `bool` | No | Default(`true`) | Active subscription status flag. |

**Relationships**:
- One-to-Many with `Invoice` (`Tenant.Invoices`)
- One-to-Many with `InventoryItem` (`Tenant.InventoryItems`)

---

### `Invoice` (Multi-Tenant Billing Entity)
Implements `ITenantEntity`.

| Property | Type | Nullable | Constraints & Default | Description |
| :--- | :--- | :---: | :--- | :--- |
| `Id` | `Guid` | No | Primary Key | Unique invoice record ID. |
| `TenantId` | `Guid` | No | Foreign Key (`Tenants.Id`), Index | Tenant ownership foreign key. |
| `InvoiceNumber` | `string` | No | MaxLength(50), Unique per Tenant | Human-readable sequential invoice number. |
| `CustomerName` | `string` | No | MaxLength(200) | Billed customer/company name. |
| `CustomerEmail` | `string` | Yes | MaxLength(256) | Customer contact email. |
| `IssueDate` | `DateTime` | No | `DateTime.UtcNow` | Issue date in UTC. |
| `DueDate` | `DateTime` | No | Default(`IssueDate + 30d`) | Payment due date in UTC. |
| `SubTotal` | `decimal` | No | `Precision(18, 2)` | Net total before tax. |
| `TaxAmount` | `decimal` | No | `Precision(18, 2)` | Computed tax amount. |
| `TotalAmount` | `decimal` | No | `Precision(18, 2)` | Gross total payable. |
| `Currency` | `string` | No | MaxLength(10), Default("USD") | ISO currency code. |
| `Status` | `InvoiceStatus` | No | `enum` as `int` | `Draft (0)`, `Sent (1)`, `Paid (2)`, `Overdue (3)`, `Cancelled (4)`. |
| `Notes` | `string` | Yes | MaxLength(1000) | Optional invoice remarks. |

**Composite Indexes**:
- `UniqueIndex(TenantId, InvoiceNumber)`: Enforces invoice number uniqueness strictly within the tenant organization.

---

### `InvoiceLineItem` (Child Item Entity)

| Property | Type | Nullable | Constraints & Default | Description |
| :--- | :--- | :---: | :--- | :--- |
| `Id` | `Guid` | No | Primary Key | Unique line item ID. |
| `InvoiceId` | `Guid` | No | Foreign Key (`Invoices.Id`), Cascade | Parent invoice reference. |
| `InventoryItemId` | `Guid` | Yes | Foreign Key (`InventoryItems.Id`), SetNull | Optional inventory reference. |
| `Description` | `string` | No | MaxLength(500) | Item or service description. |
| `Quantity` | `int` | No | Min(1) | Billed units. |
| `UnitPrice` | `decimal` | No | `Precision(18, 2)` | Price per unit. |
| `TotalPrice` | `decimal` | No | `Precision(18, 2)` | Quantity × UnitPrice. |

---

### `InventoryItem` (Multi-Tenant Supply Chain Entity)
Implements `ITenantEntity`.

| Property | Type | Nullable | Constraints & Default | Description |
| :--- | :--- | :---: | :--- | :--- |
| `Id` | `Guid` | No | Primary Key | Unique inventory item ID. |
| `TenantId` | `Guid` | No | Foreign Key (`Tenants.Id`), Index | Tenant ownership foreign key. |
| `SKU` | `string` | No | MaxLength(100) | Stock Keeping Unit code. |
| `Name` | `string` | No | MaxLength(200) | Product/item name. |
| `Description` | `string` | Yes | MaxLength(1000) | Detailed item specifications. |
| `UnitPrice` | `decimal` | No | `Precision(18, 2)` | Standard selling unit price. |
| `StockQuantity` | `int` | No | Min(0) | Available warehouse stock units. |
| `ReorderThreshold` | `int` | No | Default(10) | Minimum threshold triggering agent restock alerts. |
| `IsActive` | `bool` | No | Default(`true`) | Item catalog active status. |

**Composite Indexes**:
- `UniqueIndex(TenantId, SKU)`: Enforces SKU uniqueness strictly within each tenant, permitting different tenants to use identical SKU codes.
