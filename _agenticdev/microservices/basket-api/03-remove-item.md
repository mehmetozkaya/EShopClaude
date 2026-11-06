# basket-api 03 — remove item

**Status:** Not started

## Goal
Let a buyer remove a line from the basket (use case 2, "and remove a line"). The storefront's **Remove** button on `/cart` (PLAN.md 5.3) calls this endpoint. Remove deletes the whole line. There is no quantity editor or decrement (PROJECT.md "Out of scope").

Context: after basket-api/02, `BasketStore` has `GetAsync` and `SaveAsync` (no delete), and `Program.cs` maps `GET /baskets/{buyerId}` and `POST /baskets/{buyerId}/items`. Removing a line only needs those two store methods, and it calls no other service, so the AppHost doesn't change.

It comes after add-item so that removing a real line can be verified. The endpoint and its `.http` samples are one spec because neither can be checked without the other.

## Depends on
- basket-api/02-add-item

## Scope
- [ ] `Program.cs`: after the `POST /baskets/{buyerId}/items` mapping, map inline
  `app.MapDelete("/baskets/{buyerId}/items/{productId:int}", async (string buyerId, int productId, BasketStore store) => ...)`. The handler:
  1. Loads `await store.GetAsync(buyerId)`. If there isn't one, it returns `Results.Ok(new Basket { BuyerId = buyerId })` and writes nothing to Redis.
  2. Calls `basket.Items.RemoveAll(i => i.ProductId == productId)`. If it removed nothing, it returns `Results.Ok(basket)` unchanged without calling `SaveAsync`.
  3. Otherwise it calls `await store.SaveAsync(basket)` and returns `Results.Ok(basket)`. When the last line is removed, the basket is saved with `items: []` and the Redis key is kept.
  - Keep `GET /`, `GET /baskets/{buyerId}`, `POST /baskets/{buyerId}/items` and `MapDefaultEndpoints()` unchanged.
- [ ] `Basket.Api.http`: after the existing POSTs, add two requests:
  - `DELETE {{Basket.Api_HostAddress}}/baskets/demo-user/items/2` (removes Mechanical Keyboard)
  - `DELETE {{Basket.Api_HostAddress}}/baskets/demo-user/items/999` (missing line, basket unchanged)
- [ ] No changes to `BasketStore.cs`, `Models.cs`, `CatalogClient.cs`, the csproj, appsettings, `AppHost.cs` or `EShopClaude.ServiceDefaults`.

Files touched:
- `src/EShopClaude.Basket.Api/`: `Program.cs` (edit), `Basket.Api.http` (edit).

## Explicit non-goals
- No decrement of quantity. Remove deletes the whole line (PROJECT.md "Out of scope": cart quantity editor).
- No store delete method and no deleting of the Redis key. Both belong to checkout (basket-api/04).
- No checkout, no `OrderingClient`, no AppHost changes (basket-api/04).
- No calls to other services (no Catalog lookup on remove).
- No buyer id or product id validation beyond the `:int` route constraint. A non-int product id gives a routing `404`.
- No concurrency control: a plain read-modify-write, where the last write wins.
- No retries, fallbacks, TTL or shared contracts library (PROJECT.md "Out of scope").
- No Web changes. The Cart page and its Remove button are PLAN.md 5.3.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded` with 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard Resources page every resource (`postgres`, pgAdmin, `catalogdb`, `orderingdb`, `basket-cache`, `catalog-api`, `ordering-api`, `basket-api`, `webfrontend`) is Running and Healthy.
4. On a fresh `aspire run`, send `POST http://localhost:5102/baskets/demo-user/items` with `{ "productId": 1, "quantity": 1 }`, then with `{ "productId": 2, "quantity": 1 }` → the second response is `200` with 2 lines: `{1, "Wireless Mouse", 24.99, 1}` and `{2, "Mechanical Keyboard", 89.00, 1}`.
5. `DELETE http://localhost:5102/baskets/demo-user/items/2` → `200` with body `{"buyerId":"demo-user","items":[{"productId":1,"productName":"Wireless Mouse","unitPrice":24.99,"quantity":1}]}`. Then `GET http://localhost:5102/baskets/demo-user` → `200` with the same body.
6. `DELETE http://localhost:5102/baskets/demo-user/items/999` → `200` with exactly the body from check 5 (basket unchanged). On the dashboard, that request's `basket-api` trace has a Redis `GET` span and no Redis `SET` span.
7. `DELETE http://localhost:5102/baskets/demo-user/items/1` → `200` with body `{"buyerId":"demo-user","items":[]}`. `docker exec <basket-cache container> redis-cli -a <password> GET basket:demo-user` returns `{"buyerId":"demo-user","items":[]}` (the key is kept).
8. `DELETE http://localhost:5102/baskets/nobody/items/1` → `200` with body `{"buyerId":"nobody","items":[]}`, and `redis-cli ... EXISTS basket:nobody` → `0`.
9. `DELETE http://localhost:5102/baskets/demo-user/items/abc` → `404` (route constraint).
10. Dashboard → Traces → the check 5 `DELETE /baskets/{buyerId}/items/{productId}` trace for `basket-api` has Redis `GET` and `SET` spans on `basket:demo-user` and no outbound HTTP span to `catalog-api`.
11. Regression: `POST http://localhost:5102/baskets/demo-user/items` `{ "productId": 999, "quantity": 1 }` → `404`, and `GET http://localhost:5102/baskets/new-buyer` → `200` `{"buyerId":"new-buyer","items":[]}` (basket-api/01 and 02 still work).
12. `git diff --stat main` → only this spec, the README status table (when marking Done), `Program.cs` and `Basket.Api.http`.

## References
- PROJECT.md §2: use case 2 (Add to cart "and remove a line") and "Out of scope" (cart quantity editor, retries/fallbacks, TTL, shared contracts library).
- PROJECT.md §3: "Code style per API" (flat files, inline Minimal API endpoints).
- PROJECT.md §4.2: `DELETE /baskets/{buyerId}/items/{productId}` → removes the line, `200 Basket`; Redis key `basket:{buyerId}`.
- PROJECT.md §5: `/cart` page, **Remove** per line.
- PLAN.md 3.3 (Basket.Api specs, `03-remove-item`), 5.3 (Cart page Remove consumes this endpoint).
- basket-api/02-add-item.md: `BasketStore.SaveAsync`, the `.http` POSTs and the seeded product values this spec reuses.
