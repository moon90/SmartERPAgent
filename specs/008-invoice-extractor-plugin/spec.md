# Feature Specification: Invoice Extractor Plugin (Document Intelligence to InvoiceDto)

**Feature Branch**: `008-invoice-extractor-plugin`  
**Created**: 2026-09-05  
**Status**: Ready for Planning  
**Input**: User description: "Implement the InvoiceExtractorPlugin in the SmartErpAgent.AgentEngine project to parse unstructured invoice data (like Emails or PDF text) into structured DTOs.
1. Create a native C# class named `InvoiceExtractorPlugin`.
2. Add a `[KernelFunction]` named `ExtractInvoiceDetailsAsync`. It must accept a string parameter named `rawContent` (representing the raw email body or OCR'd PDF content).
3. The function should return a strongly typed `InvoiceDto` (containing InvoiceNumber, CustomerName, IssueDate, DueDate, and TotalAmount). For the initial implementation, write robust mock logic or Regex that simulates extracting these fields from standard invoice text formats.
4. Decorate the method and its parameters with highly descriptive `[Description]` attributes. The LLM must understand that this function is triggered when a user asks to "process an invoice", "read this email", or "extract invoice details".
5. Update the `SemanticKernelAgentOrchestrator` to inject and import `InvoiceExtractorPlugin` into the Kernel, alongside the existing Inventory and Reporting plugins."

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Unstructured Invoice Text Parsing to Structured InvoiceDto (Priority: P1) 🎯 MVP

An accounts payable specialist, finance officer, or warehouse manager receives an invoice via an unstructured email body or OCR-scanned PDF text. The operator provides the raw invoice text to the AI agent or system pipeline. The agent executes the extraction capability, parsing key fields—such as Invoice Number, Customer/Vendor Name, Issue Date, Due Date, and Total Monetary Amount—and returns a validated, strongly-typed `InvoiceDto`.

**Why this priority**: Eliminates repetitive manual data entry, reduces billing transposition errors, and creates a clean, structured bridge between incoming emails/PDF text and ERP transaction records.

**Independent Test**: Can be tested independently by supplying a plain-text invoice snippet (e.g. "Invoice INV-9021 for Acme Logistics issued on 2026-09-01 due on 2026-09-15 total $4,250.00") and asserting that the returned record contains exact parsed values for `InvoiceNumber`, `CustomerName`, `IssueDate`, `DueDate`, and `TotalAmount`.

**Acceptance Scenarios**:

1. **Given** raw email or OCR text containing clear invoice metadata (number, customer, dates, total),  
   **When** the extraction function is executed with `rawContent`,  
   **Then** the function returns an `InvoiceDto` populated with the extracted values matching the source text.
2. **Given** raw text where certain non-critical fields (e.g., explicit due date) are omitted,  
   **When** extracted,  
   **Then** the function applies sensible defaults (e.g., Due Date defaults to 14 or 30 days past Issue Date) without throwing exceptions.
3. **Given** an empty, null, or whitespace string provided as `rawContent`,  
   **When** the function is invoked,  
   **Then** the function handles the boundary gracefully, returning a placeholder or empty DTO with descriptive error notes rather than crashing.

---

### User Story 2 - Robust Pattern Recognition for Email and Document Formats (Priority: P2)

Invoices arrive in diverse layouts: forwarded email headers ("From: billing@vendor.com", "Subject: Invoice #..."), tabular summaries, or scanned receipts with various date and currency formats (e.g., "$1,250.00", "1250.00 USD", "2026-08-15", "Aug 15, 2026"). The parser must employ robust heuristic matching and regular expressions to accurately capture headers, customer emails, line items, and monetary totals across these variations.

**Why this priority**: Real-world business communications do not follow a single rigid format. Heuristic resilience ensures high extraction accuracy across common vendor formats.

**Independent Test**: Can be tested by running an automated suite of varied text samples (forwarded email format, tabular text, compact single-line receipts) and verifying that key metadata is correctly parsed for each variant.

**Acceptance Scenarios**:

1. **Given** a forwarded email containing "Invoice Number: INV-2026-0042", "Bill To: Contoso Solutions", and "Total: $1,800.00",  
   **When** evaluated,  
   **Then** the system extracts `InvoiceNumber="INV-2026-0042"`, `CustomerName="Contoso Solutions"`, and `TotalAmount=1800.00`.
2. **Given** raw text containing itemized line descriptions, quantities, and unit prices,  
   **When** extracted,  
   **Then** the resulting `InvoiceDto` populates the `LineItems` collection with corresponding item records.

---

### User Story 3 - Autonomous AI Copilot Triggering & Tool Invocation (Priority: P3)

An ERP business operator converses naturally with the AI Copilot, using phrases like "Process this invoice email", "Extract invoice details from this text", or "Read this invoice and create a record". The AI orchestrator recognizes the analytical intent via semantic descriptions, invokes `ExtractInvoiceDetailsAsync` on `InvoiceExtractorPlugin`, and streams cognitive progress updates before returning the parsed invoice summary to the operator.

**Why this priority**: Enables frictionless interaction where business users do not need to know internal API endpoints or field names to ingest incoming invoices.

**Independent Test**: Can be tested by sending prompts like "Please process this invoice: [invoice text]" to the orchestrator and verifying that `InvoiceExtractorPlugin.ExtractInvoiceDetailsAsync` is invoked and its output is synthesized in the response.

**Acceptance Scenarios**:

1. **Given** a conversational prompt containing phrases such as "process an invoice", "read this email", or "extract invoice details" along with raw document text,  
   **When** submitted to the AI orchestrator,  
   **Then** the system autonomously selects `InvoiceExtractorPlugin.ExtractInvoiceDetailsAsync`.
2. **Given** the extraction tool completes execution,  
   **When** returning the result to the operator,  
   **Then** the orchestrator streams intermediate thought steps and presents a clear, structured summary of the extracted invoice.

---

## Edge Cases

- **Zero or Whitespace Text**: When empty or null content is passed, the extractor returns a non-null `InvoiceDto` with placeholder flags or default notes, preventing unhandled null reference exceptions.
- **Varied Date Formats**: Handles common ISO formats (`YYYY-MM-DD`), US formats (`MM/DD/YYYY`), and written dates (`September 5, 2026`).
- **Currency Symbols & Commas**: Accurately strips currency symbols (`$`, `€`, `£`, `USD`) and thousands separators (`,` in `$1,500.00`) during numeric parsing.
- **Missing Customer Name**: When no explicit customer or vendor is detected, defaults to "Unknown Customer" or extracts domain names from email addresses.
- **Calculated Subtotal vs Total**: If tax or shipping is present, calculates or preserves `SubTotal` and `TotalAmount` coherently.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a native C# plugin class named `InvoiceExtractorPlugin` in `SmartErpAgent.AgentEngine.Plugins`.
- **FR-002**: System MUST implement a `[KernelFunction]` method named `ExtractInvoiceDetailsAsync` taking a string parameter named `rawContent` representing the unstructured invoice text or email body.
- **FR-003**: `ExtractInvoiceDetailsAsync` MUST return a strongly-typed `InvoiceDto` containing `InvoiceNumber`, `CustomerName`, `IssueDate`, `DueDate`, and `TotalAmount` (along with associated financial header fields and line items).
- **FR-004**: System MUST implement robust regex and heuristic parsing logic to reliably extract invoice identifiers, party names, dates, amounts, and line items from standard invoice and email formats.
- **FR-005**: All plugin methods and parameters MUST be annotated with precise `[Description]` attributes that clarify triggering conditions (e.g. "process an invoice", "read this email", "extract invoice details").
- **FR-006**: System MUST register `InvoiceExtractorPlugin` in the application dependency injection container.
- **FR-007**: System MUST update `SemanticKernelAgentOrchestrator` to inject, import, and expose `InvoiceExtractorPlugin` in the Semantic Kernel alongside existing `InventoryAgentPlugin`, `InvoiceAgentPlugin`, and `ReportingAgentPlugin`.
- **FR-008**: System MUST support offline/rule-based fallback execution in `SemanticKernelAgentOrchestrator` when external LLM API credentials are not configured.

### Key Entities

- **InvoiceDto**:
  - `Id`: Unique GUID identifier for the invoice draft.
  - `TenantId`: Scoped tenant organization identifier.
  - `InvoiceNumber`: Alphanumeric invoice code (e.g., `INV-2026-001`).
  - `CustomerName`: Extracted customer or client organization name.
  - `CustomerEmail`: Extracted customer email address.
  - `IssueDate`: Timestamp of invoice issuance.
  - `DueDate`: Payment due date timestamp.
  - `SubTotal`: Sum of line item amounts before tax.
  - `TaxAmount`: Calculated or extracted tax value.
  - `TotalAmount`: Final gross invoice total.
  - `Currency`: ISO currency code (default: USD).
  - `Status`: Invoice workflow status (Draft).
  - `LineItems`: Collection of extracted line items (`InvoiceLineItemDto`).

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Operators can parse standard unstructured invoice text into a structured `InvoiceDto` in under 500 milliseconds.
- **SC-002**: Automated test suite validates 100% extraction accuracy for `InvoiceNumber`, `CustomerName`, and `TotalAmount` across standard email, receipt, and invoice text samples.
- **SC-003**: AI Copilot triggers `ExtractInvoiceDetailsAsync` for 100% of natural language requests to "process an invoice", "read this email", or "extract invoice details".
- **SC-004**: 100% test pass rate across unit tests covering edge cases (empty strings, missing due dates, varied currency formatting).

---

## Assumptions

- Multi-tenant isolation: Extracted invoices are created within the active tenant's context.
- Default currency: Assumes USD if no currency symbol or ISO code is specified in `rawContent`.
- Mock/Regex phase: Initial implementation leverages high-performance regular expressions and heuristic tokenizers; future iterations can layer multimodal OCR services.
