# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A minimal e-commerce demo: three .NET 10 Minimal API microservices (Catalog, Basket, Ordering) and a Blazor storefront, orchestrated by .NET Aspire 13 and talking over plain synchronous HTTP.

The project is built spec-first:
- `_agenticdev/PROJECT.md` is the source of truth for scope, API contracts, data models and resource names.
- `_agenticdev/PLAN.md` orders the work into numbered steps (1.1, 2.1, ...).

Read the relevant sections of both before you implement a step. Don't add anything the PROJECT.md "Out of scope" list excludes (auth, messaging, retries/compensation, migrations, shared contracts library, TTL, paging, etc.).

## Commands

Run these from `src/`, where the solution lives:

```
dotnet build EShopClaude.sln
dotnet test EShopClaude.sln
dotnet test --filter "FullyQualifiedName~<TestName>"   # single test
aspire run        # starts the AppHost (src/aspire.config.json points at it); open the dashboard to check health/traces
```

- Infrastructure resources (Postgres, pgAdmin, Redis) run as containers, so Docker must be running for `aspire run`. The planned E2E test (`Aspire.Hosting.Testing`) needs Docker too.
- `/health` and `/alive` are mapped only in the Development environment (see `ServiceDefaults/Extensions.cs`).

## Way of working

The spec-driven delivery rules (specs, branches, merges, the per-spec cycle and session hygiene) live in `_agenticdev/WOW.md`:

@_agenticdev/WOW.md

## Workflow (from PLAN.md)

- One plan step = one branch named `feature/<step-id>-<slug>`, branched off `main` and merged back once the step's checks pass.
- Every step must leave `main` green: the build succeeds, the tests pass, and `aspire run` shows every resource healthy.
- Only one automated test is in scope: the E2E test in step 6.1. Until then, "test" just means the suite still builds and passes. Verify other behavior manually (`.http` files, dashboard traces, pgAdmin).

## Architecture

```
Web (Blazor Server) --> catalog-api  --> Postgres/catalogdb
                    --> basket-api   --> Redis/basket-cache
                           |--> catalog-api   (product lookup + price snapshot on add)
                           |--> ordering-api  (create order on checkout)
                    --> ordering-api --> Postgres/orderingdb
```

- **AppHost resource names are contracts.** Clients resolve services as `https+http://<resource-name>`, and integrations bind by connection name. The names are `catalog-api`, `basket-api`, `ordering-api`, `webfrontend`, `postgres`, `catalogdb`, `orderingdb` and `basket-cache`. Every project gets `WithHttpHealthCheck("/health")` and a `WaitFor` on each reference.
- **Each service owns its data.** No service reads another service's store.
- **The buyer is hard-coded.** `BuyerId = "demo-user"` is defined only in Web and passed to the APIs in the URL, so the APIs stay buyer-agnostic.
- **Basket.Api orchestrates checkout** (PROJECT.md §4.4):
  1. Read the basket and return `400` if it is empty.
  2. POST `/orders` to Ordering.
  3. Delete the basket.
  4. Return `{ orderId }`.

  If the order call fails, the basket is kept. There is no compensation. Prices are snapshotted into the basket at add time, and checkout doesn't re-price.
- **Ordering computes `Total`** (Σ UnitPrice × Quantity) when it creates an order and stores it. Order lines copy the product name and price, so orders never depend on Catalog.
- **Resilience, service discovery, OTel and health checks come from `EShopClaude.ServiceDefaults`.** Keep that project unchanged. Each service calls `AddServiceDefaults()` and `MapDefaultEndpoints()`.

## Code conventions

- **Flat files per API, no layers, mediators or repositories:**
  - `Program.cs`: DI and endpoint mapping.
  - `Models.cs`.
  - Either `XxxDbContext.cs` (with a seed) or `BasketStore.cs`.
  - One typed `HttpClient` class per outbound service (`CatalogClient`, `OrderingClient`).
- **Persistence uses no migrations and no volumes.**
  - Call `EnsureCreated()` at startup, plus a code seed for Catalog.
  - Data resets when the AppHost restarts.
- **Use `decimal` for money.**
- **DTOs are duplicated on purpose** in each consumer (for example, Web's typed clients declare local DTO records). Don't create a shared contracts project.
- **Basket storage:** Redis key `basket:{buyerId}`, JSON value, no TTL.
