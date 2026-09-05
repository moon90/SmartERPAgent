# Quickstart & Verification Guide: Foundational EF Core Multi-Tenancy

**Feature**: `001-ef-core-multitenancy`  
**Date**: 2026-09-05

---

## 1. Automated Test Execution

The multi-tenant isolation rules and automatic `TenantId` stamping are validated via automated xUnit tests in `backend/tests/SmartErpAgent.UnitTests`.

### Run Test Suite
```bash
cd backend
dotnet test --filter "FullyQualifiedName~TenantIsolationTests"
```

### Expected Output
```text
Test run for .../SmartErpAgent.UnitTests.dll (.NETCoreApp,Version=v8.0)
Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2
```

---

## 2. API End-to-End Verification

### A. Run Web API Host
```bash
cd backend
dotnet run --project src/SmartErpAgent.Api/SmartErpAgent.Api.csproj
```

### B. Verify Multi-Tenancy Scenarios

#### Scenario 1: Access Invoices without Tenant Header (Should Fail)
```bash
curl -X GET http://localhost:5000/api/invoices
```
**Expected Response**: `400 Bad Request`  
```json
{
  "message": "Header 'X-Tenant-ID' is required to access tenant invoices."
}
```

#### Scenario 2: Access Invoices with Valid Tenant Header (Should Return Tenant Records)
```bash
curl -X GET http://localhost:5000/api/invoices \
  -H "X-Tenant-ID: <TENANT_A_GUID>"
```
**Expected Response**: `200 OK` with JSON array containing only invoices belonging to `<TENANT_A_GUID>`.

#### Scenario 3: Create an Inventory Item (Auto-stamps Tenant ID)
```bash
curl -X POST http://localhost:5000/api/inventory \
  -H "Content-Type: application/json" \
  -H "X-Tenant-ID: <TENANT_A_GUID>" \
  -d '{
    "sku": "WIDGET-X",
    "name": "Industrial Widget X",
    "description": "High precision component",
    "unitPrice": 45.00,
    "stockQuantity": 50,
    "reorderThreshold": 10
  }'
```
**Expected Response**: `200 OK` with `tenantId` matching `<TENANT_A_GUID>`.
