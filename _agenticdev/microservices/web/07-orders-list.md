# web 07 — orders list

**Status:** Not started

## Goal
Show the buyer's orders on `/orders` (use case 4). The page is a table of id, date, item count and total, newest first. Each row links to its detail page from web/05. When there are no orders, the page shows an empty state. This spec adds the **Orders** nav link, which completes the storefront journey.

## Depends on
- web/02-add-to-cart (the `BuyerId` constant)
- web/05-order-detail (`OrderingApiClient` and the detail page the rows link to)
- ordering-api/04-list-orders

## Scope
- [ ] `OrderingApiClient`: add `GetOrdersAsync(buyerId)` → `GET /orders?buyerId={buyerId}`. It declares a local `OrderSummaryDto(int Id, DateTime CreatedAt, decimal Total, int ItemCount)` record.
- [ ] `Orders.razor` at `/orders`: a table with id, date, item count and total, in the order Ordering returns (newest first). Each row links to `/orders/{id}`.
- [ ] Empty state: when there are no orders, show a message with a link to Products.
- [ ] `NavMenu.razor`: add **Orders** (`/orders`) after Cart.

## Explicit non-goals
- No paging, filtering or sorting controls (PROJECT.md "Out of scope").
- No status, cancellation or editing of orders.
- No AppHost change. `webfrontend` already references `ordering-api`.
- Prices come only from what Basket and Ordering return. The page shows the stored `total` and `itemCount`, doesn't recompute them and doesn't call Catalog.
- Checkout is one call to Basket. No page calls Ordering to create anything.
- No page reads a database.
- Nothing from the PROJECT.md "Out of scope" list is added.
- No changes to Catalog.Api, Basket.Api, Ordering.Api, ServiceDefaults or `AppHost.cs`.

## Definition of Done
1. From `src/`: `dotnet build EShopClaude.sln` → `Build succeeded`, 0 errors.
2. From `src/`: `dotnet test EShopClaude.sln` → exits 0.
3. From `src/`: `aspire run`. On the dashboard, every resource is Running and Healthy.
4. Browser: on a fresh `aspire run`, open `/orders`. It shows the empty state.
5. Browser: place order A (Wireless Mouse × 1, total 24.99), then order B (Mechanical Keyboard × 2, total 178.00). `/orders` shows two rows, B first and then A, with item counts 2 and 1 and totals 178.00 and 24.99.
6. HTTP: `GET http://localhost:5103/orders?buyerId=demo-user` returns the same ids, item counts and totals in the same order.
7. HTTP: create an order for another buyer (`POST http://localhost:5102/baskets/other-buyer/items` `{ "productId": 1, "quantity": 1 }`, then `POST http://localhost:5102/baskets/other-buyer/checkout`). After a refresh, `/orders` still shows only orders A and B.
8. Browser: clicking the row for B opens `/orders/{B}` with its lines and total.
9. Trace: the trace for loading `/orders` has `webfrontend` → `ordering-api` `GET /orders` with a Postgres span. It has no span on `catalog-api` or `basket-api`.
10. Browser: the nav shows **Products**, **Cart** and **Orders**. Full journey: list → add 2 products → remove one → place order → order detail → **Orders** shows it → **Cart** is empty.

## References
