# Smart ERP Agent (B2B SaaS Multi-Agent Platform)

An enterprise-grade B2B SaaS multi-agent automation platform designed using the **4+1 Architectural View Model**, featuring an **Nx monorepo with Angular host and modular workspace libraries**, decoupled **ASP.NET Core 8 Web API** Clean Architecture, **Microsoft SQL Server managed via EF Core** with strict multi-tenancy isolation, and **Microsoft Semantic Kernel** for agent orchestration.

---

## Architecture Overview (4+1 View Model)

```
+---------------------------------------------------------------------------------+
|                                USE CASE VIEW (+1)                               |
|   - Multi-tenant ERP Management (Invoices, Inventory, Tenant Isolation)        |
|   - Autonomous Agent Actions (Stock reorder alerts, Draft Invoices)             |
+---------------------------------------------------------------------------------+
         |                                                               |
         v                                                               v
+------------------------------------+          +------------------------------------+
|            LOGICAL VIEW            |          |            PROCESS VIEW            |
| - Domain Entities & Abstractions   |          | - Multi-tenant Resolution Pipeline |
| - Application Use Cases & DTOs     |          | - Async Event & Agent Loop         |
| - Semantic Kernel Agent Plugins    |          | - Global EF Core Query Filters     |
| - Strict TypeScript Contracts      |          | - Non-blocking RESTful Execution   |
+------------------------------------+          +------------------------------------+
         |                                                               |
         v                                                               v
+------------------------------------+          +------------------------------------+
|          DEVELOPMENT VIEW          |          |           PHYSICAL VIEW            |
| - Nx Monorepo (apps & libs)        |          | - Angular Host (SPA / CDN / Nginx) |
| - Decoupled ASP.NET Core Solution  |          | - Web API Container (Kestrel)      |
| - Typed Specs (Spec Kit Ready)     |          | - Microsoft SQL Server (Azure/RDS) |
| - Modular Shared Workspace Libs    |          | - LLM Endpoints (OpenAI / Azure)   |
+------------------------------------+          +------------------------------------+
```

---

## Directory Structure & Component Mapping

```
Smart ERP Agent/
├── frontend/                                   # Nx Monorepo (Angular 19+)
│   ├── apps/
│   │   └── smart-erp-web/                      # Angular Host Dashboard Application
│   │       └── src/app/                        # Standalone host component & layout
│   ├── libs/                                   # Shared Workspace Libraries
│   │   ├── shared/
│   │   │   ├── data-access/                    # @smart-erp/data-access (Models, Signals, Interceptors)
│   │   │   └── ui/                             # @smart-erp/ui (BadgeComponent, TenantSelectorComponent)
│   │   └── agents/
│   │       └── feature-chat/                   # @smart-erp/feature-chat (AI Copilot drawer & tool logs)
│   ├── nx.json
│   ├── package.json
│   └── tsconfig.base.json                      # Strict mode enabled for Spec-Driven Development
│
├── backend/                                    # ASP.NET Core 8 Web API Solution
│   ├── SmartErpAgent.sln
│   ├── src/
│   │   ├── SmartErpAgent.Core/                 # Domain Entities (Tenant, Invoice, InventoryItem)
│   │   ├── SmartErpAgent.Application/          # Contracts (IApplicationDbContext, IAgentOrchestrator)
│   │   ├── SmartErpAgent.Infrastructure/       # EF Core SQL Server DbContext, Tenant Query Filters
│   │   ├── SmartErpAgent.AgentEngine/          # Semantic Kernel, Native Plugins (Invoice, Inventory)
│   │   └── SmartErpAgent.Api/                  # Web API Host, MultiTenantMiddleware, Controllers
│   └── tests/
│       └── SmartErpAgent.UnitTests/            # Multi-tenancy isolation and auto-stamping unit tests
│
└── README.md
```

---

## Exact CLI Commands Executed to Scaffold

### 1. Backend Solution & Decoupled Clean Architecture
```bash
# ১. ডিরেক্টরি ও সলিউশন ফাইল তৈরি
mkdir -p backend/src backend/tests
cd backend
dotnet new sln -n SmartErpAgent

# ২. লেয়ারগুলো (Class Libraries ও Web API) তৈরি
dotnet new classlib -n SmartErpAgent.Core -o src/SmartErpAgent.Core -f net8.0
dotnet new classlib -n SmartErpAgent.Application -o src/SmartErpAgent.Application -f net8.0
dotnet new classlib -n SmartErpAgent.Infrastructure -o src/SmartErpAgent.Infrastructure -f net8.0
dotnet new classlib -n SmartErpAgent.AgentEngine -o src/SmartErpAgent.AgentEngine -f net8.0
dotnet new webapi -n SmartErpAgent.Api -o src/SmartErpAgent.Api -f net8.0 --use-controllers
dotnet new xunit -n SmartErpAgent.UnitTests -o tests/SmartErpAgent.UnitTests -f net8.0

# ৩. লেয়ারগুলোর মধ্যে ডিপেন্ডেন্সি রেফারেন্স যুক্ত করা (Clean Architecture)
# Application লেয়ার Core-এর উপর নির্ভরশীল
dotnet add src/SmartErpAgent.Application reference src/SmartErpAgent.Core

# Infrastructure লেয়ার Core ও Application-এর উপর নির্ভরশীল
dotnet add src/SmartErpAgent.Infrastructure reference src/SmartErpAgent.Core src/SmartErpAgent.Application

# AgentEngine লেয়ার Core ও Application-এর উপর নির্ভরশীল
dotnet add src/SmartErpAgent.AgentEngine reference src/SmartErpAgent.Core src/SmartErpAgent.Application

# API (Presentation) লেয়ার সবগুলোর উপর নির্ভরশীল
dotnet add src/SmartErpAgent.Api reference src/SmartErpAgent.Application src/SmartErpAgent.Infrastructure src/SmartErpAgent.AgentEngine

# ৪. সলিউশন ফাইলে সবগুলো প্রজেক্ট যুক্ত করা
dotnet sln add src/SmartErpAgent.Core src/SmartErpAgent.Application src/SmartErpAgent.Infrastructure src/SmartErpAgent.AgentEngine src/SmartErpAgent.Api tests/SmartErpAgent.UnitTests

# ৫. প্রয়োজনীয় এন্টারপ্রাইজ প্যাকেজ যুক্ত করা
# EF Core এবং SQL Server (Infrastructure লেয়ারে)
dotnet add src/SmartErpAgent.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/SmartErpAgent.Infrastructure package Microsoft.EntityFrameworkCore.Design

# Semantic Kernel (AgentEngine লেয়ারে)
dotnet add src/SmartErpAgent.AgentEngine package Microsoft.SemanticKernel

# API প্রজেক্টে EF Core Design টুল (মাইগ্রেশন রান করার জন্য)
dotnet add src/SmartErpAgent.Api package Microsoft.EntityFrameworkCore.Design
```

### 2. Frontend Nx Monorepo & Modular Workspace Libraries

```bash
# Nx workspace এবং Angular host অ্যাপ তৈরি (Modular Monorepo Architecture)
npx create-nx-workspace@latest frontend \
  --preset=angular-monorepo \
  --appName=smart-erp-web \
  --style=scss \
  --routing=true \
  --standaloneApi=true \
  --e2eTestRunner=none \
  --interactive=false \
  --skipGit=true \
  --nxCloud=skip

cd frontend

# মডুলার লাইব্রেরিগুলো তৈরি করা (Nx v23+ syntax with importPath mapping)
npx nx g @nx/angular:library --name=data-access --directory=libs/shared/data-access --importPath=@smart-erp/data-access --standalone=true --interactive=false
npx nx g @nx/angular:library --name=ui --directory=libs/shared/ui --importPath=@smart-erp/ui --standalone=true --interactive=false
npx nx g @nx/angular:library --name=feature-chat --directory=libs/agents/feature-chat --importPath=@smart-erp/feature-chat --standalone=true --interactive=false

cd ..
```

> **Note on `--preset=angular-monorepo` vs `--preset=angular-standalone`**:
> - `--preset=angular-monorepo` creates an enterprise multi-project workspace (`apps/smart-erp-web` and `libs/`), allowing future micro-frontends and isolated shared libraries.
> - `--preset=angular-standalone` creates a single-app flat repository where code lives at the root `src/`, which restricts multi-app scalability.
> - In Nx v23+, the generator requires explicit `--name` and `--directory` flags rather than positional arguments.

---

## Local Development Quickstart

### 1. Run the Backend Web API
```bash
cd backend
dotnet run --project src/SmartErpAgent.Api/SmartErpAgent.Api.csproj
```
- Swagger UI will be available at: `https://localhost:5001/swagger` (or `http://localhost:5000/swagger`)
- Health Check: `http://localhost:5000/health`

### 2. Run the Angular Frontend
```bash
cd frontend
npx nx serve smart-erp-web
```
- Frontend application will open at: `http://localhost:4200`

### 3. Run Backend Unit Tests
```bash
cd backend
dotnet test SmartErpAgent.sln
```

---

## Spec-Driven Development (GitHub Spec Kit)

This repository is configured with **[GitHub Spec Kit](https://github.com/github/spec-kit)** to practice **Spec-Driven Development (SDD)** with AI coding agents (Antigravity, GitHub Copilot, Cursor, Claude Code).

### Installation & Initialization
```bash
# ১. uv ব্যবহার করে specify-cli ইনস্টল করা
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git

# ২. প্রজেক্টে Spec Kit সক্রিয় করা (Antigravity & Copilot ইন্টিগ্রেশন)
specify init --here --force --non-interactive --integration agy
specify integration install copilot --force
```

### SDD ডেভলপমেন্ট ওয়ার্কফ্লো
AI এজেন্টের সাথে কাজ করার জন্য নিচের স্ল্যাশ কমান্ড বা স্কিলগুলো ব্যবহার করুন:
1. `/speckit-constitution` - প্রজেক্টের কোর আর্কিটেকচারাল নিয়মাবলী ও প্রিন্সিপাল আপডেট করা (`.specify/memory/constitution.md`)।
2. `/speckit-specify` - নতুন ফিচার বা মডিউলের জন্য স্পেসিফিকেশন তৈরি করা (`.specify/specs/`)।
3. `/speckit-plan` - স্পেক অনুযায়ী টেকনিক্যাল ইমপ্লিমেন্টেশন প্ল্যান তৈরি করা।
4. `/speckit-tasks` - প্ল্যানকে ছোট ছোট অ্যাকশনেবল টাস্কে ভাগ করা।
5. `/speckit-implement` - স্পেক ও কনস্টিটিউশন অনুসরণ করে স্বয়ংক্রিয়ভাবে কোড তৈরি ও এক্সিকিউট করা।
6. `/speckit-converge` - কোডবেস অডিট করে অবশিষ্ট কাজগুলো টাস্ক হিসেবে চিহ্নিত করা।

