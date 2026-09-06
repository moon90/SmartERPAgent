# Quickstart & Validation: Automated Restocking & Purchase Order Agent Plugin

**Feature Branch**: `009-purchase-order-agent`  
**Date**: 2026-09-06

---

## 1. Prerequisites
- Docker container `smarterp-sql` running on `localhost:1433`.
- Backend API running on `http://localhost:5009`.

---

## 2. Automated Test Verification

Run all unit tests targeting `PurchaseOrderAgentPlugin` and its orchestrator integration:

```bash
dotnet test backend/SmartErpAgent.sln --filter "FullyQualifiedName~PurchaseOrder"
```

**Expected Result**:
```text
Passed!  - Failed: 0, Passed: N, Skipped: 0, Total: N
```

---

## 3. Live API Validation via `curl`

### Scenario A: Inquire About Low-Stock Items
```bash
curl -X POST http://localhost:5009/api/agent/prompt \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" \
  -d '{"prompt": "Which items are low in stock?"}'
```

**Expected Response**:
```text
[Smart ERP Agent - Low Stock Alerts]
Found 1 low-stock item requiring replenishment:
  - Industrial Lithium Grease Cartridge 400g (SKU: SKU-LOW): 4 units available (Threshold: 25).
    Suggested Reorder: 46 units @ $12.00 = $552.00
```

---

### Scenario B: Generate Draft Purchase Order to Replenish Inventory
```bash
curl -X POST http://localhost:5009/api/agent/prompt \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" \
  -d '{"prompt": "Generate draft purchase orders to replenish our inventory"}'
```

**Expected Response**:
```text
[Smart ERP Agent - Purchase Order Created]
Draft Purchase Order PO-202609-0001 successfully generated and saved in ERP ledger!
Supplier: Primary Replenishment Supplier
Total Cost: USD $552.00 across 1 line item(s).
Line items:
  - SKU-LOW: 46 units @ $12.00 = $552.00
```

---

## 4. Database Verification
Verify the persisted Purchase Order in SQL Server:
```sql
SELECT Id, TenantId, OrderNumber, SupplierName, TotalAmount, Status, CreatedAtUtc 
FROM SmartErpAgentDb.dbo.PurchaseOrders;
```
