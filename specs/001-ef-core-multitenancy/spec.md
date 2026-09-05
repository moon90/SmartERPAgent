# Feature Specification: Foundational EF Core Multi-Tenancy DbContext

**Feature Branch**: `001-ef-core-multitenancy`

**Created**: 2026-09-05

**Status**: Draft

**Input**: User description: "Implement the foundational EF Core DbContext for the ERP. 1. Create entities: Tenant, Invoice, and InventoryItem inheriting from an ITenantEntity interface. 2. Implement ApplicationDbContext in the Infrastructure layer. 3. Apply EF Core global query filters for TenantId on all tenant entities. 4. Override SaveChangesAsync to automatically inject the current TenantId (from ITenantContext) for new inserts."

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Tenant Data Isolation on Queries (Priority: P1)

As a tenant organization administrator, I want our business data (invoices, inventory, financial records) to be completely isolated from all other organizations using the platform, so that no unauthorized organization can view, query, or leak our proprietary ERP data.

**Why this priority**: Strict tenant isolation is the foundational security requirement for any B2B SaaS platform. A failure here causes critical data breaches.

**Independent Test**: Can be independently tested by seeding records for Tenant A and Tenant B, querying under Tenant A's context, and verifying that only Tenant A's records are returned while Tenant B's records are invisible.

**Acceptance Scenarios**:

1. **Given** Tenant A has 5 invoices and Tenant B has 10 invoices in the database, **When** a user authenticated under Tenant A queries invoices, **Then** exactly 5 invoices belonging to Tenant A are returned.
2. **Given** an inventory item belongs to Tenant B, **When** a user authenticated under Tenant A attempts to fetch that item by its identifier, **Then** the system returns a not found result.
3. **Given** a system or administrative workflow operates without an active tenant context, **When** querying records, **Then** records across all tenants can be accessed for maintenance or migration tasks.

---

### User Story 2 - Automated Tenant Identity Assignment on Insertion (Priority: P2)

As an ERP user or autonomous AI agent creating business records, I want new invoices and inventory items to automatically be stamped with our active tenant identity, so that developers and agents do not need to manually manage or hardcode tenant identifiers on every creation operation.

**Why this priority**: Eliminates human and agent error during entity creation, guaranteeing that orphaned records or misallocated tenant data can never be written.

**Independent Test**: Can be independently tested by adding a new entity without specifying a Tenant ID, calling the persistence layer within an active tenant context, and verifying that the entity is saved with the active tenant's ID populated.

**Acceptance Scenarios**:

1. **Given** an active tenant context for Tenant A, **When** a new invoice or inventory item is saved without an explicit Tenant ID, **Then** the system automatically populates the Tenant ID with Tenant A's identifier prior to persistence.
2. **Given** an active tenant context for Tenant A, **When** an entity is created, **Then** the creation timestamp is automatically assigned in UTC.

---

### User Story 3 - Auditing and Soft-Delete Preservation (Priority: P3)

As a business compliance auditor, I want deleted entities to be preserved in the database with soft-delete flags and timestamps, so that historical records and ledgers are protected against accidental permanent deletion.

**Why this priority**: Enterprise ERPs require non-destructive data handling for accounting compliance and historical audit trails.

**Independent Test**: Can be tested by issuing a delete command on an entity, asserting that the record is marked as deleted with an updated timestamp, and confirming that standard tenant queries no longer return the record.

**Acceptance Scenarios**:

1. **Given** an active invoice, **When** a deletion action is performed, **Then** the entity is marked as deleted with a UTC modification timestamp and omitted from future tenant queries.

---

### Edge Cases

- **Missing Tenant Context on Creation**: What happens when a tenant-scoped entity is saved while no tenant context is established? The system must reject the operation or require an explicit administrative tenant assignment.
- **Explicit Cross-Tenant Id Override**: What happens if a caller attempts to save an entity with Tenant ID 'B' while running in the context of Tenant 'A'? The system must enforce tenant boundary integrity and prevent cross-tenant writes.
- **Uniqueness across Tenants**: What happens if Tenant A and Tenant B use the identical SKU or Invoice number? The system must scope uniqueness constraints per tenant so collisions between different organizations do not occur.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST define an entity abstraction (`ITenantEntity`) marking entities subject to multi-tenant data isolation.
- **FR-002**: The system MUST provide core business entities for `Tenant`, `Invoice`, and `InventoryItem` adhering to multi-tenant ownership boundaries.
- **FR-003**: The system MUST automatically enforce global query filters on all tenant entities, restricting query output to the active tenant identifier.
- **FR-004**: The system MUST automatically inject the active tenant identifier into newly created tenant entities during save operations when not pre-populated.
- **FR-005**: The system MUST automatically populate creation and update UTC timestamps on all persistent entity lifecycle transitions.
- **FR-006**: The system MUST implement soft-deletion by default for persistent business entities, setting deletion flags and filtering deleted items from standard queries.
- **FR-007**: The system MUST allow administrative and background workflows to bypass tenant query filters when explicitly requested for platform maintenance.

### Key Entities

- **Tenant**: Represents an isolated enterprise customer organization. Key attributes include unique organizational Code, Display Name, Subscription Tier, and Active Status.
- **Invoice**: Represents a tenant-owned billing statement. Key attributes include Tenant ID, unique Invoice Number (per tenant), Customer details, Financial Totals (Subtotal, Tax, Total), Due Date, and Status (Draft, Sent, Paid, Overdue, Cancelled).
- **InventoryItem**: Represents physical or service warehouse goods. Key attributes include Tenant ID, unique SKU (per tenant), Name, Description, Unit Price, Stock Quantity, and Low-Stock Reorder Threshold.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Zero cross-tenant data leakage: 100% of queries executed within an active tenant context return strictly the data belonging to that tenant.
- **SC-002**: 100% of new tenant-scoped entities created within an active tenant context are automatically stamped with the appropriate Tenant ID without manual caller intervention.
- **SC-003**: Automated multi-tenant unit and integration tests pass with a 100% success rate across all tenant boundary scenarios.
- **SC-004**: Soft-deleted entities are filtered from 100% of standard tenant queries while remaining intact in the persistent storage layer for audit compliance.

---

## Assumptions

- The active tenant context is resolved from the incoming transport layer (HTTP header or authentication token) prior to database context initialization.
- Primary database keys utilize globally unique identifiers (UUID/GUID) to eliminate cross-tenant key collision risks.
- Standard tax and invoice status transitions follow default enterprise accounting practices.
