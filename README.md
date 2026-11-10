# EShopClaude

Distributed EShop microservices on **.NET 10** and **.NET Aspire 13**, built with **Claude Code** and **Spec-Driven Development (SDD)**.

> ### 🎓 Udemy Course
> This repository is the source code of the Udemy course
> **[Agentic Coding with Claude Code: Spec-Driven Microservices](https://www.udemy.com/course/agentic-coding-with-claude-code-spec-driven-microservices/)**.
>
> In the course we build this whole solution step by step with Claude Code, driven by written specs instead of ad-hoc prompts.
> Every commit, branch and spec in this repo is a step you'll see in the lectures.

---

## What this repository is

EShopClaude is a deliberately **small** e-commerce demo. The point isn't the shop; it's **how it was built**:

- **Spec first.** Every change starts as a Markdown spec. Claude Code reads the spec, proposes a plan in plan mode, a human approves it, and only then does Claude implement it.
- **One spec = one sub-feature = one branch = one merge.** Each spec touches exactly one microservice, and `main` always builds and runs.
- **Claude Code as a teammate.** The repo includes the project memory (`CLAUDE.md`), custom skills (slash commands) and subagents that automate the spec cycle.

The result is three Minimal API microservices and a Blazor storefront, wired together by .NET Aspire and talking over plain synchronous HTTP.

## Features (use cases)

1. **List products:** browse the seeded catalog.
2. **Add to cart / remove a line:** the price is snapshotted into the basket when you add an item.
3. **Checkout:** place an order from the basket.
4. **See my orders:** the order list and the order detail.

The buyer is hard-coded as `demo-user`. Auth, payments, messaging, migrations and the like are **out of scope on purpose** (see [PROJECT.md §2](_agenticdev/PROJECT.md)).

## Architecture

```
Web (Blazor Server) --HTTP--> catalog-api  --> PostgreSQL / catalogdb
                    --HTTP--> basket-api   --> Redis / basket-cache
                                 |--HTTP--> catalog-api   (product lookup + price snapshot on add)
                                 |--HTTP--> ordering-api  (create order on checkout)
                    --HTTP--> ordering-api --> PostgreSQL / orderingdb
```

| Project | Kind | Owns |
|---|---|---|
| `EShopClaude.AppHost` | .NET Aspire host | Resource wiring (Postgres + pgAdmin, Redis, services) |
| `EShopClaude.ServiceDefaults` | Library | OpenTelemetry, health checks, resilience, service discovery |
| `EShopClaude.Catalog.Api` | Minimal API | Read-only, seeded product catalog (PostgreSQL `catalogdb`) |
| `EShopClaude.Basket.Api` | Minimal API | Baskets in Redis; orchestrates checkout |
| `EShopClaude.Ordering.Api` | Minimal API | Orders and order lines (PostgreSQL `orderingdb`) |
| `EShopClaude.Web` | Blazor Interactive Server | Storefront UI only, no data of its own |
| `EShopClaude.Tests` | xUnit + Aspire.Hosting.Testing | End-to-end test |

Key design rules:

- **Each service owns its data.** No service reads another service's database.
- **Aspire resource names are contracts:** `catalog-api`, `basket-api`, `ordering-api`, `webfrontend`, `postgres`, `catalogdb`, `orderingdb`, `basket-cache`. Clients resolve services as `https+http://<resource-name>`.
- **Checkout is orchestrated by Basket.Api:** read the basket (`400` if empty), `POST /orders` to Ordering, delete the basket, return `{ orderId }`. If the order call fails, the basket is kept.
- **Ordering computes the total** (Σ UnitPrice × Quantity) and copies product name and price into the order lines, so orders never depend on Catalog.
- **Flat code, no ceremony:** `Program.cs`, `Models.cs`, a `DbContext` or `BasketStore`, and one typed `HttpClient` per outbound service. No layers, mediators or repositories. DTOs are duplicated in each consumer on purpose.

### API endpoints

| Service | Method | Route |
|---|---|---|
| catalog-api | GET | `/products` |
| catalog-api | GET | `/products/{id}` |
| basket-api | GET | `/baskets/{buyerId}` |
| basket-api | POST | `/baskets/{buyerId}/items` |
| basket-api | DELETE | `/baskets/{buyerId}/items/{productId}` |
| basket-api | POST | `/baskets/{buyerId}/checkout` |
| ordering-api | POST | `/orders` |
| ordering-api | GET | `/orders?buyerId=` |
| ordering-api | GET | `/orders/{id}` |

Each API has a `.http` file next to its `Program.cs` for manual testing.

## Spec-Driven Development (SDD) in this repo

```
_agenticdev/
  PROJECT.md          # Source of truth: scope, API contracts, data models, resource names
  PLAN.md             # Build order: numbered steps (1.1, 2.1, 3.x, ...)
  WOW.md              # Way of Working: specs, branches, merges, session hygiene
  templates/
    spec-template.md  # Every spec follows this template
  microservices/
    README.md         # Status table for all specs
    catalog-api/      # 01-product-data, 02-product-seed, 03-list-products, 04-get-product
    ordering-api/     # 01-order-data, 02-create-order, 03-get-order, 04-list-orders
    basket-api/       # 01-get-basket, 02-add-item, 03-remove-item, 04-checkout
    web/              # 01-products-list ... 07-orders-list
```

| File | Role |
|---|---|
| [`_agenticdev/PROJECT.md`](_agenticdev/PROJECT.md) | **What** we build: the single source of truth |
| [`_agenticdev/PLAN.md`](_agenticdev/PLAN.md) | **In which order** we build it |
| [`_agenticdev/WOW.md`](_agenticdev/WOW.md) | **How** we deliver each change |
| [`_agenticdev/microservices/README.md`](_agenticdev/microservices/README.md) | **Where we are:** the spec backlog and its status |
| [`CLAUDE.md`](CLAUDE.md) | Project memory Claude Code loads every session: commands, architecture, conventions |

### The cycle, per spec

1. **Pick** the next ready spec from the status table (all of its dependencies are Done).
2. **Branch** `feature/<service>-<NN>-<short-name>` from `main` and commit the spec file first.
3. **Fresh session:** clear the context and point Claude Code at that single spec.
4. **Plan** in plan mode; a human reviews and approves.
5. **Implement** the approved plan, and nothing beyond it.
6. **Verify** the spec's Definition of Done (build, `aspire run`, `.http` requests, traces, pgAdmin, browser).
7. **Commit and merge** with `git merge --no-ff`.
8. **Mark Done** in the spec and in the status table.
9. **Delete** the feature branch.

### Claude Code customizations (`.claude/`)

| Type | Name | What it does |
|---|---|---|
| Skill | `/new-spec` | Drafts a new spec from PROJECT.md and PLAN.md, asks the open design questions and updates the status table |
| Skill | `/start-spec` | Preflight checks, creates the feature branch, commits the spec, then plans in plan mode and stops for approval |
| Skill | `/finish-spec` | Implements the approved plan, verifies the DoD, marks the spec Done, merges with `--no-ff` and deletes the branch |
| Skill | `/auto-spec` | Runs one spec end to end (`/new-spec` → `/start-spec` → `/finish-spec`) with two human gates |
| Subagent | `spec-compliance-reviewer` | Reviews a finished feature branch against its spec and PROJECT.md before the merge |
| Subagent | `integration-smoke-tester` | Walks the failure-handling table in PROJECT.md against the running services |

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Aspire CLI](https://learn.microsoft.com/dotnet/aspire/)
- [Docker](https://www.docker.com/) running (PostgreSQL, pgAdmin and Redis run as containers)
- [Claude Code](https://claude.com/claude-code), to follow the SDD workflow

### Build, test and run

From the `src/` folder:

```bash
dotnet build EShopClaude.sln
dotnet test EShopClaude.sln
aspire run
```

`aspire run` starts the AppHost and prints the dashboard URL. From the dashboard you can:

- open the **storefront** (`webfrontend`),
- check that every resource is **healthy**,
- follow a checkout across services on the **Traces** page,
- open **pgAdmin** to browse `catalogdb` and `orderingdb`.

> Data isn't persisted: there are no EF Core migrations and no volumes. The databases are created with `EnsureCreated()`, the catalog is seeded in code, and everything resets when the AppHost restarts.

## Tech stack

- .NET 10, ASP.NET Core Minimal APIs, Blazor Interactive Server
- .NET Aspire 13 (orchestration, service discovery, OpenTelemetry, health checks, resilience)
- PostgreSQL + EF Core (Npgsql), pgAdmin
- Redis (StackExchange.Redis)
- xUnit + Aspire.Hosting.Testing
- Claude Code (skills, subagents, plan mode, CLAUDE.md project memory)

## Course

👉 **[Agentic Coding with Claude Code: Spec-Driven Microservices on Udemy](https://www.udemy.com/course/agentic-coding-with-claude-code-spec-driven-microservices/)**

If this repository helps you, please give it a ⭐ and check out the course for the full step-by-step walkthrough.
