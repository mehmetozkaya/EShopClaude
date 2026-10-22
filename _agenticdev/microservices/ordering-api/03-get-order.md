# ordering-api 03 — get order

**Status:** Not started

## Goal
Return one order with its lines via `GET /orders/{id}`. The storefront's order detail page `/orders/{id:int}` reads this endpoint, and that page also serves as the checkout confirmation (use case 4, PROJECT.md §4.4 step 7). The E2E test (PROJECT.md §6) reads it too, to check the lines and the `Total`.

Context: ordering-api/01 and 02 are merged. `Order`/`OrderLine` entities and the `CreateOrder*` records are in `Models.cs`. `OrderingDbContext` has `Orders` and `OrderLines` in a one-to-many with cascade delete, and `OrderLine` has no back-navigation to `Order`. `Program.cs` maps `GET /`, `POST /orders` (which returns `Location: /orders/{id}`) and the default endpoints. `Ordering.Api.http` (`@Ordering.Api_HostAddress = http://localhost:5103`) holds the sample POST. Ordering stays buyer-agnostic, and the buyer id arrives only in URLs and bodies.

It is one spec because the single read endpoint and its `.http` requests are verified together by one POST-then-GET round trip.

## Depends on
ordering-api/02-create-order

## Scope
- [ ] `Program.cs`: after `POST /orders`, map inline (the same pattern as catalog-api/04):
  ```csharp
  app.MapGet("/orders/{id:int}", async (int id, OrderingDbContext db) =>
      await db.Orders.AsNoTracking()
          .Include(o => o.Lines.OrderBy(l => l.Id))
          .FirstOrDefaultAsync(o => o.Id == id) is { } order
          ? Results.Ok(order)
          : Results.NotFound());
  ```
  - Response: the `Order` entity itself, with no DTO. JSON shape:
    ```
    { id: int, buyerId: string, createdAt: DateTime (UTC, "Z"), total: decimal,
      lines: [ { id: int, orderId: int, productId: int, productName: string, unitPrice: decimal, quantity: int } ] }
    ```
  - Lines are ordered by line `Id`, which is their insert order.
  - The route constraint is `{id:int}`, so a non-integer id 404s at routing.
  - There's no buyer check. Any existing id is returned.
- [ ] `Ordering.Api.http`: append two requests:
  - `GET {{Ordering.Api_HostAddress}}/orders/1`, which returns the order created by the POST above.
  - `GET {{Ordering.Api_HostAddress}}/orders/9999`, which is expected to return `404`.
- [ ] No changes to `Models.cs`, `OrderingDbContext.cs`, the AppHost, the csproj, appsettings or `EShopClaude.ServiceDefaults`.

## Explicit non-goals
- No order list: `GET /orders?buyerId=` is ordering-api/04.
- No response DTO and no reshaping of the entity.
- No buyer ownership check and no auth (PROJECT.md "Out of scope").
- No order status, lifecycle, cancellation or editing (PROJECT.md "Out of scope").
- No migrations, no paging and no shared contracts library (PROJECT.md "Out of scope").
- No Web or Basket changes. The Web `OrderingApiClient` and order detail page are Step 5.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded` with 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard Resources page every resource is Running and Healthy.
4. Send the existing `POST {{Ordering.Api_HostAddress}}/orders` in `Ordering.Api.http` → `201`, `Location: /orders/1`, body `{"id":1}`. This is the first order since `aspire run`.
5. `GET http://localhost:5103/orders/1` → `200` with body:
   `id: 1`, `buyerId: "demo-user"`, `total: 138.98`, `createdAt` an ISO-8601 UTC timestamp ending in `Z` within a minute of now, and `lines` with exactly 2 entries in this order:
   `{ orderId: 1, productId: 1, productName: "Wireless Mouse", unitPrice: 24.99, quantity: 2 }`,
   `{ orderId: 1, productId: 2, productName: "Mechanical Keyboard", unitPrice: 89.00 (or 89), quantity: 1 }`.
6. `GET http://localhost:5103/orders/9999` → `404`.
7. `GET http://localhost:5103/orders/abc` → `404`, from the `{id:int}` route constraint.
8. Dashboard → Traces → the `ordering-api` `GET /orders/{id:int}` trace has an EF/Npgsql child span whose `SELECT` reads `"Orders"` joined with `"OrderLines"`.
9. `GET http://localhost:5103/` → `200` with body `Ordering API is running.`
10. `git diff --stat main` → only this spec, the README status table, `Program.cs` and `Ordering.Api.http`.

## References
- PROJECT.md §4.3: Ordering.Api `GET /orders/{id}` → `200 Order` with lines, or `404`, plus the `Order`/`OrderLine` data model.
- PROJECT.md §4.4 step 7: Web navigates to `/orders/{orderId}` after checkout.
- PROJECT.md §5: the `/orders/{id:int}` order detail page (id, date, lines, total) that consumes this endpoint.
- PROJECT.md §6: the E2E test reads `GET ordering-api /orders/{orderId}` to check the lines and Total.
- PROJECT.md §2 "Out of scope": auth, order status/lifecycle, migrations, paging, shared contracts library.
- PLAN.md 3.2: Ordering.Api specs.
- ordering-api/01-order-data.md and ordering-api/02-create-order.md: the entities read here and the POST used to create test data.
