# web 04 — remove line

**Status:** Done (d37b28e)

## Goal
Let the buyer delete a line from the cart (use case 2, "and remove a line"). A **Remove** button on each `/cart` line calls Basket's delete endpoint. The page then shows the basket that Basket returns. Remove deletes the whole line, with no decrement.

## Depends on
- web/03-cart-view
- basket-api/03-remove-item

## Scope
- [ ] `BasketApiClient.cs`: add `Task<BasketDto> RemoveItemAsync(string buyerId, int productId, CancellationToken ct = default)`. It calls `http.DeleteAsync($"/baskets/{buyerId}/items/{productId}", ct)`, then `EnsureSuccessStatusCode()`, then returns `ReadFromJsonAsync<BasketDto>(ct)`, or `new BasketDto(buyerId, [])` when the body is null. No new DTO (the request has no body). No error handling: failures propagate (same as web/02 and web/03).
- [ ] `Cart.razor` table: add a fifth column after "Line total" with an empty header `<th></th>`. Each row's cell holds `<button class="btn btn-danger btn-sm" @onclick="() => RemoveAsync(i)">Remove</button>`. The `<tfoot>` row keeps `colspan="3"` for "Total" and gets an extra empty `<th></th>` after the total, so the amount stays under "Line total".
- [ ] `Cart.razor` `@code`: add `RemoveAsync(BasketItemDto i)`, which sets `basket = await Basket.RemoveItemAsync(Buyer.Id, i.ProductId)`. The page re-renders from the returned basket (no second `GET`), so the total updates. When the last line is removed, the existing empty state shows.

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
- PROJECT.md §2: use case 2, "Add to cart (and remove a line)".
- PROJECT.md §2 "Out of scope": no cart quantity editor; remove deletes the line.
- PROJECT.md §4.2: `DELETE /baskets/{buyerId}/items/{productId}` → removes the line, `200 Basket`.
- PROJECT.md §5: route `/cart` with a **Remove** per line and the empty state linking to Products; typed clients with local DTO records.
- PLAN.md Step 5: storefront spec backlog (`04-remove-line`); no new AppHost reference in this spec.
