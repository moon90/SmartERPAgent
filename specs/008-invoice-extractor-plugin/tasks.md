# Tasks: Invoice Extractor Plugin (Document Intelligence to InvoiceDto)

**Feature Branch**: `008-invoice-extractor-plugin`  
**Specification**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)  
**Status**: Completed

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify and prepare shared data contracts and application models.

- [x] T001 Verify `InvoiceDto` and `InvoiceLineItemDto` definitions and required properties in `backend/src/SmartErpAgent.Application/DTOs/InvoiceDto.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core dependency injection registration required before user story integration.

- [x] T002 Verify and ensure `InvoiceExtractorPlugin` registration as a scoped service in `backend/src/SmartErpAgent.AgentEngine/DependencyInjection.cs`

---

## Phase 3: User Story 1 - Unstructured Invoice Text Parsing to Structured InvoiceDto (Priority: P1) 🎯 MVP

**Goal**: Implement `[KernelFunction]` `ExtractInvoiceDetailsAsync(string rawContent)` on `InvoiceExtractorPlugin` returning strongly-typed `InvoiceDto` with populated header fields (`InvoiceNumber`, `CustomerName`, `IssueDate`, `DueDate`, `TotalAmount`).

**Independent Test**: Supply a plain-text invoice snippet (invoice number, customer, dates, total) and assert that the returned `InvoiceDto` contains exact parsed values.

### Tests for User Story 1
- [x] T003 [P] [US1] Create unit tests for `ExtractInvoiceDetailsAsync` verifying extraction of invoice number, customer name, issue/due dates, total amount, and empty input handling in `backend/tests/SmartErpAgent.UnitTests/InvoiceExtractorPluginTests.cs`

### Implementation for User Story 1
- [x] T004 [US1] Implement `ExtractInvoiceDetailsAsync` in `InvoiceExtractorPlugin` with regex tokenizers for invoice numbers, customer names, dates, amounts, and `InvoiceDto` mapping in `backend/src/SmartErpAgent.AgentEngine/Plugins/InvoiceExtractorPlugin.cs`

**Checkpoint**: At this point, User Story 1 (MVP) is fully functional and testable independently.

---

## Phase 4: User Story 2 - Robust Pattern Recognition for Email and Document Formats (Priority: P2)

**Goal**: Enhance parser with multi-pattern heuristics for forwarded emails, multi-line item tables, currency variations, and defensive date fallbacks.

**Independent Test**: Pass varied email formats and multi-line item tables to verify `LineItems` collection and currency normalization.

### Tests for User Story 2
- [x] T005 [P] [US2] Add unit tests for forwarded email headers, multi-currency tokens ($/USD), tabular line items, and missing due date default calculation (+14 days) in `backend/tests/SmartErpAgent.UnitTests/InvoiceExtractorPluginTests.cs`

### Implementation for User Story 2
- [x] T006 [US2] Enhance regex cascading in `InvoiceExtractorPlugin` to capture tabular line items, tax, subtotal, and contact emails in `backend/src/SmartErpAgent.AgentEngine/Plugins/InvoiceExtractorPlugin.cs`

**Checkpoint**: At this point, User Stories 1 and 2 are both independently functional.

---

## Phase 5: User Story 3 - Autonomous AI Copilot Triggering & Tool Invocation (Priority: P3)

**Goal**: Decorate `ExtractInvoiceDetailsAsync` with rich `[Description]` attributes and update `SemanticKernelAgentOrchestrator` to import the plugin and route invoice processing prompts.

**Independent Test**: Send prompts like "Process this invoice email" or "Extract invoice details from this text" to the orchestrator and verify tool invocation and thought streaming.

### Tests for User Story 3
- [x] T007 [P] [US3] Add unit tests verifying orchestrator tool selection and fallback routing for invoice processing prompts in `backend/tests/SmartErpAgent.UnitTests/InvoiceExtractorPluginTests.cs`

### Implementation for User Story 3
- [x] T008 [US3] Decorate method with `[Description]` attributes and update `SemanticKernelAgentOrchestrator` to import `InvoiceExtractorPlugin` into the Kernel and wire fallback routing for invoice processing prompts in `backend/src/SmartErpAgent.AgentEngine/Services/SemanticKernelAgentOrchestrator.cs`

**Checkpoint**: All three user stories are completely integrated and accessible via conversational AI copilot.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: End-to-end verification, test suite regression checks, and documentation.

- [x] T009 [P] Verify 100% test suite pass rate across all unit tests via `dotnet test backend/SmartErpAgent.sln`
- [x] T010 Execute end-to-end quickstart validation scenarios per `specs/008-invoice-extractor-plugin/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies
- **Phase 1 (Setup)**: Completed.
- **Phase 2 (Foundational)**: Completed.
- **Phase 3 (User Story 1 - MVP)**: Completed.
- **Phase 4 (User Story 2)**: Completed.
- **Phase 5 (User Story 3)**: Completed.
- **Phase 6 (Polish)**: Completed.

### Parallel Opportunities
- T003, T005, and T007 tests were verified together within `InvoiceExtractorPluginTests.cs`.
- Setup (T001) and Foundational (T002) were validated concurrently.
