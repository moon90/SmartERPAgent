# Tasks: Invoice Extractor & Document Intelligence Plugin

**Feature**: `006-invoice-extractor-plugin`  
**Specification**: [specs/006-invoice-extractor-plugin/spec.md](./spec.md)  
**Implementation Plan**: [specs/006-invoice-extractor-plugin/plan.md](./plan.md)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize extraction DTOs and DI wiring

- [ ] T001 Create extraction DTO file in `backend/src/SmartErpAgent.Application/DTOs/InvoiceExtractionDto.cs`
- [ ] T002 [P] Register `InvoiceExtractorPlugin` in DI container in `backend/src/SmartErpAgent.AgentEngine/DependencyInjection.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core DTO models and plugin structure

**⚠️ CRITICAL**: Must complete before any user story can be implemented

- [ ] T003 Implement `ExtractedInvoiceData` and `ExtractedInvoiceLineItem` classes in `backend/src/SmartErpAgent.Application/DTOs/InvoiceExtractionDto.cs`
- [ ] T004 Create `InvoiceExtractorPlugin` class skeleton with `IApplicationDbContext` injection in `backend/src/SmartErpAgent.AgentEngine/Plugins/InvoiceExtractorPlugin.cs`

**Checkpoint**: Core extraction models and plugin skeleton ready - user story implementation can begin.

---

## Phase 3: User Story 1 - Intelligent Invoice Text Parsing & Extraction (Priority: P1) 🎯 MVP

**Goal**: Parse raw unstructured text or email body into structured invoice headers and line items.

**Independent Test**: Provide unstructured invoice text and verify `ExtractInvoiceFromTextAsync` returns valid JSON with customer name, email, dates, and item breakdown.

### Implementation for User Story 1

- [ ] T005 [US1] Implement `ExtractInvoiceFromTextAsync` regex and heuristic text parsing engine in `backend/src/SmartErpAgent.AgentEngine/Plugins/InvoiceExtractorPlugin.cs`
- [ ] T006 [P] [US1] Add unit tests for text parsing and line item extraction in `backend/tests/SmartErpAgent.UnitTests/InvoiceExtractorTests.cs`
- [ ] T007 [US1] Register `InvoiceExtractorPlugin` in `SemanticKernelAgentOrchestrator` constructor and plugin collection in `backend/src/SmartErpAgent.AgentEngine/Services/SemanticKernelAgentOrchestrator.cs`

**Checkpoint**: Text parsing and line item extraction functional and independently tested (MVP ready).

---

## Phase 4: User Story 2 - SKU Matching & Catalog Item Correlation (Priority: P2)

**Goal**: Correlate extracted line items with tenant inventory catalog (`InventoryItems`) using SKU and product name matching.

**Independent Test**: Parse text with known SKU "SKU-123" and verify `MatchedInventoryItemId` and catalog pricing are linked under the active tenant.

### Implementation for User Story 2

- [ ] T008 [US2] Implement `CorrelateWithInventoryCatalogAsync` with exact SKU and product name matching in `backend/src/SmartErpAgent.AgentEngine/Plugins/InvoiceExtractorPlugin.cs`
- [ ] T009 [P] [US2] Add unit tests for SKU correlation and tenant isolation in `backend/tests/SmartErpAgent.UnitTests/InvoiceExtractorTests.cs`

**Checkpoint**: Catalog correlation and multi-tenant inventory validation functional.

---

## Phase 5: User Story 3 - Autonomous Draft Invoice Staging & Database Persistence (Priority: P3)

**Goal**: Persist validated invoice data into `Invoices` and `InvoiceLineItems` tables with status `Draft` and multi-tenant isolation.

**Independent Test**: Call `StageExtractedInvoiceAsync` with validated data and assert new invoice and line item records exist in SQL Server for the current tenant.

### Implementation for User Story 3

- [ ] T010 [US3] Implement `StageExtractedInvoiceAsync` creating `Invoice` and `InvoiceLineItems` entities in `backend/src/SmartErpAgent.AgentEngine/Plugins/InvoiceExtractorPlugin.cs`
- [ ] T011 [P] [US3] Add unit tests for draft invoice staging and line item persistence in `backend/tests/SmartErpAgent.UnitTests/InvoiceExtractorTests.cs`
- [ ] T012 [US3] Add intermediate thought process streaming via `ReceiveThoughtProcess` during extraction, correlation, and staging in `backend/src/SmartErpAgent.AgentEngine/Services/SemanticKernelAgentOrchestrator.cs`

**Checkpoint**: End-to-end extraction and draft invoice persistence in ERP database validated.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: DI container verification and full test suite execution

- [ ] T013 [P] Verify DI resolution of `InvoiceExtractorPlugin` from service container in `backend/tests/SmartErpAgent.UnitTests/InvoiceExtractorTests.cs`
- [ ] T014 Execute full backend test suite (`dotnet test backend/SmartErpAgent.sln`) and quickstart validation per `specs/006-invoice-extractor-plugin/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 - BLOCKS all user stories.
- **User Story 1 (Phase 3 - MVP)**: Can start after Phase 2 is complete.
- **User Story 2 (Phase 4)**: Depends on Phase 3 extraction output.
- **User Story 3 (Phase 5)**: Depends on Phase 4 validated data.
- **Polish (Phase 6)**: Final DI and full suite execution.

### Parallel Execution Opportunities

- T002 can run in parallel with T001.
- T006 can run in parallel with T005.
- T009 can run in parallel with T008.
- T011 can run in parallel with T010.
- T013 can run in parallel with T014.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (`InvoiceExtractionDto.cs`, DI registration)
2. Complete Phase 2: Foundational (DTO classes, Plugin skeleton)
3. Complete Phase 3: User Story 1 (`ExtractInvoiceFromTextAsync`, unit tests)
4. **VALIDATE**: Run `dotnet test backend/SmartErpAgent.sln`.

### Incremental Delivery

1. Setup + Foundation Complete → DTOs and plugin skeleton ready
2. Add US1 → Text parsing and line item extraction (MVP)
3. Add US2 → SKU matching and inventory catalog correlation
4. Add US3 → Database draft invoice staging and SignalR thought streaming
5. Polish → Full test suite verification and build
