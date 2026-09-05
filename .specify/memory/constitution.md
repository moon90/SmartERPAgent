<!--
Sync Impact Report:
- Version Change: 1.0.0 -> 1.1.0 (MINOR: Formalized Controller Logic Isolation, strict C#/TypeScript production standards, and refined Semantic Kernel safety rules)
- Modified Principles:
  * "I. 4+1 View Model & Clean Architecture" -> "I. Clean Architecture & Controller Logic Isolation (NON-NEGOTIABLE)"
  * "II. Strict Multi-Tenancy & Zero Data Leakage" -> "II. Strict Multi-Tenancy via EF Core Global Query Filters"
  * "III. Spec-Driven Development & Strict Typing" -> "III. Strict Production-Grade Code Standards (C# & TypeScript)"
  * "IV. Modular Monorepo Governance (Nx)" -> "IV. Modular Monorepo Architecture (Angular Nx)"
  * "V. Agent Tool Decoupling & Safety" -> "V. Decoupled AI Agent Orchestration (Microsoft Semantic Kernel)"
- Added Sections: None
- Removed Sections: None
- Follow-up Deferred Items: None
-->

# Smart ERP Agent Platform Constitution

This Constitution establishes the foundational architectural principles, technical constraints, and quality standards for the **Smart ERP Agent** B2B SaaS platform. All Spec Kit specifications, implementation plans, and automated code generation must strictly comply with this document.

---

## Core Principles

### I. Clean Architecture & Controller Logic Isolation (NON-NEGOTIABLE)
API controllers MUST act purely as thin presentation adapters responsible solely for request routing, transport validation, tenant header forwarding, and HTTP status code mapping. API controllers MUST NOT contain domain logic, direct database mutations, complex conditional workflows, or AI orchestration routines. All business logic MUST reside strictly within the Application (`SmartErpAgent.Application`) and Domain (`SmartErpAgent.Core`) layers.

### II. Strict Multi-Tenancy via EF Core Global Query Filters
All tenant-scoped domain entities MUST implement `ITenantEntity` with a strongly-typed `TenantId: Guid`. The `ApplicationDbContext` in `SmartErpAgent.Infrastructure` MUST enforce global query filters (`e => !e.IsDeleted && (CurrentTenantId == null || e.TenantId == CurrentTenantId)`) preventing cross-tenant data access at the database engine level. Bypassing query filters or executing multi-tenant queries without scoped tenant context is strictly prohibited in customer-facing APIs.

### III. Strict Production-Grade Code Standards (C# & TypeScript)
- **C# / .NET 8**: All backend code MUST be production-grade with `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>` strictly enforced. Code must adhere to Clean Architecture boundaries, use strongly-typed DTOs/records, avoid raw string queries, and provide structured logging.
- **TypeScript / Angular 19**: Frontend code MUST enforce strict compiler standards (`"strict": true`, `"noImplicitOverride": true`, `"noPropertyAccessFromIndexSignature": true`, `"noImplicitReturns": true`). The use of `any` is strictly prohibited in public contracts, models, and shared library interfaces.

### IV. Modular Monorepo Architecture (Angular Nx)
The frontend MUST follow an Nx monorepo architecture (`frontend/`) where the host application (`apps/smart-erp-web`) remains a lean shell. Reusable business logic, models, signals-based state, and UI primitives MUST be encapsulated into modular workspace libraries (`@smart-erp/data-access`, `@smart-erp/ui`, `@smart-erp/feature-chat`) under `libs/` to enable horizontal scaling and future micro-frontends.

### V. Decoupled AI Agent Orchestration (Microsoft Semantic Kernel)
All autonomous multi-agent reasoning, function calling, and workflow execution MUST be orchestrated using Microsoft Semantic Kernel in `SmartErpAgent.AgentEngine`. Agent plugins MUST interact strictly through scoped application interfaces (`IApplicationDbContext`, Repositories), maintaining tenant boundaries. Direct execution of arbitrary or unvalidated database queries by LLM agents is strictly prohibited.

---

## Technology Stack & Architectural Constraints
- **Backend Architecture**: ASP.NET Core 8 Web API, C# 12, Clean Architecture (`Core`, `Application`, `Infrastructure`, `AgentEngine`, `Api`).
- **Frontend Architecture**: Angular 19+ (Standalone components, Signals, SCSS, Nx Monorepo 23+).
- **Persistence Layer**: Microsoft SQL Server 2022 / Azure SQL Database managed via Entity Framework Core 8.
- **AI Layer**: Microsoft Semantic Kernel 1.40+ (.NET) with auto-invoked domain plugins.
- **Specification System**: GitHub Spec Kit (`specify-cli`) enforcing Spec-Driven Development (SDD).

---

## Development Workflow & Quality Gates
1. **Spec-Driven Precedence**: Requirements must be captured in `.specify/specs/` using `/speckit-specify` before writing implementation code.
2. **Architecture Review**: Plans must verify compliance with this Constitution using `/speckit-plan`.
3. **Automated Verification**: Backend test suites (`dotnet test`) and frontend compilation (`npx nx build`) must succeed with 0 errors and 0 warnings before any pull request or deployment.

---

## Governance
This Constitution is the highest-ranking architectural authority for the Smart ERP Agent platform and supersedes ad-hoc implementation choices. Any modification to these principles requires formal documentation, impact analysis, and a semantic version increment.

**Version**: 1.1.0 | **Ratified**: 2026-09-05 | **Last Amended**: 2026-09-05
