# EShopClaude — Project Document

## 1. Purpose
A minimal e-commerce demo that shows agentic coding with microservices: small scope, few tokens, fast delivery.
Three microservices and one Blazor storefront, orchestrated by .NET Aspire, talking over plain synchronous HTTP.

## 2. Scope
### Use cases (each has a storefront page)
1. List products
2. Add to cart (and remove a line)
3. Checkout
4. See the order (list of my orders + order detail)

### Out of scope (deliberately)
- Login, identity, users: a single hard-coded buyer `demo-user`
- Payments, shipping, addresses, checkout form
- Deployment, containers for the services, CI
- API gateway, message broker, events, sagas, outbox
- Retries, fallbacks, compensation, circuit breakers beyond the Aspire ServiceDefaults
- Stock/inventory, categories, search, paging, product images, product detail page
- Admin/CRUD for products (catalog is read-only and seeded)
- Order status/lifecycle, cancellation, editing orders
- Re-pricing at checkout (the basket snapshot price is final)
- Cart quantity editor (re-adding increments, remove deletes the line)
- Basket expiry/TTL
- EF Core migrations (EnsureCreated + code seed)
- A shared contracts library (DTOs are duplicated on purpose)
- Persistent data volumes (data resets when the AppHost restarts)

## 3. Architecture

    [Web (Blazor)] --HTTP--> catalog-api  --> PostgreSQL / catalogdb
          |        --HTTP--> basket-api   --> Redis (basket-cache)
          |                     |--HTTP--> catalog-api   (product lookup on add)
          |                     |--HTTP--> ordering-api  (create order on checkout)
          |        --HTTP--> ordering-api --> PostgreSQL / orderingdb

- All calls are synchronous HTTP using Aspire service discovery (`https+http://<resource-name>`).
- Each service owns its data store; no service reads another service's database.
- The buyer ID is a constant (`demo-user`) defined **only in Web** and passed to APIs in the URL. The APIs are buyer-agnostic, so real auth later only changes Web.

### Projects (`src/`)
| Project | Kind | Owns |
|---|---|---|
| EShopClaude.AppHost | Aspire host | resource wiring |
| EShopClaude.ServiceDefaults | library | OTel, health, resilience defaults (unchanged) |
| EShopClaude.Catalog.Api | Minimal API | catalogdb (products) |
| EShopClaude.Basket.Api | Minimal API | Redis baskets, checkout orchestration |
| EShopClaude.Ordering.Api | Minimal API | orderingdb (orders, order lines) |
| EShopClaude.Web | Blazor Interactive Server | UI only, no data |
| EShopClaude.Tests | xUnit + Aspire.Hosting.Testing | one end-to-end test |

Starter cleanup: delete `EShopClaude.ApiService`, the Weather and Counter pages, `WeatherApiClient.cs` and `WeatherForecastTests.cs`.

### AppHost resources
| Resource name | Type | References / WaitFor |
|---|---|---|
| `postgres` | PostgreSQL server + pgAdmin (`.WithPgAdmin()`) | — |
| `catalogdb` | database on `postgres` | — |
| `orderingdb` | database on `postgres` | — |
| `basket-cache` | Redis | — |
| `catalog-api` | project | catalogdb |
| `ordering-api` | project | orderingdb |
| `basket-api` | project | basket-cache, catalog-api, ordering-api |
| `webfrontend` | project, external HTTP | catalog-api, basket-api, ordering-api |

Every project has `WithHttpHealthCheck("/health")` and `WaitFor` on each reference.

pgAdmin runs as a container next to `postgres` and has the server pre-registered, so you can browse the data in both `catalogdb` and `orderingdb` from its link on the Aspire dashboard. It's a dev-time tool only and no service uses it.

### Code style per API
Minimal APIs, flat files: `Program.cs` (DI + endpoint mapping), `Models.cs`, plus either `XxxDbContext.cs` (with a seed) or `BasketStore.cs`, plus a typed `HttpClient` class for each outbound service. No layers, mediators or repositories. Use `decimal` for money.

## 4. Services

### 4.1 Catalog.Api: product catalog (read-only)
**Data (catalogdb)**: `Product { int Id; string Name; string Description; decimal Price }`
**Startup**: `EnsureCreated()`, then seed about 8 products if the table is empty.
**Integration**: `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` (`AddNpgsqlDbContext<CatalogDbContext>("catalogdb")`).

| Method | Route | Response |
|---|---|---|
| GET | `/products` | `200 Product[]` ordered by Id |
| GET | `/products/{id}` | `200 Product` or `404` |

### 4.2 Basket.Api: basket and checkout orchestrator
**Data (Redis, key `basket:{buyerId}`)**: JSON of
`Basket { string BuyerId; List<BasketItem> Items }`
`BasketItem { int ProductId; string ProductName; decimal UnitPrice; int Quantity }`
Name and price are a **snapshot taken from Catalog.Api at add time**. No TTL.
**Integration**: `Aspire.StackExchange.Redis` (`AddRedisClient("basket-cache")`).
**Outbound**: `CatalogClient` → `https+http://catalog-api`, `OrderingClient` → `https+http://ordering-api`.

| Method | Route | Body | Behavior |
|---|---|---|---|
| GET | `/baskets/{buyerId}` | — | `200 Basket`; an empty basket if none exists |
| POST | `/baskets/{buyerId}/items` | `{ productId, quantity }` | Calls Catalog `GET /products/{id}` (404 → return 404). If the line exists, increments its quantity; otherwise adds a line with the snapshot name/price. Saves and returns `200 Basket`. |
| DELETE | `/baskets/{buyerId}/items/{productId}` | — | Removes the line; `200 Basket` |
| POST | `/baskets/{buyerId}/checkout` | — | See 4.4. `200 { orderId }`, or `400` if the basket is empty |

### 4.3 Ordering.Api: orders
**Data (orderingdb)**:
`Order { int Id; string BuyerId; DateTime CreatedAt (UTC); decimal Total; List<OrderLine> Lines }`
`OrderLine { int Id; int OrderId; int ProductId; string ProductName; decimal UnitPrice; int Quantity }`
Lines copy the name and price, so an order never depends on Catalog. `Total` is computed by Ordering.Api on create (Σ UnitPrice × Quantity) and stored. No status.
**Startup**: `EnsureCreated()`, no seed.

| Method | Route | Body | Response |
|---|---|---|---|
| POST | `/orders` | `{ buyerId, lines: [{ productId, productName, unitPrice, quantity }] }` | `201 { id }` (sets CreatedAt and Total) |
| GET | `/orders?buyerId=` | — | `200 OrderSummary[] { id, createdAt, total, itemCount }`, newest first |
| GET | `/orders/{id}` | — | `200 Order` with lines, or `404` |

### 4.4 Checkout flow (owned by Basket.Api)
1. Web: user clicks **Place order** on `/cart` → `POST basket-api /baskets/demo-user/checkout`.
2. Basket.Api reads `basket:demo-user` from Redis. If it is missing or empty, return `400`.
3. Basket.Api → `POST ordering-api /orders` with buyerId + lines mapped 1:1 from the basket snapshot (no re-pricing).
4. Ordering.Api saves Order + lines, computes the Total, and returns `201 { id }`.
5. Basket.Api deletes `basket:demo-user` from Redis.
6. Basket.Api returns `200 { orderId }`.
7. Web navigates to `/orders/{orderId}`.

Failure handling is only what ServiceDefaults provides. If step 3 fails, the error propagates and the basket is kept. No compensation.

## 5. Storefront (EShopClaude.Web)
- Blazor Interactive Server, the existing template layout.
- `BuyerId = "demo-user"` constant in one place in Web.
- Typed clients: `CatalogApiClient`, `BasketApiClient`, `OrderingApiClient`, each with DTO records declared locally.
- Nav menu: **Products** (`/`) | **Cart** (`/cart`) | **Orders** (`/orders`).

| Route | Page | Content and actions |
|---|---|---|
| `/` | Products | Grid/table of products (name, description, price) with an **Add to cart** button per product (quantity 1). Stays on the page and shows a short "Added {name}" message. |
| `/cart` | Cart | Lines (name, unit price, qty, line total), basket total, a **Remove** per line, and **Place order** (disabled when empty) → `/orders/{id}`. The empty state links to Products. |
| `/orders` | My orders | Table of the orders for demo-user (id, date, item count, total), each linking to its detail. Has an empty state. |
| `/orders/{id:int}` | Order detail | Order id, date, lines, total. Also serves as the confirmation page after checkout. |

User journey: Products → Add to cart (×n) → Cart → (Remove) → Place order → Order detail → Orders list.

## 6. Testing
`EShopClaude.Tests` uses `Aspire.Hosting.Testing` with a single E2E test that starts the AppHost (needs Docker) and then:
1. `GET catalog-api /products` → not empty
2. `POST basket-api /baskets/{uniqueBuyer}/items` twice (same product ×2, plus another product)
3. `POST basket-api /baskets/{uniqueBuyer}/checkout` → orderId
4. `GET ordering-api /orders/{orderId}` → lines and quantities match, and Total = Σ price × qty
5. `GET basket-api /baskets/{uniqueBuyer}` → empty

The test uses a unique GUID buyerId so it doesn't interfere with the `demo-user` data.

## 7. Definition of done
- `aspire run` (or running the AppHost) starts postgres, pgAdmin, redis, the 3 APIs and web, and the dashboard shows them all as healthy.
- pgAdmin (opened from the dashboard) shows the seeded `catalogdb` products and, after checkout, the new rows in `orderingdb`.
- Manual journey: list → add 2 products → remove one → place order → order detail → orders list shows it → cart is empty.
- `dotnet test` passes the E2E test.
