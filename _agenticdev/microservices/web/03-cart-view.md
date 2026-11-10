# web 03 — cart view

**Status:** Not started

## Goal
Show the buyer's basket on `/cart` (use case 2). Each line shows its name, unit price, quantity and line total, and the page shows the basket total. An empty basket shows an empty state that links to Products. This spec adds the **Cart** nav link. Every number comes from the Basket snapshot.

## Depends on
- web/02-add-to-cart (the `BuyerId` constant, `BasketApiClient` and a way to fill the basket)
- basket-api/01-get-basket

## Scope
- [ ] `BasketApiClient`: add `GetBasketAsync(buyerId)` → `GET /baskets/{buyerId}`.
- [ ] `Cart.razor` at `/cart`: a table of the lines (name, unit price, qty, line total = unit price × qty) and the basket total (Σ line totals). It shows only what Basket returned.
- [ ] Empty state: when the basket has no lines, show a message with a link to Products (`/`) instead of the table.
- [ ] `NavMenu.razor`: add **Cart** (`/cart`) after Products.

## Explicit non-goals
- No **Remove** button (web/04).
- No **Place order** button (web/06).
- No quantity editor (PROJECT.md "Out of scope").
- No Orders pages or `ordering-api` reference (web/05, web/07). No AppHost change in this spec.
- Prices come only from what Basket and Ordering return. This page doesn't call Catalog, and it sends no price to any service.
- Checkout is one call to Basket. No page calls Ordering to create anything.
- No page reads a database or Redis.
- Nothing from the PROJECT.md "Out of scope" list is added.
- No changes to Catalog.Api, Basket.Api, Ordering.Api, ServiceDefaults or `AppHost.cs`.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded`, 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard, every resource is Running and Healthy.
4. Browser: on a fresh `aspire run`, open `/cart`. It shows the empty state, and its link goes to `/`.
5. Browser: on `/`, add Wireless Mouse twice and Mechanical Keyboard once, then click **Cart** in the nav. `/cart` shows Wireless Mouse 24.99 × 2 = 49.98, Mechanical Keyboard 89.00 × 1 = 89.00, and a total of 138.98.
6. HTTP: `GET http://localhost:5102/baskets/demo-user` returns the same lines, prices and quantities as the page.
7. Trace: the trace for loading `/cart` has `webfrontend` → `basket-api` `GET /baskets/{buyerId}`, and has no span on `catalog-api`.
8. Browser: the nav shows **Products** and **Cart**.

## References
