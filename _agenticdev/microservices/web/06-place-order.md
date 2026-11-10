# web 06 — place order

**Status:** Not started

## Goal
Let the buyer check out from the cart (use case 3). A **Place order** button on `/cart` makes one call to Basket's checkout endpoint. Basket creates the order in Ordering and clears the basket. The page then navigates to the order detail page from web/05. The button is disabled when the cart is empty.

## Depends on
- web/03-cart-view (the `/cart` page)
- web/05-order-detail (the page the button navigates to)
- basket-api/04-checkout

## Scope
- [ ] `BasketApiClient`: add `CheckoutAsync(buyerId)` → `POST /baskets/{buyerId}/checkout`, which returns the `orderId`. It declares a local `CheckoutResultDto(int OrderId)` record.
- [ ] `Cart.razor`: a **Place order** button. It is disabled when the basket has no lines. Clicking it calls `CheckoutAsync` once and navigates to `/orders/{orderId}`.

## Explicit non-goals
- No checkout form, address, payment or shipping (PROJECT.md "Out of scope").
- No re-pricing and no confirmation step before the call.
- No retries or compensation in Web. If checkout fails, the error shows and the basket stays (PROJECT.md §4.4).
- No `/orders` list (web/07).
- No AppHost change. `webfrontend` already references `basket-api`, and this page doesn't call `ordering-api`.
- Prices come only from what Basket and Ordering return. No page asks Catalog for a price again, and the checkout call sends no body.
- Checkout is one call to Basket. No page calls Ordering to create anything.
- No page reads a database or Redis.
- Nothing from the PROJECT.md "Out of scope" list is added.
- No changes to Catalog.Api, Basket.Api, Ordering.Api, ServiceDefaults or `AppHost.cs`.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded`, 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard, every resource is Running and Healthy.
4. Browser: on a fresh `aspire run`, open `/cart`. It shows the empty state, and **Place order** is disabled or not shown.
5. Browser: add Wireless Mouse twice and Mechanical Keyboard once, then open `/cart` and click **Place order**. The browser lands on `/orders/N` and shows Wireless Mouse × 2, Mechanical Keyboard × 1 and a total of 138.98.
6. Browser: open `/cart` again. It shows the empty state. HTTP: `GET http://localhost:5102/baskets/demo-user` → `{"buyerId":"demo-user","items":[]}`.
7. HTTP: `GET http://localhost:5103/orders/N` → `buyerId` `demo-user`, the 2 lines and `total` 138.98.
8. Trace: the Place order trace goes `webfrontend` → `basket-api` `POST /baskets/{buyerId}/checkout` → `ordering-api` `POST /orders` (with a Postgres insert), and has a Redis `DEL` on `basket:demo-user`. There is no direct `webfrontend` → `ordering-api` `POST` span.
9. pgAdmin: `orderingdb` has the new order row and its 2 line rows.

## References
