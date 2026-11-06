# basket-api 04 — checkout

**Status:** Not started

## Goal
Turn the basket into an order (use case 3). Basket.Api orchestrates checkout as PROJECT.md §4.4 describes. It reads the basket, sends its snapshot lines to Ordering.Api, deletes the basket and returns the new order id. The storefront's **Place order** button on `/cart` (PLAN.md 5.3) calls this endpoint and then navigates to `/orders/{orderId}`.

Context: after basket-api/03, `BasketStore` has `GetAsync` and `SaveAsync` but no delete. `Program.cs` maps `GET /baskets/{buyerId}`, `POST /baskets/{buyerId}/items` and `DELETE /baskets/{buyerId}/items/{productId}`, and the only outbound client is `CatalogClient`. In `AppHost.cs`, `ordering-api` is registered but not assigned to a variable, and `basket-api` references only `basket-cache` and `catalog-api`. Ordering.Api already accepts `POST /orders` and returns `201 { id }` (ordering-api/02), and `GET /orders/{id}` works (ordering-api/03).

This is one spec because the endpoint, the Ordering client, the store delete and the AppHost reference can't be verified apart: checkout is the first and only caller of each. All of this work is in Basket.Api plus its own AppHost registration.

## Depends on
- basket-api/02-add-item
- ordering-api/02-create-order

## Scope
- [ ] `OrderingClient.cs` (new), following the `CatalogClient.cs` pattern, namespace `EShopClaude.Basket.Api`:
  - Local DTO records, duplicated on purpose from Ordering.Api's shapes:
    ```csharp
    public record CreateOrderRequest(string BuyerId, List<CreateOrderLine> Lines);
    public record CreateOrderLine(int ProductId, string ProductName, decimal UnitPrice, int Quantity);
    public record CreateOrderResponse(int Id);
    ```
  - `public class OrderingClient(HttpClient http)` with `public async Task<int> CreateOrderAsync(Basket basket)`. It:
    1. Maps `basket.BuyerId` and each `BasketItem` 1:1 to a `CreateOrderLine`, with no re-pricing.
    2. Sends `PostAsJsonAsync("/orders", request)`.
    3. Calls `response.EnsureSuccessStatusCode()` with no catch.
    4. Reads `CreateOrderResponse` and returns its `Id`.
- [ ] `BasketStore.cs`: add `public Task DeleteAsync(string buyerId) => redis.GetDatabase().KeyDeleteAsync(Key(buyerId));`, ignoring the returned bool. `GetAsync` and `SaveAsync` stay unchanged.
- [ ] `Models.cs`: add `public record CheckoutResponse(int OrderId);`, which serializes to `{"orderId":N}`.
- [ ] `Program.cs`:
  - Register `builder.Services.AddHttpClient<OrderingClient>(c => c.BaseAddress = new("https+http://ordering-api"));` next to the `CatalogClient` registration.
  - After the `MapDelete` mapping, map inline
    `app.MapPost("/baskets/{buyerId}/checkout", async (string buyerId, BasketStore store, OrderingClient ordering) => ...)`. There is no request body. The handler:
    1. Loads `await store.GetAsync(buyerId)`. If the basket is `null` **or** `Items.Count == 0`, it returns `Results.BadRequest()` with no body.
    2. Calls `var orderId = await ordering.CreateOrderAsync(basket);`. If Ordering fails, the exception propagates. There is no try/catch: the existing `UseExceptionHandler()` + `AddProblemDetails()` return a `500` ProblemDetails, and the basket is not deleted.
    3. Calls `await store.DeleteAsync(buyerId)`.
    4. Returns `Results.Ok(new CheckoutResponse(orderId))`.
  - Keep `GET /`, the three existing basket endpoints and `MapDefaultEndpoints()` unchanged.
- [ ] `EShopClaude.AppHost/AppHost.cs`:
  - Change the ordering registration to `var orderingApi = builder.AddProject<Projects.EShopClaude_Ordering_Api>("ordering-api")...`.
  - On `basket-api`, add `.WithReference(orderingApi).WaitFor(orderingApi)` after the `catalogApi` pair and before `WithHttpHealthCheck`.
  - Make no other AppHost changes. `webfrontend` stays as it is, because its references belong to Step 5.
- [ ] `Basket.Api.http`: after the existing DELETEs, add:
  - `POST {{Basket.Api_HostAddress}}/baskets/demo-user/checkout` (filled basket, expect `200 { orderId }`)
  - `POST {{Basket.Api_HostAddress}}/baskets/new-buyer/checkout` (no basket, expect `400`)
- [ ] No changes to `CatalogClient.cs`, the csproj, appsettings, `EShopClaude.ServiceDefaults`, Ordering.Api, Catalog.Api or Web.

Files touched:
- `src/EShopClaude.Basket.Api/`: `OrderingClient.cs` (new), `BasketStore.cs`, `Models.cs`, `Program.cs` and `Basket.Api.http` (edits).
- `src/EShopClaude.AppHost/AppHost.cs` (edit).

## Explicit non-goals
- No re-pricing at checkout. The basket snapshot price is final (PROJECT.md "Out of scope").
- No compensation, retries, fallbacks or circuit breakers beyond ServiceDefaults. No catching of the Ordering failure and no mapping of it to `502`.
- No locking or idempotency. Two concurrent checkouts of the same basket can create two orders.
- If the Redis delete fails after Ordering has created the order, the order stays and the basket is kept. There is no rollback.
- No buyer id validation and no request body.
- No payment, shipping, address or checkout form (PROJECT.md "Out of scope").
- No changes to Ordering.Api, which already computes `Total`.
- No Web changes. The Place order button is PLAN.md 5.3, the confirmation page is 5.4, and the `webfrontend` references come in Step 5.
- No E2E test. That is PLAN.md 6.1.
- No TTL, shared contracts library, messaging or events (PROJECT.md "Out of scope").

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded` with 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard Resources page every resource (`postgres`, pgAdmin, `catalogdb`, `orderingdb`, `basket-cache`, `catalog-api`, `ordering-api`, `basket-api`, `webfrontend`) is Running and Healthy. `basket-api`'s details list references to `basket-cache`, `catalog-api` and `ordering-api`, and `basket-api` stays in Waiting until `ordering-api` is healthy.
4. On a fresh `aspire run`, `POST http://localhost:5102/baskets/new-buyer/checkout` → `400` with an empty body, and its `basket-api` trace (dashboard → Traces) has no outbound HTTP span to `ordering-api`.
5. Empty-but-existing basket: `POST http://localhost:5102/baskets/emptied/items` `{ "productId": 1, "quantity": 1 }`, then `DELETE http://localhost:5102/baskets/emptied/items/1`, then `POST http://localhost:5102/baskets/emptied/checkout` → `400`.
6. Fill the basket: `POST http://localhost:5102/baskets/demo-user/items` with `{ "productId": 1, "quantity": 1 }` twice, then with `{ "productId": 2, "quantity": 1 }` → `200` with lines `{1, "Wireless Mouse", 24.99, 2}` and `{2, "Mechanical Keyboard", 89.00, 1}`.
7. `POST http://localhost:5102/baskets/demo-user/checkout` → `200` with body `{"orderId":N}`, where N is a positive int.
8. `GET http://localhost:5102/baskets/demo-user` → `200` `{"buyerId":"demo-user","items":[]}`, and `docker exec <basket-cache container> redis-cli -a <password> EXISTS basket:demo-user` → `0` (the key is deleted).
9. `GET http://localhost:5103/orders/N` → `200` with `buyerId` `"demo-user"`, `total` `138.98`, and 2 lines: `{productId 1, "Wireless Mouse", 24.99, quantity 2}` and `{productId 2, "Mechanical Keyboard", 89.00, quantity 1}`.
10. pgAdmin → `orderingdb`: the orders table has a row with id N, `BuyerId` `demo-user` and `Total` `138.98`, and the order lines table has the 2 rows from check 9 with `OrderId` N.
11. Dashboard → Traces → the check 7 `POST /baskets/{buyerId}/checkout` trace for `basket-api` contains a Redis `GET`, an outbound HTTP `POST /orders` to `ordering-api` with a child postgres `INSERT` span, and then a Redis `DEL` on `basket:demo-user`.
12. Repeat check 7 → `400` (the basket is gone).
13. Failure check:
    1. Add product 1 to `demo-user` again.
    2. On the dashboard, stop `ordering-api`.
    3. `POST http://localhost:5102/baskets/demo-user/checkout` → `500` ProblemDetails after a delay of up to ~30 s (ServiceDefaults resilience).
    4. `GET http://localhost:5102/baskets/demo-user` → still has the product 1 line.
    5. Start `ordering-api` again and repeat the checkout → `200 { orderId }`.
14. Regression:
    - `GET http://localhost:5102/baskets/new-buyer` → `200` `{"buyerId":"new-buyer","items":[]}`.
    - `POST http://localhost:5102/baskets/demo-user/items` `{ "productId": 999, "quantity": 1 }` → `404`.
    - `DELETE http://localhost:5102/baskets/demo-user/items/999` → `200`.
15. `git diff --stat main` → only this spec, the README status table (when marking Done), the five Basket.Api files listed in Scope and `AppHost.cs`.

## References
- PROJECT.md §2: use case 3 (Checkout) and "Out of scope" (payments/checkout form, retries/fallbacks/compensation, re-pricing at checkout, TTL, shared contracts library).
- PROJECT.md §3: AppHost resources (`basket-api` → `basket-cache`, `catalog-api`, `ordering-api`, with `WaitFor`), and "Code style per API" (flat files, one typed `HttpClient` per outbound service).
- PROJECT.md §4.2: `POST /baskets/{buyerId}/checkout` → `200 { orderId }` or `400`; `OrderingClient` → `https+http://ordering-api`; Redis key `basket:{buyerId}`.
- PROJECT.md §4.3: `POST /orders` body `{ buyerId, lines: [...] }` → `201 { id }`; Ordering computes `Total`.
- PROJECT.md §4.4: the checkout flow and its failure handling.
- PLAN.md 3.3 (Basket.Api specs, `04-checkout` needs `ordering-api/02`), 4 (Basket → Ordering communication now lives in 3.3), 5.3 (Place order consumes this endpoint).
- basket-api/02-add-item.md and 03-remove-item.md: the store methods, `.http` samples and seeded product values this spec reuses.
