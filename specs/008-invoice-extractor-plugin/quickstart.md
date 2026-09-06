# Quickstart & Validation: Invoice Extractor Plugin (Document Intelligence to InvoiceDto)

**Feature Branch**: `008-invoice-extractor-plugin`  
**Date**: 2026-09-05

---

## 1. Prerequisites
- Docker container `smarterp-sql` or local SQL Server running.
- Backend API running or test runner available (`dotnet test`).

---

## 2. Automated Test Verification

Run all unit tests targeting the `InvoiceExtractorPlugin` and orchestrator integration:

```bash
dotnet test backend/SmartErpAgent.sln --filter "FullyQualifiedName~InvoiceExtractor"
```

**Expected Result**:
```text
Passed!  - Failed: 0, Passed: N, Skipped: 0, Total: N
```

---

## 3. Live API Validation via `curl`

Send a prompt asking the ERP agent to process an email invoice:

```bash
curl -X POST http://localhost:5009/api/agent/prompt \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" \
  -d '{
    "prompt": "Process this invoice email:\nFrom: billing@apex-mfg.com\nSubject: Invoice INV-2026-0089\nDate: 2026-09-02\nDue Date: 2026-09-16\nBill To: Apex Manufacturing Inc\nLine items:\nPrecision Ball Bearing (ABEC-7) 10 x $45.00 = $450.00\nTotal Amount: $450.00"
  }'
```

**Expected JSON Response**:
```json
{
  "content": "[Invoice Extractor]\nSuccessfully extracted invoice INV-2026-0089 for Apex Manufacturing Inc. Total: $450.00 (Due: 2026-09-16)...",
  "thoughtProcess": "Evaluated via Semantic Kernel Execution Pipeline.",
  "executedActions": [
    {
      "pluginName": "InvoiceExtractorPlugin",
      "functionName": "ExtractInvoiceDetailsAsync",
      "argumentsJson": "{...}"
    }
  ],
  "status": "Success"
}
```

---

## 4. Edge Case Validation Checklist
- [ ] Empty/whitespace text handles gracefully without throwing exceptions.
- [ ] Missing DueDate defaults to IssueDate + 14 days.
- [ ] Multiple date and currency formats ($450, 450.00 USD) normalize correctly.
