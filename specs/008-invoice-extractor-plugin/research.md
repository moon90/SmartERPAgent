# Research: Invoice Extractor Plugin (Document Intelligence to InvoiceDto)

**Feature Branch**: `008-invoice-extractor-plugin`  
**Date**: 2026-09-05  
**Status**: Completed

---

## 1. Context & Architectural Objectives

The objective is to provide a native Semantic Kernel plugin (`InvoiceExtractorPlugin`) with a kernel function `ExtractInvoiceDetailsAsync(string rawContent)` that accepts unstructured text (email bodies, OCR'd documents, receipts) and returns a strongly-typed `InvoiceDto` containing `InvoiceNumber`, `CustomerName`, `IssueDate`, `DueDate`, and `TotalAmount`.

---

## 2. Research Decisions

### Decision 1: Kernel Function Signature & Return Type
- **Chosen**:
  ```csharp
  [KernelFunction, Description("Extracts structured invoice details (InvoiceNumber, CustomerName, IssueDate, DueDate, TotalAmount, and line items) from raw email bodies or OCR'd invoice text.")]
  public async Task<InvoiceDto> ExtractInvoiceDetailsAsync(
      [Description("Raw text content of the email or OCR invoice document to extract.")] string rawContent,
      CancellationToken cancellationToken = default);
  ```
- **Rationale**:
  - Returning `InvoiceDto` directly fulfills requirement 3 ("return a strongly typed `InvoiceDto`").
  - Semantic Kernel handles strongly-typed return values by converting them to JSON for the LLM context while preserving direct type-safe consumption in C# orchestrators and unit tests.
- **Alternatives Considered**:
  - *Returning JSON string*: While flexible, requiring callers to re-deserialize `InvoiceDto` adds boilerplate and violates strong typing principles.
  - *Returning a custom extractor DTO*: An existing `InvoiceDto` already exists in `SmartErpAgent.Application.DTOs` with all necessary fields (`InvoiceNumber`, `CustomerName`, `IssueDate`, `DueDate`, `TotalAmount`, `Currency`, `Status`, `LineItems`). Reusing `InvoiceDto` promotes cohesion and prevents duplication.

---

### Decision 2: Regex & Heuristic Parsing Strategy
- **Chosen**: Multi-pattern regex cascade with normalization and defensive defaults.
  - **Invoice Number**:
    - Regex: `(?i)(?:invoice\s*(?:number|no|#)?[:\s-]*|inv[:\s#-]*)([A-Za-z0-9_-]{3,25})`
    - Fallback: Generate sequential draft identifier `INV-DRAFT-{timestamp}`.
  - **Customer Name**:
    - Regex: `(?i)(?:bill\s*to|customer|client|sold\s*to|attention|att)[:\s]+([^\r\n,;]+)`
    - Fallback: Extract name prefix from email (e.g. `billing@acme.com` -> `Acme`) or default to `"Unknown Customer"`.
  - **Dates (IssueDate & DueDate)**:
    - Regex: `(?i)(?:date|issue\s*date|invoiced)[:\s]+([A-Za-z0-9\s,\/-]+)`
    - Due Date Regex: `(?i)(?:due\s*date|payment\s*due|due)[:\s]+([A-Za-z0-9\s,\/-]+)`
    - Parsing: `DateTime.TryParse` with standard US and ISO invariant culture.
    - Fallback: `IssueDate` defaults to `DateTime.UtcNow`. `DueDate` defaults to `IssueDate.AddDays(14)`.
  - **Total Amount**:
    - Regex: `(?i)(?:total\s*(?:amount)?|grand\s*total|amount\s*due|balance\s*due)[:\s]*[\$€£]?\s*([0-9,]+\.[0-9]{2}|[0-9]+)`
    - Number formatting: Strips currency symbols and commas (`1,250.00` -> `1250.00m`).
    - Fallback: Sum of line items if total header is omitted.
  - **Line Items**:
    - Tabular line item regex: `(?:^|\n)\s*([A-Za-z0-9\s\-_]+?)\s+(\d+)\s+(?:x|@)?\s*[\$€£]?([0-9,]+\.[0-9]{2})\s+[\$€£]?([0-9,]+\.[0-9]{2})`
- **Rationale**: Provides zero-dependency, sub-millisecond execution without requiring external OCR API keys during development and offline testing.
- **Alternatives Considered**:
  - *Cloud OCR / Azure Document Intelligence*: Adds operational cost and external network dependencies for offline/development tasks; can be added as a secondary service layer in future iterations.

---

### Decision 3: Semantic Kernel Orchestrator Integration & Description Attributes
- **Chosen**:
  - Annotate `ExtractInvoiceDetailsAsync` with `[Description("Extracts structured invoice details including InvoiceNumber, CustomerName, IssueDate, DueDate, TotalAmount, and line items from unstructured text, email body, or OCR content.")]`.
  - Annotate `rawContent` with `[Description("Raw text content of the email or OCR invoice document to parse.")]`.
  - In `SemanticKernelAgentOrchestrator`:
    - Register `_invoiceExtractorPlugin` in the Kernel via `builder.Plugins.AddFromObject(_invoiceExtractorPlugin, nameof(InvoiceExtractorPlugin))`.
    - In fallback routing, check for prompt patterns:
      `(?i)(?:process\s+(?:this\s+)?invoice|read\s+(?:this\s+)?email|extract\s+invoice\s+details)`
    - Stream thoughts via SignalR: `"Extracting invoice details from document text via InvoiceExtractorPlugin..."`.
    - Execute `ExtractInvoiceDetailsAsync(rawContent)`, format a structured summary, and track `executedActions`.
- **Rationale**: Ensures the AI planner (when OpenAI key is present) and rule-based fallback (when offline) can both trigger invoice extraction seamlessly.

---

### Decision 4: Dependency Injection & Clean Architecture Boundaries
- **Chosen**:
  - `InvoiceExtractorPlugin` lives in `SmartErpAgent.AgentEngine.Plugins`.
  - Injects `IApplicationDbContext` to access tenant context (and optionally correlate SKUs).
  - Registered as `services.AddScoped<InvoiceExtractorPlugin>()` in `AgentEngine/DependencyInjection.cs`.
- **Rationale**: Fully compliant with Platform Constitution Principles I (Clean Architecture) and V (Decoupled Semantic Kernel plugins).
