# Feature Specification: Invoice Extractor & Document Intelligence Plugin

**Feature Branch**: `006-invoice-extractor-plugin`

**Created**: 2026-09-05

**Status**: Draft

**Input**: User description: "Add an InvoiceExtractorPlugin to the AgentEngine that parses unstructured text, emails, or OCR invoice documents, automatically extracts line items, quantities, and prices, validates extracted items against active tenant Inventory SKUs, and generates draft invoices with full multi-tenant isolation."

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Intelligent Invoice Text Parsing & Extraction (Priority: P1) 🎯 MVP

As an accounts payable clerk or ERP operator, I want to paste raw text from an email, receipt, or OCR-scanned invoice into the AI chat interface, so that the agent automatically parses and structures the invoice header and individual line items without manual data entry.

**Why this priority**: Eliminates repetitive manual data entry, prevents transposition errors, and delivers immediate productivity value from raw unstructured inputs.

**Independent Test**: Can be tested independently by submitting a plain text invoice snippet containing vendor details, invoice date, line items with quantities, and prices, and verifying that the agent returns structured JSON representing the parsed invoice header and line items.

**Acceptance Scenarios**:

1. **Given** raw unstructured invoice text containing customer details and line items, **When** the extraction function is executed, **Then** the customer name, customer email, issue date, and item breakdown are accurately extracted into structured records.
2. **Given** invoice text containing multiple items with quantities and unit prices, **When** extracted, **Then** line totals and subtotals are computed and verified against the source text.
3. **Given** ambiguous or incomplete text lacking an email or due date, **When** extracted, **Then** reasonable defaults (e.g. 14 days due date, default contact placeholder) are applied without failing.

---

### User Story 2 - SKU Matching & Catalog Item Correlation (Priority: P2)

As an inventory manager, I want the extracted line items to be automatically compared against our active tenant's inventory product catalog (SKUs), so that recognized catalog items are linked to their inventory records and unrecognized items are flagged.

**Why this priority**: Bridges financial accounts payable with inventory stock management, ensuring seamless traceability between billing line items and actual inventory items.

**Independent Test**: Can be tested by parsing an invoice containing both existing catalog items ("SKU-123") and custom non-catalog services, verifying that recognized SKUs are linked to their `InventoryItem` record while preserving custom line items.

**Acceptance Scenarios**:

1. **Given** an extracted line item matching an active tenant SKU, **When** correlated with the inventory catalog, **Then** the line item is linked to the existing `InventoryItemId` and unit catalog prices are cross-referenced.
2. **Given** an extracted line item that does not match any existing SKU, **When** checked against the catalog, **Then** it is marked as a non-inventory custom item without blocking invoice processing.
3. **Given** a cross-tenant inventory request, **When** the plugin searches for SKUs, **Then** only SKUs belonging to the current tenant are searched, preventing data leakage across organizations.

---

### User Story 3 - Autonomous Draft Invoice Staging & Database Persistence (Priority: P3)

As a finance manager, I want to review the extracted invoice summary and instruct the agent to stage/create a draft invoice directly in the ERP database, so that it enters the billing workflow for approval.

**Why this priority**: Completes the end-to-end automation loop from raw text ingestion to real database persistence in the ERP ledger.

**Independent Test**: Can be tested by prompting the agent to create a draft invoice from extracted data, verifying that the new `Invoice` and `InvoiceLineItems` entities are persisted in SQL Server under the active `TenantId`.

**Acceptance Scenarios**:

1. **Given** validated extraction data, **When** the user commands the agent to stage the invoice, **Then** a new `Invoice` entity is saved with status `Draft`, sequential invoice number, and calculated tax/totals.
2. **Given** successfully staged invoice line items, **When** queried by the ERP invoice API, **Then** the line items reflect the exact quantities, prices, and inventory linkages.
3. **Given** an extraction with mathematical discrepancies (e.g. line items sum differs from stated total), **When** staging is requested, **Then** a warning is returned detailing the discrepancy before committing.

---

### Edge Cases

- **Foreign or Missing Currencies**: Default to tenant's base currency (e.g. `USD`) if no currency symbol or code is found.
- **Worded Numbers (e.g. "two packages at ten dollars")**: Resilient extraction parsing handles both numeric digits ("2", "$10.00") and common worded formats.
- **Zero or Negative Prices**: Input validation flags zero or negative unit prices as warnings requiring operator confirmation.
- **Duplicate Invoices**: Checks if an invoice with the identical invoice number or customer reference already exists for the tenant, warning the operator to prevent double billing.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide an `InvoiceExtractorPlugin` in `SmartErpAgent.AgentEngine.Plugins` with Semantic Kernel `[KernelFunction]` annotations.
- **FR-002**: System MUST parse unstructured text into a strongly-typed `ExtractedInvoiceData` structure including customer name, email, invoice number, issue date, due date, currency, line items, and totals.
- **FR-003**: System MUST extract table-like and free-form line items containing description, quantity, unit price, and total price.
- **FR-004**: System MUST correlate extracted line items against the current tenant's `InventoryItems` by SKU or product name, associating matched `InventoryItemId` values.
- **FR-005**: System MUST compute and validate line item totals against the invoice subtotal, reporting tax calculation (e.g. 10%) and grand total.
- **FR-006**: System MUST provide a function `StageExtractedInvoiceAsync` that persists the extracted data as a `Draft` invoice in `Invoices` and `InvoiceLineItems` tables.
- **FR-007**: System MUST strictly enforce multi-tenant isolation, ensuring that product matching and database inserts apply exclusively to the authenticated `TenantId`.
- **FR-008**: System MUST register `InvoiceExtractorPlugin` in the application's dependency injection container via `AddAgentEngine()`.
- **FR-009**: The orchestrator (`SemanticKernelAgentOrchestrator`) MUST support auto-invoking `InvoiceExtractorPlugin` functions and streaming thought process steps via SignalR.

---

### Key Entities

- **ExtractedInvoiceData**: Transport model representing extracted invoice header and line items before persistence.
  - `CustomerName: string`, `CustomerEmail: string`, `InvoiceNumber: string`, `IssueDate: DateTime`, `DueDate: DateTime`, `Currency: string`, `LineItems: List<ExtractedInvoiceLineItem>`, `SubTotal: decimal`, `TaxAmount: decimal`, `TotalAmount: decimal`, `ConfidenceScore: double`.
- **ExtractedInvoiceLineItem**:
  - `Description: string`, `Quantity: int`, `UnitPrice: decimal`, `TotalPrice: decimal`, `MatchedSKU: string?`, `MatchedInventoryItemId: Guid?`.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Unstructured text containing standard invoice details is extracted into structured data in under 2 seconds.
- **SC-002**: 100% of persisted draft invoices strictly enforce `TenantId` isolation and appear in tenant-filtered queries.
- **SC-003**: 100% of line item subtotals and grand totals are mathematically verified before database commit.
- **SC-004**: SKU matching accurately identifies exact-match SKUs from the tenant inventory catalog.
- **SC-005**: Automated unit tests for `InvoiceExtractorPlugin` achieve 100% pass rate.
- **SC-006**: Both backend solution and frontend applications build with 0 errors and 0 warnings.

---

## Assumptions

- Text input can be provided via chat prompt or raw text copy-paste. OCR pre-processing for binary PDFs can feed plain text into this extractor.
- Standard default sales tax is 10% unless specified in the invoice document.
- Default due date is 14 days from issue date if not explicitly found in the document text.
- Tenant context is resolved via `ITenantContext` and enforced at the database layer via EF Core Global Query Filters.
