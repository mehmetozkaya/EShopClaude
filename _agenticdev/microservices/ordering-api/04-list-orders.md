# ordering-api 04 — list orders

**Status:** Not started

## Goal
Return one buyer's orders as summaries via `GET /orders?buyerId=`, newest first. The storefront's "My orders" page `/orders` (use case 4, PROJECT.md §5) reads this endpoint to show a table of id, date, item count and total for `demo-user`, where each row links to the order detail page that ordering-api/03 serves.

Context: ordering-api/01, 02 and 03 are merged. `Order`/`OrderLine` entities and the `CreateOrder*` records are in `Models.cs`. `OrderingDbContext` has `Orders` and `OrderLines` in a one-to-many relationship. `Program.cs` maps `GET /`, `POST /orders`, `GET /orders/{id:int}` and the default endpoints. `Ordering.Api.http` (`@Ordering.Api_HostAddress = http://localhost:5103`) holds the sample POST for `demo-user` and the two GET-by-id requests. Ordering stays buyer-agnostic, and the buyer id arrives only in URLs and bodies.

It is one spec because the single read endpoint, its summary record and its `.http` requests are verified together by one set of POSTs followed by list GETs.

## Depends on
ordering-api/02-create-order

## Scope
- [ ] `Models.cs`: add the response record after `CreateOrderResponse`:
  ```csharp
  public record OrderSummary(int Id, DateTime CreatedAt, decimal Total, int ItemCount);
  ```
  - JSON shape: `{ id: int, createdAt: DateTime (UTC, "Z"), total: decimal, itemCount: int }`.
  - `ItemCount` is the sum of `Quantity` across the order's lines, not the number of lines. For example, 2 mice and 1 keyboard gives `3`.
- [ ] `Program.cs`: after `GET /orders/{id:int}`, map inline:
  ```csharp
  app.MapGet("/orders", async (string buyerId, OrderingDbContext db) =>
      await db.Orders.AsNoTracking()
          .Where(o => o.BuyerId == buyerId)
          .OrderByDescending(o => o.CreatedAt)
          .ThenByDescending(o => o.Id)
          .Select(o => new OrderSummary(o.Id, o.CreatedAt, o.Total, o.Lines.Sum(l => l.Quantity)))
          .ToListAsync());
  ```
  - The query is one server-side EF projection, so it runs as a single SQL statement with a `SUM` subquery and no `Include`.
  - Newest first is `CreatedAt` descending. `Id` descending breaks ties.
  - `buyerId` is a required query parameter. If it is missing, minimal-API binding returns `400` with no extra code.
  - The `buyerId` match is exact and case-sensitive.
  - A buyer with no orders gets `200 []`.
- [ ] `Ordering.Api.http`: append three requests:
  - `POST {{Ordering.Api_HostAddress}}/orders` for `"buyerId": "other-buyer"` with one line: `{ "productId": 3, "productName": "USB-C Hub", "unitPrice": 39.50, "quantity": 1 }`.
  - `GET {{Ordering.Api_HostAddress}}/orders?buyerId=demo-user`.
  - `GET {{Ordering.Api_HostAddress}}/orders?buyerId=nobody`, which is expected to return `[]`.
- [ ] No changes to `OrderingDbContext.cs` (no indexes), the AppHost, the csproj, appsettings or `EShopClaude.ServiceDefaults`.

## Explicit non-goals
- No paging, sorting options or filtering beyond `buyerId` (PROJECT.md "Out of scope": paging).
- No lines in the summary. `GET /orders/{id}` (ordering-api/03) returns them.
- No list of all orders when `buyerId` is missing. That request returns `400`.
- No auth and no buyer ownership check (PROJECT.md "Out of scope").
- No order status, lifecycle, cancellation or editing (PROJECT.md "Out of scope").
- No migrations, no new DB indexes and no shared contracts library (PROJECT.md "Out of scope").
- No Web or Basket changes. The Web `OrderingApiClient` and the `/orders` page are PLAN.md 5.4.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded` with 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run` (fresh start, so `orderingdb` is empty). On the dashboard Resources page every resource is Running and Healthy.
4. In `Ordering.Api.http`, send in this order:
   - the existing `demo-user` POST → `201`, body `{"id":1}`;
   - the new `other-buyer` POST → `201`, body `{"id":2}`;
   - the `demo-user` POST again → `201`, body `{"id":3}`.
5. `GET http://localhost:5103/orders?buyerId=demo-user` → `200` with a JSON array of exactly 2 entries in this order:
   - `{ id: 3, total: 138.98, itemCount: 3, createdAt: <ISO-8601 UTC ending in Z> }`
   - `{ id: 1, total: 138.98, itemCount: 3, createdAt: <ISO-8601 UTC ending in Z> }`

   Each entry has exactly the four properties `id`, `createdAt`, `total` and `itemCount`, with no `lines` and no `buyerId`.
6. `GET http://localhost:5103/orders?buyerId=other-buyer` → `200` with exactly `[{ id: 2, total: 39.5 (or 39.50), itemCount: 1, createdAt: ... }]`.
7. `GET http://localhost:5103/orders?buyerId=nobody` → `200` with body `[]`.
8. `GET http://localhost:5103/orders` (no `buyerId`) → `400`.
9. `GET http://localhost:5103/orders/1` → still `200` with the order from ordering-api/03, including its 2 lines.
10. Dashboard → Traces → the `ordering-api` `GET /orders` trace has one EF/Npgsql child span whose `SELECT` reads `"Orders"` with a `WHERE` on `"BuyerId"`, a `SUM` over `"OrderLines"."Quantity"` and an `ORDER BY` on `"CreatedAt" DESC`.
11. `git diff --stat main` → only this spec, the README status table, `Models.cs`, `Program.cs` and `Ordering.Api.http`.

## References
- PROJECT.md §4.3: Ordering.Api `GET /orders?buyerId=` → `200 OrderSummary[] { id, createdAt, total, itemCount }`, newest first, plus the `Order`/`OrderLine` data model.
- PROJECT.md §2 use case 4: see my orders (list + detail).
- PROJECT.md §5: the `/orders` "My orders" page (id, date, item count, total, empty state) that consumes this endpoint.
- PROJECT.md §2 "Out of scope": auth, order status/lifecycle, migrations, paging, shared contracts library.
- PLAN.md 3.2: Ordering.Api specs (`04-list-orders.md`).
- PLAN.md 5.4: the Web orders pages that will call this endpoint.
- ordering-api/02-create-order.md and ordering-api/03-get-order.md: the POST used to create test data and the neighbouring read endpoint.
