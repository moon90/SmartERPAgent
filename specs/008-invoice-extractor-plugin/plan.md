# Implementation Plan: Invoice Extractor Plugin (Document Intelligence to InvoiceDto)

**Branch**: `008-invoice-extractor-plugin` | **Date**: 2026-09-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/008-invoice-extractor-plugin/spec.md`

---

## Summary

Implement `ExtractInvoiceDetailsAsync` on the native C# `InvoiceExtractorPlugin` in `SmartErpAgent.AgentEngine`. The method parses unstructured invoice text (emails, OCR text, receipts) via regular expressions and heuristic tokenizers, returning a strongly-typed `InvoiceDto` with populated header fields (`InvoiceNumber`, `CustomerName`, `IssueDate`, `DueDate`, `TotalAmount`) and line items. The plugin is decorated with descriptive semantic attributes, registered in DI, and imported into `SemanticKernelAgentOrchestrator` with autonomous keyword fallback routing.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8 (`<Nullable>enable</Nullable>`)  
**Primary Dependencies**: Microsoft Semantic Kernel 1.40+, Entity Framework Core 8, System.Text.Json, System.Text.RegularExpressions  
**Storage**: Microsoft SQL Server 2022 (via `IApplicationDbContext`)  
**Testing**: xUnit, FluentAssertions, Moq (`dotnet test`)  
**Target Platform**: ASP.NET Core 8 Web API  
**Project Type**: Clean Architecture enterprise ERP micro-service  
**Performance Goals**: Sub-50ms document parsing for typical invoice/email bodies up to 100KB text  
**Constraints**: Zero unhandled exceptions on malformed text, strict multi-tenant context tagging, zero raw SQL queries  
**Scale/Scope**: Supports standard B2B invoice email and text formats across all tenant organizations  

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate Status | Architecture Compliance Notes |
| :--- | :--- | :--- |
| **I. Clean Architecture & Controller Isolation** | ✅ PASSED | Extraction logic resides in `SmartErpAgent.AgentEngine` and uses `SmartErpAgent.Application.DTOs`. Controllers remain pure transport adapters. |
| **II. Strict Multi-Tenancy** | ✅ PASSED | `InvoiceDto` stamps `TenantId` from the ambient tenant context (`ITenantContext`), preventing data leakage. |
| **III. Strict Production-Grade Code Standards** | ✅ PASSED | Strict C# nullable reference types, strongly typed records, defensive input validation, and zero compiler warnings. |
| **IV. Modular Monorepo Architecture** | ✅ PASSED | Backend API contracts mirror standard JSON representations consumable by `@smart-erp/data-access`. |
| **V. Decoupled AI Agent Orchestration** | ✅ PASSED | Plugin functions use `[KernelFunction]` and `[Description]` attributes, imported into Semantic Kernel without coupling to specific LLM models. |

---

## Project Structure

### Documentation (this feature)

```text
specs/008-invoice-extractor-plugin/
├── spec.md                              # Functional requirements and user stories
├── plan.md                              # This implementation plan
├── research.md                          # Architectural decisions and regex strategies
├── data-model.md                        # InvoiceDto and extraction entity mappings
├── contracts/
│   └── invoice-extractor-plugin.json    # JSON tool definition schema
├── checklists/
│   └── requirements.md                  # Quality specification checklist
└── quickstart.md                        # Verification guide and test commands
```

### Source Code Touch-points

```text
backend/
├── src/
│   ├── SmartErpAgent.Application/
│   │   └── DTOs/
│   │       └── InvoiceDto.cs            # Verified existing strongly-typed InvoiceDto and InvoiceLineItemDto
│   └── SmartErpAgent.AgentEngine/
│       ├── Plugins/
│       │   └── InvoiceExtractorPlugin.cs# Add ExtractInvoiceDetailsAsync returning InvoiceDto
│       ├── DependencyInjection.cs       # Verify InvoiceExtractorPlugin is registered
│       └── Services/
│           └── SemanticKernelAgentOrchestrator.cs # Import plugin into Kernel + add fallback trigger
└── tests/
    └── SmartErpAgent.UnitTests/
        └── InvoiceExtractorPluginTests.cs# Add comprehensive unit tests for regex and DTO mapping
```

---

## Implementation Phases

### Phase 1: Plugin Method Implementation (`ExtractInvoiceDetailsAsync`)
- Add `ExtractInvoiceDetailsAsync(string rawContent, CancellationToken cancellationToken = default)` to `InvoiceExtractorPlugin`.
- Decorate with `[KernelFunction]` and descriptive `[Description]` attributes highlighting triggers: "process an invoice", "read this email", "extract invoice details".
- Implement robust regex extraction for:
  - `InvoiceNumber`: `(?i)(?:invoice\s*(?:number|no|#)?[:\s-]*|inv[:\s#-]*)([A-Za-z0-9_-]+)`
  - `CustomerName`: `(?i)(?:bill\s*to|customer|client|sold\s*to|attention|att)[:\s]+([^\r\n,;]+)`
  - `CustomerEmail`: Standard email regex match
  - `IssueDate` & `DueDate`: US and ISO date format tokenizers with 14-day default fallback
  - `TotalAmount`: `(?i)(?:total|amount\s*due|balance\s*due)[:\s]*[\$€£]?\s*([0-9,]+\.[0-9]{2}|[0-9]+)`
  - `LineItems`: Tabular regex line item parser capturing Description, Quantity, UnitPrice, and TotalPrice.
- Return strongly-typed `InvoiceDto` with populated fields and `InvoiceStatus.Draft`.

### Phase 2: Dependency Injection & Orchestrator Integration
- Verify `services.AddScoped<InvoiceExtractorPlugin>()` in `DependencyInjection.cs`.
- In `SemanticKernelAgentOrchestrator.cs`:
  - Ensure `_invoiceExtractorPlugin` is registered via `builder.Plugins.AddFromObject(_invoiceExtractorPlugin, nameof(InvoiceExtractorPlugin))`.
  - Update offline fallback routing to match prompts with "process invoice", "read this email", or "extract invoice details".
  - Stream cognitive progress message via SignalR and return formatted invoice summary.

### Phase 3: Verification & Test Automation
- Create `InvoiceExtractorPluginTests.cs` covering:
  - Standard invoice email text extraction
  - OCR plain text invoice extraction
  - Graceful handling of empty or whitespace text
  - Date format parsing and default fallbacks
  - Currency symbol stripping and line item math
  - Orchestrator prompt routing to `InvoiceExtractorPlugin`
- Execute full test suite `dotnet test backend/SmartErpAgent.sln` to guarantee 100% pass rate.
