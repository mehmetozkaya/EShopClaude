# EShopClaude — Build Plan

Source of truth: `_agenticdev/PROJECT.md` (section numbers below refer to it). This plan only orders the work.

## Working agreement
- **One step = one feature branch** named `feature/<step-id>-<slug>` (for example `feature/3.1-catalog-api`). Branch off `main`, merge back when the step's checks pass.
- **Every step must leave `main` green.** `dotnet build src/EShopClaude.sln` succeeds, `dotnet test` passes and `aspire run` starts with every resource healthy.
- **Verification tools used below**
  - **Build:** `dotnet build`
  - **Test:** `dotnet test`
  - **Run:** `aspire run`, then check that the dashboard shows every resource as healthy
  - **HTTP:** a request from a `.http` file or `curl` against the endpoint shown on the dashboard
  - **Trace:** the span tree on the dashboard's Traces page
  - **pgAdmin:** open it from the dashboard and look at the rows
  - **Browser:** click through the storefront
- Only one automated test is in scope (§6). Until step 6.1, "Test" just means the suite still builds and passes.

---

## Step 1 — Project skeleton
**1.1 Starter cleanup and empty services wired in the AppHost** (`feature/1.1-skeleton`)
- Tasks
  - Delete `EShopClaude.ApiService`. Delete `Weather.razor`, `Counter.razor`, `WeatherApiClient.cs` and `WeatherForecastTests.cs`. Remove their references from the sln, the AppHost, Web and Tests.
  - Create `EShopClaude.Catalog.Api`, `EShopClaude.Basket.Api` and `EShopClaude.Ordering.Api` as empty Minimal API projects. Each one references ServiceDefaults and calls `AddServiceDefaults()` and `MapDefaultEndpoints()`. Add all three to the sln.
  - In `AppHost.cs`, add the projects `catalog-api`, `ordering-api` and `basket-api`. Give each one `WithHttpHealthCheck("/health")`.
  - Wire the project references with `WaitFor`:
    - `basket-api` references `catalog-api` and `ordering-api`.
    - `webfrontend` (external HTTP) references all three APIs.
  - Remove the `apiservice` registration from Web's `Program.cs`.
- Projects: AppHost, Catalog.Api, Basket.Api, Ordering.Api, Web, Tests, sln
- Verify
  - Build and test pass.
  - Run shows the 3 APIs and web as healthy.
  - HTTP: `GET /health` on each API returns `200`.
  - Browser: the home page loads, and the nav no longer has Weather or Counter.

## Step 2 — Data persistence design
**2.1 Infrastructure resources in the AppHost** (`feature/2.1-infra-resources`)
- Scope: resources and packages only. No DbContext, entities, seed, Redis client code, `AddNpgsqlDbContext`/`AddRedisClient` calls, or `WithReference`/`WaitFor` between the APIs and the new resources (that wiring lands in 2.2–2.4).
- Sub-steps (one commit each)
  - **2.1.1 PostgreSQL server and databases.** Add the `Aspire.Hosting.PostgreSQL` package to the AppHost. Add `postgres` with the databases `catalogdb` and `orderingdb` on it. Use no data volume.
  - **2.1.2 pgAdmin.** Add `.WithPgAdmin()` to `postgres`.
  - **2.1.3 Redis.** Add the `Aspire.Hosting.Redis` package to the AppHost. Add `basket-cache` (Redis). Use no data volume.
  - **2.1.4 Client integration packages (packages only).** Add `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` to Catalog.Api and Ordering.Api, and `Aspire.StackExchange.Redis` to Basket.Api.
- Projects: AppHost, Catalog.Api, Ordering.Api, Basket.Api (csproj only)
- Verify
  - Build and test pass.
  - Run shows postgres, catalogdb, orderingdb, pgAdmin and basket-cache healthy, and the APIs and web are still healthy.
  - The postgres and Redis containers have no data volumes.
  - pgAdmin opens with the server pre-registered, and both databases are listed.

## Step 3 — Microservices, one by one
Each service is built as a backlog of small specs, one feature branch per spec, following `_agenticdev/WOW.md`.
The spec files are the source of truth for scope and checks, and `_agenticdev/microservices/README.md` tracks their status.
Spec branches use the WOW.md name (`feature/<service>-<NN>-<short-name>`), not the step-id pattern above.

**3.1 Catalog.Api**: specs in `_agenticdev/microservices/catalog-api/`
- Owns: the read-only, seeded product catalog in `catalogdb` (§4.1).
- Specs: `01-product-data.md`, `02-product-seed.md`, `03-list-products.md`, `04-get-product.md`

**3.2 Ordering.Api**: specs in `_agenticdev/microservices/ordering-api/`
- Owns: orders and their lines in `orderingdb`, including the `Total` it computes on create (§4.3).
- Specs: `01-order-data.md`, `02-create-order.md`, `03-get-order.md`, `04-list-orders.md`

**3.3 Basket.Api**: specs in `_agenticdev/microservices/basket-api/`
- Owns: baskets in Redis (`basket:{buyerId}`), the name and price snapshot taken from Catalog at add time, and checkout orchestration (§4.2, §4.4).
- Specs: `01-get-basket.md`, `02-add-item.md`, `03-remove-item.md`, `04-checkout.md`
- `02-add-item` needs `catalog-api/04` merged, and `04-checkout` needs `ordering-api/02` merged.

## Step 4 — Service communication
This step is now part of 3.3: `basket-api/02-add-item` (Basket → Catalog) and `basket-api/04-checkout` (Basket → Ordering).
The step number is kept so that the ids of Steps 5 and 6 don't change.

## Step 5 — Storefront pages
**5.1 Web clients, buyer constant and nav** (`feature/5.1-web-clients`)
- Tasks
  - Add the `BuyerId = "demo-user"` constant in one place.
  - Add typed clients `CatalogApiClient`, `BasketApiClient` and `OrderingApiClient`, each with local DTO records, and register them in `Program.cs`.
  - Set the NavMenu to Products (`/`), Cart (`/cart`) and Orders (`/orders`).
- Projects: Web
- Verify
  - Build passes.
  - Browser: the nav shows the 3 links, and the app starts healthy.

**5.2 Products page (use cases 1 and 2)** (`feature/5.2-products-page`)
- Tasks
  - Make `Home.razor` at `/` a product table with name, description and price, plus an **Add to cart** button (qty 1) on each row.
  - After a click, show "Added {name}" and stay on the page.
- Projects: Web
- Verify
  - Browser: the products list. Clicking add shows the message.
  - HTTP: `GET basket-api /baskets/demo-user` shows the line.
  - Trace: webfrontend → basket-api → catalog-api.

**5.3 Cart page (use cases 2 and 3)** (`feature/5.3-cart-page`)
- Tasks
  - Add `/cart` with each line's name, unit price, qty and line total, plus the basket total.
  - Add **Remove** on each line.
  - Add **Place order**, which is disabled when the cart is empty and navigates to `/orders/{id}`.
  - Show an empty state that links to Products.
- Projects: Web
- Verify
  - Browser: the totals are correct. Remove deletes the line. The empty state shows.
  - Browser: Place order navigates to the order route. That page arrives in 5.4, so a 404 page is expected until then.

**5.4 Orders list and order detail (use case 4)** (`feature/5.4-orders-pages`)
- Tasks
  - Add `/orders` with a table of id, date, item count and total for demo-user. Each row links to its detail, and the page has an empty state.
  - Add `/orders/{id:int}` with the id, date, lines and total. This page is also the checkout confirmation.
- Projects: Web
- Verify
  - Browser: after checkout, the detail page shows the order, and the list shows it newest first.
  - Browser: the empty state shows when there are no orders.

## Step 6 — End-to-end verification
**6.1 Automated E2E test** (`feature/6.1-e2e-test`)
- Tasks
  - In `EShopClaude.Tests`, add `Aspire.Hosting.Testing` and reference the AppHost.
  - Add one test that starts the AppHost and runs §6 steps 1–5 with a unique GUID buyerId:
    1. Get the products.
    2. Add the same product twice and one other product.
    3. Check out.
    4. Assert that the order's lines, quantities and Total are correct.
    5. Assert that the basket is empty.
- Projects: Tests
- Verify
  - `dotnet test` passes. Docker must be running.

**6.2 Manual journey and definition of done** (`feature/6.2-dod`)
- Tasks
  - Walk through §7 and fix anything that is off.
  - Update `README.md` with how to run the app and the test.
- Projects: README, plus any small fixes
- Verify. This is the §7 checklist:
  - Run: postgres, pgAdmin, redis, the 3 APIs and web are all healthy.
  - Browser: list → add 2 products → remove one → place order → order detail → the orders list shows the order → the cart is empty.
  - Trace: the checkout trace spans web, basket, ordering and their stores.
  - pgAdmin: the seeded `catalogdb` products and the new `orderingdb` rows are there.
  - `dotnet test` is green.

## Step overview
| Step | Branch | Main project | Primary verification |
|---|---|---|---|
| 1.1 | skeleton | AppHost + 3 new APIs | run, `/health` |
| 2.1 | infra-resources | AppHost | run, pgAdmin |
| 3.1 | `catalog-NN-*` (4 specs) | Catalog.Api | per spec |
| 3.2 | `ordering-NN-*` (4 specs) | Ordering.Api | per spec |
| 3.3 | `basket-NN-*` (4 specs) | Basket.Api | per spec |
| 4 | in 3.3 | — | — |
| 5.1 | web-clients | Web | build, browser |
| 5.2 | products-page | Web | browser, trace |
| 5.3 | cart-page | Web | browser |
| 5.4 | orders-pages | Web | browser |
| 6.1 | e2e-test | Tests | `dotnet test` |
| 6.2 | dod | all | §7 checklist |
