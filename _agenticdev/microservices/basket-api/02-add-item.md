# basket-api 02 — add item

**Status:** Done (946a7ee)

## Goal
Let a buyer add a product to the basket (use case 2). Basket.Api looks the product up in Catalog (`GET /products/{id}`) and copies its name and price into the basket line as a snapshot. From then on that basket price is final, because checkout does not re-price (PROJECT.md §4.2, §4.4). The storefront's **Add to cart** button (PLAN.md 5.2) and the E2E test (PROJECT.md §6 step 2) both call this endpoint.

Context: today `BasketStore` can only read (`GetAsync`), and `Program.cs` maps only `GET /baskets/{buyerId}`. The AppHost wires `basket-api` to `basket-cache`, but not to `catalog-api`, and the `catalog-api` registration isn't assigned to a variable. Catalog.Api already serves `GET /products/{id:int}`, which returns `200 Product` or `404` (catalog-api/04). Service discovery and resilience for outbound `HttpClient`s come from ServiceDefaults.

The Catalog client, the store save, the endpoint and the AppHost reference make up one spec, because none of them can be verified without the others. This is also the first Basket → Catalog call (PLAN.md Step 4).

## Depends on
- basket-api/01-get-basket
- catalog-api/04-get-product

## Scope
- [ ] `Models.cs`: add the request record (local to Basket.Api):
  ```csharp
  public record AddItemRequest(int ProductId, int Quantity);
  ```
- [ ] `CatalogClient.cs` (new): a typed client and its local DTO (no shared contracts):
  ```csharp
  public record CatalogProduct(int Id, string Name, decimal Price);

  public class CatalogClient(HttpClient http)
  {
      public async Task<CatalogProduct?> GetProductAsync(int id) { ... }
  }
  ```
  - It calls `GET /products/{id}`.
  - On `404` it returns `null`.
  - On any other non-success status it calls `EnsureSuccessStatusCode()`, so the error propagates and ends in a `500` ProblemDetails. There is no fallback.
  - On success it deserializes the body with `ReadFromJsonAsync<CatalogProduct>()`. The `description` field is ignored.
- [ ] `BasketStore.cs`: add `public async Task SaveAsync(Basket basket)`.
  - It calls `StringSetAsync` on `basket:{basket.BuyerId}` with the existing Web `JsonOptions`.
  - It sets no expiry.
  - There is no delete method.
- [ ] `Program.cs`:
  - Register `builder.Services.AddHttpClient<CatalogClient>(c => c.BaseAddress = new("https+http://catalog-api"));`.
  - Map inline `app.MapPost("/baskets/{buyerId}/items", async (string buyerId, AddItemRequest request, CatalogClient catalog, BasketStore store) => ...)`. The handler:
    1. Calls `catalog.GetProductAsync(request.ProductId)`. If it returns `null`, it returns `Results.NotFound()` with an empty body and saves nothing.
    2. Loads `await store.GetAsync(buyerId)`. If there isn't one, it creates `new Basket { BuyerId = buyerId }`.
    3. If a line with the same `ProductId` exists, it adds `request.Quantity` to that line's `Quantity` and keeps the line's original `ProductName` and `UnitPrice`. Otherwise it appends a `BasketItem { ProductId, ProductName = product.Name, UnitPrice = product.Price, Quantity = request.Quantity }`.
    4. Calls `await store.SaveAsync(basket)` and returns `Results.Ok(basket)`.
  - Keep `GET /`, `GET /baskets/{buyerId}` and `MapDefaultEndpoints()` unchanged.
- [ ] `AppHost.cs`:
  - Assign the `catalog-api` registration to `var catalogApi`.
  - Chain `.WithReference(catalogApi).WaitFor(catalogApi)` onto the `basket-api` registration, next to its existing `basket-cache` reference.
  - Nothing else changes.
- [ ] `Basket.Api.http`: after the existing GETs, add four requests, each `POST {{Basket.Api_HostAddress}}/baskets/demo-user/items` with `Content-Type: application/json`:
  - `{ "productId": 1, "quantity": 1 }` (first add)
  - `{ "productId": 1, "quantity": 1 }` (second add, increments the line)
  - `{ "productId": 2, "quantity": 1 }`
  - `{ "productId": 999, "quantity": 1 }` (expect `404`)
- [ ] No changes to the csproj, appsettings, `Models.cs` classes `Basket`/`BasketItem`, or `EShopClaude.ServiceDefaults`.

Files touched:
- `src/EShopClaude.Basket.Api/`: `Models.cs` (edit), `CatalogClient.cs` (new), `BasketStore.cs` (edit), `Program.cs` (edit), `Basket.Api.http` (edit).
- `src/EShopClaude.AppHost/AppHost.cs` (edit: the `catalog-api` variable and the `basket-api` registration only).

## Explicit non-goals
- No remove endpoint (basket-api/03), no checkout, and no store delete (basket-api/04).
- No `OrderingClient` and no AppHost reference from `basket-api` to `ordering-api` (basket-api/04).
- No quantity validation (zero, negative, missing) and no buyer id validation. Web always sends quantity 1.
- No quantity editor or decrement. Re-adding increments; that's all (PROJECT.md "Out of scope").
- No re-pricing of existing lines. A re-add keeps the first snapshot's name and price.
- No concurrency control: a plain read-modify-write, where the last write wins.
- No retries, fallbacks or circuit breakers beyond ServiceDefaults (PROJECT.md "Out of scope").
- No TTL and no shared contracts library (PROJECT.md "Out of scope").
- No Web changes. The Web `BasketApiClient` and the Add to cart button are PLAN.md 5.1 and 5.2.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded` with 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard Resources page every resource (`postgres`, pgAdmin, `catalogdb`, `orderingdb`, `basket-cache`, `catalog-api`, `ordering-api`, `basket-api`, `webfrontend`) is Running and Healthy.
4. On the dashboard: `basket-api` shows **Waiting** until both `basket-cache` and `catalog-api` are Healthy, and it starts only after that.
5. Dashboard → `basket-api` → resource details → Environment variables: `services__catalog-api__http__0` (the catalog-api service discovery entry) is present, next to `ConnectionStrings__basket-cache`.
6. On a fresh `aspire run`, send the `.http` requests in order:
   - First `POST /baskets/demo-user/items` `{ "productId": 1, "quantity": 1 }` → `200` with body `{"buyerId":"demo-user","items":[{"productId":1,"productName":"Wireless Mouse","unitPrice":24.99,"quantity":1}]}`.
   - Second identical POST → `200` with one line, `quantity: 2`, still `Wireless Mouse` and `24.99`.
   - POST `{ "productId": 2, "quantity": 1 }` → `200` with 2 lines: `{1, "Wireless Mouse", 24.99, 2}` and `{2, "Mechanical Keyboard", 89.00, 1}`. These name and price values match `GET http://localhost:5101/products/2`.
7. POST `{ "productId": 999, "quantity": 1 }` → `404` with an empty body. Then `GET http://localhost:5102/baskets/demo-user` → `200` with exactly the same 2 lines as in check 6, so the basket is unchanged.
8. `GET http://localhost:5102/baskets/demo-user` → `200` with the saved 2-line basket from check 6. The value persists in Redis: `docker exec <basket-cache container> redis-cli -a <password> GET basket:demo-user` returns the same JSON (camelCase), and `TTL basket:demo-user` → `-1` (no expiry).
9. Dashboard → Traces → a `basket-api` `POST /baskets/{buyerId}/items` trace contains:
   - an outbound HTTP `GET` span to catalog-api;
   - the `catalog-api` `GET /products/{id}` span with its Npgsql `SELECT` child;
   - Redis `GET` and `SET` spans on `basket:demo-user`.
   The 999 request's trace shows catalog-api returning `404` and no Redis `SET` span.
10. `GET http://localhost:5102/baskets/new-buyer` → `200` with body `{"buyerId":"new-buyer","items":[]}` (the basket-api/01 behavior still works).
11. `git diff --stat main` → only this spec, the README status table (when marking Done), and the 6 files listed under Scope.

## References
- PROJECT.md §2: use case 2 (Add to cart) and "Out of scope" (quantity editor, re-pricing, retries/fallbacks, TTL, shared contracts library).
- PROJECT.md §3: architecture (basket-api → catalog-api over `https+http://catalog-api`), AppHost resources (`basket-api` references `catalog-api` with `WaitFor`), and "Code style per API" (a typed `HttpClient` class per outbound service, `decimal` for money).
- PROJECT.md §4.2: `POST /baskets/{buyerId}/items` behavior and the snapshot rule; `CatalogClient` outbound.
- PROJECT.md §4.1: Catalog `GET /products/{id}` → `200 Product` or `404`.
- PROJECT.md §6 step 2: the E2E test adds the same product twice plus another product.
- PLAN.md 3.3 (Basket.Api specs, `02-add-item` needs catalog-api/04), Step 4 (service communication is part of 3.3), 5.2 (Add to cart consumes this endpoint).
- basket-api/01-get-basket.md: `BasketStore`, the models and the Web JSON options this spec extends.
