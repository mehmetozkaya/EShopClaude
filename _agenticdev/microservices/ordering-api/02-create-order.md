# ordering-api 02 — create order

**Status:** Done (f1547b5)

## Goal
Let callers place an order with `POST /orders`. Basket.Api calls this at checkout (basket-api/04, PROJECT.md §4.4) with the buyer id and the lines it has snapshotted. Ordering owns the order total, so it computes `Total = Σ UnitPrice × Quantity` and stores it, and it sets `CreatedAt` itself. The lines are stored as sent, with no Catalog call and no re-pricing.

Context: ordering-api/01 is merged. `Order`/`OrderLine` are in `Models.cs`, and `OrderingDbContext` (`Orders`, `OrderLines`, one-to-many with cascade delete) is registered with `AddNpgsqlDbContext<OrderingDbContext>("orderingdb")`. `EnsureCreatedAsync()` runs at startup, and the AppHost already references and waits for `orderingdb`. `Program.cs` maps only `GET /` and the default endpoints, and Ordering.Api has no `.http` file yet. ordering-api listens on `http://localhost:5103` (launchSettings).

It is one spec because the endpoint, its request/response records and the `.http` sample are verified together by one request and one pgAdmin check.

## Depends on
ordering-api/01-order-data

## Scope
- [ ] `Models.cs`: add the request/response records. They are local to Ordering.Api, and Basket declares its own copies (no shared contracts):
  ```csharp
  public record CreateOrderRequest(string BuyerId, List<CreateOrderLine> Lines);
  public record CreateOrderLine(int ProductId, string ProductName, decimal UnitPrice, int Quantity);
  public record CreateOrderResponse(int Id);
  ```
- [ ] `Program.cs`: map `POST /orders` inline as a `MapPost` lambda taking `CreateOrderRequest` and `OrderingDbContext`. The lambda:
  - builds an `Order` with `BuyerId = request.BuyerId` and `CreatedAt = DateTime.UtcNow`;
  - maps each request line 1:1 to a new `OrderLine` with `ProductId`, `ProductName`, `UnitPrice` and `Quantity`, ignoring any client-supplied ids;
  - sets `Total = Lines.Sum(l => l.UnitPrice * l.Quantity)` in `decimal`, with no explicit rounding;
  - adds the order and calls `SaveChangesAsync()` once;
  - returns `Results.Created($"/orders/{order.Id}", new CreateOrderResponse(order.Id))`, which is `201`, `Location: /orders/{id}`, body `{ "id": n }`.
- [ ] `Program.cs`: keep the `GET /` placeholder and `MapDefaultEndpoints()`.
- [ ] `Ordering.Api.http` (new, `src/EShopClaude.Ordering.Api/`): `@Ordering.Api_HostAddress = http://localhost:5103` and one `POST {{Ordering.Api_HostAddress}}/orders` with `Content-Type: application/json` and body:
  ```json
  {
    "buyerId": "demo-user",
    "lines": [
      { "productId": 1, "productName": "Wireless Mouse", "unitPrice": 24.99, "quantity": 2 },
      { "productId": 2, "productName": "Mechanical Keyboard", "unitPrice": 89.00, "quantity": 1 }
    ]
  }
  ```
  The expected `Total` is 24.99 × 2 + 89.00 × 1 = **138.98**.
- [ ] No changes to `OrderingDbContext.cs`, the AppHost, the csproj, appsettings or `EShopClaude.ServiceDefaults`.

## Explicit non-goals
- No read endpoints: `GET /orders/{id}` is ordering-api/03 and `GET /orders?buyerId=` is ordering-api/04. The `Location` header points at a route that 404s until 03 is merged.
- No input validation (empty `lines`, blank `buyerId`, non-positive quantity). Basket.Api already rejects an empty basket. Invalid data that the database rejects ends in a `500` ProblemDetails.
- No calls to Catalog and no re-pricing. The lines are taken as sent.
- No order status, lifecycle, cancellation or editing (PROJECT.md "Out of scope").
- No migrations, no seed and no shared contracts library (PROJECT.md "Out of scope").
- No Basket.Api changes. The checkout client is basket-api/04.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded` with 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard Resources page every resource is Running and Healthy.
4. Send the `POST {{Ordering.Api_HostAddress}}/orders` request in `Ordering.Api.http` (`http://localhost:5103/orders`) → `201 Created`, header `Location: /orders/1`, body `{"id":1}`. This is the first order since `aspire run`, so the id is 1.
5. Send the same request again → `201`, `Location: /orders/2`, body `{"id":2}`.
6. pgAdmin → `orderingdb` → Query Tool:
   `SELECT "Id", "BuyerId", "Total", "CreatedAt" FROM "Orders" ORDER BY "Id";`
   → 2 rows, `1 | demo-user | 138.98 | <now>` and `2 | demo-user | 138.98 | <now>`, where `CreatedAt` is a `timestamp with time zone` matching the current UTC time (within a minute of `SELECT now() AT TIME ZONE 'UTC';`).
7. pgAdmin Query Tool on `orderingdb`:
   `SELECT "OrderId", "ProductId", "ProductName", "UnitPrice", "Quantity" FROM "OrderLines" WHERE "OrderId" = 1 ORDER BY "Id";`
   → exactly 2 rows: `1 | 1 | Wireless Mouse | 24.99 | 2` and `1 | 2 | Mechanical Keyboard | 89.00 | 1`.
8. Dashboard → Traces → the `ordering-api` `POST /orders` trace has EF/Npgsql child spans that `INSERT INTO "Orders"` and `INSERT INTO "OrderLines"`.
9. `GET http://localhost:5103/` → `200` with body `Ordering API is running.`
10. `git diff --stat main` → only this spec, the README status table, `Models.cs`, `Program.cs` and `Ordering.Api.http`.

## References
- PROJECT.md §4.3: Ordering.Api `POST /orders` body and `201 { id }` response, `Total` computed on create and stored, lines copy name and price, no status.
- PROJECT.md §4.4: checkout flow steps 3–4 (Basket → `POST /orders`, lines mapped 1:1, no re-pricing).
- PROJECT.md §3 "Code style per API": flat files, endpoints in `Program.cs`, `decimal` for money, DTOs duplicated per consumer.
- PROJECT.md §2 "Out of scope": order status/lifecycle, re-pricing, migrations, shared contracts library.
- PLAN.md 3.2: Ordering.Api; PLAN.md 3.3: basket-api/04 depends on this spec.
- ordering-api/01-order-data.md: the entities and DbContext this spec writes to.
