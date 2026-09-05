# Quickstart & Verification Guide: Invoice Extractor Plugin

**Feature**: `006-invoice-extractor-plugin`  
**Date**: 2026-09-05  
**Status**: Ready

---

## 1. Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server 2022 instance running locally or via Docker (`smarterp-sql` container on port 1433)
- Angular host (`smart-erp-web`) running or built

---

## 2. Automated Test Suite Execution

Run the backend unit test suite to verify `InvoiceExtractorPlugin`, SKU matching, tax calculation, and draft invoice staging:

```bash
cd backend
dotnet test SmartErpAgent.sln
```

### Expected Output
```text
Passed!  - Failed: 0, Passed: 22+, Skipped: 0, Total: 22+
- InvoiceExtractorPluginTests:
  * ExtractInvoiceFromText_ShouldExtractHeaderAndLineItems: PASSED
  * CorrelateWithInventoryCatalog_ShouldLinkMatchedSKUs: PASSED
  * CorrelateWithInventoryCatalog_ShouldEnforceTenantIsolation: PASSED
  * StageExtractedInvoice_ShouldPersistDraftInvoiceAndLineItems: PASSED
  * Orchestrator_ShouldExecuteExtractionPrompt_AndStreamMilestones: PASSED
```

---

## 3. End-to-End Chat Copilot Verification

1. **Start the API Server**:
   ```bash
   cd backend
   dotnet run --project src/SmartErpAgent.Api/SmartErpAgent.Api.csproj
   ```

2. **Start the Angular Dashboard**:
   ```bash
   cd frontend
   npx nx serve smart-erp-web
   ```

3. **Interact via the AI Copilot Drawer**:
   Submit the following prompt in the chat input:
   ```text
   Extract and stage this invoice:
   Vendor: Global Bearing Corp
   Customer: Apex Engineering (apex@example.com)
   Date: 2026-09-05
   Items:
   - SKU-123 Precision Ball Bearing, Qty: 5, Unit Price: $45.00
   ```

4. **Verify Observations in UI**:
   - Intermediate thoughts stream dynamically via SignalR:
     - *"Parsing raw text and extracting invoice line items..."*
     - *"Correlating items with tenant inventory catalog for SKU-123..."*
     - *"Staged draft invoice INV-202609-XXXX in ERP ledger."*
   - A final response confirms the created draft invoice with its total ($247.50 with 10% tax).
   - Check the invoice in the ERP Invoices table to confirm it appears under the active tenant.
