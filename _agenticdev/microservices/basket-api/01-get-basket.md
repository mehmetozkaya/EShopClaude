# basket-api 01 — get basket

**Status:** Not started

## Goal
Give Basket.Api its Redis store and its first endpoint, `GET /baskets/{buyerId}`. The storefront's cart page (`/cart`) reads the basket through it, and the E2E test (PROJECT.md §6 step 5) reads it to check that the basket is empty after checkout. The later basket specs (add, remove, checkout) build on `BasketStore`.

Context: today Basket.Api only calls `AddServiceDefaults()` and `AddProblemDetails()`, maps `GET /` ("Basket API is running.") and calls `MapDefaultEndpoints()`. The csproj already references `Aspire.StackExchange.Redis` 13.6.0 (added in step 2.1), so no package changes are needed. The AppHost already declares `basket-cache` (Redis, no volume). `basket-api` is registered with only `WithHttpHealthCheck("/health")`, with no reference to `basket-cache` and no wait. launchSettings uses `http://localhost:5102`.

- **Connection string: Aspire only.** `WithReference(basketCache)` injects `ConnectionStrings__basket-cache` into basket-api, and `AddRedisClient("basket-cache")` reads it by that connection name. There's no appsettings entry, so the API runs only under the AppHost.
- `AddRedisClient` registers `IConnectionMultiplexer` as a singleton, a Redis health check (so basket-api's `/health` covers Redis) and Redis tracing.
- The data doesn't persist (no volume, no TTL). Every `aspire run` starts with an empty Redis.

The store, the AppHost wiring and the GET endpoint are one spec because a store with no caller has no behavior to verify. GET is the smallest endpoint that exercises it.

## Depends on
Nothing.

## Scope
- [ ] `Models.cs`: two plain classes with settable properties (classes rather than records, so basket-api/02 can increment `Quantity` in place):
  ```csharp
  public class Basket
  {
      public string BuyerId { get; set; } = string.Empty;
      public List<BasketItem> Items { get; set; } = [];
  }

  public class BasketItem
  {
      public int ProductId { get; set; }
      public string ProductName { get; set; } = string.Empty;
      public decimal UnitPrice { get; set; }
      public int Quantity { get; set; }
  }
  ```
- [ ] `BasketStore.cs`: `BasketStore(IConnectionMultiplexer redis)` primary-constructor class with:
  - a `private static readonly JsonSerializerOptions` built from `JsonSerializerDefaults.Web` (camelCase), so the stored JSON matches the API response;
  - a private key helper that returns `$"basket:{buyerId}"`;
  - `public async Task<Basket?> GetAsync(string buyerId)`: `StringGetAsync` on the key from `redis.GetDatabase()`. It returns `null` when the key is missing, and otherwise the deserialized `Basket`.
  - No save or delete methods.
- [ ] `Program.cs`:
  - `builder.AddRedisClient("basket-cache")` after `AddServiceDefaults()`.
  - `builder.Services.AddSingleton<BasketStore>()`.
  - Map inline:
    ```csharp
    app.MapGet("/baskets/{buyerId}", async (string buyerId, BasketStore store) =>
        Results.Ok(await store.GetAsync(buyerId) ?? new Basket { BuyerId = buyerId }));
    ```
    The empty basket for a missing key is built in the endpoint, not in the store. No buyer id validation: any route string is accepted.
  - Keep the `GET /` placeholder and `MapDefaultEndpoints()`.
- [ ] `AppHost.cs`: chain `.WithReference(basketCache).WaitFor(basketCache)` onto the `basket-api` registration. The other resources don't change.
- [ ] `Basket.Api.http` (new): `@Basket.Api_HostAddress = http://localhost:5102`, with:
  - `GET {{Basket.Api_HostAddress}}/baskets/demo-user`
  - `GET {{Basket.Api_HostAddress}}/baskets/new-buyer`
- [ ] No changes to the csproj, appsettings or `EShopClaude.ServiceDefaults`.

Files touched: `src/EShopClaude.Basket.Api/Models.cs` (new), `BasketStore.cs` (new), `Program.cs` (edit), `Basket.Api.http` (new), and `src/EShopClaude.AppHost/AppHost.cs` (edit: only the `basket-api` registration).

## Explicit non-goals
- No store save or delete: save arrives in basket-api/02, delete in basket-api/04.
- No add, remove or checkout endpoints (basket-api/02, 03, 04).
- No `CatalogClient` or `OrderingClient`, no calls to catalog-api or ordering-api, and no AppHost references to them.
- No buyer id validation, auth or ownership check (PROJECT.md "Out of scope").
- No TTL, no data volume, and no Redis admin UI resource in the AppHost (PROJECT.md "Out of scope").
- No shared contracts library (PROJECT.md "Out of scope").
- No Web changes. The Web `BasketApiClient` and the cart page are Step 5.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded` with 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard Resources page every resource (`postgres`, pgAdmin, `catalogdb`, `orderingdb`, `basket-cache`, `catalog-api`, `ordering-api`, `basket-api`, `webfrontend`) is Running and Healthy.
4. On the dashboard: while `basket-cache` is not yet Healthy, `basket-api` shows the state **Waiting**. It moves to Starting/Running only after `basket-cache` is Healthy.
5. Dashboard → `basket-api` → resource details → Environment variables: `ConnectionStrings__basket-cache` is present.
6. `GET http://localhost:5102/baskets/new-buyer` → `200` with body exactly `{"buyerId":"new-buyer","items":[]}`.
7. Dashboard → Traces → the `basket-api` `GET /baskets/{buyerId}` trace has a Redis child span for the `GET` command on key `basket:new-buyer`.
8. Reading an existing key:
   - Find the `basket-cache` container (`docker ps --filter name=basket-cache`) and its password (in the `basket-cache` connection string on the dashboard).
   - Run `docker exec <container> redis-cli -a <password> SET basket:demo-user '{"buyerId":"demo-user","items":[{"productId":1,"productName":"Wireless Mouse","unitPrice":24.99,"quantity":2}]}'` → `OK`.
   - `GET http://localhost:5102/baskets/demo-user` → `200` with `buyerId: "demo-user"` and exactly 1 item `{ productId: 1, productName: "Wireless Mouse", unitPrice: 24.99, quantity: 2 }`.
9. `GET http://localhost:5102/health` → `200` with body `Healthy`. Click **Stop** on `basket-cache` in the dashboard, then `GET http://localhost:5102/health` → `503` with body `Unhealthy`. Click **Start** on `basket-cache`, and once it is Healthy, `GET http://localhost:5102/health` → `200` `Healthy`.
10. `GET http://localhost:5102/` → `200` with body `Basket API is running.`
11. `git diff --stat main` → only this spec, the README status table (when marking Done) and the 5 files listed under Scope.

## References
- PROJECT.md §3: AppHost resources (`basket-api` → `basket-cache`, `WaitFor` on each reference), service discovery, and "Code style per API" (flat files, `BasketStore.cs`, `decimal` for money).
- PROJECT.md §4.2: Basket.Api data (`Basket`, `BasketItem`, Redis key `basket:{buyerId}`, JSON value, no TTL), integration (`AddRedisClient("basket-cache")`) and `GET /baskets/{buyerId}` → `200 Basket`, an empty basket if none exists.
- PROJECT.md §5: the `/cart` page that consumes this endpoint.
- PROJECT.md §6 step 5: the E2E test reads `GET basket-api /baskets/{uniqueBuyer}`.
- PROJECT.md §2 "Out of scope": auth, TTL, data volumes, shared contracts library.
- PLAN.md 2.1.3 / 2.1.4: `basket-cache` and the `Aspire.StackExchange.Redis` package already exist. PLAN.md 3.3: Basket.Api specs.
- ordering-api/01-order-data.md: the Aspire-only connection string and AppHost `WaitFor` pattern this spec mirrors.
