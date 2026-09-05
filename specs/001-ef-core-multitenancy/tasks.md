# Tasks: Foundational EF Core Multi-Tenancy DbContext

**Feature**: `001-ef-core-multitenancy`  
**Specification**: [specs/001-ef-core-multitenancy/spec.md](./spec.md)  
**Implementation Plan**: [specs/001-ef-core-multitenancy/plan.md](./plan.md)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Solution initialization, dependencies, and testing harness

- [X] T001 Verify project structure and layer references in `backend/SmartErpAgent.sln`
- [X] T002 Add EF Core SQL Server and Design packages to `backend/src/SmartErpAgent.Infrastructure/SmartErpAgent.Infrastructure.csproj`
- [X] T003 [P] Setup InMemory EF Core testing package in `backend/tests/SmartErpAgent.UnitTests/SmartErpAgent.UnitTests.csproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core entity and tenancy contracts required across all user stories

**⚠️ CRITICAL**: Must complete before any user story can be implemented

- [X] T004 Create `ITenantEntity` contract in `backend/src/SmartErpAgent.Core/Interfaces/ITenantEntity.cs`
- [X] T005 [P] Create `ITenantContext` interface in `backend/src/SmartErpAgent.Core/Interfaces/ITenantContext.cs`
- [X] T006 [P] Create `BaseEntity` root class in `backend/src/SmartErpAgent.Core/Entities/BaseEntity.cs`
- [X] T007 [P] Implement scoped `TenantContext` in `backend/src/SmartErpAgent.Infrastructure/Tenancy/TenantContext.cs`
- [X] T008 Create `IApplicationDbContext` contract in `backend/src/SmartErpAgent.Application/Common/Interfaces/IApplicationDbContext.cs`

**Checkpoint**: Core tenancy contracts established - user story implementation can begin.

---

## Phase 3: User Story 1 - Tenant Data Isolation on Queries (Priority: P1) 🎯 MVP

**Goal**: Complete tenant data isolation on all database queries via EF Core Global Query Filters.

**Independent Test**: Seed records for Tenant A and Tenant B; assert that querying under Tenant A context strictly yields Tenant A records and never leaks Tenant B records.

### Implementation for User Story 1

- [X] T009 [P] [US1] Create `Tenant` root entity in `backend/src/SmartErpAgent.Core/Entities/Tenant.cs`
- [X] T010 [P] [US1] Create `Invoice` and `InvoiceLineItem` entities in `backend/src/SmartErpAgent.Core/Entities/Invoice.cs`
- [X] T011 [P] [US1] Create `InventoryItem` entity in `backend/src/SmartErpAgent.Core/Entities/InventoryItem.cs`
- [X] T012 [US1] Implement `ApplicationDbContext` with EF Core Global Query Filters in `backend/src/SmartErpAgent.Infrastructure/Persistence/ApplicationDbContext.cs`
- [X] T013 [P] [US1] Configure Fluent API precision and unique indexes in `backend/src/SmartErpAgent.Infrastructure/Persistence/Configurations/InvoiceConfiguration.cs`
- [X] T014 [P] [US1] Configure Fluent API composite SKU index in `backend/src/SmartErpAgent.Infrastructure/Persistence/Configurations/InventoryItemConfiguration.cs`
- [X] T015 [US1] Implement query isolation unit test in `backend/tests/SmartErpAgent.UnitTests/TenantIsolationTests.cs`

**Checkpoint**: User Story 1 is fully functional and delivers independent data isolation (MVP ready).

---

## Phase 4: User Story 2 - Automated Tenant Identity Assignment on Insertion (Priority: P2)

**Goal**: Automatically inject the current active `TenantId` and UTC timestamps into newly added entities during `SaveChangesAsync`.

**Independent Test**: Add an entity without specifying `TenantId`; verify saved record has `TenantId` matching active `ITenantContext`.

### Implementation for User Story 2

- [X] T016 [US2] Override `SaveChangesAsync` to automatically populate `TenantId` on added `ITenantEntity` instances in `backend/src/SmartErpAgent.Infrastructure/Persistence/ApplicationDbContext.cs`
- [X] T017 [US2] Implement auto-stamping unit test in `backend/tests/SmartErpAgent.UnitTests/TenantIsolationTests.cs`

**Checkpoint**: User Stories 1 and 2 operate in tandem with automated tenant identity stamping.

---

## Phase 5: User Story 3 - Auditing and Soft-Delete Preservation (Priority: P3)

**Goal**: Automatically stamp timestamps on create/update and convert deletions into soft-deletes.

**Independent Test**: Delete an entity; verify record is marked `IsDeleted = true` and omitted from queries while preserved in database.

### Implementation for User Story 3

- [X] T018 [US3] Intercept `EntityState.Deleted` to apply soft-delete in `backend/src/SmartErpAgent.Infrastructure/Persistence/ApplicationDbContext.cs`
- [X] T019 [US3] Apply `!e.IsDeleted` filter across all entities in `backend/src/SmartErpAgent.Infrastructure/Persistence/ApplicationDbContext.cs`
- [X] T020 [US3] Implement soft-delete verification test in `backend/tests/SmartErpAgent.UnitTests/TenantIsolationTests.cs`

**Checkpoint**: All user stories functional, preserving full auditability and tenant isolation.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Integration with HTTP pipeline and end-to-end verification

- [X] T021 [P] Register DbContext and TenantContext in `backend/src/SmartErpAgent.Infrastructure/DependencyInjection.cs`
- [X] T022 [P] Implement `MultiTenantMiddleware` header extraction in `backend/src/SmartErpAgent.Api/Middlewares/MultiTenantMiddleware.cs`
- [X] T023 Configure Swagger UI with `X-Tenant-ID` header in `backend/src/SmartErpAgent.Api/Program.cs`
- [X] T024 Run quickstart verification test suite per `specs/001-ef-core-multitenancy/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1; blocks all user stories.
- **User Story 1 (Phase 3 - MVP)**: Depends on Phase 2; no dependencies on other stories.
- **User Story 2 (Phase 4)**: Depends on Phase 3 (`ApplicationDbContext` and entities).
- **User Story 3 (Phase 5)**: Depends on Phase 3 and Phase 4.
- **Polish (Phase 6)**: Depends on completion of all desired user stories.

### Parallel Execution Opportunities

- T005, T006, T007 can execute in parallel once T004 is started.
- T009, T010, T011 (Entities) can be written in parallel.
- T013, T014 (Fluent Configurations) can be written in parallel.
- T021, T022 can be written in parallel in Polish phase.

---

## Implementation Strategy

### MVP First (User Story 1 Only)
1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1 (Entities + DbContext + Query Filters + Unit Test)
4. **VALIDATE**: Run `dotnet test --filter TenantIsolationTests`

### Incremental Delivery
1. Foundation Complete → Entities & Contracts ready
2. Add US1 → Multi-tenant data isolation verified (MVP)
3. Add US2 → Automatic tenant stamping on insert verified
4. Add US3 → Soft-delete & auditing verified
5. Add Polish → Swagger header integration & end-to-end verification
