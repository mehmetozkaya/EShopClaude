# web 04 — remove line

**Status:** Not started

## Goal
Let the buyer delete a line from the cart (use case 2, "and remove a line"). A **Remove** button on each `/cart` line calls Basket's delete endpoint. The page then shows the basket that Basket returns. Remove deletes the whole line, with no decrement.

## Depends on
- web/03-cart-view
- basket-api/03-remove-item

## Scope
- [ ] `BasketApiClient`: add `RemoveItemAsync(buyerId, productId)` → `DELETE /baskets/{buyerId}/items/{productId}`.
- [ ] `Cart.razor`: a **Remove** button on each line. After the call, it re-renders from the returned basket, so the total updates. When the last line is removed, the page shows the empty state.

## Explicit non-goals
- No quantity decrement or editor. Remove deletes the line (PROJECT.md "Out of scope").
- No confirmation dialog and no undo.
- No **Place order** button (web/06).
- No AppHost change.
- Prices come only from what Basket and Ordering return. No page asks Catalog for a price again, and no page sends a price to a service.
- Checkout is one call to Basket. No page calls Ordering to create anything.
- No page reads a database or Redis.
- Nothing from the PROJECT.md "Out of scope" list is added.
- No changes to Catalog.Api, Basket.Api, Ordering.Api, ServiceDefaults or `AppHost.cs`.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded`, 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard, every resource is Running and Healthy.
4. Browser: on a fresh `aspire run`, add Wireless Mouse twice and Mechanical Keyboard once, then open `/cart`. Click **Remove** on Mechanical Keyboard. Only Wireless Mouse 24.99 × 2 = 49.98 is left, and the total is 49.98. The URL stays `/cart`.
5. HTTP: `GET http://localhost:5102/baskets/demo-user` → one line, Wireless Mouse with quantity 2.
6. Browser: click **Remove** on Wireless Mouse. The empty state shows. `GET http://localhost:5102/baskets/demo-user` → `{"buyerId":"demo-user","items":[]}`.
7. Trace: the trace for a Remove click has `webfrontend` → `basket-api` `DELETE /baskets/{buyerId}/items/{productId}`, plus Redis `GET` and `SET` spans. It has no span on `catalog-api`.

## References
