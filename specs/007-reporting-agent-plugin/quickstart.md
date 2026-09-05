# Quickstart Guide: Executive Business Intelligence Reporting Plugin

**Feature**: `007-reporting-agent-plugin`  
**Target**: Developer & QA Verification

---

## 1. Prerequisites
- Running Microsoft SQL Server container (`smarterp-sql` on port 1433) with seeded demo tenants (`ACME_CORP`, `GLOBAL_LOGISTICS`, `BIOTECH_MED`).
- .NET 8.0 SDK installed.

---

## 2. Automated Test Execution

Run the automated test suite covering unit tests for `ReportingAgentPlugin`, multi-tenant isolation, and orchestrator integration:

```bash
# Run all unit tests
dotnet test backend/SmartErpAgent.sln --filter "FullyQualifiedName~Reporting"
```

Expected result: All reporting tests pass with 0 failures.

---

## 3. Live End-to-End Validation via Web API

Start the backend API:
```bash
dotnet run --project backend/src/SmartErpAgent.Api/SmartErpAgent.Api.csproj
```

### Scenario A: Inventory Valuation Report
Query the agent for total warehouse stock valuation for `ACME_CORP`:
```bash
curl -s -X POST http://localhost:5009/api/agent/chat \
  -H "Content-Type: application/json" \
  -H "X-Tenant-ID: 11111111-1111-1111-1111-111111111111" \
  -d '{"prompt": "What is the total value of our warehouse inventory?"}' | jq
```

**Expected Outcome**:
- Response calculates the total valuation:
  - SKU-101: 45 * $125.00 = $5,625.00
  - SKU-102: 120 * $15.50 = $1,860.00
  - SKU-123: 88 * $45.00 = $3,960.00
  - SKU-LOW: 4 * $12.00 = $48.00
  - SKU-MOTOR: 12 * $850.00 = $10,200.00
  - Total = $21,693.00
- Identifies Top 3 SKUs: `SKU-MOTOR` ($10,200), `SKU-101` ($5,625), `SKU-123` ($3,960).

---

### Scenario B: Time-Bounded Financial Summary
Query the agent for 30-day financial performance:
```bash
curl -s -X POST http://localhost:5009/api/agent/chat \
  -H "Content-Type: application/json" \
  -H "X-Tenant-ID: 11111111-1111-1111-1111-111111111111" \
  -d '{"prompt": "Give me a summary of our financials for the last 30 days"}' | jq
```

**Expected Outcome**:
- Returns revenue total and count of invoices issued within the last 30 days for ACME_CORP (`INV-202608-0001` and `INV-202609-0002`).

---

### Scenario C: Multi-Tenant Isolation Verification
Query valuation for `BIOTECH_MED` (`33333333-3333-3333-3333-333333333333`):
```bash
curl -s -X POST http://localhost:5009/api/agent/chat \
  -H "Content-Type: application/json" \
  -H "X-Tenant-ID: 33333333-3333-3333-3333-333333333333" \
  -d '{"prompt": "What is our inventory valuation?"}' | jq
```

**Expected Outcome**:
- Values reflect strictly BioTech's 3 items (`SKU-REAGENT-A`, `SKU-PIPETTE-03`, `SKU-VIAL-CRY`), with zero cross-contamination from ACME_CORP.
