# Research & Technical Decisions: Invoice Extractor & Document Intelligence Plugin

**Feature**: `006-invoice-extractor-plugin`  
**Date**: 2026-09-05  
**Status**: Completed

---

## 1. Information Extraction Strategy (LLM + Deterministic Parser)

### Context & Challenge
In an enterprise ERP copilot, users paste raw invoice receipts, email purchase orders, or OCR transcripts containing tabular and free-form data. The system must extract structured data reliably both in production with an LLM (Semantic Kernel) and in automated testing / offline environments without live cloud credentials.

### Decision
Implement a **dual-engine extraction pipeline** inside `InvoiceExtractorPlugin`:
1. **Semantic Kernel LLM Function Calling**: When an active LLM connector (OpenAI / Azure OpenAI) is present, the agent uses structured prompt schemas to extract invoice headers and tabular line items.
2. **Deterministic Heuristic / Regex Fallback**: A built-in regex-based parser that handles common invoice patterns (Customer name, email, dates, tabular lines formatted as `[Description] [Qty] [Unit Price]`) enabling 100% reliable local unit testing without cloud API keys.

### Rationale
- Complies with Constitution Principle V (Decoupled AI Agent Orchestration) and Principle III (Production-grade standards).
- Guarantees that automated CI/CD test suites run deterministically in sub-second time without external network calls or API costs.

---

## 2. SKU Catalog Matching & Inventory Correlation

### Context & Challenge
Extracted invoice line items must correlate with the tenant's actual inventory database (`InventoryItems`) without corrupting line items that are custom services or non-catalog parts.

### Decision
- **Phase 1 (Exact Match)**: Search `_dbContext.InventoryItems` where `SKU == extractedCode`.
- **Phase 2 (Name Match)**: Search `_dbContext.InventoryItems` where `Name` is contained within the line item description.
- **Correlation Result**:
  - If match is found: Assign `InventoryItemId = item.Id` and cross-reference `catalogPrice` against `extractedUnitPrice`. If a discrepancy > 10% is detected, attach a warning flag.
  - If no match is found: Set `InventoryItemId = null` and retain as a custom non-inventory line item.
- **Tenant Isolation**: All queries run through EF Core with Global Query Filters (`TenantId == CurrentTenantId`), preventing cross-tenant leakage.

---

## 3. Invoice Staging & Database Persistence

### Context & Challenge
Extracted invoices should not be marked as `Paid` or `Sent` immediately; they must be staged as `Draft` for accounting review.

### Decision
`StageExtractedInvoiceAsync`:
1. Validates mathematical consistency: `Sum(LineItem.TotalPrice) == SubTotal`.
2. Automatically calculates tax (default 10% unless specified) and sets `TotalAmount = SubTotal + TaxAmount`.
3. Formats sequential invoice number: `INV-{yyyyMM}-{Count + 1:D4}`.
4. Persists the `Invoice` with status `InvoiceStatus.Draft` and links child `InvoiceLineItem` entities.
5. Emits a structured JSON confirmation containing `InvoiceId`, `InvoiceNumber`, and total item count.

---

## 4. SignalR Thought Streaming Integration

### Context & Challenge
Invoice extraction involves multiple processing steps (text parsing, SKU cross-referencing, mathematical verification, database staging). The operator should see these milestones stream in real time.

### Decision
The `SemanticKernelAgentOrchestrator` captures each phase and streams intermediate thoughts over `/hubs/agent` via `ReceiveThoughtProcess`:
1. `"Parsing invoice document and extracting customer details and line items..."`
2. `"Cross-referencing extracted items with active tenant inventory catalog..."`
3. `"Mathematical validation: Subtotal and Tax verified..."`
4. `"Staged draft invoice {InvoiceNumber} in ERP ledger."`
