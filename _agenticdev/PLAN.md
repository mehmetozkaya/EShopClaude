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
- Tasks
  - Add `postgres` with `.WithPgAdmin()`, plus the databases `catalogdb` and `orderingdb` on it. Add `basket-cache` (Redis). Use no data volumes.
  - Wire the data references with `WaitFor`:
    - `catalogdb` to `catalog-api`
    - `orderingdb` to `ordering-api`
    - `basket-cache` to `basket-api`
  - Add the AppHost hosting packages (PostgreSQL, Redis).
- Projects: AppHost
- Verify
  - Build passes.
  - Run shows postgres, pgAdmin and basket-cache healthy, and the APIs start only after their stores.
  - pgAdmin opens with the server pre-registered, and both databases are listed.

**2.2 Catalog data and seed** (`feature/2.2-catalog-data`)
- Tasks
  - In Catalog.Api, add `Models.cs` with `Product` and `CatalogDbContext.cs`.
  - Register the context with `AddNpgsqlDbContext<CatalogDbContext>("catalogdb")`.
  - At startup, call `EnsureCreated()` and seed about 8 products when the table is empty. Use `decimal` for prices.
- Projects: Catalog.Api
- Verify
  - Build passes.
  - Run: catalog-api is healthy.
  - pgAdmin: `catalogdb` has the `Products` table with about 8 rows.
  - Restarting the AppHost re-seeds the products (no volumes).

**2.3 Ordering data model** (`feature/2.3-ordering-data`)
- Tasks
  - In Ordering.Api, add `Models.cs` with `Order` and `OrderLine` and `OrderingDbContext.cs`. Configure `Order` to own `Lines` and set the money column precision.
  - Register the context with `AddNpgsqlDbContext<OrderingDbContext>("orderingdb")`.
  - At startup, call `EnsureCreated()`. There is no seed.
- Projects: Ordering.Api
- Verify
  - Build passes.
  - Run: ordering-api is healthy.
  - pgAdmin: `orderingdb` has empty `Orders` and `OrderLines` tables.

**2.4 Basket Redis store** (`feature/2.4-basket-store`)
- Tasks
  - In Basket.Api, add `Models.cs` with `Basket` and `BasketItem`.
  - Register the client with `AddRedisClient("basket-cache")`.
  - Add `BasketStore.cs` with get, save and delete on the key `basket:{buyerId}`, storing JSON with no TTL.
- Projects: Basket.Api
- Verify
  - Build passes.
  - Run: basket-api is healthy, and its Redis health check shows on the dashboard.

## Step 3 — Microservices, one by one (endpoints over their own data)
**3.1 Catalog.Api endpoints** (`feature/3.1-catalog-api`)
- Tasks
  - Add `GET /products`, ordered by Id.
  - Add `GET /products/{id}`, which returns `404` when the product is missing (§4.1).
  - Add `Catalog.Api.http`.
- Projects: Catalog.Api
- Verify
  - Build passes.
  - HTTP: the list returns the seed. A valid id returns `200`. An unknown id returns `404`.
  - Trace: the request spans include the EF/Npgsql child spans.

**3.2 Ordering.Api endpoints** (`feature/3.2-ordering-api`)
- Tasks
  - Add `POST /orders`. It sets `CreatedAt` (UTC) and computes `Total = Σ UnitPrice × Quantity`, then returns `201 { id }`.
  - Add `GET /orders?buyerId=`. It returns `OrderSummary` newest first, with `itemCount`.
  - Add `GET /orders/{id}`. It returns the order with its lines, or `404`.
  - Add `Ordering.Api.http`.
- Projects: Ordering.Api
- Verify
  - Build passes.
  - HTTP: POST a sample order and check that the `201` id and the Total are correct.
  - HTTP: the list shows the order first, and the detail returns its lines.
  - HTTP: an unknown id returns `404`.
  - pgAdmin: the rows appear in `orderingdb`.

**3.3 Basket.Api local endpoints** (`feature/3.3-basket-api`)
- Tasks
  - Add `GET /baskets/{buyerId}`, which returns an empty basket when none exists.
  - Add `DELETE /baskets/{buyerId}/items/{productId}`.
  - Add `Basket.Api.http`. POST items and checkout wait until step 4, because they need other services.
- Projects: Basket.Api
- Verify
  - Build passes.
  - HTTP: GET for a new buyer returns `{ buyerId, items: [] }`.
  - HTTP: DELETE on a missing line returns `200` with the basket unchanged.

## Step 4 — Service communication
**4.1 Basket → Catalog: add to basket** (`feature/4.1-basket-add-item`)
- Tasks
  - Add a typed `CatalogClient` with the base address `https+http://catalog-api`.
  - Add `POST /baskets/{buyerId}/items` with the body `{ productId, quantity }`. It works like this:
    - It looks up the product, and returns `404` when Catalog does.
    - If the product's line already exists, it increments the quantity. Otherwise it adds a line with a snapshot of the name and price.
    - It saves the basket and returns it.
- Projects: Basket.Api
- Verify
  - HTTP: adding product A twice gives qty 2. Adding product B gives a new line. An unknown id returns `404`.
  - HTTP: DELETE removes a line.
  - Trace: one trace runs basket-api → catalog-api → postgres, plus a Redis span.

**4.2 Basket → Ordering: checkout flow** (`feature/4.2-checkout`)
- Tasks
  - Add a typed `OrderingClient` with the base address `https+http://ordering-api`.
  - Add `POST /baskets/{buyerId}/checkout`, which follows §4.4:
    1. Read the basket. If it is missing or empty, return `400`.
    2. POST `/orders` with the lines mapped 1:1 from the basket.
    3. Delete the basket.
    4. Return `200 { orderId }`.
  - If the order call fails, keep the basket. There is no compensation.
- Projects: Basket.Api
- Verify
  - HTTP: checkout of an empty basket returns `400`.
  - HTTP: checkout of a filled basket returns an `orderId`. `GET ordering-api /orders/{id}` then matches the lines and Total, and the basket is empty afterwards.
  - Trace: basket-api → ordering-api → postgres, plus the Redis delete.
  - pgAdmin: the new order rows are in `orderingdb`.
  - Failure check: stop ordering-api from the dashboard and run checkout. The call fails, and the basket is still there.

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
| 2.2 | catalog-data | Catalog.Api | pgAdmin seed |
| 2.3 | ordering-data | Ordering.Api | pgAdmin tables |
| 2.4 | basket-store | Basket.Api | run, health |
| 3.1 | catalog-api | Catalog.Api | HTTP, trace |
| 3.2 | ordering-api | Ordering.Api | HTTP, pgAdmin |
| 3.3 | basket-api | Basket.Api | HTTP |
| 4.1 | basket-add-item | Basket.Api | HTTP, trace |
| 4.2 | checkout | Basket.Api | HTTP, trace, pgAdmin |
| 5.1 | web-clients | Web | build, browser |
| 5.2 | products-page | Web | browser, trace |
| 5.3 | cart-page | Web | browser |
| 5.4 | orders-pages | Web | browser |
| 6.1 | e2e-test | Tests | `dotnet test` |
| 6.2 | dod | all | §7 checklist |
